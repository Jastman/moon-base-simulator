using UnityEngine;

namespace MoonBase.Rover
{
    /// <summary>
    /// Lunar rover vehicle controller using Unity WheelColliders.
    ///
    /// Physics notes:
    ///   - Lunar gravity = 1.62 m/s² (set globally in ModeManager via Physics.gravity)
    ///   - Low gravity = much longer air time over bumps, floatier feel
    ///   - Moon has no air drag — set Rigidbody drag to ~0.02 (just a tiny bit for stability)
    ///   - Wheels use WheelColliders which handle suspension, friction, and rolling contact
    ///
    /// Prefab setup (important!):
    ///   Rover (Rigidbody, RoverController)
    ///   ├── Body (visual mesh)
    ///   ├── WheelColliders (empty parent)
    ///   │   ├── WheelCollider_FL  (WheelCollider component)
    ///   │   ├── WheelCollider_FR
    ///   │   ├── WheelCollider_RL
    ///   │   └── WheelCollider_RR
    ///   └── WheelMeshes (empty parent)
    ///       ├── WheelMesh_FL  (visual wheel mesh, no physics)
    ///       ├── WheelMesh_FR
    ///       ├── WheelMesh_RL
    ///       └── WheelMesh_RR
    ///
    /// WheelCollider settings (set in Inspector, these are starting points):
    ///   - Mass: 20 (per wheel)
    ///   - Radius: 0.4m (adjust to your mesh)
    ///   - Suspension Distance: 0.3m
    ///   - Spring: 8000, Damper: 800
    ///   - Forward Friction Stiffness: 1.5
    ///   - Sideways Friction Stiffness: 2.0
    ///
    /// Input: WASD or arrow keys to drive, Space for handbrake.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RoverController : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Wheel Colliders")]
        public WheelCollider wheelFL;
        public WheelCollider wheelFR;
        public WheelCollider wheelRL;
        public WheelCollider wheelRR;

        [Header("Wheel Visual Meshes")]
        [Tooltip("Visual meshes that rotate/turn to match the WheelColliders. No physics on these.")]
        public Transform wheelMeshFL;
        public Transform wheelMeshFR;
        public Transform wheelMeshRL;
        public Transform wheelMeshRR;

        [Header("Drive Settings")]
        [Tooltip("Max motor torque in Newton-meters per driven wheel.")]
        public float motorTorque = 800f;

        [Tooltip("Max steering angle in degrees.")]
        [Range(10f, 50f)]
        public float maxSteeringAngle = 30f;

        [Tooltip("Braking torque applied when braking or handbrake is held.")]
        public float brakeTorque = 3000f;

        [Tooltip("Torque applied to non-driven wheels when coasting (rolling resistance simulation). " +
                 "Small value keeps the rover from rolling forever on flat terrain.")]
        public float rollingResistanceTorque = 50f;

        [Header("Speed Limits")]
        [Tooltip("Max forward speed in m/s (~18 km/h at default). Realistic LRV max was ~18 km/h.")]
        public float maxSpeedMps = 5f;

        [Tooltip("Max reverse speed in m/s.")]
        public float maxReverseSpeedMps = 2f;

        [Header("Suspension (Lunar)")]
        [Tooltip("Center of mass Y offset. Lower = more stable. " +
                 "Low gravity means the rover can tip more easily — lower the CoM.")]
        public float centerOfMassYOffset = -0.3f;

        [Header("Anti-Roll")]
        [Tooltip("Anti-roll bar stiffness. Prevents the rover from tipping in low gravity. " +
                 "Higher = stiffer. ~3000 is a good starting point.")]
        public float antiRollStiffness = 3000f;

        // ── Properties ─────────────────────────────────────────────────────────
        /// <summary>Current speed in m/s (always positive).</summary>
        public float SpeedMps => Mathf.Abs(rb.linearVelocity.magnitude);

