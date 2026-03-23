// MoonEllipsoidSetup.cs
// Creates a Moon ellipsoid ScriptableObject asset and assigns it to the
// CesiumGeoreference in the active scene.
// Run via: MoonBase -> Scene Setup -> Set Moon Ellipsoid

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CesiumForUnity;

namespace MoonBase.Editor
{
    public static class MoonEllipsoidSetup
    {
        // Moon mean radius: 1737.4 km (IAU 2015)
        private const double MoonRadiusMeters = 1737400.0;
        private const string EllipsoidAssetPath = "Assets/CesiumSettings/MoonEllipsoid.asset";

        [MenuItem("MoonBase/Scene Setup/Set Moon Ellipsoid", priority = 2)]
        public static void SetMoonEllipsoid()
        {
            // 1. Create or load the Moon ellipsoid asset
            var ellipsoid = AssetDatabase.LoadAssetAtPath<CesiumEllipsoid>(EllipsoidAssetPath);
            if (ellipsoid == null)
            {
                ellipsoid = ScriptableObject.CreateInstance<CesiumEllipsoid>();
                ellipsoid.name = "Moon (IAU2015)";
                // Moon is nearly spherical: all three radii ~1737.4 km
                ellipsoid.SetRadii(new Unity.Mathematics.double3(
                    MoonRadiusMeters, MoonRadiusMeters, MoonRadiusMeters));
                AssetDatabase.CreateAsset(ellipsoid, EllipsoidAssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[MoonEllipsoidSetup] Created Moon ellipsoid asset at " + EllipsoidAssetPath);
            }
            else
            {
                // Ensure radii are correct even on existing asset
                ellipsoid.SetRadii(new Unity.Mathematics.double3(
                    MoonRadiusMeters, MoonRadiusMeters, MoonRadiusMeters));
                EditorUtility.SetDirty(ellipsoid);
                AssetDatabase.SaveAssets();
                Debug.Log("[MoonEllipsoidSetup] Updated existing Moon ellipsoid asset.");
            }

            // 2. Find CesiumGeoreference in scene and assign ellipsoid override
            var georef = Object.FindFirstObjectByType<CesiumGeoreference>();
            if (georef == null)
            {
                Debug.LogError("[MoonEllipsoidSetup] No CesiumGeoreference found in scene. " +
                               "Run MoonBase -> Scene Setup -> Build Lunar Ops Scene first.");
                return;
            }

            var so = new SerializedObject(georef);
            var prop = so.FindProperty("_ellipsoidOverride");
            if (prop != null)
            {
                prop.objectReferenceValue = ellipsoid;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(georef);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(georef.gameObject.scene);
                Debug.Log("[MoonEllipsoidSetup] Moon ellipsoid assigned to CesiumGeoreference.");
            }
            else
            {
                Debug.LogWarning("[MoonEllipsoidSetup] Could not find '_ellipsoidOverride' property. " +
                                 "Drag the asset at " + EllipsoidAssetPath + 
                                 " onto the 'Ellipsoid Override' field in the CesiumGeoreference Inspector manually.");
            }

            EditorUtility.DisplayDialog("Moon Ellipsoid Set",
                "Moon ellipsoid (1737.4 km) assigned to CesiumGeoreference.\n\n" +
                "Save the scene (Ctrl+S) to persist the change.",
                "OK");
        }
    }
}
#endif
