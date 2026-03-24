using UnityEditor;
using UnityEngine;
using System.IO;
using System.Reflection;

namespace MoonBase.Editor
{
    /// <summary>
    /// Fixes Cesium configuration issues:
    /// - Sets Project Default Access Token in CesiumRuntimeSettings
    /// - Configures CesiumGeoreference for the Moon (custom ellipsoid)
    /// - Verifies tileset asset ID is 2684829
    /// Run via MoonBase > Fix Cesium Setup
    /// </summary>
    public static class CesiumSetupFixer
    {
        [MenuItem("MoonBase/Fix Cesium Setup")]
        public static void FixCesiumSetup()
        {
            string tokenPath = Path.Combine(Application.dataPath, "Resources", "CesiumIonToken.txt");
            if (!File.Exists(tokenPath))
            {
                Debug.LogError("[CesiumSetupFixer] CesiumIonToken.txt not found.");
                return;
            }
            string token = File.ReadAllText(tokenPath).Trim();

            int fixes = 0;

            // ── 1. Set token in CesiumRuntimeSettings asset ──────────────────
            fixes += SetRuntimeSettingsToken(token);

            // ── 2. Set CesiumGeoreference ellipsoid to Moon ───────────────────
            fixes += FixGeoreferenceEllipsoid();

            // ── 3. Verify tileset asset ID ────────────────────────────────────
            fixes += VerifyTilesetAssetId();

            // ── 4. Save everything ────────────────────────────────────────────
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Debug.Log($"[CesiumSetupFixer] Done. Applied {fixes} fix(es). Check Console for details.");
            EditorUtility.DisplayDialog("Cesium Setup", $"Applied {fixes} fix(es). See Console for details.", "OK");
        }

        private static int SetRuntimeSettingsToken(string token)
        {
            // Find or create CesiumRuntimeSettings asset
            string[] guids = AssetDatabase.FindAssets("t:CesiumRuntimeSettings");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[CesiumSetupFixer] CesiumRuntimeSettings asset not found. " +
                    "Open Window > Cesium > Select or create the default access token manually.");
                return 0;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (settings == null) { Debug.LogWarning("[CesiumSetupFixer] Could not load CesiumRuntimeSettings."); return 0; }

            // Set via reflection (field name varies by version)
            var type = settings.GetType();
            string[] candidateFields = { "_defaultIonAccessToken", "defaultIonAccessToken", 
                                          "m_DefaultIonAccessToken", "ionAccessToken" };
            foreach (var fieldName in candidateFields)
            {
                var field = type.GetField(fieldName, 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(settings, token);
                    EditorUtility.SetDirty(settings);
                    Debug.Log($"[CesiumSetupFixer] ✓ Token set in CesiumRuntimeSettings.{fieldName}");
                    return 1;
                }
            }

            // Try properties
            string[] candidateProps = { "defaultIonAccessToken", "DefaultIonAccessToken" };
            foreach (var propName in candidateProps)
            {
                var prop = type.GetProperty(propName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(settings, token);
                    EditorUtility.SetDirty(settings);
                    Debug.Log($"[CesiumSetupFixer] ✓ Token set via CesiumRuntimeSettings.{propName}");
                    return 1;
                }
            }

            Debug.LogWarning("[CesiumSetupFixer] Could not find token field on CesiumRuntimeSettings. " +
                "Set the token manually: Window > Cesium > Token.");
            return 0;
        }

