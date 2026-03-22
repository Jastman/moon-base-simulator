using UnityEngine;
using MoonBase.Core;

namespace MoonBase.DataLayers
{
    /// <summary>
    /// Simulates surface temperature distribution across the lunar terrain,
    /// based on Diviner Lunar Radiometer Experiment data characteristics.
    ///
    /// Temperature model:
    ///   - Fully illuminated flat surface: +120°C
    ///   - Permanently shadowed regions: -170°C (Diviner PSR measurements)
    ///   - Intermediate points: scaled by sin(sun_elevation) × cos(slope_to_sun)
    ///
    /// Visualization: applies a color-coded overlay to terrain using
    /// MaterialPropertyBlock on any mesh renderer in the TerrainOverlayRoot,
    /// OR sets a global shader property for a custom Cesium raster overlay shader.
    ///
    /// Setup:
    ///   - Attach to a persistent GameObject.
    ///   - Assign sunLight (Directional Light) in Inspector.
    ///   - Optionally assign terrainOverlayRoot with a mesh for color-coded overlay.
    ///   - Subscribe to DataLayerManager to activate via layer toggle.
    /// </summary>
    public class TemperatureOverlayManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        public static TemperatureOverlayManager Instance { get; private set; }

        // ── Temperature Constants (Diviner-derived) ────────────────────────────
        public const float TempSunlitMaxC   = 120f;
        public const float TempShadowMinC   = -170f;
        public const float TempAmbientC     = -50f;   // Subsurface baseline

        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("References")]
        [Tooltip("The directional light used as the Sun. Direction drives temperature model.")]
        public Light sunLight;

        [Tooltip("Optional mesh renderer root to colorize as temperature overlay. " +
                 "If null, temperature is query-only (no visual).")]
        public Renderer[] overlayRenderers;

        [Header("Overlay Shader")]
        [Tooltip("Shader property name for temperature color on overlay materials. " +
                 "Set to '_TemperatureColor' or your custom property name.")]
        public string shaderColorProperty = "_TemperatureColor";

        [Tooltip("Shader property name for blend factor (0 = terrain, 1 = full overlay).")]
        public string shaderBlendProperty = "_OverlayBlend";

        [Tooltip("Overlay blend strength when active.")]
        [Range(0f, 1f)]
        public float overlayBlendStrength = 0.65f;

        [Header("Terrain Sampling")]
        [Tooltip("Layer mask for downward raycasts to sample terrain normals.")]
        public LayerMask terrainLayerMask = ~0;

        [Tooltip("Shadow ray distance when checking sun occlusion for temperature.")]
        public float shadowRayDistance = 8000f;

        [Header("Update Rate")]
        [Tooltip("How many real seconds between full overlay refreshes. " +
                 "Lower = more responsive, higher = cheaper. 2s is a good default.")]
        [Range(0.5f, 30f)]
        public float overlayUpdateIntervalSeconds = 2f;

        // ── Runtime State ──────────────────────────────────────────────────────
        /// <summary>True when temperature overlay visualization is active.</summary>
        public bool IsOverlayActive { get; private set; }

        private float overlayTimer = 0f;
        private MaterialPropertyBlock propBlock;
        private static readonly int ShaderColorId = Shader.PropertyToID("_TemperatureColor");
        private static readonly int ShaderBlendId = Shader.PropertyToID("_OverlayBlend");

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            propBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            if (LunarSimulationClock.Instance != null)
                LunarSimulationClock.Instance.OnSimTick += OnSimTick;

            if (sunLight == null)
                Debug.LogWarning("[TemperatureOverlayManager] sunLight not assigned. " +
                                 "Falling back to LunarSimulationClock for sun direction.");
        }

        private void OnDestroy()
        {
            if (LunarSimulationClock.Instance != null)
                LunarSimulationClock.Instance.OnSimTick -= OnSimTick;
        }

        private void Update()
        {
            if (!IsOverlayActive) return;
            if (LunarSimulationClock.Instance == null)
            {
                overlayTimer -= Time.deltaTime;
                if (overlayTimer <= 0f)
                {
                    overlayTimer = overlayUpdateIntervalSeconds;
                    RefreshOverlay();
                }
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns estimated surface temperature in Celsius at a world position.
        ///
        /// Uses terrain normal from a downward raycast, dot-product with sun direction,
        /// and a shadow occlusion check to model Diviner radiometer readings.
        /// </summary>
        public float GetTemperatureAt(Vector3 worldPos)
        {
            Vector3 sunDir = GetSunDirection();
            float sunElevation = Mathf.Max(0f, sunDir.y); // how high sun is (0 = horizon)

            // Sample terrain normal
            Vector3 surfaceNormal = Vector3.up;
            bool inShadow = false;

            if (Physics.Raycast(worldPos + Vector3.up * 2f, Vector3.down,
                                out RaycastHit hit, 500f, terrainLayerMask))
            {
                surfaceNormal = hit.normal;

                // Check if sun is occluded (PSR detection)
                Vector3 shadowOrigin = hit.point + hit.normal * 0.5f;
                inShadow = Physics.Raycast(shadowOrigin, sunDir, shadowRayDistance, terrainLayerMask);
            }

            if (inShadow)
                return TempShadowMinC;

            // Dot product of surface normal with sun direction (angle of incidence)
            float normalDot = Mathf.Max(0f, Vector3.Dot(surfaceNormal, sunDir));

            // If sun is below this slope's horizon, it's in shadow
            if (normalDot <= 0f)
                return TempShadowMinC + 30f; // slightly warmer than deep PSR

            // Scale from ambient up to max based on incidence + elevation
            float elevationFactor = Mathf.InverseLerp(0f, LunarSimulationClock.SunElevationMax,
                                                      LunarSimulationClock.Instance?.SunElevationDegrees ?? 3f);
            float incidenceFactor = normalDot * elevationFactor;

            return Mathf.Lerp(TempAmbientC, TempSunlitMaxC, incidenceFactor);
        }

        /// <summary>
        /// Activates the temperature color overlay on terrain renderers.
        /// </summary>
        public void ShowOverlay()
        {
            IsOverlayActive = true;
            RefreshOverlay();
        }

        /// <summary>
        /// Hides the temperature overlay, returning terrain to its default appearance.
        /// </summary>
        public void HideOverlay()
        {
            IsOverlayActive = false;
            SetOverlayBlend(0f);
        }

        /// <summary>
        /// Returns a color representing a temperature value for UI display.
        /// Gradient: deep blue (-170°C) → purple → orange → white-yellow (+120°C).
        /// </summary>
        public Color GetTemperatureColor(float celsius)
        {
            float t = Mathf.InverseLerp(TempShadowMinC, TempSunlitMaxC, celsius);

            if (t < 0.25f)
                return Color.Lerp(new Color(0f, 0f, 0.5f), new Color(0.4f, 0f, 0.6f), t / 0.25f);
            if (t < 0.5f)
                return Color.Lerp(new Color(0.4f, 0f, 0.6f), new Color(0.8f, 0.2f, 0.1f), (t - 0.25f) / 0.25f);
            if (t < 0.75f)
                return Color.Lerp(new Color(0.8f, 0.2f, 0.1f), new Color(1f, 0.7f, 0.1f), (t - 0.5f) / 0.25f);

            return Color.Lerp(new Color(1f, 0.7f, 0.1f), Color.white, (t - 0.75f) / 0.25f);
        }

        /// <summary>
        /// Returns a human-readable temperature label and status color for a given position.
        /// Useful for rover HUD and module placement UI.
        /// </summary>
        public (string label, Color color) GetTemperatureLabel(Vector3 worldPos)
        {
            float temp = GetTemperatureAt(worldPos);
            Color col  = GetTemperatureColor(temp);
            string label = $"{temp:+0;-0;0}°C";

            if (temp <= TempShadowMinC + 20f)
                label = $"PSR {temp:+0;-0;0}°C";

            return (label, col);
        }

        // ── Private Helpers ────────────────────────────────────────────────────
        private void OnSimTick(double metSeconds)
        {
            if (!IsOverlayActive) return;
            overlayTimer -= Time.deltaTime * (LunarSimulationClock.Instance?.timeScale ?? 1f);
            // Convert interval to sim-time
            float simInterval = overlayUpdateIntervalSeconds * (LunarSimulationClock.Instance?.timeScale ?? 1f);
            if (overlayTimer <= 0f)
            {
                overlayTimer = simInterval;
                RefreshOverlay();
            }
        }

        private void RefreshOverlay()
        {
            if (overlayRenderers == null || overlayRenderers.Length == 0) return;

            // For a simple color-coded overlay, sample a central position
            // In a full implementation, sample a grid of points and blend into a texture
            Vector3 origin = Camera.main != null
                ? Camera.main.transform.position + Vector3.down * 100f
                : Vector3.zero;

            float temp  = GetTemperatureAt(origin);
            Color color = GetTemperatureColor(temp);

            SetOverlayBlend(overlayBlendStrength);
            SetOverlayColor(color);
        }

        private void SetOverlayColor(Color color)
        {
            if (overlayRenderers == null) return;
            foreach (var r in overlayRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(propBlock);
                propBlock.SetColor(ShaderColorId, color);
                r.SetPropertyBlock(propBlock);
            }
        }

        private void SetOverlayBlend(float blend)
        {
            if (overlayRenderers == null) return;
            foreach (var r in overlayRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(propBlock);
                propBlock.SetFloat(ShaderBlendId, blend);
                r.SetPropertyBlock(propBlock);
            }
        }

        private Vector3 GetSunDirection()
        {
            if (sunLight != null)
                return -sunLight.transform.forward;

            if (LunarSimulationClock.Instance != null)
            {
                float elevRad = LunarSimulationClock.Instance.SunElevationDegrees * Mathf.Deg2Rad;
                float azimRad = LunarSimulationClock.Instance.SunAzimuthDegrees * Mathf.Deg2Rad;
                return new Vector3(
                    Mathf.Sin(azimRad) * Mathf.Cos(elevRad),
                    Mathf.Sin(elevRad),
                    Mathf.Cos(azimRad) * Mathf.Cos(elevRad)
                ).normalized;
            }

            // Last-resort fallback: low-angle sun from south
            return new Vector3(0f, 0.026f, -0.9997f).normalized;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            Gizmos.color = GetTemperatureColor(GetTemperatureAt(transform.position));
            Gizmos.DrawSphere(transform.position, 5f);
        }
#endif
    }
}
