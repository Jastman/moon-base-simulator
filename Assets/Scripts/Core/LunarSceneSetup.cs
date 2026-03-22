// ============================================================
// LunarSceneSetup.cs  â€”  LUNAR OPS
// Runtime one-shot scene initializer. Runs before any other
// Start() via [DefaultExecutionOrder(-100)].
//
// Responsibilities:
//   - Apply lunar physics, ambient light, and URP sky settings
//   - Validate all manager singletons are present
//   - Apply Cesium ion token from Resources/CesiumIonToken.txt
//   - Configure the moon tileset origin and physics mesh flag
//   - Log a structured boot report to the console
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using MoonBase.DataLayers;

#if CESIUM_FOR_UNITY
using CesiumForUnity;
#endif

namespace MoonBase.Core
{
    /// <summary>
    /// Drop this on any persistent GameObject (SceneBootstrap puts it on
    /// "CesiumMoonSetup" inside the Managers hierarchy). It runs at the very
    /// start of the scene, before any other MonoBehaviour Start().
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class LunarSceneSetup : MonoBehaviour
    {
        // â”€â”€ Inspector â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        [Header("Target Location")]
        [Tooltip("Initial latitude. Default = Shackleton Crater rim (south pole).")]
        public double originLatitude  = -89.54;
        public double originLongitude =   0.0;
        [Tooltip("Height above surface in meters for the georeference origin.")]
        public double originHeight    = 3000.0;

        [Header("Cesium")]
        [Tooltip("Maximum screen-space error for the moon terrain tileset. Lower = sharper, slower.")]
        [Range(2f, 32f)]
        public float tilesetSSE = 8f;

        [Header("Lighting")]
        [Tooltip("Intensity of the directional sun light.")]
        [Range(0f, 5f)]
        public float sunIntensity = 1.3f;

        [Tooltip("Ambient light color in lunar night (very dark, slight blue).")]
        public Color ambientNightColor = new Color(0.02f, 0.02f, 0.04f);

        [Header("Debug")]
        public bool verboseLogging = true;

        // â”€â”€ Lifecycle â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void Awake()
        {
            ApplyLunarPhysics();
            ApplyRenderSettings();
        }

        private IEnumerator Start()
        {
            // Wait one frame so all singletons have run their Awake()
            yield return null;

            ApplyCesiumToken();
            ConfigureCesiumTileset();
            ValidateManagers();
            LogBootReport();
        }

        // â”€â”€ Physics â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void ApplyLunarPhysics()
        {
            Physics.gravity = new Vector3(0f, -1.62f, 0f);
            Physics.defaultSolverIterations      = 8;
            Physics.defaultSolverVelocityIterations = 4;
            Log("[LunarSceneSetup] Lunar gravity applied: -1.62 m/sÂ²");
        }

        // â”€â”€ Render settings â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void ApplyRenderSettings()
        {
            // No atmospheric fog on the Moon
            RenderSettings.fog = false;

            // Very dark ambient â€” Moon surface gets nearly all light from direct sun
            RenderSettings.ambientMode  = AmbientMode.Flat;
            RenderSettings.ambientLight = ambientNightColor;

            // Point the scene sun at the first directional light found
            var dirLight = FindFirstObjectByType<Light>();
            if (dirLight != null && dirLight.type == LightType.Directional)
            {
                dirLight.intensity = sunIntensity;
                dirLight.shadows   = LightShadows.Soft;
                RenderSettings.sun = dirLight;
                Log($"[LunarSceneSetup] Sun light configured: intensity {sunIntensity}");
            }
            else
            {
                Debug.LogWarning("[LunarSceneSetup] No directional light found â€” add a Sun light to the scene.");
            }
        }

        // â”€â”€ Cesium ion token â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void ApplyCesiumToken()
        {
            var tokenAsset = Resources.Load<TextAsset>("CesiumIonToken");
            if (tokenAsset == null)
            {
                Debug.LogWarning("[LunarSceneSetup] Resources/CesiumIonToken.txt not found. " +
                                 "Set token manually: Cesium â†’ Cesium ion in the editor.");
                return;
            }

            string token = tokenAsset.text?.Trim();
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogWarning("[LunarSceneSetup] CesiumIonToken.txt is empty.");
                return;
            }

#if CESIUM_FOR_UNITY
            var serverMgr = CesiumIonServerManager.instance;
            if (serverMgr != null)
            {
                serverMgr.defaultServer.defaultIonAccessToken = token;
                Log($"[LunarSceneSetup] Cesium ion token applied ({token.Length} chars).");
            }
            else
            {
                Debug.LogWarning("[LunarSceneSetup] CesiumIonServerManager.instance is null â€” " +
                                 "ensure Cesium package is installed and token file is in Resources.");
            }
#else
            Debug.LogWarning("[LunarSceneSetup] Cesium for Unity package not found. " +
                             "Install via Package Manager: com.cesium.unity");
#endif
        }