        /// <summary>Current speed in km/h.</summary>
        public float SpeedKph => SpeedMps * 3.6f;

        /// <summary>True if the rover is touching the ground on at least one wheel.</summary>
        public bool IsGrounded => wheelFL.isGrounded || wheelFR.isGrounded ||
                                  wheelRL.isGrounded || wheelRR.isGrounded;

        // ── Private ────────────────────────────────────────────────────────────
        private Rigidbody rb;
        private float throttleInput;
        private float steerInput;
        private bool brakeInput;

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            ConfigureRigidbody();
            ValidateWheelColliders();
        }

        private void ConfigureRigidbody()
        {
            // Low lunar gravity is set globally by ModeManager (Physics.gravity = -1.62)
            // Rover-specific rigidbody settings for lunar feel:
            rb.mass = 500f;       // Rough mass of rover + crew in kg
            rb.linearDamping = 0.02f;   // Near zero — no atmosphere
            rb.angularDamping = 0.5f;  // Small angular damping prevents endless spinning

            // Lower center of mass prevents tipping in low-G
            rb.centerOfMass = new Vector3(0f, centerOfMassYOffset, 0f);

            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        private void Update()
        {
            // Read input in Update (more responsive)
            throttleInput = Input.GetAxis("Vertical");   // W/S or Up/Down
            steerInput = Input.GetAxis("Horizontal");     // A/D or Left/Right
            brakeInput = Input.GetKey(KeyCode.Space);
        }

        private void FixedUpdate()
        {
            ApplyDrive();
            ApplySteering();
            ApplyBraking();
            ApplyAntiRoll();
            UpdateWheelMeshes();
        }

        // ── Drive ──────────────────────────────────────────────────────────────
        private void ApplyDrive()
        {
            // Speed limit check
            float currentSpeed = rb.linearVelocity.magnitude;
            bool overSpeedForward = throttleInput > 0 && currentSpeed > maxSpeedMps;
            bool overSpeedReverse = throttleInput < 0 && currentSpeed > maxReverseSpeedMps;

            float torque = (overSpeedForward || overSpeedReverse) ? 0f
                         : throttleInput * motorTorque;

            // 4-wheel drive
            wheelFL.motorTorque = torque;
            wheelFR.motorTorque = torque;
            wheelRL.motorTorque = torque;
            wheelRR.motorTorque = torque;

            // Apply rolling resistance when no throttle input
            if (Mathf.Abs(throttleInput) < 0.05f && !brakeInput)
            {
                wheelFL.brakeTorque = rollingResistanceTorque;
                wheelFR.brakeTorque = rollingResistanceTorque;
                wheelRL.brakeTorque = rollingResistanceTorque;
                wheelRR.brakeTorque = rollingResistanceTorque;
            }
        }

        private void ApplySteering()
        {
            // Ackermann steering approximation: front wheels steer, rear wheels fixed
            // Left wheel steers less than right wheel on a right turn (and vice versa)
            float wheelBase = 2.0f;   // Distance front-to-rear axle (meters) — adjust to your prefab
            float trackWidth = 1.5f;  // Distance left-to-right wheel — adjust to your prefab

            float steerAngle = steerInput * maxSteeringAngle;

            if (Mathf.Abs(steerAngle) > 0.1f)
            {
                // Approximate Ackermann: inner wheel turns sharper than outer
                float turnRadius = wheelBase / Mathf.Tan(steerAngle * Mathf.Deg2Rad);
                float innerAngle = Mathf.Atan(wheelBase / (turnRadius - trackWidth * 0.5f)) * Mathf.Rad2Deg;
                float outerAngle = Mathf.Atan(wheelBase / (turnRadius + trackWidth * 0.5f)) * Mathf.Rad2Deg;

                if (steerInput > 0) // Turning right
                {
                    wheelFR.steerAngle = innerAngle;
                    wheelFL.steerAngle = outerAngle;
                }
                else // Turning left
                {
                    wheelFL.steerAngle = -innerAngle;
                    wheelFR.steerAngle = -outerAngle;
                }
            }
            else
            {
                wheelFL.steerAngle = 0f;
                wheelFR.steerAngle = 0f;
            }
        }

        private void ApplyBraking()
        {
            if (brakeInput)
            {
                wheelFL.brakeTorque = brakeTorque;
                wheelFR.brakeTorque = brakeTorque;
                wheelRL.brakeTorque = brakeTorque;
                wheelRR.brakeTorque = brakeTorque;
            }
            else if (Mathf.Abs(throttleInput) > 0.05f)
            {
                // Clear brakes when accelerating
                wheelFL.brakeTorque = 0f;
                wheelFR.brakeTorque = 0f;
                wheelRL.brakeTorque = 0f;
                wheelRR.brakeTorque = 0f;
            }
        }

        // ── Anti-Roll ──────────────────────────────────────────────────────────
        /// <summary>
        /// Anti-roll bar simulation. Counteracts the low-gravity tipping tendency by
        /// applying corrective torques based on suspension travel difference.
        /// </summary>
        private void ApplyAntiRoll()
        {
            ApplyAntiRollForAxle(wheelFL, wheelFR);
            ApplyAntiRollForAxle(wheelRL, wheelRR);
        }

        private void ApplyAntiRollForAxle(WheelCollider leftWheel, WheelCollider rightWheel)
        {
            WheelHit hitL, hitR;
            float travelL = 1.0f;
            float travelR = 1.0f;

            bool groundedL = leftWheel.GetGroundHit(out hitL);
            bool groundedR = rightWheel.GetGroundHit(out hitR);

            if (groundedL)
                travelL = (-leftWheel.transform.InverseTransformPoint(hitL.point).y
                           - leftWheel.radius) / leftWheel.suspensionDistance;

            if (groundedR)
                travelR = (-rightWheel.transform.InverseTransformPoint(hitR.point).y
                           - rightWheel.radius) / rightWheel.suspensionDistance;

            float antiRollForce = (travelL - travelR) * antiRollStiffness;

            if (groundedL)
                rb.AddForceAtPosition(leftWheel.transform.up * -antiRollForce,
                                      leftWheel.transform.position);
            if (groundedR)
                rb.AddForceAtPosition(rightWheel.transform.up * antiRollForce,
                                      rightWheel.transform.position);
        }

        // ── Wheel Mesh Updates ─────────────────────────────────────────────────
        private void UpdateWheelMeshes()
        {
            UpdateWheelMesh(wheelFL, wheelMeshFL);
            UpdateWheelMesh(wheelFR, wheelMeshFR);
            UpdateWheelMesh(wheelRL, wheelMeshRL);
            UpdateWheelMesh(wheelRR, wheelMeshRR);
        }

        private void UpdateWheelMesh(WheelCollider collider, Transform mesh)
        {
            if (collider == null || mesh == null) return;
            collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
            mesh.position = pos;
            mesh.rotation = rot;
        }

        // ── Validation ─────────────────────────────────────────────────────────
        private void ValidateWheelColliders()
        {
            bool ok = true;
            if (wheelFL == null) { Debug.LogError("[RoverController] wheelFL not assigned"); ok = false; }
            if (wheelFR == null) { Debug.LogError("[RoverController] wheelFR not assigned"); ok = false; }
            if (wheelRL == null) { Debug.LogError("[RoverController] wheelRL not assigned"); ok = false; }
            if (wheelRR == null) { Debug.LogError("[RoverController] wheelRR not assigned"); ok = false; }
            if (!ok) enabled = false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Visualize center of mass
            Gizmos.color = Color.red;
            if (rb != null)
                Gizmos.DrawSphere(transform.TransformPoint(rb.centerOfMass), 0.1f);
        }
#endif
    }
}
