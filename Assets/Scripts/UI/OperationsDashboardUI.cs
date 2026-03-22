using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MoonBase.Core;

namespace MoonBase.UI
{
    /// <summary>
    /// Main mission control operations dashboard for LUNAR OPS.
    ///
    /// Displays real-time resource readouts, color-coded status indicators,
    /// a 24-sim-hour power sparkline, and a scrolling alert feed.
    ///
    /// Subscribes to ResourceSimulator.OnResourceUpdate and LunarSimulationClock events.
    /// No polling — all updates are event-driven.
    ///
    /// UI Hierarchy expected (create in Unity Canvas):
    ///   DashboardPanel/
    ///     PowerSection/
    ///       PowerGenLabel       (TMP)
    ///       PowerConLabel       (TMP)
    ///       NetPowerLabel       (TMP)
    ///       BatteryBar          (Slider or Image fillAmount)
    ///       BatteryLabel        (TMP)
    ///       BatteryStatusIcon   (Image)
    ///     WaterSection/
    ///       WaterLabel          (TMP)
    ///       WaterExtractionLabel(TMP)
    ///     LifeSupportSection/
    ///       O2Label             (TMP)
    ///     MissionTimeLabel      (TMP)
    ///     SparklineContainer    (RectTransform — sparkline draws here)
    ///     AlertScrollView/
    ///       AlertContent        (Transform — alert entries parented here)
    ///
    /// Setup: Assign all [SerializeField] references in Inspector.
    ///        Optional: assign alertEntryPrefab (a TMP Text prefab).
    /// </summary>
    public class OperationsDashboardUI : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Power Readouts")]
        [SerializeField] private TextMeshProUGUI powerGenLabel;
        [SerializeField] private TextMeshProUGUI powerConLabel;
        [SerializeField] private TextMeshProUGUI netPowerLabel;
        [SerializeField] private TextMeshProUGUI batteryLabel;
        [SerializeField] private Image           batteryFillImage;
        [SerializeField] private Image           batteryStatusIcon;
        [SerializeField] private Slider          batterySlider;

        [Header("Water & Life Support")]
        [SerializeField] private TextMeshProUGUI waterStoredLabel;
        [SerializeField] private TextMeshProUGUI waterExtractionLabel;
        [SerializeField] private TextMeshProUGUI o2Label;

        [Header("Mission Time")]
        [SerializeField] private TextMeshProUGUI missionTimeLabel;
        [SerializeField] private TextMeshProUGUI lunarDayLabel;
        [SerializeField] private TextMeshProUGUI timeScaleLabel;

        [Header("Sparkline (Power Balance History)")]
        [SerializeField] private RectTransform sparklineContainer;
        [Tooltip("Line renderer or custom Image used for sparkline drawing. " +
                 "If null, sparkline is skipped.")]
        [SerializeField] private LineRenderer   sparklineRenderer;

        [Header("Alert Feed")]
        [SerializeField] private Transform      alertContent;
        [SerializeField] private GameObject     alertEntryPrefab;
        [SerializeField] private ScrollRect     alertScrollRect;
        [Tooltip("Maximum number of alert entries to retain in the feed.")]
        [SerializeField] private int            maxAlerts = 50;

        [Header("Status Colors")]
        [SerializeField] private Color colorGood       = new Color(0.2f, 0.9f, 0.2f);
        [SerializeField] private Color colorWarning    = new Color(1f, 0.75f, 0.1f);
        [SerializeField] private Color colorCritical   = new Color(1f, 0.2f, 0.1f);
        [SerializeField] private Color colorNeutral    = new Color(0.7f, 0.7f, 0.7f);

        // ── Sparkline Data ─────────────────────────────────────────────────────
        private const int SparklineHistorySize = 144; // 24 sim-hours × 6 samples/hour
        private readonly float[] powerHistory = new float[SparklineHistorySize];
        private int historyHead = 0;
        private int historyCount = 0;
        private float sparklineSampleTimer = 0f;
        private const float SparklineSampleIntervalSimHours = 0.25f; // sample every 15 sim-minutes

        // ── Alert Feed ─────────────────────────────────────────────────────────
        private readonly Queue<string> pendingAlerts = new();
        private int alertCount = 0;

        // ── Cached State ───────────────────────────────────────────────────────
        private ResourceSnapshot lastSnapshot;
        private bool dashboardVisible = true;

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void OnEnable()
        {
            if (ResourceSimulator.Instance != null)
                ResourceSimulator.Instance.OnResourceUpdate   += OnResourceUpdate;

            if (ResourceSimulator.Instance != null)
            {
                ResourceSimulator.Instance.OnPowerCritical  += OnPowerCritical;
                ResourceSimulator.Instance.OnPowerRestored  += OnPowerRestored;
            }

            if (LunarSimulationClock.Instance != null)
            {
                LunarSimulationClock.Instance.OnLunarDayStart     += OnLunarDayStart;
                LunarSimulationClock.Instance.OnLunarNightStart   += OnLunarNightStart;
                LunarSimulationClock.Instance.OnTimeScaleChanged  += OnTimeScaleChanged;
                LunarSimulationClock.Instance.OnSimTick           += OnSimTick;
            }
        }

