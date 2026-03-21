using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MoonBase.DataLayers;

namespace MoonBase.UI
{
    /// <summary>
    /// Data layer toggle panel (top-right corner in Design Mode).
    ///
    /// Displays one toggle button per data layer registered in DataLayerManager.
    /// Also includes a Sun position slider for SolarExposureManager.
    ///
    /// Setup (uGUI):
    ///   Canvas → DesignModeUI
    ///   └── DataLayerPanel (vertical layout group, top-right anchored)
    ///       ├── PanelHeader (TextMeshProUGUI "DATA LAYERS")
    ///       ├── LayerButtonContainer (vertical layout, buttons spawned here)
    ///       └── SunControlsSection
    ///           ├── SunElevationLabel (TextMeshProUGUI)
    ///           ├── SunElevationSlider (Slider, 0-10)
    ///           ├── SunAzimuthLabel (TextMeshProUGUI)
    ///           ├── SunAzimuthSlider (Slider, 0-360)
    ///           └── AnimateSunToggle (Toggle)
    /// </summary>
    public class DataLayerUI : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("References")]
        public DataLayerManager dataLayerManager;
        public SolarExposureManager solarExposureManager;

        [Header("Layer Toggle Container")]
        public Transform layerButtonContainer;
        public GameObject layerToggleButtonPrefab;

        [Header("Sun Controls")]
        public Slider sunElevationSlider;
        public TextMeshProUGUI sunElevationLabel;
        public Slider sunAzimuthSlider;
        public TextMeshProUGUI sunAzimuthLabel;
        public Toggle animateSunToggle;

        [Header("Button Colors")]
        public Color layerOnColor = new Color(0.3f, 0.7f, 1f, 0.9f);
        public Color layerOffColor = new Color(0.2f, 0.2f, 0.3f, 0.8f);

        // ── Private ────────────────────────────────────────────────────────────
        private Button[] layerButtons;

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Start()
        {
            if (dataLayerManager == null)
                dataLayerManager = DataLayerManager.Instance;
            if (solarExposureManager == null)
                solarExposureManager = SolarExposureManager.Instance;

            BuildLayerButtons();
            SetupSunControls();

            if (dataLayerManager != null)
                dataLayerManager.OnLayerToggled += OnLayerToggled;
        }

        private void OnDestroy()
        {
            if (dataLayerManager != null)
                dataLayerManager.OnLayerToggled -= OnLayerToggled;
        }

        // ── Layer Buttons ──────────────────────────────────────────────────────
        private void BuildLayerButtons()
        {
            if (dataLayerManager == null || layerButtonContainer == null ||
                layerToggleButtonPrefab == null) return;

            var layers = dataLayerManager.Layers;
            layerButtons = new Button[layers.Count];

            for (int i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                var btnGO = Instantiate(layerToggleButtonPrefab, layerButtonContainer);
                btnGO.name = $"LayerBtn_{layer.layerName}";

                var btn = btnGO.GetComponent<Button>();
                var label = btnGO.GetComponentInChildren<TextMeshProUGUI>();
                var icon = btnGO.transform.Find("Icon")?.GetComponent<Image>();

                if (label != null) label.text = layer.layerName;
                if (icon != null && layer.icon != null) icon.sprite = layer.icon;

                int capturedIndex = i;
                btn.onClick.AddListener(() => dataLayerManager.ToggleLayer(capturedIndex));

                layerButtons[i] = btn;
                UpdateButtonVisual(i, layer.visibleByDefault);
            }
        }

        private void OnLayerToggled(int index, bool visible)
        {
            UpdateButtonVisual(index, visible);
        }

        private void UpdateButtonVisual(int index, bool active)
        {
            if (layerButtons == null || index < 0 || index >= layerButtons.Length) return;
            var btn = layerButtons[index];
            if (btn == null) return;

            var img = btn.GetComponent<Image>();
            if (img != null)
                img.color = active ? layerOnColor : layerOffColor;

            // Update text
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null && dataLayerManager != null)
            {
                var layer = dataLayerManager.Layers[index];
                label.text = $"{(active ? "● " : "○ ")}{layer.layerName}";
            }
        }

        // ── Sun Controls ───────────────────────────────────────────────────────
        private void SetupSunControls()
        {
            if (solarExposureManager == null) return;

            if (sunElevationSlider != null)
            {
                sunElevationSlider.minValue = 0f;
                sunElevationSlider.maxValue = 10f;
                sunElevationSlider.value = solarExposureManager.sunElevationDegrees;
                sunElevationSlider.onValueChanged.AddListener(OnElevationChanged);
                UpdateElevationLabel(solarExposureManager.sunElevationDegrees);
            }

            if (sunAzimuthSlider != null)
            {
                sunAzimuthSlider.minValue = 0f;
                sunAzimuthSlider.maxValue = 360f;
                sunAzimuthSlider.value = solarExposureManager.sunAzimuthDegrees;
                sunAzimuthSlider.onValueChanged.AddListener(OnAzimuthChanged);
                UpdateAzimuthLabel(solarExposureManager.sunAzimuthDegrees);
            }

            if (animateSunToggle != null)
            {
                animateSunToggle.isOn = solarExposureManager.animateSunPosition;
                animateSunToggle.onValueChanged.AddListener(OnAnimateSunToggled);
            }
        }

        private void OnElevationChanged(float value)
        {
            solarExposureManager?.SetSunPosition(value, solarExposureManager.sunAzimuthDegrees);
            UpdateElevationLabel(value);
        }

        private void OnAzimuthChanged(float value)
        {
            solarExposureManager?.SetSunPosition(solarExposureManager.sunElevationDegrees, value);
            UpdateAzimuthLabel(value);
        }

        private void OnAnimateSunToggled(bool on)
        {
            if (solarExposureManager != null)
                solarExposureManager.animateSunPosition = on;
        }

        private void UpdateElevationLabel(float value)
        {
            if (sunElevationLabel != null)
                sunElevationLabel.text = $"Sun Elevation: {value:F1}°";
        }

        private void UpdateAzimuthLabel(float value)
        {
            if (sunAzimuthLabel != null)
            {
                string dir = GetCompassDirection(value);
                sunAzimuthLabel.text = $"Sun Azimuth: {value:F0}° ({dir})";
            }
        }

        private string GetCompassDirection(float azimuth)
        {
            string[] dirs = { "N", "NE", "E", "SE", "S", "SW", "W", "NW", "N" };
            return dirs[Mathf.RoundToInt(azimuth / 45f) % 8];
        }
    }
}
