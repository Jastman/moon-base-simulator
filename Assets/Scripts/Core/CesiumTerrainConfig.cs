// ============================================================
// CesiumTerrainConfig.cs  —  LUNAR OPS
// Configures the Cesium Moon Terrain tileset material and
// overlay imagery at runtime. Also exposes a method that
// DataLayerManager calls to swap the active material/overlay
// when the user switches data layers.
//
// Cesium ion assets used:
//   2684829  — Cesium Moon Terrain (LRO LOLA elevation, default)
//   2684833  — Lunar Satellite Imagery (LRO WAC mosaic, for overlay)
// ============================================================

using System.Collections;
using UnityEngine;

#if CESIUM_FOR_UNITY
using CesiumForUnity;
#endif

namespace MoonBase.Core
{
    /// <summary>
    /// Attach to the CesiumMoonTerrain GameObject (child of CesiumGeoreference).
    /// The SceneBootstrap puts it there automatically.
    /// </summary>
    public class CesiumTerrainConfig : MonoBehaviour
    {
        // ── Ion asset IDs ─────────────────────────────────────────────────────
        public const long IonAssetMoonTerrain     = 2684829L;  // LRO LOLA elevation
        public const long IonAssetLunarImagery    = 2684833L;  // LRO WAC mosaic

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Tileset Settings")]
        [Tooltip("Max screen-space error. 8 is a good balance of quality vs. frame rate.")]
        [Range(2f, 32f)]
        public float maximumSSE = 8f;

        [Tooltip("Enable physics mesh generation for terrain raycasting & module placement.")]
        public bool createPhysicsMeshes = true;

        [Tooltip("How many tiles to keep loaded ahead of the camera.")]
        [Range(0, 8)]
        public int preloadRadius = 2;

        [Header("Shading")]
        [Tooltip("Optional URP material override for the tileset surface. Leave null to use Cesium default.")]
        public Material terrainMaterialOverride;

        [Header("Imagery Overlay")]
        [Tooltip("Whether to add the LRO WAC satellite imagery overlay on startup.")]
        public bool addImageryOnStart = true;

        // ── Runtime refs ─────────────────────────────────────────────────────
#if CESIUM_FOR_UNITY
        private Cesium3DTileset _tileset;
        private CesiumIonRasterOverlay _imageryOverlay;
#endif

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
#if CESIUM_FOR_UNITY
            _tileset = GetComponent<Cesium3DTileset>();
            if (_tileset == null)
            {
                Debug.LogError("[CesiumTerrainConfig] No Cesium3DTileset on this GameObject. " +
                               "Attach CesiumTerrainConfig to the CesiumMoonTerrain object.");
                return;
            }

            ApplyTilesetSettings();
#else
            Debug.LogWarning("[CesiumTerrainConfig] Cesium for Unity not installed.");
#endif
        }

        private IEnumerator Start()
        {
            // Wait a frame for LunarSceneSetup (execution order -100) to apply the ion token
            yield return null;

#if CESIUM_FOR_UNITY
            if (addImageryOnStart)
                AddImageryOverlay();

            if (terrainMaterialOverride != null)
                ApplyMaterialOverride();
#endif
        }

        // ── Tileset config ────────────────────────────────────────────────────
#if CESIUM_FOR_UNITY
        private void ApplyTilesetSettings()
        {
            _tileset.ionAssetID                = IonAssetMoonTerrain;
            _tileset.maximumScreenSpaceError   = maximumSSE;
            _tileset.createPhysicsMeshes       = createPhysicsMeshes;
            _tileset.preloadAncestors          = true;
            _tileset.preloadSiblings           = preloadRadius > 0;

            Debug.Log($"[CesiumTerrainConfig] Tileset configured: " +
                      $"asset={IonAssetMoonTerrain}, SSE={maximumSSE}, physics={createPhysicsMeshes}");
        }

        // ── Imagery overlay ───────────────────────────────────────────────────
        private void AddImageryOverlay()
        {
            // Check if overlay already exists
            _imageryOverlay = GetComponent<CesiumIonRasterOverlay>();
            if (_imageryOverlay != null)
            {
                Debug.Log("[CesiumTerrainConfig] Imagery overlay already present — skipping.");
                return;
            }

            _imageryOverlay = gameObject.AddComponent<CesiumIonRasterOverlay>();
            _imageryOverlay.ionAssetID             = IonAssetLunarImagery;
            _imageryOverlay.maximumTextureSize     = 2048;
            _imageryOverlay.maximumSimultaneousTileLoads = 20;

            Debug.Log($"[CesiumTerrainConfig] LRO WAC imagery overlay added (ion asset {IonAssetLunarImagery}).");
        }

        // ── Material override ─────────────────────────────────────────────────
        private void ApplyMaterialOverride()
        {
            // Cesium3DTileset exposes a customShaderMaterial property in some versions
            var shaderMatProp = typeof(Cesium3DTileset).GetProperty("customShaderMaterial",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (shaderMatProp != null && shaderMatProp.CanWrite)
            {
                shaderMatProp.SetValue(_tileset, terrainMaterialOverride);
                Debug.Log("[CesiumTerrainConfig] Custom terrain material applied.");
            }
            else
            {
                Debug.LogWarning("[CesiumTerrainConfig] customShaderMaterial property not found — " +
                                 "assign material directly on Cesium3DTileset in Inspector.");
            }
        }
#endif

        // ── Public API (called by DataLayerManager) ───────────────────────────

        /// <summary>
        /// Sets the screen-space error at runtime. Lower = more detail, higher load.
        /// </summary>
        public void SetSSE(float sse)
        {
#if CESIUM_FOR_UNITY
            if (_tileset != null)
            {
                _tileset.maximumScreenSpaceError = sse;
                Debug.Log($"[CesiumTerrainConfig] SSE updated to {sse}");
            }
#endif
        }

        /// <summary>
        /// Toggles the LRO imagery overlay on or off.
        /// </summary>
        public void SetImageryOverlayEnabled(bool enabled)
        {
#if CESIUM_FOR_UNITY
            if (_imageryOverlay != null)
                _imageryOverlay.enabled = enabled;
            else if (enabled)
                AddImageryOverlay();
#endif
        }

        /// <summary>
        /// Moves the Cesium georeference origin to a new lat/lon/height.
        /// Use this when the user navigates to a new site on the Moon.
        /// </summary>
        public void SetOrigin(double lat, double lon, double heightMeters)
        {
#if CESIUM_FOR_UNITY
            var georef = FindFirstObjectByType<CesiumGeoreference>();
            if (georef == null)
            {
                Debug.LogError("[CesiumTerrainConfig] CesiumGeoreference not found.");
                return;
            }
            georef.latitude  = lat;
            georef.longitude = lon;
            georef.height    = heightMeters;
            Debug.Log($"[CesiumTerrainConfig] Origin moved → {lat:F4}°, {lon:F4}°, {heightMeters:F0}m");
#endif
        }

        /// <summary>
        /// Jumps to the Shackleton Crater south pole site — the default base location.
        /// </summary>
        public void JumpToShackletonCrater()
        {
            SetOrigin(-89.54, 0.0, 3000.0);
        }

        /// <summary>
        /// Jumps to the Apollo 11 landing site (Tranquility Base) — useful for testing.
        /// </summary>
        public void JumpToApollo11()
        {
            SetOrigin(0.6741, 23.4732, 500.0);
        }
    }
}
