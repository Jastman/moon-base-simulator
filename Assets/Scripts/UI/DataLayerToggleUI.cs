using UnityEngine;
using UnityEngine.UI;
using MoonBase.Core;

namespace MoonBase.UI
{
    public class DataLayerToggleUI : MonoBehaviour
    {
        [SerializeField] private Toggle solarToggle;
        [SerializeField] private Toggle iceToggle;
        [SerializeField] private Toggle tempToggle;

        private void Start()
        {
            // Wire toggle listeners
            if (solarToggle != null)
            {
                solarToggle.onValueChanged.AddListener(OnSolarToggleChanged);
            }

            if (iceToggle != null)
            {
                iceToggle.onValueChanged.AddListener(OnIceToggleChanged);
            }

            if (tempToggle != null)
            {
                tempToggle.onValueChanged.AddListener(OnTempToggleChanged);
            }
        }

        private void OnDestroy()
        {
            // Clean up listeners
            if (solarToggle != null)
            {
                solarToggle.onValueChanged.RemoveListener(OnSolarToggleChanged);
            }

            if (iceToggle != null)
            {
                iceToggle.onValueChanged.RemoveListener(OnIceToggleChanged);
            }

            if (tempToggle != null)
            {
                tempToggle.onValueChanged.RemoveListener(OnTempToggleChanged);
            }
        }

        private void Update()
        {
            HandleKeyboardShortcuts();
        }

        private void HandleKeyboardShortcuts()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (solarToggle != null)
                {
                    solarToggle.isOn = !solarToggle.isOn;
                }
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (iceToggle != null)
                {
                    iceToggle.isOn = !iceToggle.isOn;
                }
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                if (tempToggle != null)
                {
                    tempToggle.isOn = !tempToggle.isOn;
                }
            }
        }

        private void OnSolarToggleChanged(bool isOn)
        {
            DataLayerManager manager = FindObjectOfType<DataLayerManager>();
            if (manager != null)
            {
                manager.SetLayerVisible("SolarExposure", isOn);
                Debug.Log($"Solar Exposure layer {(isOn ? "enabled" : "disabled")}");
            }
        }

        private void OnIceToggleChanged(bool isOn)
        {
            DataLayerManager manager = FindObjectOfType<DataLayerManager>();
            if (manager != null)
            {
                manager.SetLayerVisible("IceDeposits", isOn);
                Debug.Log($"Ice Deposits layer {(isOn ? "enabled" : "disabled")}");
            }
        }

        private void OnTempToggleChanged(bool isOn)
        {
            DataLayerManager manager = FindObjectOfType<DataLayerManager>();
            if (manager != null)
            {
                manager.SetLayerVisible("Temperature", isOn);
                Debug.Log($"Temperature layer {(isOn ? "enabled" : "disabled")}");
            }
        }
    }
}
