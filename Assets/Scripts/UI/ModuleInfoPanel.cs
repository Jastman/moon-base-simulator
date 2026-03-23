using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MoonBase.Core;
using MoonBase.Modules;

namespace MoonBase.UI
{
    public class ModuleInfoPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI moduleNameLabel;
        [SerializeField] private TextMeshProUGUI moduleTypeLabel;
        [SerializeField] private TextMeshProUGUI powerLabel;
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private TextMeshProUGUI dustLabel;

        [SerializeField] private Button clearDustButton;
        [SerializeField] private Button removeModuleButton;

        [SerializeField] private GameObject panelRoot;

        private BaseModule currentModule;

        private void Start()
        {
            // Wire button listeners
            if (clearDustButton != null)
            {
                clearDustButton.onClick.AddListener(OnClearDustClicked);
            }

            if (removeModuleButton != null)
            {
                removeModuleButton.onClick.AddListener(OnRemoveModuleClicked);
            }

            // Start hidden
            Hide();
        }

        public void Show(BaseModule module)
        {
            if (module == null)
            {
                Hide();
                return;
            }

            currentModule = module;

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            // Populate fields
            if (moduleNameLabel != null)
            {
                moduleNameLabel.text = module.moduleName;
            }

            if (moduleTypeLabel != null)
            {
                moduleTypeLabel.text = module.moduleType;
            }

            // Get power info if available
            if (powerLabel != null)
            {
                PowerModule powerModule = module.GetComponent<PowerModule>();
                if (powerModule != null)
                {
                    powerLabel.text = $"Power: {powerModule.powerConsumption} kW";
                }
                else
                {
                    powerLabel.text = "Power: N/A";
                }
            }

            // Status
            if (statusLabel != null)
            {
                statusLabel.text = $"Status: {(module.isOperational ? "Online" : "Offline")}";
            }

            // Dust accumulation
            if (dustLabel != null)
            {
                dustLabel.text = $"Dust: {module.dustAccumulation:F1}%";
            }
        }

        public void Hide()
        {
            currentModule = null;

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            // Clear any button listeners to avoid memory leaks
            if (clearDustButton != null)
            {
                clearDustButton.onClick.RemoveListener(OnClearDustClicked);
            }

            if (removeModuleButton != null)
            {
                removeModuleButton.onClick.RemoveListener(OnRemoveModuleClicked);
            }
        }

        private void OnClearDustClicked()
        {
            if (currentModule != null)
            {
                currentModule.ClearDust();
                Debug.Log($"Dust cleared on module: {currentModule.moduleName}");

                // Refresh display
                Show(currentModule);
            }
        }

        private void OnRemoveModuleClicked()
        {
            if (currentModule != null)
            {
                string moduleName = currentModule.moduleName;
                Destroy(currentModule.gameObject);
                Debug.Log($"Module removed: {moduleName}");
                Hide();
            }
        }
    }
}
