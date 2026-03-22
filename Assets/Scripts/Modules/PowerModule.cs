using UnityEngine;
using MoonBase.Core;

namespace MoonBase.Modules
{
    /// <summary>
    /// Power-specific behavior for lunar base modules.
    ///
    /// Extends BaseModule to implement IPowerProducer, IPowerConsumer,
    /// and IWaterProducer from ResourceSimulator. Automatically registers
    /// with ResourceSimulator on placement and unregisters on destruction.
    ///
    /// Supported module roles (set via ModuleRole enum):
    ///   SolarArray  — generates power scaled by sun efficiency + dust degradation
    ///   Battery     — stores energy; reports stored kWh to ResourceSimulator
    ///   Habitat     — constant power consumer, critical system
    ///   Greenhouse  — constant power consumer, non-critical (shed in emergency)
    ///   ISRU        — power consumer + water producer; rate scales with ice probability
    ///   MiningDrill — high power consumer, produces water; non-critical
    ///
    /// Dust accumulation: passive degradation accumulates at 0.1% per lunar day.
    /// Cleared by a rover maintenance visit (call ClearDust()).
    ///
    /// Setup: Add to any module prefab alongside BaseModule.
    ///        Set moduleRole in Inspector to match the module type.
    ///        basePowerKW should match ModuleDefinition.powerGenerationKW.
    /// </summary>
    [RequireComponent(typeof(BaseModule))]
    public class PowerModule : MonoBehaviour,
                               IPowerProducer,
                               IPowerConsumer,
                               IWaterProducer
    {
        // ── Role Enum ──────────────────────────────────────────────────────────
        public enum ModuleRole
        {
            SolarArray,
            Battery,
            Habitat,
            Greenhouse,
            ISRU,
            MiningDrill
        }

        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Module Role")]
        [Tooltip("Determines power behavior. Must match the module's ModuleDefinition type.")]
        public ModuleRole moduleRole = ModuleRole.SolarArray;

        [Header("Power Configuration")]
        [Tooltip("Base power generation (kW) at 100% sun efficiency. For SolarArray only.")]
        public float basePowerKW = 10f;

        [Tooltip("Constant power consumption (kW). Used for Habitat, Greenhouse, ISRU, Drill.")]
        public float powerConsumptionKW = 2f;

        [Header("Battery Storage (Battery role only)")]
        [Tooltip("Battery capacity in kWh. Only used when moduleRole = Battery.")]
        public float batteryCapacityKWh = 50f;

        [Tooltip("Maximum charge/discharge rate (kW).")]
        public float maxChargeRateKW = 5f;

        [Header("ISRU / Water Production")]
        [Tooltip("Max water extraction rate (L/day) at full ice probability and full power. " +
                 "Actual rate scales with IceDepositManager.GetIceProbabilityAt() at this position.")]
        public float maxWaterExtractionLPerDay = 100f;

        [Header("Dust Accumulation")]
        [Tooltip("Dust accumulation rate per simulated lunar day (fraction, default 0.001 = 0.1%/day).")]
        [Range(0f, 0.01f)]
        public float dustAccumulationRatePerDay = 0.001f;

        [Tooltip("Maximum dust accumulation before performance floor is hit (fraction 0-1).")]
        [Range(0f, 1f)]
        public float maxDustAccumulation = 0.5f;

        // ── Runtime State ──────────────────────────────────────────────────────
        /// <summary>Current dust accumulation fraction (0 = clean, 1 = maxDustAccumulation).</summary>
        public float DustAccumulation { get; private set; } = 0f;

        /// <summary>Current power output this tick (kW). Valid for SolarArray only.</summary>
        public float CurrentOutputKW { get; private set; }

        /// <summary>Last computed ice probability at this module's position.</summary>
        public float IceProbabilityAtSite { get; private set; }

        // ── Private ────────────────────────────────────────────────────────────
        private BaseModule baseModule;
        private double lastDustUpdateMET = 0.0;
        private bool isRegistered = false;

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            baseModule = GetComponent<BaseModule>();
        }

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void OnDestroy()
        {
            Unregister();
            if (LunarSimulationClock.Instance != null)
                LunarSimulationClock.Instance.OnSimTick -= OnSimTick;
        }

        private void Start()
        {
            if (LunarSimulationClock.Instance != null)
                LunarSimulationClock.Instance.OnSimTick += OnSimTick;
            else
                Debug.LogWarning($"[PowerModule] {name}: LunarSimulationClock not found. " +
                                 "Dust accumulation won't update.");
        }

        // ── IPowerProducer ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns current power output in kW.
        /// SolarArray: basePowerKW × sunEfficiency × (1 - dustAccumulation).
        /// Battery: reports 0 generation (charge/discharge handled by ResourceSimulator).
        /// All other roles: 0.
        /// </summary>
        public float GetCurrentOutputKW(float sunEfficiency)
        {
            if (moduleRole == ModuleRole.SolarArray)
            {
                float dustPenalty = 1f - DustAccumulation;
                CurrentOutputKW  = basePowerKW * sunEfficiency * dustPenalty;
                return CurrentOutputKW;
            }
            CurrentOutputKW = 0f;
            return 0f;
        }

