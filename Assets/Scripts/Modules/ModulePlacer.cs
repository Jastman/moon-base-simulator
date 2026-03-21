using System.Collections.Generic;
using UnityEngine;
using MoonBase.Core;

namespace MoonBase.Modules
{
    /// <summary>
    /// Manages the module placement workflow:
    ///   1. User selects a module type from the UI toolbar
    ///   2. A ghost preview follows the mouse over terrain
    ///   3. Valid placement = green ghost; invalid (too steep / overlapping) = red ghost
    ///   4. Left click confirms placement; Escape cancels
    ///   5. Adjacent modules can snap to each other's grid
    ///
    /// Setup: Attach to a persistent GameObject. Assign references in Inspector.
    /// ModulePlacer needs TerrainRaycaster and MoonBaseManager present in the scene.
    /// </summary>
    public class ModulePlacer : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        public static ModulePlacer Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("References")]
        public TerrainRaycaster terrainRaycaster;
        public MoonBaseManager moonBaseManager;

        [Header("Ghost Preview Materials")]
        [Tooltip("Material applied to the ghost when placement is valid (green-ish transparent).")]
        public Material ghostValidMaterial;

        [Tooltip("Material applied to the ghost when placement is invalid (red-ish transparent).")]
        public Material ghostInvalidMaterial;

        [Header("Placement Settings")]
        [Tooltip("How close the cursor must be to an existing module edge to trigger snap (meters).")]
        public float snapDetectionRadius = 8f;

        [Tooltip("Layer mask for placed module colliders (for overlap detection and snap detection).")]
        public LayerMask modulesLayer;

        // ── Properties ─────────────────────────────────────────────────────────
        public bool IsInPlacementMode => currentDefinition != null;
        public ModuleDefinition CurrentDefinition => currentDefinition;

        // ── Events ─────────────────────────────────────────────────────────────
        public System.Action<BaseModule> OnModulePlaced;
        public System.Action OnPlacementCancelled;
        public System.Action<BaseModule> OnModuleSelected;
        public System.Action OnModuleDeselected;

        // ── Private State ──────────────────────────────────────────────────────
        private ModuleDefinition currentDefinition;
        private GameObject ghostInstance;
        private Renderer[] ghostRenderers;
        private bool lastPlacementValid;
        private BaseModule selectedModule;

        // For local grid snapping: reference point of the first placed module
        private Vector3? localGridOrigin;
        private Vector3 localGridRight;
        private Vector3 localGridForward;

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // Auto-resolve references if not set in Inspector
            if (terrainRaycaster == null) terrainRaycaster = TerrainRaycaster.Instance;
            if (moonBaseManager == null) moonBaseManager = MoonBaseManager.Instance;

            if (terrainRaycaster == null)
                Debug.LogError("[ModulePlacer] TerrainRaycaster not found.");
            if (moonBaseManager == null)
                Debug.LogError("[ModulePlacer] MoonBaseManager not found.");
        }

        private void Update()
        {
            if (IsInPlacementMode)
            {
                HandlePlacementMode();
            }
            else
            {
                HandleSelectionMode();
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Called by ModuleSelectorUI when the user clicks a module in the toolbar.
        /// Enters placement mode with the given definition.
        /// </summary>
        public void BeginPlacement(ModuleDefinition definition)
        {
            if (definition == null) return;

            // Cancel any existing placement
            if (IsInPlacementMode) CancelPlacement();

            // Deselect any selected module
            DeselectCurrentModule();

            currentDefinition = definition;
            SpawnGhost(definition);

            Debug.Log($"[ModulePlacer] Began placement mode: {definition.moduleName}");
        }

        /// <summary>Cancel placement without placing anything.</summary>
        public void CancelPlacement()
        {
            DestroyGhost();
            currentDefinition = null;
            OnPlacementCancelled?.Invoke();
        }

        /// <summary>Programmatically delete the selected module.</summary>
        public void DeleteSelectedModule()
        {
            if (selectedModule == null) return;
            var toDelete = selectedModule;
            DeselectCurrentModule();
            Destroy(toDelete.gameObject);
        }

        // ── Placement Mode ─────────────────────────────────────────────────────
        private void HandlePlacementMode()
        {
            // Cancel on Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacement();
                return;
            }

            // Get terrain hit under cursor
            var hit = terrainRaycaster.RaycastFromMouse();

            if (!hit.didHit)
            {
                SetGhostVisibility(false);
                return;
            }

            SetGhostVisibility(true);

            // Calculate placement position (with optional snap)
            Vector3 placementPos = CalculatePlacementPosition(hit.worldPosition, hit.surfaceNormal);

            // Update ghost position and rotation
            UpdateGhost(placementPos, hit.surfaceNormal);

            // Validate placement
            bool valid = ValidatePlacement(placementPos, hit, currentDefinition);

            // Update ghost material
            if (valid != lastPlacementValid)
            {
                ApplyGhostMaterial(valid ? ghostValidMaterial : ghostInvalidMaterial);
                lastPlacementValid = valid;
            }

            // Confirm placement on left click (only if valid and not over UI)
            if (valid && Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            {
                ConfirmPlacement(placementPos, hit.surfaceNormal);
            }
        }

        private void ConfirmPlacement(Vector3 position, Vector3 surfaceNormal)
        {
            if (currentDefinition?.prefab == null)
            {
                Debug.LogError($"[ModulePlacer] Module definition '{currentDefinition?.moduleName}' " +
                               "has no prefab assigned!");
                return;
            }

            // Instantiate the real module
            GameObject moduleGO = Instantiate(currentDefinition.prefab,
                                               position,
                                               Quaternion.identity,
                                               moonBaseManager?.placedModulesRoot);

            var module = moduleGO.GetComponent<BaseModule>();
            if (module == null)
            {
                Debug.LogError($"[ModulePlacer] Prefab '{currentDefinition.prefab.name}' " +
                               "is missing a BaseModule component!");
                Destroy(moduleGO);
                return;
            }

            module.InitializePlacement(currentDefinition, position, surfaceNormal);

            // Set up the local grid origin if this is the first module
            if (!localGridOrigin.HasValue)
                EstablishLocalGrid(position, surfaceNormal);

            OnModulePlaced?.Invoke(module);

            Debug.Log($"[ModulePlacer] Placed: {currentDefinition.moduleName} at {position}");

            // Stay in placement mode (user can keep placing the same module type)
            // Press Escape or click a different toolbar button to exit
        }

        // ── Selection Mode ─────────────────────────────────────────────────────
        private void HandleSelectionMode()
        {
            // Delete key
            if (Input.GetKeyDown(KeyCode.Delete) && selectedModule != null)
            {
                DeleteSelectedModule();
                return;
            }

            // Left click — try to select a module
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            {
                TrySelectModule();
            }
        }

        private void TrySelectModule()
        {
            if (Camera.main == null) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // First try the modules layer specifically
            if (Physics.Raycast(ray, out RaycastHit hit, 10000f, modulesLayer))
            {
                var module = hit.collider.GetComponentInParent<BaseModule>();
                if (module != null)
                {
                    SelectModule(module);
                    return;
                }
            }

            // Clicked terrain/empty — deselect
            DeselectCurrentModule();
        }

        private void SelectModule(BaseModule module)
        {
            if (selectedModule == module) return;
            DeselectCurrentModule();
            selectedModule = module;
            module.Select();
            OnModuleSelected?.Invoke(module);
        }

        private void DeselectCurrentModule()
        {
            if (selectedModule == null) return;
            selectedModule.Deselect();
            selectedModule = null;
            OnModuleDeselected?.Invoke();
        }

        // ── Ghost Management ───────────────────────────────────────────────────
        private void SpawnGhost(ModuleDefinition definition)
        {
            var ghostPrefab = definition.ghostPrefab != null ? definition.ghostPrefab : definition.prefab;
            if (ghostPrefab == null) return;

            ghostInstance = Instantiate(ghostPrefab);
            ghostInstance.name = "GhostPreview";

            // Disable all colliders on the ghost so it doesn't interfere with raycasts
            foreach (var col in ghostInstance.GetComponentsInChildren<Collider>())
                col.enabled = false;

            // Disable any BaseModule logic on the ghost
            foreach (var bm in ghostInstance.GetComponentsInChildren<BaseModule>())
                bm.enabled = false;

            ghostRenderers = ghostInstance.GetComponentsInChildren<Renderer>();
            ApplyGhostMaterial(ghostValidMaterial);
            lastPlacementValid = true;
        }

        private void DestroyGhost()
        {
            if (ghostInstance != null)
            {
                Destroy(ghostInstance);
                ghostInstance = null;
            }
            ghostRenderers = null;
        }

        private void UpdateGhost(Vector3 position, Vector3 surfaceNormal)
        {
            if (ghostInstance == null) return;
            ghostInstance.transform.position = position;

            // Align to surface normal
            float slopeAngle = Vector3.Angle(surfaceNormal, Vector3.up);
            if (slopeAngle <= 15f)
                ghostInstance.transform.rotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
            else
                ghostInstance.transform.rotation = Quaternion.Euler(0f, ghostInstance.transform.eulerAngles.y, 0f);
        }

        private void SetGhostVisibility(bool visible)
        {
            if (ghostInstance != null)
                ghostInstance.SetActive(visible);
        }

        private void ApplyGhostMaterial(Material mat)
        {
            if (ghostRenderers == null || mat == null) return;
            foreach (var r in ghostRenderers)
            {
                if (r == null) continue;
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }
        }

        // ── Placement Validation ───────────────────────────────────────────────
        private bool ValidatePlacement(Vector3 position,
                                       TerrainRaycaster.TerrainHit hit,
                                       ModuleDefinition definition)
        {
            if (definition == null) return false;

            // 1. Slope check
            float maxSlope = definition.GetEffectiveMaxSlope(
                terrainRaycaster != null ? terrainRaycaster.maxPlacementSlopeDegrees : 20f);

            if (hit.slopeAngleDegrees > maxSlope) return false;

            // 2. Overlap check (radius = half the smaller footprint dimension)
            float checkRadius = Mathf.Min(definition.footprintMeters.x, definition.footprintMeters.y) * 0.45f;
            if (!terrainRaycaster.IsAreaClearOfModules(position, checkRadius)) return false;

            return true;
        }

        // ── Local Grid Snapping ────────────────────────────────────────────────
        /// <summary>
        /// Establishes a local coordinate frame at the first placed module.
        /// All subsequent modules can snap to a grid defined in this frame.
        /// </summary>
        private void EstablishLocalGrid(Vector3 origin, Vector3 surfaceNormal)
        {
            localGridOrigin = origin;

            // Build a local tangent frame from the surface normal
            // Use world-north (approx) as the forward reference
            Vector3 worldNorth = Vector3.ProjectOnPlane(Vector3.forward, surfaceNormal).normalized;
            if (worldNorth.magnitude < 0.01f)
                worldNorth = Vector3.ProjectOnPlane(Vector3.right, surfaceNormal).normalized;

            localGridForward = worldNorth;
            localGridRight = Vector3.Cross(surfaceNormal, localGridForward).normalized;

            Debug.Log($"[ModulePlacer] Local grid established at {origin}");
        }

        /// <summary>
        /// Snaps a world position to the local grid defined by the first placed module.
        /// Returns the snapped position.
        /// </summary>
        private Vector3 SnapToLocalGrid(Vector3 worldPos, ModuleDefinition definition)
        {
            if (!localGridOrigin.HasValue) return worldPos;

            Vector3 offset = worldPos - localGridOrigin.Value;
            float gridX = definition.footprintMeters.x + definition.snapGapMeters;
            float gridZ = definition.footprintMeters.y + definition.snapGapMeters;

            float projRight = Vector3.Dot(offset, localGridRight);
            float projForward = Vector3.Dot(offset, localGridForward);

            float snappedRight = Mathf.Round(projRight / gridX) * gridX;
            float snappedForward = Mathf.Round(projForward / gridZ) * gridZ;

            return localGridOrigin.Value
                   + localGridRight * snappedRight
                   + localGridForward * snappedForward;
        }

        private Vector3 CalculatePlacementPosition(Vector3 rawHitPos, Vector3 surfaceNormal)
        {
            // If a local grid exists and the cursor is near an existing module, snap to grid
            if (localGridOrigin.HasValue && currentDefinition != null)
            {
                var nearbyModules = Physics.OverlapSphere(rawHitPos, snapDetectionRadius, modulesLayer);
                if (nearbyModules.Length > 0)
                    return SnapToLocalGrid(rawHitPos, currentDefinition);
            }

            return rawHitPos;
        }

        // ── Utility ────────────────────────────────────────────────────────────
        private bool IsPointerOverUI()
        {
            return UnityEngine.EventSystems.EventSystem.current != null &&
                   UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }
    }
}
