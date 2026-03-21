using UnityEngine;
using MoonBase.Core;

namespace MoonBase.DataLayers
{
    /// <summary>
    /// Runtime solar exposure estimation system.
    ///
    /// Two modes:
    ///   1. REAL-TIME: Uses the scene's directional light (representing the Sun) to
    ///      evaluate whether a point on the terrain is currently illuminated.
    ///      Results update as the Sun moves (if you animate it).
    ///
    ///   2. STATIC PREVIEW: Samples terrain normals via downward raycasts and colors
    ///      a mesh overlay based on how well each point faces the Sun. Gives a
    ///      "heatmap" view of solar viability for the current Sun angle.
    ///
    /// The Sun position is set to a realistic low-angle representation of the Moon's
    /// south pole solar geometry (Sun near the horizon, ~1.5° max elevation above
    /// the pole due to the Moon's axial tilt of ~1.54°).
    ///
    /// For real data, replace this with a Cesium raster overlay from LOLA illumination
    /// products (see DataLayerManager). This runtime estimator is a solid fallback.
    ///
    /// Setup: Attach to a persistent GameObject.
    ///        Assign sunLight (your Directional Light) in Inspector.
    ///        Assign moonTileset reference.
    /// </summary>
    public class SolarExposureManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        public static SolarExposureManager Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Sun Light")]
        [Tooltip("The directional light representing the Sun. " +
                 "Position doesn't matter — only the rotation (direction) does.")]
        public Light sunLight;

        [Header("Lunar South Pole Solar Settings")]
        [Tooltip("Sun elevation above horizon in degrees. At the south pole, max is ~1.54° " +
                 "(Moon's axial tilt). Keep this between 0 and 5 for realism.")]
        [Range(0f, 10f)]
        public float sunElevationDegrees = 1.5f;

        [Tooltip("Sun azimuth (compass direction) in degrees from North. Varies over a lunar day.")]
        [Range(0f, 360f)]
        public float sunAzimuthDegrees = 180f;

        [Tooltip("If true, animate the Sun azimuth slowly (simulating a lunar day). " +
                 "A full lunar day = 29.5 Earth days. This is compressed for visualization.")]
        public bool animateSunPosition = false;

        [Tooltip("How many seconds in-game equals one full lunar rotation (360°). " +
                 "Default 120 seconds = one minute for a full day cycle (very fast, for demo).")]
        public float lunarDayCycleDurationSeconds = 120f;

        [Header("Solar Evaluation")]
        [Tooltip("Layer mask for terrain raycasts when checking shadow occlusion.")]
        public LayerMask terrainLayerMask = ~0;

        [Tooltip("How far to cast shadow-check rays toward the Sun.")]
        public float shadowRayDistance = 5000f;

        // ── Properties ─────────────────────────────────────────────────────────
        /// <summary>Current Sun direction in world space (points FROM the scene TOWARD the Sun).</summary>
        public Vector3 SunDirection { get; private set; }

        /// <summary>True if the solar overlay is active.</summary>
        public bool IsOverlayActive { get; private set; }

