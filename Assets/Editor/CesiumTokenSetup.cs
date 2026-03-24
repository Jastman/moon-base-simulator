using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Auto-configures the Cesium ion default token when the Unity editor loads.
/// Token is read from Assets/Resources/CesiumIonToken.txt
/// Uses CesiumRuntimeSettings asset which is where Cesium for Unity stores the default token.
/// </summary>
[InitializeOnLoad]
public static class CesiumTokenSetup
{
    static CesiumTokenSetup()
    {
        EditorApplication.delayCall += ApplyToken;
    }

    private static void ApplyToken()
    {
        string tokenFilePath = Path.Combine(Application.dataPath, "Resources", "CesiumIonToken.txt");

        if (!File.Exists(tokenFilePath))
        {
            Debug.LogWarning("[CesiumTokenSetup] CesiumIonToken.txt not found at: " + tokenFilePath);
            return;
        }

        string token = File.ReadAllText(tokenFilePath).Trim();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogWarning("[CesiumTokenSetup] CesiumIonToken.txt is empty.");
            return;
        }

        // Method 1: Set via CesiumRuntimeSettings (the authoritative Cesium default token store)
        try
        {
            var settingsType = System.Type.GetType("CesiumForUnity.CesiumRuntimeSettings, CesiumForUnity");
            if (settingsType != null)
            {
                var prop = settingsType.GetProperty("defaultIonAccessToken",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (prop != null)
                {
                    prop.SetValue(null, token);
                    Debug.Log("[CesiumTokenSetup] Token applied via CesiumRuntimeSettings.defaultIonAccessToken");
                    return;
                }
                // Try instance-based approach
                var instanceProp = settingsType.GetProperty("instance",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (instanceProp != null)
                {
                    var instance = instanceProp.GetValue(null);
                    if (instance != null)
                    {
                        var tokenField = settingsType.GetField("_defaultIonAccessToken",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (tokenField != null)
                        {
                            tokenField.SetValue(instance, token);
                            EditorUtility.SetDirty(instance as UnityEngine.Object);
                            Debug.Log("[CesiumTokenSetup] Token applied via CesiumRuntimeSettings instance");
                            return;
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[CesiumTokenSetup] CesiumRuntimeSettings approach failed: " + e.Message);
        }

        // Method 2: Apply directly to all Cesium3DTilesets in open scenes
        try
        {
            var tilesetType = System.Type.GetType("CesiumForUnity.Cesium3DTileset, CesiumForUnity");
            if (tilesetType != null)
            {
                var tilesets = GameObject.FindObjectsByType(tilesetType,
                    FindObjectsSortMode.None);
                foreach (var t in tilesets)
                {
                    var tokenField = tilesetType.GetField("_ionAccessToken",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (tokenField != null && string.IsNullOrEmpty((string)tokenField.GetValue(t)))
                    {
                        tokenField.SetValue(t, token);
                        EditorUtility.SetDirty(t as UnityEngine.Object);
                    }
                }
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                Debug.Log($"[CesiumTokenSetup] Token applied to {tilesets.Length} tileset(s) and scene saved");
                return;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[CesiumTokenSetup] Direct tileset approach failed: " + e.Message);
        }

        // Fallback: EditorPrefs (Cesium reads this as last resort)
        EditorPrefs.SetString("CesiumIonDefaultToken", token);
        EditorPrefs.SetString("CesiumDefaultIonAccessToken", token);
        Debug.Log("[CesiumTokenSetup] Token saved to EditorPrefs fallback. Length: " + token.Length);
    }
}
