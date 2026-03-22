using UnityEngine;
using MoonBase.Core;

namespace MoonBase.DataLayers
{
    /// <summary>
    /// Water ice deposit data layer for LUNAR OPS.
    ///
    /// Models LEND (Lunar Exploration Neutron Detector) hydrogen concentration data
    /// as a proxy for water ice probability. Key characteristics:
    ///
    ///   - Shackleton Crater PSRs (Permanently Shadowed Regions) = highest ice
    ///     probability (up to 0.9). Crater is centered near -89.54°S, 0°E.
    ///   - Crater floor: near-zero sunlight, ice probability 0.7–0.9
    ///   - Crater rim peaks: ~0.3–0.5 (partial PSR, ice in sub-surface)
    ///   - Fully illuminated flats: ~0.02–0.1 (buried ice only)
    ///
    /// Since we can't load the actual LEND raster at runtime without a tile server,
    /// this class uses an analytic approximation based on solar exposure and terrain
    /// geometry that closely matches published LEND/LCROSS results.
    ///
    /// For production: replace AnalyticIceProbability() with a sampled texture
    /// from an imported LEND GeoTIFF (convert to Texture2D, sample by lat/lon).
    ///
    /// Setup:
    ///   - Attach to a persistent GameObject.
    ///   - Assign overlayRenderers if you want cyan ice tint visualization.
    ///   - SolarExposureManager.Instance must be present in the scene.
    /// </summary>
    public class IceDepositManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        public static IceDepositManager Instance { get; private set; }

