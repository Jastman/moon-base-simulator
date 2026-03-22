using System.Collections.Generic;
using UnityEngine;
using MoonBase.DataLayers;

namespace MoonBase.Core
{
    /// <summary>
    /// Master resource tracker for LUNAR OPS.
    ///
    /// Aggregates power generation and consumption across all registered
    /// IPowerProducer and IPowerConsumer components, charges/drains the battery,
    /// and exposes resource state to the rest of the simulation.
    ///
    /// Driven by LunarSimulationClock.OnSimTick — updates at sim-time rate,
    /// not real-time. Falls back to Unity Update() if no clock is present.
    ///
    /// Setup: Attach to a persistent GameObject. No inspector wiring required
    ///        beyond optional capacity overrides.
    /// </summary>
    public class ResourceSimulator : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        public static ResourceSimulator Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Battery Configuration")]
        [Tooltip("Total battery capacity across all storage units (kWh).")]
        public float batteryCapacityKWh = 200f;

        [Tooltip("Starting battery charge fraction (0-1).")]
        [Range(0f, 1f)]
        public float startingChargeFraction = 0.8f;

        [Header("Water & Life Support")]
        [Tooltip("Maximum water storage capacity (liters).")]
        public float maxWaterLiters = 10000f;

        [Tooltip("Maximum O2 level (percent, 0-100).")]
        public float maxO2Percent = 100f;

        [Tooltip("Baseline O2 consumption per crew per sim-hour (percent/hour).")]
        public float o2ConsumptionPerCrewPerHour = 0.5f;

        [Header("Thresholds")]
        [Tooltip("Battery percent below which OnPowerCritical fires.")]
        [Range(0f, 30f)]
        public float powerCriticalThreshold = 10f;

        [Tooltip("Battery percent below which non-critical systems auto-shed.")]
        [Range(0f, 20f)]
        public float powerEmergencyThreshold = 5f;

        // ── Runtime Properties ─────────────────────────────────────────────────
        /// <summary>Total solar + other power generated this tick (kW).</summary>
        public float PowerGeneratedKW { get; private set; }

        /// <summary>Total power consumed by all active modules this tick (kW).</summary>
        public float PowerConsumedKW { get; private set; }

        /// <summary>Net power balance (kW). Positive = surplus, negative = deficit.</summary>
        public float NetPowerKW => PowerGeneratedKW - PowerConsumedKW;

        /// <summary>Current battery stored energy (kWh).</summary>
        public float PowerStoredKWh { get; private set; }

        /// <summary>Battery charge as a fraction (0-1).</summary>
        public float BatteryChargePercent => batteryCapacityKWh > 0f
            ? Mathf.Clamp01(PowerStoredKWh / batteryCapacityKWh) * 100f
            : 0f;

        /// <summary>Total water extracted and stored (liters).</summary>
        public float WaterStoredLiters { get; private set; }

        /// <summary>Water extracted per simulated day by ISRU modules (L/day).</summary>
        public float WaterExtractionRateLPerDay { get; private set; }

        /// <summary>Current O2 level (0-100%).</summary>
        public float O2Percent { get; private set; }

        /// <summary>True when battery is in critical state.</summary>
        public bool IsPowerCritical { get; private set; }

        /// <summary>True when battery is in emergency state (non-critical systems shed).</summary>
        public bool IsPowerEmergency { get; private set; }

        // ── Events ─────────────────────────────────────────────────────────────
        /// <summary>Fired when battery drops below powerCriticalThreshold. Arg = battery %.</summary>
        public event System.Action<float> OnPowerCritical;

        /// <summary>Fired when battery recovers above powerCriticalThreshold after being critical.</summary>
        public event System.Action OnPowerRestored;

        /// <summary>Fired every sim tick with a ResourceSnapshot. Safe to subscribe to from UI.</summary>
        public event System.Action<ResourceSnapshot> OnResourceUpdate;

        // ── Internal registries ────────────────────────────────────────────────
        private readonly List<IPowerProducer> producers = new();
        private readonly List<IPowerConsumer> consumers = new();
        private readonly List<IWaterProducer> waterProducers = new();

