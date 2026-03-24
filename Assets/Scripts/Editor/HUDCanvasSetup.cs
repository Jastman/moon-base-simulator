using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using MoonBase.UI;

namespace MoonBase.Editor
{
    public class HUDCanvasSetup
    {
        [MenuItem("MoonBase/Create HUD Canvas")]
        public static void CreateHUDCanvas()
        {
            // Check if HUD Canvas already exists
            Canvas existingCanvas = Object.FindFirstObjectByType<Canvas>();
            if (existingCanvas != null && existingCanvas.name == "HUDCanvas")
            {
                EditorUtility.DisplayDialog("HUD Canvas", "HUD Canvas already exists in scene.", "OK");
                return;
            }

            // Create root Canvas
            GameObject canvasGO = new GameObject("HUDCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;

            // Create Header Panel
            GameObject headerGO = new GameObject("HeaderPanel");
            headerGO.transform.SetParent(canvasGO.transform, false);
            RectTransform headerRect = headerGO.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0, 1);
            headerRect.offsetMin = new Vector2(0, -48);
            headerRect.offsetMax = Vector2.zero;

            Image headerBg = headerGO.AddComponent<Image>();
            headerBg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);

            // Header Left - Title
            GameObject titleGO = new GameObject("TitleText");
            titleGO.transform.SetParent(headerGO.transform, false);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.5f);
            titleRect.anchorMax = new Vector2(0, 0.5f);
            titleRect.sizeDelta = new Vector2(400, 48);
            titleRect.anchoredPosition = new Vector2(10, 0);

            TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "LUNAR OPS";
            titleText.fontSize = 36;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.Left;

