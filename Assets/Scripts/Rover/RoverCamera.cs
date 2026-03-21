using UnityEngine;

namespace MoonBase.Rover
{
    /// <summary>
    /// Handles rover camera — toggles between third-person (orbit behind rover)
    /// and first-person (cockpit/hood camera) views.
    ///
    /// Toggle key: V (configurable)
    ///
    /// Third-person: Camera orbits behind the rover, following at a fixed distance.
    ///               Mouse look rotates the orbit. The rover steers independently.
    ///
    /// First-person: Camera locked to a mount point on the rover (e.g., driver's POV
    ///               or a front-facing camera). Mouse look rotates the camera within
    ///               a limited range (not full 360° — realistic head movement).
    ///
    /// Setup:
    ///   - Attach to the camera that should be active in Rover Mode.
    ///   - Assign roverTransform (the rover root).
    ///   - Create an empty child on the rover for firstPersonMount (driver seat position).
    ///   - This camera must be a child of the scene root, NOT the rover, so it can
    ///     smoothly follow without inheriting rover jitter directly.
    ///     (Smooth follow is applied via Lerp in LateUpdate.)
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class RoverCamera : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("References")]
        [Tooltip("The rover's root transform to follow.")]
        public Transform roverTransform;

        [Tooltip("The first-person camera mount point (child of rover, e.g. at driver's head position).")]
        public Transform firstPersonMount;

        [Header("Third Person Settings")]
        [Tooltip("Distance behind rover in third-person view (meters).")]
        public float thirdPersonDistance = 8f;

        [Tooltip("Height above rover in third-person view (meters).")]
        public float thirdPersonHeight = 3f;

        [Tooltip("How fast the third-person camera follows the rover rotation. Lower = lazier.")]
        [Range(1f, 20f)]
        public float followSmoothness = 6f;

        [Tooltip("Mouse look sensitivity in third-person mode.")]
        public float thirdPersonMouseSensitivity = 2f;

        [Header("First Person Settings")]
        [Tooltip("Mouse look sensitivity in first-person mode.")]
        public float firstPersonMouseSensitivity = 1.5f;

        [Tooltip("Max vertical look angle in first-person (degrees up/down from forward).")]
        [Range(20f, 90f)]
        public float firstPersonVerticalLimit = 60f;

        [Tooltip("Max horizontal look angle in first-person. 180 = free look. " +
                 "80 = realistic head-turn limit.")]
        [Range(30f, 180f)]
        public float firstPersonHorizontalLimit = 80f;

        [Header("Controls")]
        [Tooltip("Key to toggle between third and first person.")]
        public KeyCode toggleCameraKey = KeyCode.V;

        // ── Properties ─────────────────────────────────────────────────────────
        public CameraViewMode CurrentView { get; private set; } = CameraViewMode.ThirdPerson;

        // ── Private ────────────────────────────────────────────────────────────
        private Camera cam;

        // Third-person state
        private float thirdPersonYaw;
        private float thirdPersonPitch = 15f;

        // First-person state
        private float firstPersonYaw;   // Relative to rover's forward
        private float firstPersonPitch;

        // Smooth follow
        private Vector3 smoothFollowPosition;
        private Quaternion smoothFollowRotation;

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            if (roverTransform == null)
            {
                Debug.LogError("[RoverCamera] roverTransform not assigned! Assign the rover root in Inspector.");
                enabled = false;
                return;
            }

            // Initialize third-person yaw to match rover's current facing
            thirdPersonYaw = roverTransform.eulerAngles.y;

            smoothFollowPosition = transform.position;
            smoothFollowRotation = transform.rotation;
        }

        private void Update()
        {
            // Toggle view
            if (Input.GetKeyDown(toggleCameraKey))
                ToggleCameraView();
        }

        private void LateUpdate()
        {
            if (roverTransform == null) return;

            if (CurrentView == CameraViewMode.ThirdPerson)
                UpdateThirdPerson();
            else
                UpdateFirstPerson();
        }

        // ── Public API ─────────────────────────────────────────────────────────
        public void ToggleCameraView()
        {
            CurrentView = CurrentView == CameraViewMode.ThirdPerson
                ? CameraViewMode.FirstPerson
                : CameraViewMode.ThirdPerson;

            // Reset first-person look when entering it
            if (CurrentView == CameraViewMode.FirstPerson)
            {
                firstPersonYaw = 0f;
                firstPersonPitch = 0f;
            }

            Debug.Log($"[RoverCamera] Camera view → {CurrentView}");
        }

        public void SetView(CameraViewMode view)
        {
            CurrentView = view;
        }

        // ── Third Person ───────────────────────────────────────────────────────
        private void UpdateThirdPerson()
        {
            // Mouse input for orbit (only when cursor is locked)
            float mouseX = Input.GetAxis("Mouse X") * thirdPersonMouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * thirdPersonMouseSensitivity;

            thirdPersonYaw += mouseX;
            thirdPersonPitch = Mathf.Clamp(thirdPersonPitch - mouseY, -10f, 60f);

            // Calculate desired position
            Quaternion orbitRotation = Quaternion.Euler(thirdPersonPitch, thirdPersonYaw, 0f);
            Vector3 offset = orbitRotation * new Vector3(0f, 0f, -thirdPersonDistance);
            Vector3 desiredPos = roverTransform.position
                                 + Vector3.up * thirdPersonHeight
                                 + offset;

            // Collision check — push camera forward if terrain is in the way
            Vector3 dirToCamera = desiredPos - roverTransform.position;
            if (Physics.SphereCast(roverTransform.position + Vector3.up * thirdPersonHeight,
                                   0.3f, dirToCamera.normalized,
                                   out RaycastHit hit, dirToCamera.magnitude))
            {
                desiredPos = hit.point - dirToCamera.normalized * 0.3f;
            }

            // Smooth follow
            float t = Time.deltaTime * followSmoothness;
            transform.position = Vector3.Lerp(transform.position, desiredPos, t);
            transform.LookAt(roverTransform.position + Vector3.up * 1.5f, Vector3.up);
        }

        // ── First Person ───────────────────────────────────────────────────────
        private void UpdateFirstPerson()
        {
            float mouseX = Input.GetAxis("Mouse X") * firstPersonMouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * firstPersonMouseSensitivity;

            firstPersonYaw += mouseX;
            firstPersonPitch -= mouseY;

            // Clamp to realistic head movement limits
            firstPersonYaw = Mathf.Clamp(firstPersonYaw, -firstPersonHorizontalLimit, firstPersonHorizontalLimit);
            firstPersonPitch = Mathf.Clamp(firstPersonPitch, -firstPersonVerticalLimit, firstPersonVerticalLimit);

            // Position at mount point
            Vector3 mountPos = firstPersonMount != null
                ? firstPersonMount.position
                : roverTransform.position + roverTransform.up * 1.8f;

            transform.position = mountPos;

            // Rotation: rover's facing + local head rotation
            Quaternion baseRotation = Quaternion.Euler(0f, roverTransform.eulerAngles.y, 0f);
            Quaternion lookRotation = Quaternion.Euler(firstPersonPitch, firstPersonYaw, 0f);
            transform.rotation = baseRotation * lookRotation;
        }
    }

    public enum CameraViewMode
    {
        ThirdPerson,
        FirstPerson
    }
}
