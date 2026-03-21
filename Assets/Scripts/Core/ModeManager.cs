using UnityEngine;
using MoonBase.CameraSystem;
using MoonBase.Modules;
using MoonBase.Rover;

namespace MoonBase.Core
{
    /// <summary>
    /// Manages switching between the two main simulator modes:
    ///
    ///   DESIGN MODE  — Isometric orbital camera, drag-and-drop module placement,
    ///                  data layer overlays, resource panel visible.
    ///
    ///   ROVER MODE   — First/third-person rover on the lunar surface,
    ///                  WheelCollider physics with lunar gravity (1.62 m/s²),
    ///                  placement UI hidden, rover HUD visible.
    ///
    /// The tab switcher UI calls SwitchToDesignMode() / SwitchToRoverMode().
    ///
    /// Setup:
    ///   - Attach to a persistent GameObject.
    ///   - Assign all references in Inspector.
    ///   - The rover GameObject should start disabled (ModeManager enables it on switch).
    ///   - The design camera should start enabled; rover camera starts disabled.
    /// </summary>
    public class ModeManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        public static ModeManager Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Mode Objects")]
        [Tooltip("The Main Camera with MoonCameraController (Design Mode camera).")]
        public MoonCameraController designCamera;

        [Tooltip("The Rover GameObject (has RoverController + RoverCamera). Start disabled.")]
        public GameObject roverGameObject;

        [Tooltip("The RoverCamera component on or near the rover.")]
        public RoverCamera roverCamera;

        [Header("UI Panels")]
        [Tooltip("The module selector toolbar and data layer panel (Design Mode UI).")]
        public GameObject designModeUI;

        [Tooltip("Rover HUD (speed, telemetry). Shown only in Rover Mode.")]
        public GameObject roverHUD;

        [Header("Rover Spawn")]
        [Tooltip("Where to spawn/teleport the rover when entering Rover Mode for the first time. " +
                 "If null, places at world origin + slight offset above terrain.")]
        public Transform roverSpawnPoint;

        [Header("Transition")]
        [Tooltip("Show a brief fade-to-black when switching modes.")]
        public bool fadeBetweenModes = true;

        // ── Properties ─────────────────────────────────────────────────────────
        public SimulatorMode CurrentMode { get; private set; } = SimulatorMode.Design;
        public bool IsDesignMode => CurrentMode == SimulatorMode.Design;
        public bool IsRoverMode => CurrentMode == SimulatorMode.Rover;

        // ── Events ─────────────────────────────────────────────────────────────
        public System.Action<SimulatorMode> OnModeChanged;

        // ── Private ────────────────────────────────────────────────────────────
        private bool hasSpawnedRover = false;

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // Start in Design Mode
            ApplyDesignMode(instant: true);
        }

        private void Update()
        {
            // Tab key as quick shortcut to switch modes (can be overridden by UI tabs)
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (IsDesignMode) SwitchToRoverMode();
                else SwitchToDesignMode();
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────

        public void SwitchToDesignMode()
        {
            if (IsDesignMode) return;
            CurrentMode = SimulatorMode.Design;

            // Cancel any active placement before switching
            if (ModulePlacer.Instance != null && ModulePlacer.Instance.IsInPlacementMode)
                ModulePlacer.Instance.CancelPlacement();

            ApplyDesignMode(instant: false);
            OnModeChanged?.Invoke(SimulatorMode.Design);
            Debug.Log("[ModeManager] → Design Mode");
        }

        public void SwitchToRoverMode()
        {
            if (IsRoverMode) return;
            CurrentMode = SimulatorMode.Rover;

            ApplyRoverMode(instant: false);
            OnModeChanged?.Invoke(SimulatorMode.Rover);
            Debug.Log("[ModeManager] → Rover Mode");
        }

        // ── Mode Application ───────────────────────────────────────────────────
        private void ApplyDesignMode(bool instant)
        {
            // Enable design camera
            if (designCamera != null)
            {
                designCamera.gameObject.SetActive(true);
                designCamera.enabled = true;
            }

            // Disable rover
            if (roverGameObject != null)
                roverGameObject.SetActive(false);

            if (roverCamera != null)
                roverCamera.enabled = false;

            // Swap UI
            if (designModeUI != null) designModeUI.SetActive(true);
            if (roverHUD != null) roverHUD.SetActive(false);

            // Restore physics gravity to Moon value (rover mode may have changed it)
            Physics.gravity = new Vector3(0f, -1.62f, 0f);

            // Cursor visible in design mode
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ApplyRoverMode(bool instant)
        {
            // Spawn rover if first time, otherwise just re-enable
            if (!hasSpawnedRover)
            {
                SpawnRover();
                hasSpawnedRover = true;
            }
            else
            {
                if (roverGameObject != null)
                    roverGameObject.SetActive(true);
            }

            // Enable rover camera, disable design camera
            if (roverCamera != null)
                roverCamera.enabled = true;

            if (designCamera != null)
                designCamera.enabled = false;

            // Swap UI
            if (designModeUI != null) designModeUI.SetActive(false);
            if (roverHUD != null) roverHUD.SetActive(true);

            // Make sure gravity is set to lunar
            Physics.gravity = new Vector3(0f, -1.62f, 0f);

            // Lock cursor for rover driving
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void SpawnRover()
        {
            if (roverGameObject == null)
            {
                Debug.LogError("[ModeManager] roverGameObject is not assigned! Assign the Rover prefab/GO in Inspector.");
                return;
            }

            roverGameObject.SetActive(true);

            if (roverSpawnPoint != null)
            {
                roverGameObject.transform.position = roverSpawnPoint.position;
                roverGameObject.transform.rotation = roverSpawnPoint.rotation;
            }
            else
            {
                // Try to snap rover to terrain below design camera focal point
                if (TerrainRaycaster.Instance != null && designCamera != null)
                {
                    var hit = TerrainRaycaster.Instance.SnapToGroundBelow(
                        designCamera.transform.position);

                    if (hit.didHit)
                    {
                        roverGameObject.transform.position = hit.worldPosition + Vector3.up * 1.5f;
                    }
                    else
                    {
                        roverGameObject.transform.position = Vector3.up * 5f;
                    }
                }
                else
                {
                    roverGameObject.transform.position = Vector3.up * 5f;
                }
            }

            Debug.Log($"[ModeManager] Rover spawned at {roverGameObject.transform.position}");
        }
    }

    public enum SimulatorMode
    {
        Design,
        Rover
    }
}
