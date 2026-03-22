// ============================================================
// SceneBootstrap.cs  â€”  LUNAR OPS
// Creates or rebuilds the main "LunarOps" scene hierarchy
// from scratch, wiring every Manager, Cesium component, Light
// and UI root that the simulator needs.
//
// Run via:  MoonBase â†’ Scene Setup â†’ Build Lunar Ops Scene
// ============================================================

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using MoonBase.DataLayers;
using MoonBase.CameraSystem;
using MoonBase.Modules;

namespace MoonBase.Editor
{
    public static class SceneBootstrap
    {
        private const string ScenePath   = "Assets/Scenes/LunarOps.unity";
        private const string ScenesFolder = "Assets/Scenes";

        // â”€â”€ Menu items â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        [MenuItem("MoonBase/Scene Setup/Build Lunar Ops Scene", priority = 0)]
        public static void BuildScene()
        {
            // Prompt if an existing scene has unsaved changes
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolderExists(ScenesFolder);

            // Create a new empty scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildHierarchy(scene);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            // Add scene to Build Settings if not already there
            AddSceneToBuildSettings(ScenePath);

            EditorUtility.DisplayDialog(
                "Lunar Ops Scene Created",
                $"Scene saved to:\n{ScenePath}\n\n" +
                "Next steps:\n" +
                "1. Open the scene and select CesiumGeoreference â†’ set Ellipsoid to Moon\n" +
                "2. Select CesiumMoonTerrain â†’ assign your ion token in the Cesium panel\n" +
                "3. Run MoonBase â†’ Create Default Module Assets\n" +
                "4. Hit Play",
                "OK");

            Debug.Log("[SceneBootstrap] Lunar Ops scene built successfully at " + ScenePath);
        }

