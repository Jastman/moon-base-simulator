using System.Collections.Generic;
using UnityEngine;
using CesiumForUnity;

namespace MoonBase.DataLayers
{
    /// <summary>
    /// Manages data overlay layers on the Cesium Moon terrain:
    /// - Water/ice deposit overlay (from NASA LEND/LAMP data)
    /// - Solar exposure overlay (from NASA LOLA illumination data)
    ///
    /// Each layer is a CesiumRasterOverlay attached to the tileset.
    /// Layers are toggled at runtime by enabling/disabling the overlay component.
    ///
    /// SETUP NOTES:
    /// 1. For real NASA data layers:
    ///    a) Download GeoTIFF from NASA PDS (see README)
    ///    b) Upload to Cesium ion as a raster overlay
    ///    c) Get the ion asset ID
    ///    d) Add an entry to the 'layers' list below with that asset ID
    ///    e) In the scene, manually add a CesiumIonRasterOverlay component to the
    ///       Cesium3DTileset and set its Ion Asset ID. Then drag that component
    ///       into the overlayComponent field of the DataLayerEntry.
    ///
    /// 2. For the built-in solar exposure simulation (no external data needed):
    ///    Use SolarExposureManager.cs instead — it uses the Sun direction +
    ///    terrain normals to estimate solar viability at runtime.
    ///
    /// Setup: Attach to a persistent GameObject. Assign moonTileset in Inspector.
    /// </summary>
    public class DataLayerManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        public static DataLayerManager Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Cesium Reference")]
        [Tooltip("The Cesium3DTileset that raster overlays are attached to.")]
        public Cesium3DTileset moonTileset;

        [Header("Data Layers")]
        [Tooltip("List of data layer definitions. Each layer corresponds to a CesiumRasterOverlay " +
                 "component on the tileset. Add your ion asset IDs here after uploading NASA data.")]
        public List<DataLayerEntry> layers = new();

        // ── Properties ─────────────────────────────────────────────────────────
        public IReadOnlyList<DataLayerEntry> Layers => layers;

        // ── Events ─────────────────────────────────────────────────────────────
        /// <summary>Fires when any layer's visibility changes. Passes layer index + new state.</summary>
        public System.Action<int, bool> OnLayerToggled;

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // Start with all layers in their default visible state
            for (int i = 0; i < layers.Count; i++)
                SetLayerVisible(i, layers[i].visibleByDefault);
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Toggle a layer's visibility by index.</summary>
        public void ToggleLayer(int index)
        {
            if (index < 0 || index >= layers.Count) return;
            SetLayerVisible(index, !layers[index].IsCurrentlyVisible);
        }

        /// <summary>Set a layer's visibility explicitly.</summary>
        public void SetLayerVisible(int index, bool visible)
        {
            if (index < 0 || index >= layers.Count) return;

            var layer = layers[index];
            layer.IsCurrentlyVisible = visible;

            // Toggle the actual CesiumRasterOverlay component
            if (layer.overlayComponent != null)
            {
                layer.overlayComponent.enabled = visible;
            }
            else
            {
                // If no overlay component assigned, at least update the state tracker
                if (visible)
                    Debug.LogWarning($"[DataLayerManager] Layer '{layer.layerName}' has no " +
                                     "overlayComponent assigned. Assign a CesiumRasterOverlay in Inspector.");
            }

            OnLayerToggled?.Invoke(index, visible);
            Debug.Log($"[DataLayerManager] Layer '{layer.layerName}' → {(visible ? "ON" : "OFF")}");
        }

        /// <summary>Returns whether a layer is currently visible.</summary>
        public bool IsLayerVisible(int index)
        {
            if (index < 0 || index >= layers.Count) return false;
            return layers[index].IsCurrentlyVisible;
        }

        /// <summary>
        /// Helper to add a layer at runtime (e.g., if you load ion asset IDs from a config file).
        /// </summary>
        public void AddLayer(DataLayerEntry entry)
        {
            layers.Add(entry);
        }
    }

    // ── Data Layer Entry ───────────────────────────────────────────────────────
    [System.Serializable]
    public class DataLayerEntry
    {
        [Tooltip("Display name shown in the UI toggle panel.")]
        public string layerName = "Water / Ice Deposits";

        [Tooltip("Description shown on hover.")]
        [TextArea(1, 2)]
        public string description = "LEND/LAMP instrument data showing subsurface water ice probability.";

        [Tooltip("Icon for the layer toggle button.")]
        public Sprite icon;

        [Tooltip("Color used for the layer toggle button tint.")]
        public Color layerColor = Color.blue;

        [Tooltip("Whether this layer is visible when the scene first loads.")]
        public bool visibleByDefault = false;

        [Tooltip("The CesiumRasterOverlay component (CesiumIonRasterOverlay) attached to the " +
                 "Cesium3DTileset. Drag the component here from the tileset's Inspector. " +
                 "If null, toggling this layer will warn but not crash.")]
        public CesiumRasterOverlay overlayComponent;

        [Tooltip("Cesium ion asset ID for this layer. For reference — the actual component " +
                 "on the tileset holds the ID; this is just for documentation/display.")]
        public int ionAssetId = 0;

        // Runtime state — not serialized
        [System.NonSerialized]
        public bool IsCurrentlyVisible;
    }
}
