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
            // Find CesiumGeoreference in scene
            var georefType = System.Type.GetType("CesiumForUnity.CesiumGeoreference, CesiumForUnity");
            if (georefType == null) { Debug.LogWarning("[CesiumSetupFixer] CesiumGeoreference type not found."); return 0; }

            var georef = Object.FindFirstObjectByType(georefType) as Component;
            if (georef == null) { Debug.LogWarning("[CesiumSetupFixer] No CesiumGeoreference in scene."); return 0; }

            // Try to set ellipsoid to Moon via reflection
            // In Cesium for Unity ≥1.11, CesiumGeoreference has an ellipsoidOverride property
            var type = georef.GetType();
            string[] ellipsoidFields = { "ellipsoidOverride", "_ellipsoidOverride", "ellipsoid" };
            foreach (var fieldName in ellipsoidFields)
            {
                var field = type.GetField(fieldName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    // Try to find the Moon ellipsoid ScriptableObject
                    string[] ellipsoidGuids = AssetDatabase.FindAssets("Moon t:CesiumEllipsoid");
                    if (ellipsoidGuids.Length > 0)
                    {
                        string ellipsoidPath = AssetDatabase.GUIDToAssetPath(ellipsoidGuids[0]);
                        var moonEllipsoid = AssetDatabase.LoadAssetAtPath<ScriptableObject>(ellipsoidPath);
                        if (moonEllipsoid != null)
                        {
                            field.SetValue(georef, moonEllipsoid);
                            EditorUtility.SetDirty(georef);
                            Debug.Log($"[CesiumSetupFixer] ✓ CesiumGeoreference ellipsoid set to Moon: {ellipsoidPath}");
                            return 1;
                        }
                    }
                    Debug.LogWarning("[CesiumSetupFixer] Moon CesiumEllipsoid asset not found. " +
                        "Set ellipsoidOverride manually in CesiumGeoreference Inspector.");
                    return 0;
                }
            }

            Debug.LogWarning("[CesiumSetupFixer] CesiumGeoreference ellipsoid field not found via reflection. " +
                "Set it manually in the Inspector: select CesiumGeoreference → Ellipsoid Override → Moon.");
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