        // ── IPowerConsumer ─────────────────────────────────────────────────────

        /// <summary>Returns constant power consumption in kW.</summary>
        public float GetPowerConsumptionKW()
        {
            return moduleRole switch
            {
                ModuleRole.SolarArray => 0f,
                ModuleRole.Battery    => 0f,
                _                     => powerConsumptionKW
            };
        }

        /// <summary>
        /// True for life-critical systems (Habitat). These are never shed in an emergency.
        /// Greenhouse, ISRU, and Drill are non-critical and will be shed first.
        /// </summary>
        public bool IsCriticalSystem => moduleRole == ModuleRole.Habitat;

        // ── IWaterProducer ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns water extraction rate in L/day.
        /// Only ISRU and MiningDrill produce water; all others return 0.
        /// Rate scales with IceProbabilityAtSite (updated every sim tick).
        /// </summary>
        public float GetExtractionRateLPerDay()
        {
            if (moduleRole != ModuleRole.ISRU && moduleRole != ModuleRole.MiningDrill)
                return 0f;

            // Check if power is available (non-critical, so may be shed)
            if (ResourceSimulator.Instance != null && ResourceSimulator.Instance.IsPowerEmergency)
                return 0f;

            return maxWaterExtractionLPerDay * IceProbabilityAtSite;
        }

        // ── Public Methods ─────────────────────────────────────────────────────

        /// <summary>
        /// Resets dust accumulation to zero. Call when a rover visits for maintenance.
        /// </summary>
        public void ClearDust()
        {
            DustAccumulation = 0f;
            Debug.Log($"[PowerModule] {name}: Dust cleared by rover maintenance.");
        }

        /// <summary>
        /// Adds extra battery capacity to ResourceSimulator.
        /// Call when this Battery module is placed to expand global storage.
        /// </summary>
        public void ContributeToGlobalBattery()
        {
            if (moduleRole != ModuleRole.Battery || ResourceSimulator.Instance == null) return;
            ResourceSimulator.Instance.batteryCapacityKWh += batteryCapacityKWh;
            Debug.Log($"[PowerModule] Battery module added {batteryCapacityKWh} kWh to global capacity.");
        }

        // ── Private Helpers ────────────────────────────────────────────────────
        private void Register()
        {
            if (isRegistered || ResourceSimulator.Instance == null) return;

            if (moduleRole == ModuleRole.SolarArray || moduleRole == ModuleRole.Battery)
                ResourceSimulator.Instance.RegisterProducer(this);

            if (moduleRole != ModuleRole.SolarArray && moduleRole != ModuleRole.Battery)
                ResourceSimulator.Instance.RegisterConsumer(this);

            if (moduleRole == ModuleRole.ISRU || moduleRole == ModuleRole.MiningDrill)
                ResourceSimulator.Instance.RegisterWaterProducer(this);

            if (moduleRole == ModuleRole.Battery)
                ContributeToGlobalBattery();

            isRegistered = true;
            Debug.Log($"[PowerModule] {name} registered as {moduleRole}.");
        }

        private void Unregister()
        {
            if (!isRegistered || ResourceSimulator.Instance == null) return;

            ResourceSimulator.Instance.UnregisterProducer(this);
            ResourceSimulator.Instance.UnregisterConsumer(this);
            ResourceSimulator.Instance.UnregisterWaterProducer(this);
            isRegistered = false;
        }

        private void OnSimTick(double metSeconds)
        {
            UpdateDustAccumulation(metSeconds);
            UpdateIceProbability();
        }

        private void UpdateDustAccumulation(double currentMET)
        {
            if (moduleRole != ModuleRole.SolarArray) return;

            double simDeltaSeconds = currentMET - lastDustUpdateMET;
            lastDustUpdateMET = currentMET;

            double deltaLunarDays = simDeltaSeconds / LunarSimulationClock.LunarMonthSeconds;
            float dustDelta = (float)(deltaLunarDays * dustAccumulationRatePerDay);

            DustAccumulation = Mathf.Clamp(DustAccumulation + dustDelta, 0f, maxDustAccumulation);
        }

        private void UpdateIceProbability()
        {
            if (moduleRole != ModuleRole.ISRU && moduleRole != ModuleRole.MiningDrill) return;
            if (DataLayers.IceDepositManager.Instance == null) return;

            IceProbabilityAtSite = DataLayers.IceDepositManager.Instance
                                             .GetIceProbabilityAt(transform.position);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (moduleRole == ModuleRole.SolarArray)
            {
                float dustColor = 1f - DustAccumulation;
                Gizmos.color = new Color(1f, dustColor, 0f);
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 3f, 2f);
            }
            else if (moduleRole == ModuleRole.ISRU || moduleRole == ModuleRole.MiningDrill)
            {
                Gizmos.color = new Color(0.2f, 0.9f, 1f, IceProbabilityAtSite);
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 3f, 3f);
            }
        }
#endif
    }
}