        private bool wasCritical = false;

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            PowerStoredKWh = batteryCapacityKWh * startingChargeFraction;
            O2Percent      = maxO2Percent;
        }

        private void Start()
        {
            if (LunarSimulationClock.Instance != null)
                LunarSimulationClock.Instance.OnSimTick += OnSimTick;
            else
                Debug.LogWarning("[ResourceSimulator] LunarSimulationClock not found — " +
                                 "falling back to real-time Update().");
        }

        private void OnDestroy()
        {
            if (LunarSimulationClock.Instance != null)
                LunarSimulationClock.Instance.OnSimTick -= OnSimTick;
        }

        private void Update()
        {
            // Only used if no simulation clock is present
            if (LunarSimulationClock.Instance == null)
                Tick(Time.deltaTime);
        }

        // ── Registration ───────────────────────────────────────────────────────

        /// <summary>
        /// Registers a power producer (e.g., solar array module).
        /// Call from PowerModule.OnEnable or InitializePlacement.
        /// </summary>
        public void RegisterProducer(IPowerProducer producer)
        {
            if (producer == null || producers.Contains(producer)) return;
            producers.Add(producer);
        }

        /// <summary>Unregisters a power producer.</summary>
        public void UnregisterProducer(IPowerProducer producer)
        {
            producers.Remove(producer);
        }

        /// <summary>Registers a power consumer (habitat, greenhouse, etc.).</summary>
        public void RegisterConsumer(IPowerConsumer consumer)
        {
            if (consumer == null || consumers.Contains(consumer)) return;
            consumers.Add(consumer);
        }

        /// <summary>Unregisters a power consumer.</summary>
        public void UnregisterConsumer(IPowerConsumer consumer)
        {
            consumers.Remove(consumer);
        }

        /// <summary>Registers a water producer (ISRU module).</summary>
        public void RegisterWaterProducer(IWaterProducer waterProducer)
        {
            if (waterProducer == null || waterProducers.Contains(waterProducer)) return;
            waterProducers.Add(waterProducer);
        }

        /// <summary>Unregisters a water producer.</summary>
        public void UnregisterWaterProducer(IWaterProducer waterProducer)
        {
            waterProducers.Remove(waterProducer);
        }

        // ── Public Utilities ───────────────────────────────────────────────────

        /// <summary>
        /// Returns a full snapshot of the current resource state.
        /// Used by SaveSystem and OperationsDashboardUI.
        /// </summary>
        public ResourceSnapshot GetSnapshot() => new ResourceSnapshot
        {
            powerGeneratedKW      = PowerGeneratedKW,
            powerConsumedKW       = PowerConsumedKW,
            netPowerKW            = NetPowerKW,
            powerStoredKWh        = PowerStoredKWh,
            batteryCapacityKWh    = batteryCapacityKWh,
            batteryChargePercent  = BatteryChargePercent,
            waterStoredLiters     = WaterStoredLiters,
            waterExtractionLPerDay = WaterExtractionRateLPerDay,
            o2Percent             = O2Percent,
            isPowerCritical       = IsPowerCritical,
            isPowerEmergency      = IsPowerEmergency
        };

        /// <summary>
        /// Directly sets battery stored energy. Used by SaveSystem on load.
        /// </summary>
        public void SetBatteryCharge(float kWh)
        {
            PowerStoredKWh = Mathf.Clamp(kWh, 0f, batteryCapacityKWh);
        }

        /// <summary>
        /// Directly sets water stored amount. Used by SaveSystem on load.
        /// </summary>
        public void SetWaterStored(float liters)
        {
            WaterStoredLiters = Mathf.Clamp(liters, 0f, maxWaterLiters);
        }

        // ── Internal Sim Logic ─────────────────────────────────────────────────
        private void OnSimTick(double metSeconds)
        {
            // Sim tick delta from time scale and real delta time
            float simDelta = Time.deltaTime * (LunarSimulationClock.Instance?.timeScale ?? 1f);
            Tick(simDelta);
        }

        private void Tick(float simDeltaSeconds)
        {
            float sunEfficiency = GetSunEfficiency();

            // ── Power Generation ───────────────────────────────────────────────
            float totalGenerated = 0f;
            foreach (var producer in producers)
            {
                if (producer == null) continue;
                totalGenerated += producer.GetCurrentOutputKW(sunEfficiency);
            }
            PowerGeneratedKW = totalGenerated;

            // ── Power Consumption ──────────────────────────────────────────────
            float totalConsumed = 0f;
            foreach (var consumer in consumers)
            {
                if (consumer == null) continue;
                bool isCritical = consumer.IsCriticalSystem;
                if (IsPowerEmergency && !isCritical) continue; // shed non-critical
                totalConsumed += consumer.GetPowerConsumptionKW();
            }
            PowerConsumedKW = totalConsumed;

            // ── Battery Charge/Drain ───────────────────────────────────────────
            float netKW    = NetPowerKW;
            float deltaHours = simDeltaSeconds / 3600f;
            float deltaKWh = netKW * deltaHours;
            PowerStoredKWh = Mathf.Clamp(PowerStoredKWh + deltaKWh, 0f, batteryCapacityKWh);

            // ── Water Extraction ───────────────────────────────────────────────
            float totalWaterPerDay = 0f;
            foreach (var wp in waterProducers)
            {
                if (wp == null) continue;
                totalWaterPerDay += wp.GetExtractionRateLPerDay();
            }
            WaterExtractionRateLPerDay = totalWaterPerDay;
            float waterDelta = totalWaterPerDay * (simDeltaSeconds / 86400f);
            WaterStoredLiters = Mathf.Clamp(WaterStoredLiters + waterDelta, 0f, maxWaterLiters);

            // ── Power State Checks ─────────────────────────────────────────────
            float chargePercent = BatteryChargePercent;
            bool nowCritical    = chargePercent < powerCriticalThreshold;
            bool nowEmergency   = chargePercent < powerEmergencyThreshold;

            if (nowCritical && !wasCritical)
            {
                IsPowerCritical = true;
                OnPowerCritical?.Invoke(chargePercent);
                Debug.LogWarning($"[ResourceSimulator] POWER CRITICAL: {chargePercent:F1}%");
            }
            else if (!nowCritical && wasCritical)
            {
                IsPowerCritical = false;
                OnPowerRestored?.Invoke();
                Debug.Log("[ResourceSimulator] Power restored to safe levels.");
            }
            wasCritical = nowCritical;
            IsPowerEmergency = nowEmergency;

            OnResourceUpdate?.Invoke(GetSnapshot());
        }

        private float GetSunEfficiency()
        {
            if (SolarExposureManager.Instance == null) return 0.5f;
            // Use the global sun direction's elevation as efficiency proxy
            // Full efficiency at 6° elevation, reduced toward 0° (horizon scattering)
            float elev = LunarSimulationClock.Instance?.SunElevationDegrees ?? 3f;
            return Mathf.InverseLerp(0f, LunarSimulationClock.SunElevationMax, elev);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (powerEmergencyThreshold > powerCriticalThreshold)
                powerEmergencyThreshold = powerCriticalThreshold - 1f;
        }
