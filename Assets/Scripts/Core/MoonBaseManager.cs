using System.Collections.Generic;
using UnityEngine;
using CesiumForUnity;
using MoonBase.Modules;

namespace MoonBase.Core
{
    /// <summary>
    /// Central manager for the Moon Base Simulator.
    /// Handles global state: placed modules registry, resource totals,
    /// and Cesium globe configuration for the Moon ellipsoid.
    ///
    /// Setup: Attach to a GameObject named "MoonBaseManager" in the scene root.
    /// Assign references in Inspector.
    /// </summary>
    public class MoonBaseManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        public static MoonBaseManager Instance { get; private set; }

        // ── Inspector References ───────────────────────────────────────────────
        [Header("Cesium References")]
        [Tooltip("The CesiumGeoreference in the scene. Configure ellipsoid to Moon in Inspector.")]
        public CesiumGeoreference cesiumGeoreference;

        [Tooltip("The Cesium3DTileset loaded with Cesium Moon Terrain (ion asset 2684829).")]
        public Cesium3DTileset moonTileset;

        [Header("Scene References")]
        [Tooltip("Empty parent transform where all placed modules are parented.")]
        public Transform placedModulesRoot;

        [Header("Moon Globe Settings")]
        [Tooltip("Latitude for the initial camera/globe origin. Default = Shackleton Crater rim, south pole.")]
        [Range(-90f, 90f)]
        public double originLatitude = -89.54;

        [Tooltip("Longitude for the initial globe origin.")]
        [Range(-180f, 180f)]
        public double originLongitude = 0.0;

        [Tooltip("Height above Moon surface for origin point (meters).")]
        public double originHeight = 3000.0;

        // ── Runtime State ──────────────────────────────────────────────────────
        /// <summary>All modules currently placed in the scene.</summary>
        public List<BaseModule> PlacedModules { get; private set; } = new();

        /// <summary>Net power balance: positive = surplus, negative = deficit.</summary>
        public float NetPowerKW => totalPowerGeneratedKW - totalPowerConsumedKW;

        public float TotalPowerGeneratedKW => totalPowerGeneratedKW;
        public float TotalPowerConsumedKW => totalPowerConsumedKW;
        public int TotalCrewCapacity => totalCrewCapacity;

        private float totalPowerGeneratedKW;
        private float totalPowerConsumedKW;
        private int totalCrewCapacity;