        [MenuItem("MoonBase/Scene Setup/Rebuild Hierarchy (keep scene)", priority = 1)]
        public static void RebuildHierarchy()
        {
            BuildHierarchy(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("[SceneBootstrap] Hierarchy rebuilt in active scene.");
        }

        // â”€â”€ Core builder â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private static void BuildHierarchy(Scene scene)
        {
            // â”€â”€ 1. Environment â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            var envRoot = GetOrCreate("Environment");

            // Directional light (sun)
            var sunGO = GetOrCreateChild(envRoot, "Sun_Directional");
            if (!sunGO.TryGetComponent<Light>(out var sunLight))
                sunLight = sunGO.AddComponent<Light>();
            sunLight.type      = LightType.Directional;
            sunLight.color     = new Color(1f, 0.97f, 0.92f);
            sunLight.intensity = 1.3f;
            sunLight.shadows   = LightShadows.Soft;
            sunGO.transform.rotation = Quaternion.Euler(40f, 30f, 0f);
            // Hook up LunarSkybox script (drives sun from sim clock)
            AddComponentIfMissing<MoonBase.LunarSkybox>(sunGO);

            // â”€â”€ 2. Cesium Georeference root â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            // The CesiumGeoreference GO must be in the scene root for Cesium to work properly.
            var georefGO = GetOrCreate("CesiumGeoreference");
            var georef = AddCesiumComponent(georefGO, "CesiumForUnity.CesiumGeoreference");
            if (georef != null)
            {
                // Shackleton Crater rim â€” south pole base site
                SetField(georef, "latitude",  -89.54);
                SetField(georef, "longitude",  0.0);
                SetField(georef, "height",     3000.0);
                // NOTE: Ellipsoid must be set manually in Inspector to Moon (1737.4 km)
                Debug.Log("[SceneBootstrap] CesiumGeoreference added â€” remember to set Ellipsoid â†’ Moon in Inspector.");
            }

            // â”€â”€ 3. Moon Terrain Tileset (child of georeference) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            var tilesetGO = GetOrCreateChild(georefGO, "CesiumMoonTerrain");
            var tileset = AddCesiumComponent(tilesetGO, "CesiumForUnity.Cesium3DTileset");
            if (tileset != null)
            {
                // Cesium Moon Terrain â€“ LRO LOLA Colorized (ion asset 2684829)
                SetField(tileset, "ionAssetID",         2684829L);
                SetField(tileset, "maximumScreenSpaceError", 8f);
                SetField(tileset, "preloadAncestors",   true);
                SetField(tileset, "preloadSiblings",    false);
                SetField(tileset, "createPhysicsMeshes", true);
                Debug.Log("[SceneBootstrap] Cesium3DTileset added (ion asset 2684829). " +
                          "Ion token will be applied via CesiumMoonSetup at runtime.");
            }

            // Cesium Sky Atmosphere (child of georeference)
            var skyAtmGO = GetOrCreateChild(georefGO, "CesiumSkyAtmosphere");
            AddCesiumComponent(skyAtmGO, "CesiumForUnity.CesiumSkyAtmosphere");

            // â”€â”€ 4. Managers root â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            var managersRoot = GetOrCreate("Managers");

            // CesiumMoonSetup â€” applies ion token + gravity at runtime
            var cesiumSetupGO = GetOrCreateChild(managersRoot, "CesiumMoonSetup");
            AddComponentIfMissing<MoonBase.CesiumMoonSetup>(cesiumSetupGO);

            // LunarSimulationClock
            var clockGO = GetOrCreateChild(managersRoot, "LunarSimulationClock");
            AddComponentIfMissing<MoonBase.Core.LunarSimulationClock>(clockGO);

            // MoonBaseManager (needs references assigned in Inspector after scene build)
            var mbmGO = GetOrCreateChild(managersRoot, "MoonBaseManager");
            var mbm = AddComponentIfMissing<MoonBase.Core.MoonBaseManager>(mbmGO);
            // Wire georeference reference if possible
            if (mbm != null && georef != null)
            {
                var georefComp = georefGO.GetComponent("CesiumForUnity.CesiumGeoreference");
                if (georefComp != null)
                    SetSerializedField(mbm, "cesiumGeoreference", georefComp);
            }

            // ModeManager
            var modeGO = GetOrCreateChild(managersRoot, "ModeManager");
            AddComponentIfMissing<MoonBase.Core.ModeManager>(modeGO);

            // ResourceSimulator
            var resSimGO = GetOrCreateChild(managersRoot, "ResourceSimulator");
            AddComponentIfMissing<MoonBase.Core.ResourceSimulator>(resSimGO);

            // SaveSystem
            var saveGO = GetOrCreateChild(managersRoot, "SaveSystem");
            AddComponentIfMissing<MoonBase.Core.SaveSystem>(saveGO);

            // â”€â”€ 5. Camera â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            var cameraRoot = GetOrCreate("CameraRig");
            var camGO = GetOrCreateChild(cameraRoot, "MainCamera");
            if (!camGO.TryGetComponent<Camera>(out var cam))
                cam = camGO.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.nearClipPlane  = 0.3f;
            cam.farClipPlane   = 1000000f;   // far clip for Moon-scale scene
            cam.fieldOfView    = 60f;
            camGO.transform.localPosition = Vector3.zero;

            // Universal Additional Camera Data (URP)
            AddComponentIfMissing<UniversalAdditionalCameraData>(camGO);

            // AudioListener
            AddComponentIfMissing<AudioListener>(camGO);

            // MoonCameraController
            AddComponentIfMissing<MoonBase.CameraSystem.MoonCameraController>(camGO);

            // â”€â”€ 6. Terrain utilities â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            var terrainRoot = GetOrCreate("TerrainUtils");

            var raycasterGO = GetOrCreateChild(terrainRoot, "TerrainRaycaster");
            AddComponentIfMissing<MoonBase.Core.TerrainRaycaster>(raycasterGO);

            // â”€â”€ 7. Data Layers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            var dataLayersRoot = GetOrCreate("DataLayers");

            var dataLayerMgrGO = GetOrCreateChild(dataLayersRoot, "DataLayerManager");
            AddComponentIfMissing<MoonBase.DataLayers.DataLayerManager>(dataLayerMgrGO);

            var iceGO = GetOrCreateChild(dataLayersRoot, "IceDepositManager");
            AddComponentIfMissing<MoonBase.DataLayers.IceDepositManager>(iceGO);

            var solarGO = GetOrCreateChild(dataLayersRoot, "SolarExposureManager");
            AddComponentIfMissing<MoonBase.DataLayers.SolarExposureManager>(solarGO);

            var tempGO = GetOrCreateChild(dataLayersRoot, "TemperatureOverlayManager");
            AddComponentIfMissing<MoonBase.DataLayers.TemperatureOverlayManager>(tempGO);

            // â”€â”€ 8. Module system â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            var moduleRoot = GetOrCreate("ModuleSystem");

            var modulePlacerGO = GetOrCreateChild(moduleRoot, "ModulePlacer");
            AddComponentIfMissing<MoonBase.Modules.ModulePlacer>(modulePlacerGO);

            // Empty root where placed modules get parented at runtime
            var placedRootGO = GetOrCreateChild(moduleRoot, "PlacedModulesRoot");
            if (mbm != null)
                SetSerializedField(mbm, "placedModulesRoot", placedRootGO.transform);

            // â”€â”€ 9. UI root â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            // We create an empty UI root; actual Canvas setup requires the scene to be open.
            var uiRoot = GetOrCreate("UI");
            uiRoot.layer = LayerMask.NameToLayer("UI");

            Debug.Log("[SceneBootstrap] Full hierarchy created/updated.");
        }

        // â”€â”€ Cesium component helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // We use reflection so this compiles even without Cesium package present.

        private static Component AddCesiumComponent(GameObject go, string typeName)
        {
            var t = System.Type.GetType(typeName + ", CesiumForUnity") ??
                    System.Type.GetType(typeName + ", CesiumRuntime");
            if (t == null)
            {
                Debug.LogWarning($"[SceneBootstrap] Type '{typeName}' not found â€” is Cesium for Unity installed?");
                return null;
            }
            if (go.GetComponent(t) != null) return go.GetComponent(t);
            return (Component)go.AddComponent(t);
        }

        private static void SetField(object obj, string fieldName, object value)
        {
            var f = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            if (f != null) { f.SetValue(obj, System.Convert.ChangeType(value, f.FieldType)); return; }

            var p = obj.GetType().GetProperty(fieldName,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            if (p != null && p.CanWrite)
                p.SetValue(obj, System.Convert.ChangeType(value, p.PropertyType));
        }

        private static void SetSerializedField(Component component, string fieldName, object value)
        {
            using var so = new SerializedObject(component);
            var prop = so.FindProperty(fieldName);
            if (prop == null) return;

            if (value is Component c)      prop.objectReferenceValue = c;
            else if (value is Object o)    prop.objectReferenceValue = o;
            else if (value is Transform t) prop.objectReferenceValue = t;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // â”€â”€ Generic helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private static T AddComponentIfMissing<T>(GameObject go) where T : Component
        {
            if (!go.TryGetComponent<T>(out var existing))
                return go.AddComponent<T>();
            return existing;
        }

        private static GameObject GetOrCreate(string name)
        {
            var go = GameObject.Find(name);
            if (go == null) go = new GameObject(name);
            return go;
        }

        private static GameObject GetOrCreateChild(GameObject parent, string childName)
        {
            var existing = parent.transform.Find(childName);
            if (existing != null) return existing.gameObject;
            var go = new GameObject(childName);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                var parts  = folderPath.Split('/');
                var parent = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    var full = parent + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(full))
                        AssetDatabase.CreateFolder(parent, parts[i]);
                    parent = full;
                }
            }
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
                if (s.path == scenePath) return;   // already present

            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            scenes.CopyTo(newScenes, 0);
            newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
        }
    }
}
#endif