        // â”€â”€ Cesium tileset config â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void ConfigureCesiumTileset()
        {
#if CESIUM_FOR_UNITY
            // Find the georeference and set origin
            var georef = FindFirstObjectByType<CesiumGeoreference>();
            if (georef != null)
            {
                georef.latitude  = originLatitude;
                georef.longitude = originLongitude;
                georef.height    = originHeight;
                Log($"[LunarSceneSetup] Georeference origin â†’ {originLatitude:F4}Â°, {originLongitude:F4}Â°, {originHeight:F0}m");
            }
            else
            {
                Debug.LogError("[LunarSceneSetup] CesiumGeoreference not found in scene. " +
                               "Run MoonBase â†’ Scene Setup â†’ Build Lunar Ops Scene.");
            }

            // Configure tileset
            var tileset = FindFirstObjectByType<Cesium3DTileset>();
            if (tileset != null)
            {
                tileset.maximumScreenSpaceError = tilesetSSE;
                tileset.createPhysicsMeshes     = true;
                Log($"[LunarSceneSetup] Tileset configured: SSE={tilesetSSE}, physics meshes=true");

                // Hook into OnTilesetLoaded for post-load work
                tileset.OnTilesetLoaded += OnMoonTilesetLoaded;
            }
            else
            {
                Debug.LogError("[LunarSceneSetup] Cesium3DTileset not found in scene. " +
                               "Run MoonBase â†’ Scene Setup â†’ Build Lunar Ops Scene.");
            }
#else
            Debug.LogWarning("[LunarSceneSetup] Cesium for Unity not installed â€” terrain won't load.");
#endif
        }

#if CESIUM_FOR_UNITY
        private void OnMoonTilesetLoaded()
        {
            Log("[LunarSceneSetup] Moon terrain tileset loaded and ready.");

            // Notify IceDepositManager with initial crater center
            var iceManager = IceDepositManager.Instance;
            if (iceManager != null)
            {
                // Shackleton Crater rim local-space origin (0,0,0 relative to georeference)
                iceManager.SetCraterCenter(Vector3.zero);
                Log("[LunarSceneSetup] IceDepositManager crater center set to origin.");
            }

            // Notify MoonBaseManager the terrain is ready
            if (MoonBaseManager.Instance != null)
            {
                Log("[LunarSceneSetup] MoonBaseManager found â€” terrain is ready for module placement.");
            }
        }
#endif

        // â”€â”€ Manager validation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void ValidateManagers()
        {
            CheckSingleton("MoonBaseManager",    MoonBaseManager.Instance);
            CheckSingleton("LunarSimulationClock", LunarSimulationClock.Instance);
            CheckSingleton("ModeManager",        ModeManager.Instance);
            CheckSingleton("ResourceSimulator",  ResourceSimulator.Instance);

            // IceDepositManager and other data layer managers
            var iceMgr   = FindFirstObjectByType<IceDepositManager>();
            var solarMgr = FindFirstObjectByType<SolarExposureManager>();
            LogPresence("IceDepositManager",      iceMgr);
            LogPresence("SolarExposureManager",   solarMgr);
        }

        private void CheckSingleton(string label, object instance)
        {
            if (instance == null)
                Debug.LogWarning($"[LunarSceneSetup] MISSING: {label} singleton not found. " +
                                  "Rebuild the scene hierarchy via MoonBase â†’ Scene Setup.");
            else
                Log($"[LunarSceneSetup] âœ“ {label}");
        }

        private void LogPresence(string label, Object obj)
        {
            if (obj == null)
                Debug.LogWarning($"[LunarSceneSetup] MISSING: {label} not in scene.");
            else
                Log($"[LunarSceneSetup] âœ“ {label}");
        }

        // â”€â”€ Boot report â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void LogBootReport()
        {
            Debug.Log(
                "[LunarSceneSetup] â”€â”€â”€ LUNAR OPS BOOT REPORT â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€\n" +
                $"  Gravity:       {Physics.gravity.y:F2} m/sÂ²\n" +
                $"  Fog:           {RenderSettings.fog}\n" +
                $"  Ambient:       {RenderSettings.ambientLight}\n" +
                $"  Tileset SSE:   {tilesetSSE}\n" +
                $"  Origin:        {originLatitude:F4}Â°, {originLongitude:F4}Â°, {originHeight:F0}m\n" +
                "â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€"
            );
        }

        private void Log(string msg)
        {
            if (verboseLogging) Debug.Log(msg);
        }
    }
}

