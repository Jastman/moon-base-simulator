using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MoonBase.Core;

namespace MoonBase.UI
{
    /// <summary>
    /// Tab switcher at the top of the screen for switching between Design Mode and Rover Mode.
    ///
    /// Setup (uGUI):
    ///   Canvas
    ///   └── ModeTabBar (horizontal layout group, anchored top-center)
    ///       ├── DesignModeTab (Button + TextMeshProUGUI)  → assign to designModeTab
    ///       └── RoverModeTab  (Button + TextMeshProUGUI)  → assign to roverModeTab
    ///
    /// The active tab gets a highlight color; inactive tab is dimmed.
    /// Also shows the keyboard shortcut hint (Tab key).
    /// </summary>
    public class ModeTabUI : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Tab Buttons")]
        public Button designModeTab;
        public Button roverModeTab;

        [Header("Tab Text")]
        public TextMeshProUGUI designModeLabel;
        public TextMeshProUGUI roverModeLabel;

        [Header("Visual Settings")]
        [Tooltip("Color of the active tab background image.")]
        public Color activeTabColor = new Color(0.9f, 0.9f, 1f, 1f);

        [Tooltip("Color of inactive tab background.")]
        public Color inactiveTabColor = new Color(0.3f, 0.3f, 0.4f, 0.8f);

        [Tooltip("Text color for active tab.")]
        public Color activeTextColor = Color.black;

        [Tooltip("Text color for inactive tab.")]
        public Color inactiveTextColor = new Color(0.7f, 0.7f, 0.7f);

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Start()
        {
            if (designModeTab != null)
                designModeTab.onClick.AddListener(OnDesignTabClicked);

            if (roverModeTab != null)
                roverModeTab.onClick.AddListener(OnRoverTabClicked);

            // Subscribe to mode changes so tabs stay in sync
            if (ModeManager.Instance != null)
                ModeManager.Instance.OnModeChanged += OnModeChanged;

            // Set initial state
            UpdateTabVisuals(SimulatorMode.Design);
        }

        private void OnDestroy()
        {
            if (ModeManager.Instance != null)
                ModeManager.Instance.OnModeChanged -= OnModeChanged;
        }

        // ── Callbacks ──────────────────────────────────────────────────────────
        private void OnDesignTabClicked()
        {
            if (ModeManager.Instance != null)
                ModeManager.Instance.SwitchToDesignMode();
        }

        private void OnRoverTabClicked()
        {
            if (ModeManager.Instance != null)
                ModeManager.Instance.SwitchToRoverMode();
        }

        private void OnModeChanged(SimulatorMode newMode)
        {
            UpdateTabVisuals(newMode);
        }

        // ── Visuals ────────────────────────────────────────────────────────────
        private void UpdateTabVisuals(SimulatorMode activeMode)
        {
            bool designActive = activeMode == SimulatorMode.Design;

            SetTabStyle(designModeTab, designModeLabel, designActive);
            SetTabStyle(roverModeTab, roverModeLabel, !designActive);
        }

        private void SetTabStyle(Button tab, TextMeshProUGUI label, bool active)
        {
            if (tab == null) return;

            var img = tab.GetComponent<Image>();
            if (img != null)
                img.color = active ? activeTabColor : inactiveTabColor;

            if (label != null)
            {
                label.color = active ? activeTextColor : inactiveTextColor;
                label.fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
            }
        }
    }
}