#endif
    }

    // ── Supporting Interfaces ──────────────────────────────────────────────────

    /// <summary>
    /// Implement on any module that generates power (solar arrays, RTGs, etc.).
    /// </summary>
    public interface IPowerProducer
    {
        /// <summary>Returns current output in kW, given sun efficiency 0-1.</summary>
        float GetCurrentOutputKW(float sunEfficiency);
    }

    /// <summary>
    /// Implement on any module that consumes power (habitats, equipment, etc.).
    /// </summary>
    public interface IPowerConsumer
    {
        /// <summary>Returns constant power draw in kW.</summary>
        float GetPowerConsumptionKW();

        /// <summary>If false, this consumer is shed during PowerEmergency state.</summary>
        bool IsCriticalSystem { get; }
    }

    /// <summary>
    /// Implement on ISRU modules that extract water ice.
    /// </summary>
    public interface IWaterProducer
    {
        /// <summary>Returns water extraction rate in liters per simulated day.</summary>
        float GetExtractionRateLPerDay();
    }

    // ── Data Transfer Object ───────────────────────────────────────────────────

    /// <summary>
    /// Immutable snapshot of resource state at a given sim tick.
    /// Passed through OnResourceUpdate and used by SaveSystem / UI.
    /// </summary>
    [System.Serializable]
    public struct ResourceSnapshot
    {
        public float powerGeneratedKW;
        public float powerConsumedKW;
        public float netPowerKW;
        public float powerStoredKWh;
        public float batteryCapacityKWh;
        public float batteryChargePercent;
        public float waterStoredLiters;
        public float waterExtractionLPerDay;
        public float o2Percent;
        public bool  isPowerCritical;
        public bool  isPowerEmergency;
    }
}