        private void OnDisable()
        {
            if (ResourceSimulator.Instance != null)
            {
                ResourceSimulator.Instance.OnResourceUpdate  -= OnResourceUpdate;
                ResourceSimulator.Instance.OnPowerCritical   -= OnPowerCritical;
                ResourceSimulator.Instance.OnPowerRestored   -= OnPowerRestored;
            }

            if (LunarSimulationClock.Instance != null)
            {
                LunarSimulationClock.Instance.OnLunarDayStart    -= OnLunarDayStart;
                LunarSimulationClock.Instance.OnLunarNightStart  -= OnLunarNightStart;
                LunarSimulationClock.Instance.OnTimeScaleChanged -= OnTimeScaleChanged;
                LunarSimulationClock.Instance.OnSimTick          -= OnSimTick;
            }
        }

        private void Start()
        {
            // Initial refresh with whatever state we have
            if (ResourceSimulator.Instance != null)
                RefreshResourceReadouts(ResourceSimulator.Instance.GetSnapshot());

            if (LunarSimulationClock.Instance != null)
                RefreshMissionTime();

            InitSparkline();
            PostAlert("Mission dashboard initialized.");
        }

        private void LateUpdate()
        {
            // Drain pending alerts on the main thread
            while (pendingAlerts.Count > 0)
                DisplayAlert(pendingAlerts.Dequeue());
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Shows or hides the entire dashboard panel.</summary>
        public void SetVisible(bool visible)
        {
            dashboardVisible = visible;
            gameObject.SetActive(visible);
        }

        /// <summary>
        /// Adds a message to the alert feed from any thread.
        /// Displayed in LateUpdate to ensure main-thread safety.
        /// </summary>
        public void PostAlert(string message)
        {
            string timestamp = LunarSimulationClock.Instance != null
                ? LunarSimulationClock.Instance.GetMETString()
                : System.DateTime.UtcNow.ToString("HH:mm:ss");

            pendingAlerts.Enqueue($"[{timestamp}] {message}");
        }

        // ── Event Handlers ─────────────────────────────────────────────────────
        private void OnResourceUpdate(ResourceSnapshot snap)
        {
            lastSnapshot = snap;
            RefreshResourceReadouts(snap);
            TickSparklineSample(snap.netPowerKW);
        }

        private void OnPowerCritical(float chargePercent)
        {
            PostAlert($"⚠ POWER CRITICAL — Battery at {chargePercent:F1}%");
        }

        private void OnPowerRestored()
        {
            PostAlert("✓ Power restored to safe levels.");
        }

        private void OnLunarDayStart(int dayNumber)
        {
            PostAlert($"☀ Lunar day {dayNumber} started — sun circling horizon.");
        }

        private void OnLunarNightStart()
        {
            PostAlert("◑ Minimum solar illumination — battery is primary power.");
        }

        private void OnTimeScaleChanged(float scale)
        {
            if (timeScaleLabel != null)
                timeScaleLabel.text = scale >= 1000f
                    ? $"{scale / 1000f:F1}k×"
                    : $"{scale:F0}×";
        }

        private void OnSimTick(double metSeconds)
        {
            RefreshMissionTime();
        }

        // ── Display Helpers ────────────────────────────────────────────────────
        private void RefreshResourceReadouts(ResourceSnapshot snap)
        {
            SetLabel(powerGenLabel, $"{snap.powerGeneratedKW:F1} kW", colorGood);
            SetLabel(powerConLabel, $"{snap.powerConsumedKW:F1} kW", colorNeutral);

            Color netColor = snap.netPowerKW >= 0f ? colorGood
                           : snap.netPowerKW >= -5f ? colorWarning
                           : colorCritical;
            SetLabel(netPowerLabel, $"{snap.netPowerKW:+0.0;-0.0;0.0} kW", netColor);

            // Battery
            float batt = snap.batteryChargePercent;
            Color battColor = batt > 20f ? colorGood : batt > 10f ? colorWarning : colorCritical;
            SetLabel(batteryLabel, $"{batt:F1}%", battColor);

            if (batteryFillImage != null)
            {
                batteryFillImage.fillAmount = batt / 100f;
                batteryFillImage.color = battColor;
            }
            if (batterySlider != null)
                batterySlider.value = batt / 100f;

            if (batteryStatusIcon != null)
                batteryStatusIcon.color = snap.isPowerEmergency ? colorCritical
                                        : snap.isPowerCritical  ? colorWarning
                                        : colorGood;

            // Water
            SetLabel(waterStoredLabel,     $"{snap.waterStoredLiters:F0} L",    colorNeutral);
            SetLabel(waterExtractionLabel, $"{snap.waterExtractionLPerDay:F1} L/day",
                     snap.waterExtractionLPerDay > 0f ? colorGood : colorNeutral);

            // O2
            Color o2Color = snap.o2Percent > 80f ? colorGood
                          : snap.o2Percent > 50f ? colorWarning
                          : colorCritical;
            SetLabel(o2Label, $"O₂ {snap.o2Percent:F0}%", o2Color);
        }

        private void RefreshMissionTime()
        {
            if (LunarSimulationClock.Instance == null) return;
            var clock = LunarSimulationClock.Instance;

            if (missionTimeLabel != null)
                missionTimeLabel.text = clock.GetMETString();

            if (lunarDayLabel != null)
            {
                int dayNum   = clock.LunarDayNumber;
                float tod    = clock.TimeOfLunarDay;
                float azim   = clock.SunAzimuthDegrees;
                lunarDayLabel.text = $"Day {dayNum}  |  Sun: {azim:F0}°  |  {clock.SunElevationDegrees:F1}° elev";
            }

            if (timeScaleLabel != null)
            {
                float ts = clock.timeScale;
                timeScaleLabel.text = ts >= 1000f ? $"{ts / 1000f:F1}k×" : $"{ts:F0}×";
            }
        }

        private void SetLabel(TextMeshProUGUI label, string text, Color color)
        {
            if (label == null) return;
            label.text  = text;
            label.color = color;
        }

        // ── Alert Feed ─────────────────────────────────────────────────────────
        private void DisplayAlert(string message)
        {
            if (alertContent == null) return;

            GameObject entry;
            if (alertEntryPrefab != null)
            {
                entry = Instantiate(alertEntryPrefab, alertContent);
            }
            else
            {
                // Fallback: create a basic TMP text object
                entry = new GameObject("AlertEntry", typeof(RectTransform), typeof(TextMeshProUGUI));
                entry.transform.SetParent(alertContent, false);
                var rt = entry.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0f, 24f);
            }

            var tmp = entry.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text     = message;
                tmp.fontSize = 12f;
                tmp.color    = message.Contains("CRITICAL") || message.Contains("WARN")
                               ? colorWarning
                               : message.Contains("✓") ? colorGood
                               : colorNeutral;
            }

