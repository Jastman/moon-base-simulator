using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MoonBase.Core;
using MoonBase.Modules;

namespace MoonBase.UI
{
    /// <summary>
    /// Module selector toolbar at the bottom of the screen (Design Mode only).
    ///
    /// Displays one button per ModuleDefinition. Clicking a button enters placement
    /// mode for that module type. Clicking the active button again cancels placement.
    ///
    /// Also shows a resource summary bar (power balance, crew count) above the toolbar.
    ///
    /// Setup (uGUI):
    ///   Canvas
    ///   └── DesignModeUI
    ///       ├── ResourceBar (anchored top or bottom, assign to resourceBarRoot)
    ///       │   ├── PowerText (TextMeshProUGUI)  → powerBalanceText
    ///       │   └── CrewText  (TextMeshProUGUI)  → crewCountText
    ///       └── ModuleToolbar (anchored bottom-center, horizontal layout group)
    ///           └── (buttons generated at runtime)
    ///
    /// Assign modulePrefabButton — a Button prefab with:
    ///   - Image component (for icon)
    ///   - TextMeshProUGUI child (for module name)
    ///   - Outline or color block for selection highlight
    /// </summary>
    public class ModuleSelectorUI : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("References")]
        public ModulePlacer modulePlacer;

        [Header("Module Definitions")]
        [Tooltip("List of modules to show in the toolbar. Assign your ModuleDefinition ScriptableObjects.")]
        public List<ModuleDefinition> availableModules = new();

        [Header("Toolbar Container")]
        [Tooltip("The horizontal layout group where module buttons are spawned.")]
        public Transform toolbarContainer;

        [Tooltip("Prefab for each module button (Button + Image + TextMeshProUGUI child).")]
        public GameObject modulePrefabButton;

        [Header("Selection Visuals")]
        public Color selectedButtonColor = new Color(0.4f, 0.8f, 1f);
        public Color defaultButtonColor = new Color(0.2f, 0.2f, 0.3f, 0.9f);

        [Header("Resource Bar")]
        public TextMeshProUGUI powerBalanceText;
        public TextMeshProUGUI crewCountText;
        public Image powerBalanceIcon;
        public Color powerSurplusColor = new Color(0.2f, 0.9f, 0.3f);
        public Color powerDeficitColor = new Color(0.9f, 0.3f, 0.2f);

        [Header("Info Tooltip")]
        [Tooltip("Small tooltip panel shown on button hover.")]
        public GameObject tooltipPanel;
        public TextMeshProUGUI tooltipNameText;
        public TextMeshProUGUI tooltipDescText;
        public TextMeshProUGUI tooltipStatsText;

        // ── Private ────────────────────────────────────────────────────────────
        private List<Button> moduleButtons = new();
        private int selectedIndex = -1;

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Start()
        {
            if (modulePlacer == null) modulePlacer = ModulePlacer.Instance;

            BuildToolbar();

            // Subscribe to placement events to keep button state in sync
            if (modulePlacer != null)
            {
                modulePlacer.OnPlacementCancelled += OnPlacementCancelled;
                modulePlacer.OnModulePlaced += _ => UpdateResourceBar();
                modulePlacer.OnModuleDeselected += UpdateResourceBar;
            }

            // Subscribe to resource changes
            if (MoonBaseManager.Instance != null)
                MoonBaseManager.Instance.OnResourcesChanged += UpdateResourceBar;

            HideTooltip();
            UpdateResourceBar();
        }

        private void OnDestroy()
        {
            if (modulePlacer != null)
            {
                modulePlacer.OnPlacementCancelled -= OnPlacementCancelled;
            }
            if (MoonBaseManager.Instance != null)
                MoonBaseManager.Instance.OnResourcesChanged -= UpdateResourceBar;
        }

        // ── Toolbar Construction ───────────────────────────────────────────────
        private void BuildToolbar()
        {
            if (toolbarContainer == null || modulePrefabButton == null)
            {
                Debug.LogError("[ModuleSelectorUI] toolbarContainer or modulePrefabButton not assigned.");
                return;
            }

            moduleButtons.Clear();

            for (int i = 0; i < availableModules.Count; i++)
            {
                var definition = availableModules[i];
                if (definition == null) continue;

                var buttonGO = Instantiate(modulePrefabButton, toolbarContainer);
                buttonGO.name = $"ModuleBtn_{definition.moduleTypeId}";

                var button = buttonGO.GetComponent<Button>();
                if (button == null) continue;

                // Set icon
                var iconImage = buttonGO.GetComponent<Image>();
                if (iconImage != null && definition.icon != null)
                    iconImage.sprite = definition.icon;

                // Set label
                var label = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = definition.moduleName;

                // Wire up click (capture i in closure)
                int capturedIndex = i;
                button.onClick.AddListener(() => OnModuleButtonClicked(capturedIndex));

                // Wire up hover for tooltip
                var trigger = buttonGO.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                AddHoverEvents(trigger, capturedIndex);

                SetButtonColor(button, false);
                moduleButtons.Add(button);
            }
        }

        // ── Button Callbacks ───────────────────────────────────────────────────
        private void OnModuleButtonClicked(int index)
        {
            if (index < 0 || index >= availableModules.Count) return;

            if (selectedIndex == index)
            {
                // Clicking the active module again cancels placement
                CancelSelection();
                return;
            }

            // Deselect previous
            if (selectedIndex >= 0 && selectedIndex < moduleButtons.Count)
                SetButtonColor(moduleButtons[selectedIndex], false);

            selectedIndex = index;
            SetButtonColor(moduleButtons[index], true);

            modulePlacer?.BeginPlacement(availableModules[index]);
        }

        private void OnPlacementCancelled()
        {
            CancelSelection();
        }

        private void CancelSelection()
        {
            if (selectedIndex >= 0 && selectedIndex < moduleButtons.Count)
                SetButtonColor(moduleButtons[selectedIndex], false);
            selectedIndex = -1;
        }

        // ── Tooltip ────────────────────────────────────────────────────────────
        private void AddHoverEvents(UnityEngine.EventSystems.EventTrigger trigger, int index)
        {
            var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
            };
            enterEntry.callback.AddListener(_ => ShowTooltip(index));
            trigger.triggers.Add(enterEntry);

            var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit
            };
            exitEntry.callback.AddListener(_ => HideTooltip());
            trigger.triggers.Add(exitEntry);
        }

        private void ShowTooltip(int index)
        {
            if (tooltipPanel == null || index < 0 || index >= availableModules.Count) return;

            var def = availableModules[index];
            tooltipPanel.SetActive(true);

            if (tooltipNameText != null) tooltipNameText.text = def.moduleName;
            if (tooltipDescText != null) tooltipDescText.text = def.description;
            if (tooltipStatsText != null)
            {
                string stats = "";
                if (def.powerGenerationKW > 0) stats += $"⚡ +{def.powerGenerationKW} kW\n";
                if (def.powerConsumptionKW > 0) stats += $"⚡ -{def.powerConsumptionKW} kW\n";
                if (def.crewCapacity > 0) stats += $"👤 {def.crewCapacity} crew\n";
                if (def.waterExtractionLitersPerDay > 0) stats += $"💧 {def.waterExtractionLitersPerDay} L/day";
                tooltipStatsText.text = stats.TrimEnd();
            }
        }

        private void HideTooltip()
        {
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }

        // ── Resource Bar ───────────────────────────────────────────────────────
        private void UpdateResourceBar()
        {
            var manager = MoonBaseManager.Instance;
            if (manager == null) return;

            if (powerBalanceText != null)
            {
                float balance = manager.NetPowerKW;
                powerBalanceText.text = balance >= 0
                    ? $"Power: +{balance:F1} kW surplus"
                    : $"Power: {balance:F1} kW deficit";
                powerBalanceText.color = balance >= 0 ? powerSurplusColor : powerDeficitColor;
            }

            if (crewCountText != null)
                crewCountText.text = $"Crew: {manager.TotalCrewCapacity}";
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private void SetButtonColor(Button button, bool selected)
        {
            if (button == null) return;
            var img = button.GetComponent<Image>();
            if (img != null)
                img.color = selected ? selectedButtonColor : defaultButtonColor;
        }
    }
}