        // ── Events ─────────────────────────────────────────────────────────────
        public System.Action<Vector3> OnSunDirectionChanged;

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (sunLight == null)
            {
                Debug.LogWarning("[SolarExposureManager] sunLight not assigned. Solar evaluation will " +
                                 "use estimated direction only. Assign the Directional Light in Inspector.");
            }
            UpdateSunDirection();
        }

        private void Update()
        {
            if (animateSunPosition)
            {
                float degreesPerSecond = 360f / lunarDayCycleDurationSeconds;
                sunAzimuthDegrees = (sunAzimuthDegrees + degreesPerSecond * Time.deltaTime) % 360f;
                UpdateSunDirection();
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates solar exposure at a given world position.
        /// Returns a value 0-1: 1.0 = full sun, 0.0 = completely shadowed.
        ///
        /// This is the per-point query used by ModulePlacer to advise on solar panel placement.
        /// </summary>
        public float EvaluateSolarExposure(Vector3 worldPosition)
        {
            // Step 1: Check if this point's surface normal faces the Sun at all
            // (i.e., is the Sun above the horizon from this point's perspective)
            // We'll use a downward snap to get the surface normal, then dot with Sun direction
            float normalFactor = 0f;

            if (TerrainRaycaster.Instance != null)
            {
                var snap = TerrainRaycaster.Instance.SnapToGroundBelow(worldPosition);
                if (snap.didHit)
                {
                    normalFactor = Mathf.Max(0f, Vector3.Dot(snap.surfaceNormal, SunDirection));
                }
            }
            else
            {
                // Fallback: assume flat terrain
                normalFactor = Mathf.Max(0f, Vector3.Dot(Vector3.up, SunDirection));
            }

            if (normalFactor <= 0f) return 0f; // Surface is pointing away from Sun

            // Step 2: Cast a ray toward the Sun to check for terrain shadowing
            // (important for PSRs — permanently shadowed craters near the poles)
            Vector3 rayOrigin = worldPosition + Vector3.up * 0.5f; // slight offset to avoid self-hit
            if (Physics.Raycast(rayOrigin, SunDirection, shadowRayDistance, terrainLayerMask))
            {
                // Something is blocking the Sun (a crater rim, ridge, etc.)
                return 0f;
            }

            return normalFactor;
        }

        /// <summary>
        /// Returns a color representing solar exposure level (for UI/overlay visualization).
        /// 0 = black (no sun), 0.5 = orange, 1.0 = bright yellow.
        /// </summary>
        public Color GetSolarExposureColor(float exposure)
        {
            // Gradient: dark purple → orange → yellow
            if (exposure <= 0f) return new Color(0.05f, 0.02f, 0.1f); // Deep shadow
            if (exposure < 0.3f) return Color.Lerp(new Color(0.1f, 0f, 0.2f), new Color(0.8f, 0.3f, 0f), exposure / 0.3f);
            return Color.Lerp(new Color(0.8f, 0.3f, 0f), new Color(1f, 0.95f, 0.2f), (exposure - 0.3f) / 0.7f);
        }

        /// <summary>
        /// Rates a position for solar panel placement. Returns a human-readable rating.
        /// </summary>
        public (string rating, Color color) GetSolarRating(Vector3 worldPosition)
        {
            float exposure = EvaluateSolarExposure(worldPosition);

            if (exposure > 0.7f) return ("Excellent", new Color(0.2f, 0.9f, 0.2f));
            if (exposure > 0.4f) return ("Good", new Color(0.7f, 0.9f, 0.1f));
            if (exposure > 0.15f) return ("Poor", new Color(0.9f, 0.5f, 0.1f));
            return ("Shadowed (PSR)", new Color(0.4f, 0.2f, 0.8f)); // PSR = Permanently Shadowed Region
        }

        /// <summary>
        /// Manually set the Sun elevation and azimuth (e.g., from a UI slider).
        /// </summary>
        public void SetSunPosition(float elevationDeg, float azimuthDeg)
        {
            sunElevationDegrees = Mathf.Clamp(elevationDeg, 0f, 90f);
            sunAzimuthDegrees = azimuthDeg % 360f;
            UpdateSunDirection();
        }

        // ── Private ────────────────────────────────────────────────────────────
        private void UpdateSunDirection()
        {
            // Convert azimuth + elevation to a world-space direction vector
            // Elevation 0 = horizon, 90 = zenith
            // Azimuth 0 = North (+Z), 90 = East (+X), 180 = South (-Z), 270 = West (-X)
            float elevRad = sunElevationDegrees * Mathf.Deg2Rad;
            float azimRad = sunAzimuthDegrees * Mathf.Deg2Rad;

            // Direction FROM scene toward Sun (the light direction reversed)
            SunDirection = new Vector3(
                Mathf.Sin(azimRad) * Mathf.Cos(elevRad),
                Mathf.Sin(elevRad),
                Mathf.Cos(azimRad) * Mathf.Cos(elevRad)
            ).normalized;

            // Update the actual directional light to match
            if (sunLight != null)
            {
                // Directional light points INTO the scene, so negate
                sunLight.transform.rotation = Quaternion.LookRotation(-SunDirection, Vector3.up);
            }

            OnSunDirectionChanged?.Invoke(SunDirection);
        }

#if UNITY_EDITOR
        // ── Editor Gizmos ──────────────────────────────────────────────────────
        private void OnDrawGizmosSelected()
        {
            // Draw a yellow arrow showing Sun direction from the scene origin
            UpdateSunDirection();
            Gizmos.color = Color.yellow;
            Vector3 origin = transform.position;
            Gizmos.DrawRay(origin, SunDirection * 50f);
            Gizmos.DrawSphere(origin + SunDirection * 50f, 2f);
        }
#endif
    }
}
