using UnityEngine;
using UnityEditor;
using MoonBase.Core;
using MoonBase.UI;
using MoonBase.DataLayers;
using MoonBase.CameraSystem;

namespace MoonBase.Editor
{
    public class SceneWirer
    {
        [MenuItem("MoonBase/Wire Scene References")]
        public static void WireSceneReferences()
        {
            Debug.Log("=== Scene Wiring Started ===");

            // Find and wire SolarExposureManager
            WireSolarExposureManager();

            // Find and wire LunarSkybox
            WireLunarSkybox();

            // Find and wire MoonCameraController
            WireMoonCameraController();

            // Verify CesiumMoonSetup
            VerifyCesiumSetup();

            // Check OperationsDashboardUI
            CheckOperationsDashboardUI();

            Debug.Log("=== Scene Wiring Completed ===");
            EditorUtility.DisplayDialog("Scene Wiring", "Scene references have been wired. Check console for details.", "OK");
        }

        private static void WireSolarExposureManager()
        {
            Debug.Log("--- Wiring SolarExposureManager ---");
            SolarExposureManager manager = FindObjectOfType<SolarExposureManager>();
            if (manager == null)
            {
                Debug.LogWarning("SolarExposureManager not found in scene");
                return;
            }

            Light sunLight = GameObject.Find("Sun_Directional")?.GetComponent<Light>();
            if (sunLight == null)
            {
                Debug.LogWarning("Sun_Directional not found in scene");
                return;
            }

            // Use reflection to set private field
            var field = typeof(SolarExposureManager).GetField("sunLight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(manager, sunLight);
                EditorUtility.SetDirty(manager);
                Debug.Log("SolarExposureManager.sunLight wired successfully");
            }
        }

        private static void WireLunarSkybox()
        {
            Debug.Log("--- Wiring LunarSkybox ---");
            LunarSkybox skybox = FindObjectOfType<LunarSkybox>();
            if (skybox == null)
            {
                Debug.LogWarning("LunarSkybox not found in scene");
                return;
            }

            Light sunLight = GameObject.Find("Sun_Directional")?.GetComponent<Light>();
            if (sunLight == null)
            {
                Debug.LogWarning("Sun_Directional not found in scene");
                return;
            }

            var field = typeof(LunarSkybox).GetField("directionalLight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(skybox, sunLight);
                EditorUtility.SetDirty(skybox);
                Debug.Log("LunarSkybox.directionalLight wired successfully");
            }
        }

        private static void WireMoonCameraController()
        {
            Debug.Log("--- Wiring MoonCameraController ---");
            MoonCameraController controller = FindObjectOfType<MoonCameraController>();
            if (controller == null)
            {
                Debug.LogWarning("MoonCameraController not found in scene");
                return;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("Main camera not found in scene");
                return;
            }

            var field = typeof(MoonCameraController).GetField("mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(controller, mainCamera);
                EditorUtility.SetDirty(controller);
                Debug.Log("MoonCameraController.mainCamera wired successfully");
            }
        }

        private static void VerifyCesiumSetup()
        {
            Debug.Log("--- Verifying CesiumMoonSetup ---");
            CesiumMoonSetup cesiumSetup = FindObjectOfType<CesiumMoonSetup>();
            if (cesiumSetup == null)
            {
                Debug.LogWarning("CesiumMoonSetup not found in scene");
                return;
            }

            // Check for CesiumGeoreference
            object georeference = FindObjectOfType(System.Type.GetType("CesiumForUnity.CesiumGeoreference"));
            if (georeference == null)
            {
                Debug.LogWarning("CesiumGeoreference not found in scene");
            }
            else
            {
                Debug.Log("CesiumGeoreference found");
            }

            // Check for Cesium3DTileset
            object tileset = FindObjectOfType(System.Type.GetType("CesiumForUnity.Cesium3DTileset"));
            if (tileset == null)
            {
                Debug.LogWarning("Cesium3DTileset not found in scene");
            }
            else
            {
                Debug.Log("Cesium3DTileset found");
            }
        }

        private static void CheckOperationsDashboardUI()
        {
            Debug.Log("--- Checking OperationsDashboardUI ---");
            OperationsDashboardUI dashboard = FindObjectOfType<OperationsDashboardUI>();
            if (dashboard == null)
            {
                Debug.LogWarning("OperationsDashboardUI not found in scene");
                return;
            }

            Debug.Log("OperationsDashboardUI found, checking field assignments...");

            var fields = typeof(OperationsDashboardUI).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            int nullCount = 0;
            foreach (var field in fields)
            {
                object value = field.GetValue(dashboard);
                if (value == null)
                {
                    Debug.LogWarning($"  {field.Name} is NULL");
                    nullCount++;
                }
                else
                {
                    Debug.Log($"  {field.Name} is assigned");
                }
            }

            if (nullCount > 0)
            {
                Debug.LogWarning($"OperationsDashboardUI has {nullCount} unassigned field(s)");
            }
            else
            {
                Debug.Log("All OperationsDashboardUI fields are assigned");
            }
        }

        private static T FindObjectOfType<T>() where T : Component
        {
            return Object.FindObjectOfType<T>();
        }

        private static Object FindObjectOfType(System.Type type)
        {
            return Object.FindObjectOfType(type);
        }
    }
}

