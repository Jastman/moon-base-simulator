using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MoonBase.Core;
using MoonBase.Modules;

namespace MoonBase.UI
{
    /// <summary>
    /// Info panel shown when a placed module is clicked.
    /// Uses public API from BaseModule and PowerModule.
    /// </summary>
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
            if (clearDustButton  != null) clearDustButton.onClick.AddListener(OnClearDustClicked);
            if (removeModuleButton != null) removeModuleButton.onClick.AddListener(OnRemoveModuleClicked);
            Hide();
        }

        public void Show(BaseModule module)
        {
            if (module == null) { Hide(); return; }
            currentModule = module;
            if (panelRoot != null) panelRoot.SetActive(true);

            // Module name and type from ModuleDefinition
            var def = module.ModuleDefinition;
            if (moduleNameLabel != null)
                moduleNameLabel.text = def != null ? def.moduleName : module.gameObject.name;
            if (moduleTypeLabel != null)
                moduleTypeLabel.text = def != null ? def.moduleType.ToString() : "Unknown";

            // Power info from PowerModule if present
            var power = module.GetComponent<PowerModule>();
            if (powerLabel != null)
            {
                if (power != null)
                    powerLabel.text = $"Power: {power.CurrentPowerOutput:F1} / {power.basePowerKW:F1} kW";
                else
                    powerLabel.text = "Power: N/A";
            }

            // Status — PowerModule exposes IsActive, else just show "Placed"
            if (statusLabel != null)
                statusLabel.text = power != null
                    ? $"Status: {(power.IsActive ? "Online" : "Offline")}"
                    : "Status: Placed";

            // Dust level from PowerModule
            if (dustLabel != null)
                dustLabel.text = power != null
                    ? $"Dust: {power.dustAccumulation * 100f:F0}%"
                    : "Dust: N/A";
        }

        public void Hide()
        {
            currentModule = null;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnClearDustClicked()
        {
            if (currentModule == null) return;
            var power = currentModule.GetComponent<PowerModule>();
            if (power != null) power.ClearDust();
            Show(currentModule); // refresh
        }

        private void OnRemoveModuleClicked()
        {
            if (currentModule == null) return;
            Destroy(currentModule.gameObject);
            Hide();
        }
    }
}