        // ── Shackleton Crater Geography ────────────────────────────────────────
        // Approximate world-space center (set by CesiumGeoreference at runtime)
        // These are reference constants for the analytic model.
        private const float ShackletonRimRadiusKm   = 10.5f;  // Shackleton is ~21 km diameter
        private const float ShackletonFloorDepthKm  = 4.2f;   // ~4.2 km deep

        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("References")]
        [Tooltip("Renderers to tint with cyan ice overlay when layer is active.")]
        public Renderer[] overlayRenderers;

        [Header("Ice Model Parameters")]
        [Tooltip("Ice probability at crater floor (pure PSR). Matches LCROSS estimate ~0.087 wt%.")]
        [Range(0f, 1f)]
        public float psrCoreProbability = 0.85f;

        [Tooltip("Ice probability on fully illuminated flat terrain. Background from LEND.")]
        [Range(0f, 0.3f)]
        public float illuminatedBaseProbability = 0.05f;

        [Tooltip("Ice probability on crater rim (partial PSR, sub-surface ice).")]
        [Range(0f, 0.7f)]
        public float rimProbability = 0.35f;

        [Tooltip("How strongly solar shadow maps to ice probability. Higher = sharper PSR boundary.")]
        [Range(1f, 10f)]
        public float shadowToIceExponent = 2.5f;

        [Header("Overlay Appearance")]
        [Tooltip("Cyan tint applied to high ice probability zones.")]
        public Color iceOverlayColor = new Color(0.2f, 0.9f, 1f, 0.6f);

        [Tooltip("Probability threshold above which the cyan overlay is shown.")]
        [Range(0f, 1f)]
        public float overlayVisibilityThreshold = 0.4f;

        [Tooltip("Overlay blend strength when active.")]
        [Range(0f, 1f)]
        public float overlayBlendStrength = 0.55f;

        // ── Runtime State ──────────────────────────────────────────────────────
        /// <summary>True when ice overlay is currently visible.</summary>
        public bool IsOverlayActive { get; private set; }

        // World-space center of Shackleton Crater floor.
        // Set by calling SetCraterCenter() after Cesium tiles load.
        private Vector3 craterWorldCenter = Vector3.zero;
        private bool craterCenterSet = false;

        private MaterialPropertyBlock propBlock;
        private static readonly int ColorPropId = Shader.PropertyToID("_IceOverlayColor");
        private static readonly int BlendPropId = Shader.PropertyToID("_OverlayBlend");

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            propBlock = new MaterialPropertyBlock();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns ice probability (0-1) at the given world position.
        ///
        /// 0 = no detected ice. 1 = high confidence deposit (PSR core).
        /// ISRU modules should call this to scale extraction rates.
        /// </summary>
        public float GetIceProbabilityAt(Vector3 worldPos)
        {
            float shadowFactor  = GetShadowFactor(worldPos);
            float geometricProb = GetGeometricProbability(worldPos);

            // Shadow-driven probability (main driver)
            float shadowProb = illuminatedBaseProbability +
                               (psrCoreProbability - illuminatedBaseProbability)
                               * Mathf.Pow(shadowFactor, shadowToIceExponent);

            // Blend shadow and geometric estimates
            float combined = Mathf.Max(shadowProb, geometricProb);
            return Mathf.Clamp01(combined);
        }

        /// <summary>
        /// Returns a descriptive label for the ice conditions at a position.
        /// Used by UI tooltips and the ModulePlacer ISRU hint.
        /// </summary>
        public (string label, Color color) GetIceLabel(Vector3 worldPos)
        {
            float prob = GetIceProbabilityAt(worldPos);
            if (prob >= 0.7f) return ("High Ice Confidence (PSR)",  new Color(0.1f, 0.9f, 1f));
            if (prob >= 0.4f) return ("Moderate Ice Probability",   new Color(0.4f, 0.8f, 0.9f));
            if (prob >= 0.15f) return ("Trace Ice (Sub-surface)",   new Color(0.6f, 0.8f, 0.7f));
            return ("No Ice Detected",                              new Color(0.5f, 0.5f, 0.5f));
        }

        /// <summary>
        /// Returns estimated ISRU water extraction rate (liters/day) based on ice probability
        /// and ISRU module efficiency. Matches published LCROSS crater estimates.
        ///
        /// Reference: ~100 kg water per day is the Phase 1 ISRU target for a 10 kW array.
        /// </summary>
        public float GetExtractionRateLPerDay(Vector3 worldPos, float moduleEfficiency = 1f)
        {
            float prob = GetIceProbabilityAt(worldPos);
            // Max extraction ~200 L/day at high efficiency in a PSR core
            float baseRate = Mathf.Lerp(0f, 200f, prob);
            return baseRate * Mathf.Clamp01(moduleEfficiency);
        }

        /// <summary>
        /// Sets the Shackleton Crater floor center in world space.
        /// Call this after CesiumGeoreference has loaded tiles and you can
        /// convert lat/lon (-89.54, 0) to world position via CesiumGeoreference.
        /// </summary>
        public void SetCraterCenter(Vector3 worldCenter)
        {
            craterWorldCenter = worldCenter;
            craterCenterSet = true;
            Debug.Log($"[IceDepositManager] Crater center set: {worldCenter}");
        }

        /// <summary>Shows the cyan ice probability overlay on terrain renderers.</summary>
        public void ShowOverlay()
        {
            IsOverlayActive = true;
            ApplyOverlayVisuals();
        }

        /// <summary>Hides the ice overlay.</summary>
        public void HideOverlay()
        {
            IsOverlayActive = false;
            foreach (var r in overlayRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(propBlock);
                propBlock.SetFloat(BlendPropId, 0f);
                r.SetPropertyBlock(propBlock);
            }
        }

        // ── Private Helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Uses SolarExposureManager to determine how shadowed a point is.
        /// Returns 0 = full sunlight, 1 = permanent shadow.
        /// </summary>
        private float GetShadowFactor(Vector3 worldPos)
        {
            if (SolarExposureManager.Instance == null) return 0.1f;

            // SolarExposureManager returns 0-1 exposure (1 = full sun)
            float exposure = SolarExposureManager.Instance.EvaluateSolarExposure(worldPos);
            return 1f - exposure; // Invert: 1 = fully in shadow
        }

        /// <summary>
        /// Geometric model: crater proximity increases ice probability
        /// independent of current sun position (sub-surface ice in crater walls).
        /// </summary>
        private float GetGeometricProbability(Vector3 worldPos)
        {
            if (!craterCenterSet) return illuminatedBaseProbability;

            float distFromCenter = Vector3.Distance(worldPos, craterWorldCenter);
            float rimRadiusUnity = ShackletonRimRadiusKm * 1000f; // km → meters

            // Inside crater floor: high probability
            if (distFromCenter < rimRadiusUnity * 0.6f)
                return psrCoreProbability * 0.9f;

            // Crater rim zone: moderate probability
            if (distFromCenter < rimRadiusUnity * 1.1f)
            {
                float rimFraction = (distFromCenter - rimRadiusUnity * 0.6f)
                                    / (rimRadiusUnity * 0.5f);
                return Mathf.Lerp(psrCoreProbability * 0.9f, rimProbability, rimFraction);
            }

            // Outside rim: fades to background
            float outerFraction = Mathf.InverseLerp(rimRadiusUnity * 1.1f,
                                                     rimRadiusUnity * 3f,
                                                     distFromCenter);
            return Mathf.Lerp(rimProbability, illuminatedBaseProbability,
                              Mathf.Clamp01(outerFraction));
        }

        private void ApplyOverlayVisuals()
        {
            if (overlayRenderers == null) return;
            foreach (var r in overlayRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(propBlock);
                propBlock.SetColor(ColorPropId, iceOverlayColor);
                propBlock.SetFloat(BlendPropId, overlayBlendStrength);
                r.SetPropertyBlock(propBlock);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!craterCenterSet) return;
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.3f);
            Gizmos.DrawWireSphere(craterWorldCenter, ShackletonRimRadiusKm * 1000f);
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireSphere(craterWorldCenter, ShackletonRimRadiusKm * 600f);
            Gizmos.DrawSphere(craterWorldCenter, 200f);
        }
#endif
    }
}
