using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Auto-configures the Cesium ion token when the Unity editor loads.
/// Token is read from Assets/Resources/CesiumIonToken.txt
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

        // Try to set via CesiumIonSession if available (Cesium for Unity package)
        try
        {
            var sessionType = System.Type.GetType("CesiumForUnity.CesiumIonSession, CesiumForUnity");
            if (sessionType != null)
            {
                var ionMethod = sessionType.GetMethod("Ion",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (ionMethod != null)
                {
                    var session = ionMethod.Invoke(null, null);
                    var connectMethod = sessionType.GetMethod("Connect");
                    if (connectMethod != null)
                    {
                        connectMethod.Invoke(session, new object[] { token });
                        Debug.Log("[CesiumTokenSetup] Cesium ion token applied via CesiumIonSession.");
                        return;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[CesiumTokenSetup] CesiumIonSession method failed: " + e.Message);
        }

        // Fallback: write to EditorPrefs so Cesium can pick it up
        EditorPrefs.SetString("CesiumIonDefaultToken", token);
        Debug.Log("[CesiumTokenSetup] Cesium ion token saved to EditorPrefs (fallback). Token length: " + token.Length);
    }
}