            alertCount++;

            // Cull oldest entries
            if (alertCount > maxAlerts && alertContent.childCount > 0)
            {
                Destroy(alertContent.GetChild(0).gameObject);
                alertCount--;
            }

            // Auto-scroll to bottom
            if (alertScrollRect != null)
                Canvas.ForceUpdateCanvases();
                // defer scroll to after layout: set in LateUpdate instead
        }

        // ── Sparkline ──────────────────────────────────────────────────────────
        private void InitSparkline()
        {
            if (sparklineRenderer == null) return;
            sparklineRenderer.positionCount = 0;
            sparklineRenderer.useWorldSpace = false;
        }

        private void TickSparklineSample(float netPowerKW)
        {
            if (LunarSimulationClock.Instance == null) return;

            float simHoursPerSecond = LunarSimulationClock.Instance.timeScale / 3600f;
            sparklineSampleTimer += Time.deltaTime * simHoursPerSecond;

            if (sparklineSampleTimer < SparklineSampleIntervalSimHours) return;
            sparklineSampleTimer = 0f;

            powerHistory[historyHead] = netPowerKW;
            historyHead = (historyHead + 1) % SparklineHistorySize;
            historyCount = Mathf.Min(historyCount + 1, SparklineHistorySize);

            RedrawSparkline();
        }

        private void RedrawSparkline()
        {
            if (sparklineRenderer == null || historyCount < 2) return;
            if (sparklineContainer == null) return;

            float width  = sparklineContainer.rect.width;
            float height = sparklineContainer.rect.height;

            // Find min/max for normalization
            float minVal = float.MaxValue;
            float maxVal = float.MinValue;
            for (int i = 0; i < historyCount; i++)
            {
                int idx = (historyHead - historyCount + i + SparklineHistorySize) % SparklineHistorySize;
                float v = powerHistory[idx];
                if (v < minVal) minVal = v;
                if (v > maxVal) maxVal = v;
            }

            float range = Mathf.Max(maxVal - minVal, 1f); // avoid div/0

            sparklineRenderer.positionCount = historyCount;
            Vector3 containerWorldPos = sparklineContainer.position;

            for (int i = 0; i < historyCount; i++)
            {
                int idx = (historyHead - historyCount + i + SparklineHistorySize) % SparklineHistorySize;
                float xNorm = (float)i / (SparklineHistorySize - 1);
                float yNorm = (powerHistory[idx] - minVal) / range;

                float x = (xNorm - 0.5f) * width;
                float y = (yNorm - 0.5f) * height;
                sparklineRenderer.SetPosition(i, new Vector3(x, y, 0f));
            }

            // Color the line: positive net = green, negative = red
            float latestNet = powerHistory[(historyHead - 1 + SparklineHistorySize) % SparklineHistorySize];
            sparklineRenderer.startColor = latestNet >= 0f ? colorGood : colorCritical;
            sparklineRenderer.endColor   = latestNet >= 0f ? colorGood : colorCritical;
        }
    }
}
