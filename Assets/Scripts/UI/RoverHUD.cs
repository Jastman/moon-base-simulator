using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MoonBase.Rover;

namespace MoonBase.UI
{
    /// <summary>
    /// Rover Mode HUD — shows speed, camera mode, and driving controls hint.
    ///
    /// Setup (uGUI):
    ///   Canvas → RoverHUD (starts disabled, enabled by ModeManager on rover mode entry)
    ///   ├── SpeedDisplay (TextMeshProUGUI)  → speedText
    ///   ├── CameraViewDisplay (TextMeshProUGUI) → cameraViewText
    ///   ├── GroundedIndicator (Image, green/red) → groundedIndicator
    ///   └── ControlsHint (TextMeshProUGUI, bottom of screen)
    /// </summary>
    public class RoverHUD : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("References")]
        public RoverController roverController;
        public RoverCamera roverCamera;

        [Header("HUD Elements")]
        public TextMeshProUGUI speedText;
        public TextMeshProUGUI cameraViewText;
        public Image groundedIndicator;
        public TextMeshProUGUI controlsHintText;

        [Header("Colors")]
        public Color groundedColor = new Color(0.2f, 0.9f, 0.3f);
        public Color airborneColor = new Color(0.9f, 0.4f, 0.1f);

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Start()
        {
            if (controlsHintText != null)
            {
                controlsHintText.text =
                    "W/S — Throttle / Reverse    A/D — Steer    Space — Brake\n" +
                    "V — Toggle Camera    Tab — Return to Design Mode";
            }
        }

        private void Update()
        {
            UpdateSpeedDisplay();
            UpdateCameraDisplay();
            UpdateGroundedIndicator();
        }

        // ── Update Methods ─────────────────────────────────────────────────────
        private void UpdateSpeedDisplay()
        {
            if (speedText == null || roverController == null) return;
            speedText.text = $"{roverController.SpeedKph:F1} km/h";
        }

        private void UpdateCameraDisplay()
        {
            if (cameraViewText == null || roverCamera == null) return;
            cameraViewText.text = roverCamera.CurrentView == CameraViewMode.ThirdPerson
                ? "3rd Person [V]"
                : "1st Person [V]";
        }

        private void UpdateGroundedIndicator()
        {
            if (groundedIndicator == null || roverController == null) return;
            groundedIndicator.color = roverController.IsGrounded ? groundedColor : airborneColor;
        }
    }
}