            // Header Left - Subtitle
            GameObject subtitleGO = new GameObject("SubtitleText");
            subtitleGO.transform.SetParent(headerGO.transform, false);
            RectTransform subtitleRect = subtitleGO.AddComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0, 0.5f);
            subtitleRect.anchorMax = new Vector2(0, 0.5f);
            subtitleRect.sizeDelta = new Vector2(400, 48);
            subtitleRect.anchoredPosition = new Vector2(10, -20);

            TextMeshProUGUI subtitleText = subtitleGO.AddComponent<TextMeshProUGUI>();
            subtitleText.text = "SHACKLETON BASE ALPHA";
            subtitleText.fontSize = 20;
            subtitleText.color = new Color(0, 0.831f, 1, 1); // #00D4FF
            subtitleText.alignment = TextAlignmentOptions.Left;

            // Header Center - MET Clock
            GameObject metClockGO = new GameObject("MissionTimeLabel");
            metClockGO.transform.SetParent(headerGO.transform, false);
            RectTransform metRect = metClockGO.AddComponent<RectTransform>();
            metRect.anchorMin = new Vector2(0.5f, 0.5f);
            metRect.anchorMax = new Vector2(0.5f, 0.5f);
            metRect.sizeDelta = new Vector2(300, 48);
            metRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI metText = metClockGO.AddComponent<TextMeshProUGUI>();
            metText.text = "MET 00:00:00";
            metText.fontSize = 40;
            metText.color = Color.white;
            metText.alignment = TextAlignmentOptions.Center;
            metText.fontStyle = FontStyles.Bold;

            // Header Right - Lunar Day
            GameObject lunarDayGO = new GameObject("LunarDayLabel");
            lunarDayGO.transform.SetParent(headerGO.transform, false);
            RectTransform lunarDayRect = lunarDayGO.AddComponent<RectTransform>();
            lunarDayRect.anchorMin = new Vector2(1, 0.5f);
            lunarDayRect.anchorMax = new Vector2(1, 0.5f);
            lunarDayRect.sizeDelta = new Vector2(250, 24);
            lunarDayRect.anchoredPosition = new Vector2(-10, 8);

            TextMeshProUGUI lunarDayText = lunarDayGO.AddComponent<TextMeshProUGUI>();
            lunarDayText.text = "Lunar Day 0";
            lunarDayText.fontSize = 20;
            lunarDayText.color = Color.white;
            lunarDayText.alignment = TextAlignmentOptions.BottomRight;

            // Header Right - TimeScale
            GameObject timeScaleGO = new GameObject("TimeScaleLabel");
            timeScaleGO.transform.SetParent(headerGO.transform, false);
            RectTransform timeScaleRect = timeScaleGO.AddComponent<RectTransform>();
            timeScaleRect.anchorMin = new Vector2(1, 0.5f);
            timeScaleRect.anchorMax = new Vector2(1, 0.5f);
            timeScaleRect.sizeDelta = new Vector2(250, 24);
            timeScaleRect.anchoredPosition = new Vector2(-10, -8);

            TextMeshProUGUI timeScaleText = timeScaleGO.AddComponent<TextMeshProUGUI>();
            timeScaleText.text = "1x Speed";
            timeScaleText.fontSize = 18;
            timeScaleText.color = Color.white;
            timeScaleText.alignment = TextAlignmentOptions.TopRight;

            // Create Left Sidebar
            GameObject sidebarGO = new GameObject("LeftSidebar");
            sidebarGO.transform.SetParent(canvasGO.transform, false);
            RectTransform sidebarRect = sidebarGO.AddComponent<RectTransform>();
            sidebarRect.anchorMin = new Vector2(0, 1);
            sidebarRect.anchorMax = new Vector2(0, 0);
            sidebarRect.pivot = new Vector2(0, 1);
            sidebarRect.offsetMin = new Vector2(0, -48);
            sidebarRect.offsetMax = new Vector2(280, 0);

            Image sidebarBg = sidebarGO.AddComponent<Image>();
            sidebarBg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);

            // Sidebar scroll rect for content
            ScrollRect sidebarScroll = sidebarGO.AddComponent<ScrollRect>();
            sidebarScroll.horizontal = false;

            GameObject sidebarContentGO = new GameObject("Content");
            sidebarContentGO.transform.SetParent(sidebarGO.transform, false);
            RectTransform sidebarContentRect = sidebarContentGO.AddComponent<RectTransform>();
            sidebarContentRect.anchorMin = new Vector2(0, 1);
            sidebarContentRect.anchorMax = new Vector2(1, 1);
            sidebarContentRect.pivot = new Vector2(0.5f, 1);
            sidebarContentRect.sizeDelta = new Vector2(280, 1000);

            VerticalLayoutGroup sidebarLayout = sidebarContentGO.AddComponent<VerticalLayoutGroup>();
            sidebarLayout.childForceExpandHeight = false;
            sidebarLayout.spacing = 8;
            sidebarLayout.padding = new RectOffset(10, 10, 10, 10);

            sidebarScroll.content = sidebarContentRect;

            // POWER Section
            CreateSidebarSection(sidebarContentGO, "POWER", new[] {
                new { label = "PowerGenLabel", text = "GEN: -- kW", color = Color.green },
                new { label = "PowerConLabel", text = "CON: -- kW", color = Color.white },
                new { label = "NetPowerLabel", text = "NET: -- kW", color = Color.white }
            });

            // Battery Bar
            CreateBatteryBar(sidebarContentGO);

            // WATER Section
            CreateSidebarSection(sidebarContentGO, "WATER", new[] {
                new { label = "WaterStoredLabel", text = "STORED: -- L", color = Color.white },
                new { label = "WaterExtractionLabel", text = "EXTRACT: -- L/hr", color = Color.white }
            });

            // LIFE SUPPORT Section
            CreateSidebarSection(sidebarContentGO, "LIFE SUPPORT", new[] {
                new { label = "O2Label", text = "O2: -- %", color = Color.white }
            });

            // Sparkline Container
            GameObject sparklineGO = new GameObject("SparklineContainer");
            sparklineGO.transform.SetParent(sidebarContentGO.transform, false);
            RectTransform sparklineRect = sparklineGO.AddComponent<RectTransform>();
            sparklineRect.sizeDelta = new Vector2(260, 60);

            Image sparklineBg = sparklineGO.AddComponent<Image>();
            sparklineBg.color = new Color(0.1f, 0.1f, 0.13f, 0.8f);

            LineRenderer lineRenderer = sparklineGO.AddComponent<LineRenderer>();
            lineRenderer.startColor = Color.green;
            lineRenderer.endColor = Color.green;
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.01f;
            lineRenderer.sortingLayerName = "UI";

            // Create Alert Feed Panel (bottom-right)
            GameObject alertPanelGO = new GameObject("AlertPanel");
            alertPanelGO.transform.SetParent(canvasGO.transform, false);
            RectTransform alertRect = alertPanelGO.AddComponent<RectTransform>();
            alertRect.anchorMin = new Vector2(1, 0);
            alertRect.anchorMax = new Vector2(1, 0);
            alertRect.pivot = new Vector2(1, 0);
            alertRect.sizeDelta = new Vector2(480, 200);
            alertRect.anchoredPosition = new Vector2(-10, 10);

            Image alertBg = alertPanelGO.AddComponent<Image>();
            alertBg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);

            ScrollRect alertScroll = alertPanelGO.AddComponent<ScrollRect>();
            alertScroll.horizontal = false;

            GameObject alertContentGO = new GameObject("AlertContent");
            alertContentGO.transform.SetParent(alertPanelGO.transform, false);
            RectTransform alertContentRect = alertContentGO.AddComponent<RectTransform>();
            alertContentRect.anchorMin = Vector2.zero;
            alertContentRect.anchorMax = new Vector2(1, 1);
            alertContentRect.offsetMin = Vector2.zero;
            alertContentRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup alertLayout = alertContentGO.AddComponent<VerticalLayoutGroup>();
            alertLayout.childForceExpandHeight = false;
            alertLayout.spacing = 4;
            alertLayout.padding = new RectOffset(8, 8, 8, 8);

            alertScroll.content = alertContentRect;

            // Add OperationsDashboardUI component
            OperationsDashboardUI dashboard = canvasGO.AddComponent<OperationsDashboardUI>();

            // Wire all the fields by name
            WireUIReferences(canvasGO, dashboard);

            // Add LineRenderer to sparkline
            LineRenderer sparklineRenderer = sparklineGO.GetComponent<LineRenderer>();

            EditorUtility.SetDirty(canvasGO);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Success", "HUD Canvas created and wired successfully!", "OK");
        }

        private static void CreateSidebarSection(GameObject parent, string sectionName, dynamic[] labels)
        {
            // Section header
            GameObject headerGO = new GameObject($"{sectionName}Header");
            headerGO.transform.SetParent(parent.transform, false);
            RectTransform headerRect = headerGO.AddComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(260, 20);

            TextMeshProUGUI headerText = headerGO.AddComponent<TextMeshProUGUI>();
            headerText.text = sectionName;
            headerText.fontSize = 16;
            headerText.color = new Color(0, 0.831f, 1, 1); // cyan
            headerText.alignment = TextAlignmentOptions.Left;
            headerText.fontStyle = FontStyles.Bold;

            // Separator
            GameObject sepGO = new GameObject("Separator");
            sepGO.transform.SetParent(parent.transform, false);
            RectTransform sepRect = sepGO.AddComponent<RectTransform>();
            sepRect.sizeDelta = new Vector2(260, 1);

            Image sepImage = sepGO.AddComponent<Image>();
            sepImage.color = new Color(0, 0.831f, 1, 0.3f);

            // Labels
            foreach (var labelInfo in labels)
            {
                GameObject labelGO = new GameObject(labelInfo.label);
                labelGO.transform.SetParent(parent.transform, false);
                RectTransform labelRect = labelGO.AddComponent<RectTransform>();
                labelRect.sizeDelta = new Vector2(260, 20);

                TextMeshProUGUI labelText = labelGO.AddComponent<TextMeshProUGUI>();
                labelText.text = labelInfo.text;
                labelText.fontSize = 18;
                labelText.color = labelInfo.color;
                labelText.alignment = TextAlignmentOptions.Left;
            }
        }

        private static void CreateBatteryBar(GameObject parent)
        {
            // Battery section header
            GameObject headerGO = new GameObject("BatteryHeader");
            headerGO.transform.SetParent(parent.transform, false);
            RectTransform headerRect = headerGO.AddComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(260, 20);

            TextMeshProUGUI headerText = headerGO.AddComponent<TextMeshProUGUI>();
            headerText.text = "BATTERY";
            headerText.fontSize = 16;
            headerText.color = new Color(0, 0.831f, 1, 1);
            headerText.alignment = TextAlignmentOptions.Left;
            headerText.fontStyle = FontStyles.Bold;

            // Separator
            GameObject sepGO = new GameObject("Separator");
            sepGO.transform.SetParent(parent.transform, false);
            RectTransform sepRect = sepGO.AddComponent<RectTransform>();
            sepRect.sizeDelta = new Vector2(260, 1);

            Image sepImage = sepGO.AddComponent<Image>();
            sepImage.color = new Color(0, 0.831f, 1, 0.3f);

            // Battery bar background
            GameObject barBgGO = new GameObject("BatteryBarBg");
            barBgGO.transform.SetParent(parent.transform, false);
            RectTransform barBgRect = barBgGO.AddComponent<RectTransform>();
            barBgRect.sizeDelta = new Vector2(260, 16);

            Image barBgImage = barBgGO.AddComponent<Image>();
            barBgImage.color = new Color(0.1f, 0.1f, 0.13f, 0.6f);

            // Battery bar fill
            GameObject barFillGO = new GameObject("BatteryFillImage");
            barFillGO.transform.SetParent(barBgGO.transform, false);
            RectTransform barFillRect = barFillGO.AddComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = new Vector2(1, 1);
            barFillRect.offsetMin = Vector2.zero;
            barFillRect.offsetMax = Vector2.zero;

            Image barFillImage = barFillGO.AddComponent<Image>();
            barFillImage.color = Color.green;

            // Battery label
            GameObject labelGO = new GameObject("BatteryLabel");
            labelGO.transform.SetParent(parent.transform, false);
            RectTransform labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(260, 16);

            TextMeshProUGUI labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = "BATT: -- %";
            labelText.fontSize = 14;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Left;
        }

        private static void WireUIReferences(GameObject canvasGO, OperationsDashboardUI dashboard)
        {
            var so = new SerializedObject(dashboard);

            SetObjRef(so, "missionTimeLabel",     canvasGO.transform.Find("HeaderPanel/MissionTimeLabel")?.GetComponent<TextMeshProUGUI>());
            SetObjRef(so, "lunarDayLabel",         canvasGO.transform.Find("HeaderPanel/LunarDayLabel")?.GetComponent<TextMeshProUGUI>());
            SetObjRef(so, "timeScaleLabel",        canvasGO.transform.Find("HeaderPanel/TimeScaleLabel")?.GetComponent<TextMeshProUGUI>());

            SetObjRef(so, "powerGenLabel",         canvasGO.transform.Find("LeftSidebar/Content/PowerGenLabel")?.GetComponent<TextMeshProUGUI>());
            SetObjRef(so, "powerConLabel",         canvasGO.transform.Find("LeftSidebar/Content/PowerConLabel")?.GetComponent<TextMeshProUGUI>());
            SetObjRef(so, "netPowerLabel",         canvasGO.transform.Find("LeftSidebar/Content/NetPowerLabel")?.GetComponent<TextMeshProUGUI>());

            SetObjRef(so, "batteryFillImage",      canvasGO.transform.Find("LeftSidebar/Content/BatteryBarBg/BatteryFillImage")?.GetComponent<Image>());
            SetObjRef(so, "batteryLabel",          canvasGO.transform.Find("LeftSidebar/Content/BatteryLabel")?.GetComponent<TextMeshProUGUI>());

            SetObjRef(so, "waterStoredLabel",      canvasGO.transform.Find("LeftSidebar/Content/WaterStoredLabel")?.GetComponent<TextMeshProUGUI>());
            SetObjRef(so, "waterExtractionLabel",  canvasGO.transform.Find("LeftSidebar/Content/WaterExtractionLabel")?.GetComponent<TextMeshProUGUI>());

            SetObjRef(so, "o2Label",               canvasGO.transform.Find("LeftSidebar/Content/O2Label")?.GetComponent<TextMeshProUGUI>());

            SetObjRef(so, "sparklineContainer",    canvasGO.transform.Find("LeftSidebar/Content/SparklineContainer")?.GetComponent<RectTransform>());

            SetObjRef(so, "alertScrollRect",       canvasGO.transform.Find("AlertPanel")?.GetComponent<ScrollRect>());
            SetObjRef(so, "alertContent",          canvasGO.transform.Find("AlertPanel/AlertContent")?.GetComponent<RectTransform>());

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(dashboard);
        }

        private static void SetObjRef(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null)
                prop.objectReferenceValue = value;
            else
                Debug.LogWarning($"[HUDCanvasSetup] Field '{fieldName}' not found on OperationsDashboardUI");
        }
    }
}


