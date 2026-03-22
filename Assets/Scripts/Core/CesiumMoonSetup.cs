using UnityEngine;

namespace MoonBase
{
    /// <summary>
    /// Configures lunar physics, ambient light, fog, and Cesium Ion token at runtime.
    /// Attach to a GameObject in the scene (SceneBootstrap creates "CesiumMoonSetup" for this).
    /// </summary>
    public class CesiumMoonSetup : MonoBehaviour
    {
        private void Awake()
        {
            // ── Lunar gravity ────────────────────────────────────────────────────
            Physics.gravity = new Vector3(0f, -1.62f, 0f);

            // ── Ambient & fog ────────────────────────────────────────────────────
            RenderSettings.ambientLight = new Color(0.02f, 0.02f, 0.03f);
            RenderSettings.fog = false;

            // ── Cesium Ion token from Resources ─────────────────────────────────
            // Place a TextAsset at Resources/CesiumIonToken.txt containing your token.
            var tokenAsset = Resources.Load<TextAsset>("CesiumIonToken");
            if (tokenAsset != null)
            {
                string token = tokenAsset.text?.Trim();
                if (!string.IsNullOrEmpty(token))
                {
                    ApplyCesiumToken(token);
                }
                else
                {
                    Debug.LogWarning("[CesiumMoonSetup] CesiumIonToken resource is empty.");
                }
            }
            else
            {
                Debug.LogWarning("[CesiumMoonSetup] No Resources/CesiumIonToken.txt found — set token manually in Cesium panel.");
            }

            // ── Hook tileset loaded → IceDepositManager crater center ────────────
            HookTilesetLoaded();
        }

        private static void ApplyCesiumToken(string token)
        {
#if CESIUM_FOR_UNITY
            // CesiumIonServerManager is the runtime entry point in Cesium for Unity 1.x+
            var mgr = CesiumForUnity.CesiumIonServerManager.instance;
            if (mgr != null)
            {
                mgr.defaultServer.defaultIonAccessToken = token;
                Debug.Log("[CesiumMoonSetup] Cesium Ion token applied via CesiumIonServerManager.");
            }
            else
            {
                Debug.LogWarning("[CesiumMoonSetup] CesiumIonServerManager.instance is null — token not set.");
            }
#else
            Debug.LogWarning("[CesiumMoonSetup] Cesium for Unity not present — token not applied.");
#endif
        }

        private void HookTilesetLoaded()
        {
#if CESIUM_FOR_UNITY
            var tileset = FindObjectOfType<CesiumForUnity.Cesium3DTileset>();
            if (tileset == null)
            {
                Debug.LogWarning("[CesiumMoonSetup] No Cesium3DTileset found in scene.");
                return;
            }

            tileset.OnTilesetLoaded += () =>
            {
                // Notify IceDepositManager once terrain is ready.
                // Adjust the crater center coordinates to your target site.
                var iceManager = IceDepositManager.Instance;
                if (iceManager != null)
                {
                    // Example: Shackleton Crater rim (~89.9°S, 0°E) as starting point
                    iceManager.SetCraterCenter(new Vector3(0f, 0f, 0f));
                    Debug.Log("[CesiumMoonSetup] IceDepositManager crater center set after tileset load.");
                }
            };
#endif
        }
    }
}
