using UnityEngine;
using UnityEngine.UI;
using MoonBase.DataLayers;

namespace MoonBase.UI
{
    /// <summary>
    /// Panel with three toggle buttons to show/hide data layers.
    /// F1 = Solar Exposure, F2 = Ice Deposits, F3 = Temperature
    ///
    /// DataLayerManager layers are referenced by index (matches order in Inspector list).
    /// Default expected order: 0=Solar, 1=Ice, 2=Temperature
    /// </summary>
    public class DataLayerToggleUI : MonoBehaviour
    {
        [Header("Toggles")]
        [SerializeField] private Toggle solarToggle;
        [SerializeField] private Toggle iceToggle;
        [SerializeField] private Toggle tempToggle;

        [Header("Layer Indices (match DataLayerManager.layers list order)")]
        [SerializeField] private int solarLayerIndex = 0;
        [SerializeField] private int iceLayerIndex   = 1;
        [SerializeField] private int tempLayerIndex  = 2;

        private void OnEnable()
        {
            if (solarToggle != null) solarToggle.onValueChanged.AddListener(OnSolarToggle);
            if (iceToggle   != null) iceToggle.onValueChanged.AddListener(OnIceToggle);
            if (tempToggle  != null) tempToggle.onValueChanged.AddListener(OnTempToggle);
        }

        private void OnDisable()
        {
            if (solarToggle != null) solarToggle.onValueChanged.RemoveListener(OnSolarToggle);
            if (iceToggle   != null) iceToggle.onValueChanged.RemoveListener(OnIceToggle);
            if (tempToggle  != null) tempToggle.onValueChanged.RemoveListener(OnTempToggle);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) FlipToggle(solarToggle, solarLayerIndex);
            if (Input.GetKeyDown(KeyCode.F2)) FlipToggle(iceToggle,   iceLayerIndex);
            if (Input.GetKeyDown(KeyCode.F3)) FlipToggle(tempToggle,  tempLayerIndex);
        }

        private void OnSolarToggle(bool on) => SetLayer(solarLayerIndex, on);
        private void OnIceToggle(bool on)   => SetLayer(iceLayerIndex,   on);
        private void OnTempToggle(bool on)  => SetLayer(tempLayerIndex,  on);

        private void SetLayer(int index, bool visible)
        {
            if (DataLayerManager.Instance != null)
                DataLayerManager.Instance.SetLayerVisible(index, visible);
        }

        private void FlipToggle(Toggle t, int layerIndex)
        {
            if (t == null) { SetLayer(layerIndex, !DataLayerManager.Instance?.IsLayerVisible(layerIndex) ?? false); return; }
            t.isOn = !t.isOn; // triggers onValueChanged which calls SetLayer
        }
    }
}
