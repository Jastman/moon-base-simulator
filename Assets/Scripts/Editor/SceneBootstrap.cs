using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

namespace MoonBase.Editor
{
    public class SceneBootstrap
    {
        [MenuItem("MoonBase/Setup Scene")]
        public static void SetupScene()
        {
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── Cesium Georeference ──────────────────────────────────────────────
            var georef = new GameObject("CesiumGeoreference");
            // Add CesiumGeoreference component if available
            var georefComp = georef.AddComponent(System.Type.GetType("CesiumForUnity.CesiumGeoreference, CesiumForUnity"));

            // Child: Cesium3DTileset
            var tilesetGO = new GameObject("Cesium3DTileset");
            tilesetGO.transform.SetParent(georef.transform);
            var tilesetType = System.Type.GetType("CesiumForUnity.Cesium3DTileset, CesiumForUnity");
            if (tilesetType != null)
            {
                var tileset = tilesetGO.AddComponent(tilesetType);
                // Set ionAssetID = 2684829
                var so = new SerializedObject(tileset);
                var ionAssetProp = so.FindProperty("_ionAssetID");
                if (ionAssetProp != null) { ionAssetProp.longValue = 2684829L; }
                var physicsMeshesProp = so.FindProperty("_createPhysicsMeshes");
                if (physicsMeshesProp != null) { physicsMeshesProp.boolValue = true; }
                // NOTE: To set the ellipsoid to Moon, in the Inspector find the
                // CesiumGeoreference component and set Ellipsoid to "Moon (IAU2015)".
                // This cannot be set reliably via SerializedProperty in all Cesium versions.
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("Cesium3DTileset configured: ionAssetID=2684829, createPhysicsMeshes=true");
            }
            else
            {
                Debug.LogWarning("CesiumForUnity not found — add Cesium package first, then re-run Setup Scene.");
            }

            // ── SimulationManagers ───────────────────────────────────────────────
            var simManagers = new GameObject("SimulationManagers");
            AddChildWithComponent(simManagers, "MoonBaseManager",    "MoonBase.MoonBaseManager");
            AddChildWithComponent(simManagers, "ResourceSimulator",  "MoonBase.ResourceSimulator");
            AddChildWithComponent(simManagers, "LunarSimulationClock","MoonBase.LunarSimulationClock");
            AddChildWithComponent(simManagers, "SaveSystem",         "MoonBase.SaveSystem");
            AddChildWithComponent(simManagers, "ModeManager",        "MoonBase.ModeManager");

            // ── DataLayers ───────────────────────────────────────────────────────
            var dataLayers = new GameObject("DataLayers");
            AddChildWithComponent(dataLayers, "DataLayerManager",        "MoonBase.DataLayerManager");
            AddChildWithComponent(dataLayers, "SolarExposureManager",    "MoonBase.SolarExposureManager");
            AddChildWithComponent(dataLayers, "TemperatureOverlayManager","MoonBase.TemperatureOverlayManager");
            AddChildWithComponent(dataLayers, "IceDepositManager",       "MoonBase.IceDepositManager");

            // ── ModulePlacer ─────────────────────────────────────────────────────
            var modulePlacerGO = new GameObject("ModulePlacer");
            var modulePlacerType = System.Type.GetType("MoonBase.ModulePlacer, Assembly-CSharp");
            if (modulePlacerType != null) modulePlacerGO.AddComponent(modulePlacerType);
            else Debug.LogWarning("ModulePlacer component not found — attach manually after compiling.");

            // ── DesignCamera ─────────────────────────────────────────────────────
            var camGO = new GameObject("DesignCamera");
            camGO.AddComponent<Camera>();
            var camControllerType = System.Type.GetType("MoonBase.MoonCameraController, Assembly-CSharp");
            if (camControllerType != null) camGO.AddComponent(camControllerType);
            else Debug.LogWarning("MoonCameraController not found — attach manually after compiling.");
            camGO.transform.position = new Vector3(0f, 5000f, 0f);
            camGO.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);

            // ── Sun (Directional Light) ──────────────────────────────────────────
            var sunGO = new GameObject("Sun");
            var sunLight = sunGO.AddComponent<Light>();
            sunLight.type      = LightType.Directional;
            sunLight.intensity = 1.3f;
            sunLight.color     = new Color(1f, 0.97f, 0.92f);
            // Attach LunarSkybox so it drives sun direction from clock
            var skyboxType = System.Type.GetType("MoonBase.LunarSkybox, Assembly-CSharp");
            if (skyboxType != null) sunGO.AddComponent(skyboxType);

            // ── CesiumMoonSetup (gravity + ambient) ──────────────────────────────
            var setupGO = new GameObject("CesiumMoonSetup");
            var setupType = System.Type.GetType("MoonBase.CesiumMoonSetup, Assembly-CSharp");
            if (setupType != null) setupGO.AddComponent(setupType);
            else Debug.LogWarning("CesiumMoonSetup not found — attach manually after compiling.");

            // ── Render Settings ──────────────────────────────────────────────────
            RenderSettings.ambientLight = new Color(0.02f, 0.02f, 0.03f);
            RenderSettings.fog = false;

            // ── Save Scene ───────────────────────────────────────────────────────
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            string scenePath = "Assets/Scenes/MoonBase.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();

            Debug.Log("MoonBase scene setup complete! Scene saved to " + scenePath);
        }

        // Helper: add a child GO and try to attach a named component
        private static void AddChildWithComponent(GameObject parent, string goName, string typeName)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(parent.transform);
            var t = System.Type.GetType(typeName + ", Assembly-CSharp");
            if (t != null)
                go.AddComponent(t);
            else
                Debug.LogWarning($"{typeName} not found — will attach once scripts compile.");
        }
    }
}
