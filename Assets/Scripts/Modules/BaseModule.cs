using UnityEngine;
using MoonBase.Core;

namespace MoonBase.Modules
{
    /// <summary>
    /// Attach to every placeable module prefab.
    /// Handles per-instance state: which definition it is, its placed position,
    /// whether it's selected, and ground-snapping as terrain tiles reload.
    ///
    /// Setup: Attach to root of each module prefab. Assign ModuleDefinition in
    /// the prefab (or let ModulePlacer assign it at instantiation time).
    /// Also attach a Collider component for click-selection raycasts.
    /// </summary>
    public class BaseModule : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Definition")]
        [Tooltip("The ScriptableObject defining this module type. Can be set on the prefab " +
                 "or overridden at instantiation time by ModulePlacer.")]
        public ModuleDefinition moduleDefinition;

        [Header("Visual Settings")]
        [Tooltip("Renderer(s) on this module. Used to apply selection highlight.")]
        public Renderer[] moduleRenderers;

        [Tooltip("Material applied as an overlay when this module is selected.")]
        public Material selectedOverlayMaterial;

        [Header("Ground Snapping")]
        [Tooltip("If true, this module will attempt to re-snap to terrain each frame " +
                 "(useful while tiles are still loading). Disable once fully settled.")]
        public bool continuousGroundSnap = true;

        [Tooltip("How often (in seconds) to re-snap to ground. Lower = more responsive, higher = cheaper.")]
        [Range(0.1f, 5f)]
        public float groundSnapInterval = 0.5f;

        // ── Properties ─────────────────────────────────────────────────────────
        public ModuleDefinition ModuleDefinition => moduleDefinition;
        public bool IsSelected { get; private set; }

        /// <summary>World position recorded at placement time (used for save/load).</summary>
        public Vector3 PlacedWorldPosition { get; private set; }

        /// <summary>Surface normal at the placement point.</summary>
        public Vector3 PlacedSurfaceNormal { get; private set; }

        // ── Events ─────────────────────────────────────────────────────────────
        public System.Action<BaseModule> OnSelected;
        public System.Action<BaseModule> OnDeselected;

        // ── Private ────────────────────────────────────────────────────────────
        private float groundSnapTimer;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock propertyBlock;

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            // Auto-find renderers if not assigned
            if (moduleRenderers == null || moduleRenderers.Length == 0)
                moduleRenderers = GetComponentsInChildren<Renderer>();

            groundSnapTimer = groundSnapInterval;
        }

        private void Update()
        {
            if (!continuousGroundSnap) return;

            groundSnapTimer -= Time.deltaTime;
            if (groundSnapTimer <= 0f)
            {
                groundSnapTimer = groundSnapInterval;
                TrySnapToGround();
            }
        }

        // ── Placement Initialization ───────────────────────────────────────────
        /// <summary>
        /// Called by ModulePlacer immediately after instantiation.
        /// Records placement data and aligns the module to the terrain normal.
        /// </summary>
        public void InitializePlacement(ModuleDefinition definition,
                                        Vector3 worldPosition,
                                        Vector3 surfaceNormal)
        {
            moduleDefinition = definition;
            PlacedWorldPosition = worldPosition;
            PlacedSurfaceNormal = surfaceNormal;

            transform.position = worldPosition;
            AlignToNormal(surfaceNormal);

            // Register with the manager
            if (MoonBaseManager.Instance != null)
                MoonBaseManager.Instance.RegisterModule(this);
        }

        // ── Selection ──────────────────────────────────────────────────────────
        public void Select()
        {
            if (IsSelected) return;
            IsSelected = true;
            ApplySelectionHighlight(true);
            OnSelected?.Invoke(this);
        }

        public void Deselect()
        {
            if (!IsSelected) return;
            IsSelected = false;
            ApplySelectionHighlight(false);
            OnDeselected?.Invoke(this);
        }

        // ── Destruction ────────────────────────────────────────────────────────
        private void OnDestroy()
        {
            if (MoonBaseManager.Instance != null)
                MoonBaseManager.Instance.UnregisterModule(this);
        }

        // ── Private Helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Rotates the module so its up-axis aligns with the terrain surface normal.
        /// For very steep slopes (>15°) the module stays world-vertical — this is configurable.
        /// </summary>
        private void AlignToNormal(Vector3 normal)
        {
            if (normal == Vector3.zero) return;

            float slopeAngle = Vector3.Angle(normal, Vector3.up);
            float maxTilt = moduleDefinition != null && moduleDefinition.maxSlopeOverrideDegrees >= 0
                ? moduleDefinition.maxSlopeOverrideDegrees
                : 15f;

            if (slopeAngle <= maxTilt)
            {
                // Align module up with surface normal, keep facing away from Moon center
                Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, normal);
                transform.rotation = targetRotation;
            }
            else
            {
                // Too steep — stay world-vertical (just yaw from current facing)
                Vector3 euler = transform.rotation.eulerAngles;
                transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
            }
        }

        private void TrySnapToGround()
        {
            if (TerrainRaycaster.Instance == null) return;

            var hit = TerrainRaycaster.Instance.SnapToGroundBelow(transform.position);
            if (!hit.didHit) return;

            // Only re-snap if there's a meaningful difference (avoids jitter)
            float delta = Vector3.Distance(transform.position, hit.worldPosition);
            if (delta > 0.05f)
            {
                transform.position = hit.worldPosition;
                AlignToNormal(hit.surfaceNormal);
            }
        }

        private void ApplySelectionHighlight(bool selected)
        {
            if (moduleRenderers == null) return;

            foreach (var r in moduleRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(propertyBlock);
                if (selected)
                {
                    // Bright emissive highlight (works with URP/Standard shaders)
                    propertyBlock.SetColor(EmissionColor, Color.cyan * 0.4f);
                }
                else
                {
                    propertyBlock.SetColor(EmissionColor, Color.black);
                }
                r.SetPropertyBlock(propertyBlock);
            }
        }

        // ── Click to Select ────────────────────────────────────────────────────
        /// <summary>
        /// Unity sends this when the object is clicked in-scene (requires Collider + Physics Raycaster
        /// on camera, or manual click detection from ModulePlacer).
        /// </summary>
        private void OnMouseDown()
        {
            // ModulePlacer handles selection during placement mode.
            // Outside placement mode, clicking a module selects it.
            if (ModulePlacer.Instance != null && ModulePlacer.Instance.IsInPlacementMode) return;
            Select();
        }
    }
}