        private static int FixGeoreferenceEllipsoid()
        {
            // ── Step 1: Ensure Moon ellipsoid asset exists ─────────────────────
            string moonEllipsoidPath = "Assets/CesiumSettings/MoonEllipsoid.asset";
            var ellipsoidType = System.Type.GetType("CesiumForUnity.CesiumEllipsoid, CesiumForUnity") ??
                                System.Type.GetType("CesiumForUnity.CesiumEllipsoid, CesiumRuntime");
            if (ellipsoidType == null)
            {
                Debug.LogWarning("[CesiumSetupFixer] CesiumEllipsoid type not found.");
                return 0;
            }

            ScriptableObject moonEllipsoid = AssetDatabase.LoadAssetAtPath<ScriptableObject>(moonEllipsoidPath);
            if (moonEllipsoid == null)
            {
                // Create the directory if needed
                if (!AssetDatabase.IsValidFolder("Assets/CesiumSettings"))
                    AssetDatabase.CreateFolder("Assets", "CesiumSettings");

                // Instantiate and set Moon radii (IAU2015: 1737400 x 1737400 x 1735800 m)
                moonEllipsoid = ScriptableObject.CreateInstance(ellipsoidType) as ScriptableObject;
                // Set _radii via reflection (double3: x=equatorial, y=equatorial, z=polar)
                var radiiField = ellipsoidType.GetField("_radii",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (radiiField != null)
                {
                    // double3 from Unity.Mathematics
                    var double3Type = radiiField.FieldType;
                    var d3 = System.Activator.CreateInstance(double3Type,
                        new object[] { 1737400.0, 1737400.0, 1735800.0 });
                    radiiField.SetValue(moonEllipsoid, d3);
                }
                AssetDatabase.CreateAsset(moonEllipsoid, moonEllipsoidPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[CesiumSetupFixer] ✓ Created Moon ellipsoid asset at {moonEllipsoidPath}");
            }
            else
            {
                Debug.Log($"[CesiumSetupFixer] Moon ellipsoid asset already exists: {moonEllipsoidPath}");
            }

            // ── Step 2: Assign to CesiumGeoreference ──────────────────────────
            var georefType = System.Type.GetType("CesiumForUnity.CesiumGeoreference, CesiumForUnity") ??
                             System.Type.GetType("CesiumForUnity.CesiumGeoreference, CesiumRuntime");
            if (georefType == null) { Debug.LogWarning("[CesiumSetupFixer] CesiumGeoreference type not found."); return 0; }

            var georef = Object.FindFirstObjectByType(georefType) as Component;
            if (georef == null) { Debug.LogWarning("[CesiumSetupFixer] No CesiumGeoreference in scene."); return 0; }

            // Try SerializedObject first (most reliable for ScriptableObject refs)
            using (var so = new SerializedObject(georef))
            {
                string[] propNames = { "ellipsoidOverride", "_ellipsoidOverride", "ellipsoid", "_ellipsoid" };
                foreach (var propName in propNames)
                {
                    var prop = so.FindProperty(propName);
                    if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        prop.objectReferenceValue = moonEllipsoid;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(georef);
                        Debug.Log($"[CesiumSetupFixer] ✓ CesiumGeoreference.{propName} set to Moon ellipsoid.");
                        return 1;
                    }
                }
            }

            // Fallback: reflection
            string[] ellipsoidFields = { "ellipsoidOverride", "_ellipsoidOverride", "ellipsoid" };
            foreach (var fieldName in ellipsoidFields)
            {
                var field = georefType.GetField(fieldName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(georef, moonEllipsoid);
                    EditorUtility.SetDirty(georef);
                    Debug.Log($"[CesiumSetupFixer] ✓ CesiumGeoreference.{fieldName} set to Moon ellipsoid (reflection).");
                    return 1;
                }
            }

            Debug.LogWarning("[CesiumSetupFixer] Could not assign Moon ellipsoid via code. " +
                $"In Inspector: select CesiumGeoreference → Ellipsoid Override → assign MoonEllipsoid from Assets/CesiumSettings/");
            return 0;
        }

        private static int VerifyTilesetAssetId()
        {
            var tilesetType = System.Type.GetType("CesiumForUnity.Cesium3DTileset, CesiumForUnity");
            if (tilesetType == null) return 0;

            var tileset = Object.FindFirstObjectByType(tilesetType) as Component;
            if (tileset == null) { Debug.LogWarning("[CesiumSetupFixer] No Cesium3DTileset in scene."); return 0; }

            var field = tilesetType.GetField("_ionAssetID",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                long assetId = System.Convert.ToInt64(field.GetValue(tileset));
                if (assetId != 2684829)
                {
                    field.SetValue(tileset, (long)2684829);
                    EditorUtility.SetDirty(tileset);
                    Debug.Log($"[CesiumSetupFixer] ✓ Tileset asset ID corrected to 2684829 (was {assetId})");
                    return 1;
                }
                Debug.Log($"[CesiumSetupFixer] ✓ Tileset asset ID already correct: {assetId}");
            }
            return 0;
        }
    }
}

