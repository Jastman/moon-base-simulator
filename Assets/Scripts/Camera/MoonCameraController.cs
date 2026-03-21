using UnityEngine;
using MoonBase.Core;

namespace MoonBase.CameraSystem
{
    /// <summary>
    /// Design Mode camera controller — isometric-style orbital camera.
    ///
    /// Controls:
    ///   - Right mouse drag    → orbit (rotate around focal point)
    ///   - Middle mouse drag   → pan (move focal point)
    ///   - Scroll wheel        → zoom in/out
    ///   - F key               → frame all placed modules
    ///   - Home key            → reset to default view
    ///
    /// The camera always looks at a focal point on the terrain.
    /// Distance from focal point is controlled by zoom.
    ///
    /// Setup: Attach to the Main Camera. Works alongside ModeManager — this script
    /// is enabled in Design Mode and disabled in Rover Mode.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MoonCameraController : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Orbit Settings")]
        [Tooltip("Mouse sensitivity for orbit rotation.")]
        public float orbitSensitivity = 0.4f;

        [Tooltip("Mouse sensitivity for panning.")]
        public float panSensitivity = 0.05f;

        [Tooltip("How fast the camera zooms in/out.")]
        public float zoomSpeed = 3f;

        [Header("Distance Limits")]
        [Tooltip("Minimum zoom distance from focal point (meters).")]
        public float minZoomDistance = 5f;

        [Tooltip("Maximum zoom distance from focal point (meters).")]
        public float maxZoomDistance = 2000f;

        [Header("Starting View")]
        [Tooltip("Starting pitch angle (degrees from horizontal). 45 = classic isometric-ish.")]
        [Range(10f, 89f)]
        public float initialPitch = 55f;

        [Tooltip("Starting yaw angle.")]
        public float initialYaw = 45f;

        [Tooltip("Starting zoom distance from focal point (meters).")]
        public float initialZoomDistance = 200f;

        [Header("Smoothing")]
        [Tooltip("Smoothing factor for orbit/zoom movement. Lower = snappier.")]
        [Range(1f, 20f)]
        public float smoothing = 8f;

        [Tooltip("Invert vertical orbit direction.")]
        public bool invertPitch = false;

        // ── Private State ──────────────────────────────────────────────────────
        private Vector3 focalPoint;
        private float currentYaw;
        private float currentPitch;
        private float currentZoomDistance;

        private float targetYaw;
        private float targetPitch;
        private float targetZoomDistance;
        private Vector3 targetFocalPoint;

        private bool isDraggingOrbit;
        private bool isDraggingPan;
        private Vector3 lastMousePos;

        private Camera cam;

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            // Initialize to default view
            ResetToDefaultView();
        }

        private void LateUpdate()
        {
            // LateUpdate so camera moves after physics + module snap
            HandleInput();
            SmoothApply();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Reset camera to the default starting view.</summary>
        public void ResetToDefaultView()
        {
            focalPoint = Vector3.zero;
            targetFocalPoint = Vector3.zero;
            targetYaw = initialYaw;
            targetPitch = initialPitch;
            targetZoomDistance = initialZoomDistance;
            currentYaw = targetYaw;
            currentPitch = targetPitch;
            currentZoomDistance = targetZoomDistance;
            ApplyTransform();
        }

        /// <summary>Set a new focal point to look at (e.g., frame a newly placed module).</summary>
        public void LookAt(Vector3 worldPosition)
        {
            targetFocalPoint = worldPosition;
        }

        /// <summary>
        /// Frame all placed modules in view. Called with F key.
        /// </summary>
        public void FrameAllModules()
        {
            var manager = MoonBaseManager.Instance;
            if (manager == null || manager.PlacedModules.Count == 0) return;

            // Calculate bounds of all placed modules
            Bounds bounds = new Bounds(manager.PlacedModules[0].transform.position, Vector3.zero);
            foreach (var module in manager.PlacedModules)
            {
                if (module != null)
                    bounds.Encapsulate(module.transform.position);
            }

            targetFocalPoint = bounds.center;

            // Set zoom distance to frame the bounds with some padding
            float boundsRadius = bounds.extents.magnitude;
            targetZoomDistance = Mathf.Clamp(boundsRadius * 2.5f, minZoomDistance, maxZoomDistance);
        }

        // ── Input ──────────────────────────────────────────────────────────────
        private void HandleInput()
        {
            // ── Orbit (Right Mouse) ────────────────────────────────────────────
            if (Input.GetMouseButtonDown(1))
            {
                isDraggingOrbit = true;
                lastMousePos = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(1))
                isDraggingOrbit = false;

            if (isDraggingOrbit)
            {
                Vector3 delta = Input.mousePosition - lastMousePos;
                lastMousePos = Input.mousePosition;

                targetYaw += delta.x * orbitSensitivity;
                float pitchDelta = delta.y * orbitSensitivity * (invertPitch ? 1f : -1f);
                targetPitch = Mathf.Clamp(targetPitch + pitchDelta, 10f, 89f);
            }

            // ── Pan (Middle Mouse) ─────────────────────────────────────────────
            if (Input.GetMouseButtonDown(2))
            {
                isDraggingPan = true;
                lastMousePos = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(2))
                isDraggingPan = false;

            if (isDraggingPan)
            {
                Vector3 delta = Input.mousePosition - lastMousePos;
                lastMousePos = Input.mousePosition;

                // Pan in camera-relative space, scaled by zoom distance
                float panScale = currentZoomDistance * panSensitivity * 0.1f;
                Vector3 right = transform.right;
                Vector3 up = Vector3.Cross(right, (targetFocalPoint - transform.position).normalized);

                targetFocalPoint -= right * delta.x * panScale;
                targetFocalPoint -= up * delta.y * panScale;
            }

            // ── Zoom (Scroll Wheel) ────────────────────────────────────────────
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                float zoomDelta = -scroll * zoomSpeed * currentZoomDistance * 0.3f;
                targetZoomDistance = Mathf.Clamp(targetZoomDistance + zoomDelta,
                                                  minZoomDistance, maxZoomDistance);
            }

            // ── Hotkeys ────────────────────────────────────────────────────────
            if (Input.GetKeyDown(KeyCode.F))
                FrameAllModules();

            if (Input.GetKeyDown(KeyCode.Home))
                ResetToDefaultView();
        }

        // ── Smooth Apply ───────────────────────────────────────────────────────
        private void SmoothApply()
        {
            float t = Time.deltaTime * smoothing;
            currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, t);
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, t);
            currentZoomDistance = Mathf.Lerp(currentZoomDistance, targetZoomDistance, t);
            focalPoint = Vector3.Lerp(focalPoint, targetFocalPoint, t);

            ApplyTransform();
        }

        private void ApplyTransform()
        {
            // Calculate camera position from spherical coordinates around focal point
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -currentZoomDistance);
            transform.position = focalPoint + offset;
            transform.LookAt(focalPoint, Vector3.up);
        }
    }
}