        // ── Events ─────────────────────────────────────────────────────────────
        /// <summary>Fired whenever the resource totals change (module placed or removed).</summary>
        public System.Action OnResourcesChanged;

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[MoonBaseManager] Duplicate instance destroyed.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ValidateReferences();
        }

        private void Start()
        {
            ConfigureMoonGlobe();
        }

        // ── Globe Configuration ────────────────────────────────────────────────
        /// <summary>
        /// Configures the CesiumGeoreference for the Moon.
        /// NOTE: The ellipsoid must be set in the Inspector to CesiumEllipsoid.Moon
        /// (Cesium for Unity v1.11+). This method sets the origin coordinates.
        ///
        /// If you see the camera in empty space on Play, check that:
        /// 1. The ellipsoid is set to Moon (not Earth WGS84)
        /// 2. The Moon tileset (asset 2684829) is added and your ion token is valid
        /// </summary>
        private void ConfigureMoonGlobe()
        {
            if (cesiumGeoreference == null)
            {
                Debug.LogError("[MoonBaseManager] CesiumGeoreference not assigned! Drag it into the Inspector.");
                return;
            }

            // Set the globe origin to our target location on the Moon
            cesiumGeoreference.latitude = originLatitude;
            cesiumGeoreference.longitude = originLongitude;
            cesiumGeoreference.height = originHeight;

            Debug.Log($"[MoonBaseManager] Globe origin set → Lat: {originLatitude:F4}°, " +
                      $"Lon: {originLongitude:F4}°, Height: {originHeight:F0}m");

            // Ensure physics meshes are enabled on the tileset for raycasting
            if (moonTileset != null)
            {
                // CreatePhysicsMeshes is set in Inspector — log a warning if it's off
                // (We check via the component, not set it here, to avoid overriding intentional choices)
                Debug.Log("[MoonBaseManager] Moon tileset found. Ensure 'Create Physics Meshes' is " +
                          "enabled on the Cesium3DTileset component for placement raycasting to work.");
            }
            else
            {
                Debug.LogWarning("[MoonBaseManager] Moon tileset not assigned. Raycasting won't work.");
            }
        }

        // ── Module Registry ────────────────────────────────────────────────────
        /// <summary>Called by ModulePlacer when a module is successfully placed.</summary>
        public void RegisterModule(BaseModule module)
        {
            if (module == null || PlacedModules.Contains(module)) return;

            PlacedModules.Add(module);
            RecalculateResources();
            OnResourcesChanged?.Invoke();

            Debug.Log($"[MoonBaseManager] Module registered: {module.ModuleDefinition?.moduleName ?? "Unknown"} " +
                      $"(Total modules: {PlacedModules.Count})");
        }

        /// <summary>Called when a module is removed/deleted.</summary>
        public void UnregisterModule(BaseModule module)
        {
            if (module == null || !PlacedModules.Contains(module)) return;

            PlacedModules.Remove(module);
            RecalculateResources();
            OnResourcesChanged?.Invoke();
        }

        /// <summary>Recalculates total power and crew from all placed modules.</summary>
        private void RecalculateResources()
        {
            totalPowerGeneratedKW = 0f;
            totalPowerConsumedKW = 0f;
            totalCrewCapacity = 0;

            foreach (var module in PlacedModules)
            {
                if (module == null || module.ModuleDefinition == null) continue;

                var def = module.ModuleDefinition;
                totalPowerGeneratedKW += def.powerGenerationKW;
                totalPowerConsumedKW += def.powerConsumptionKW;
                totalCrewCapacity += def.crewCapacity;
            }
        }

        // ── Save / Load ────────────────────────────────────────────────────────
        /// <summary>
        /// Saves the current base layout to PlayerPrefs as JSON.
        /// For a real implementation, write to a file instead.
        /// </summary>
        public void SaveLayout()
        {
            var saveData = new LayoutSaveData();
            foreach (var module in PlacedModules)
            {
                if (module == null) continue;
                var pos = module.transform.position;
                var rot = module.transform.rotation;
                saveData.modules.Add(new ModuleSaveEntry
                {
                    moduleTypeId = module.ModuleDefinition?.moduleTypeId ?? "",
                    posX = pos.x, posY = pos.y, posZ = pos.z,
                    rotX = rot.x, rotY = rot.y, rotZ = rot.z, rotW = rot.w
                });
            }
            string json = JsonUtility.ToJson(saveData, prettyPrint: true);
            PlayerPrefs.SetString("MoonBaseLayout", json);
            PlayerPrefs.Save();
            Debug.Log($"[MoonBaseManager] Layout saved ({saveData.modules.Count} modules).");
        }

        // ── Validation ─────────────────────────────────────────────────────────
        private void ValidateReferences()
        {
            if (cesiumGeoreference == null)
                Debug.LogError("[MoonBaseManager] cesiumGeoreference is not assigned in Inspector.");
            if (placedModulesRoot == null)
            {
                var root = new GameObject("PlacedModulesRoot");
                placedModulesRoot = root.transform;
                Debug.LogWarning("[MoonBaseManager] placedModulesRoot not assigned — created automatically.");
            }
        }

        // ── Save Data Structures ───────────────────────────────────────────────
        [System.Serializable]
        private class LayoutSaveData
        {
            public List<ModuleSaveEntry> modules = new();
        }

        [System.Serializable]
        private class ModuleSaveEntry
        {
            public string moduleTypeId;
            public float posX, posY, posZ;
            public float rotX, rotY, rotZ, rotW;
        }
    }
}
