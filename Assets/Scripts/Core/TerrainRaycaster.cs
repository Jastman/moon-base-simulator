using UnityEngine;

namespace MoonBase.Core
{
    /// <summary>
    /// Handles raycasting against Cesium terrain tiles.
    ///
    /// IMPORTANT: Cesium for Unity generates physics meshes on terrain tiles as they
    /// stream in. You MUST enable "Create Physics Meshes" on the Cesium3DTileset component
    /// for this raycaster to work. Without it, rays pass straight through the terrain.
    ///
    /// Setup: Attach to any persistent GameObject (e.g., the MoonBaseManager object).
    /// </summary>
    public class TerrainRaycaster : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        public static TerrainRaycaster Instance { get; private set; }

        // ── Inspector Settings ─────────────────────────────────────────────────
        [Header("Raycast Settings")]
        [Tooltip("Layer mask for Cesium terrain tiles. By default Cesium puts tiles on the Default layer. " +
                 "If you want, move tiles to a dedicated 'Terrain' layer and set this mask.")]
        public LayerMask terrainLayerMask = ~0; // All layers by default

        [Tooltip("Layer mask for placed modules — used to exclude them from terrain raycasts.")]
        public LayerMask moduleLayerMask;

        [Tooltip("Max raycast distance in Unity world units. 10000 is plenty for the local area.")]
        public float maxRaycastDistance = 10000f;

        [Tooltip("Max slope angle in degrees before placement is rejected. " +
                 "Steeper than this = red ghost + no placement.")]
        [Range(0f, 90f)]
        public float maxPlacementSlopeDegrees = 20f;

        [Tooltip("How far above the hit point to start a secondary 'ground snap' downward ray. " +
                 "Used to re-snap modules if terrain tiles reload at a different LOD.")]
        public float groundSnapRayLength = 500f;

        // ── State ──────────────────────────────────────────────────────────────
        private Camera mainCamera;

        // ── Result Struct ──────────────────────────────────────────────────────
        /// <summary>Result of a terrain raycast query.</summary>
        public struct TerrainHit
        {
            /// <summary>Did the ray hit terrain?</summary>
            public bool didHit;

            /// <summary>World-space position of the hit point.</summary>
            public Vector3 worldPosition;

            /// <summary>Surface normal at the hit point (pointing away from terrain).</summary>
            public Vector3 surfaceNormal;

            /// <summary>
            /// Slope angle in degrees from vertical (0 = flat, 90 = vertical cliff).
            /// Calculated as the angle between the surface normal and world-up.
            /// </summary>
            public float slopeAngleDegrees;

            /// <summary>Whether the slope is within the allowed placement threshold.</summary>
            public bool isValidForPlacement;

            /// <summary>The RaycastHit from Unity physics (for advanced use).</summary>
            public RaycastHit rawHit;
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                Debug.LogError("[TerrainRaycaster] No main camera found. Tag your camera as 'MainCamera'.");
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Casts a ray from the camera through the current mouse position and tests against terrain.
        /// Call this every frame from ModulePlacer while in placement mode.
        /// </summary>
        public TerrainHit RaycastFromMouse()
        {
            if (mainCamera == null)
                return new TerrainHit { didHit = false };

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            return RaycastFromRay(ray);
        }

        /// <summary>
        /// Casts a ray from screen position (e.g. from UI input system) and tests against terrain.
        /// </summary>
        public TerrainHit RaycastFromScreenPoint(Vector2 screenPoint)
        {
            if (mainCamera == null)
                return new TerrainHit { didHit = false };

            Ray ray = mainCamera.ScreenPointToRay(screenPoint);
            return RaycastFromRay(ray);
        }

        /// <summary>
        /// Casts an arbitrary ray against the terrain layer.
        /// </summary>
        public TerrainHit RaycastFromRay(Ray ray)
        {
            // Build a layer mask that excludes placed modules so we only hit terrain
            int combinedMask = terrainLayerMask & ~moduleLayerMask;

            if (!Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, combinedMask))
                return new TerrainHit { didHit = false };

            return BuildResult(hit);
        }

        /// <summary>
        /// Snaps a world position down to the terrain surface directly below it.
        /// Useful for re-anchoring modules after terrain tiles reload at a different LOD.
        /// </summary>
        public TerrainHit SnapToGroundBelow(Vector3 worldPosition)
        {
            // Cast a ray straight down from slightly above the position
            Vector3 rayOrigin = worldPosition + Vector3.up * groundSnapRayLength * 0.5f;
            Ray downRay = new Ray(rayOrigin, Vector3.down);

            int combinedMask = terrainLayerMask & ~moduleLayerMask;

            if (!Physics.Raycast(downRay, out RaycastHit hit, groundSnapRayLength, combinedMask))
            {
                // Terrain not loaded here yet — return a near-miss result
                return new TerrainHit { didHit = false, worldPosition = worldPosition };
            }

            return BuildResult(hit);
        }

        /// <summary>
        /// Returns true if a given world position is clear of other modules within radius.
        /// Used by ModulePlacer to check for overlapping placements.
        /// </summary>
        public bool IsAreaClearOfModules(Vector3 worldPosition, float checkRadius)
        {
            // Use an overlap sphere on the module layer to detect collisions
            Collider[] overlapping = Physics.OverlapSphere(worldPosition, checkRadius, moduleLayerMask);
            return overlapping.Length == 0;
        }

        // ── Private Helpers ────────────────────────────────────────────────────
        private TerrainHit BuildResult(RaycastHit hit)
        {
            // Surface normal from the physics hit
            Vector3 normal = hit.normal;

            // Slope angle = angle between surface normal and world up
            // On perfectly flat ground, normal == Vector3.up → slopeAngle = 0
            // On a vertical wall, normal == Vector3.forward → slopeAngle = 90
            float slopeAngle = Vector3.Angle(normal, Vector3.up);

            return new TerrainHit
            {
                didHit = true,
                worldPosition = hit.point,
                surfaceNormal = normal,
                slopeAngleDegrees = slopeAngle,
                isValidForPlacement = slopeAngle <= maxPlacementSlopeDegrees,
                rawHit = hit
            };
        }
    }
}
