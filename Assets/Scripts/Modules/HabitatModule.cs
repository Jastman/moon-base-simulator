using UnityEngine;
using MoonBase.Core;

namespace MoonBase.Modules
{
    /// <summary>
    /// Habitat module — consumes power, houses crew, produces a small amount of O2 
    /// via recycling. This is a critical system; it stays active during power emergencies.
    /// Attach alongside BaseModule on the Habitat prefab.
    /// </summary>
    [RequireComponent(typeof(BaseModule))]
    public class HabitatModule : MonoBehaviour, IPowerConsumer
    {
        [Header("Habitat Config")]
        [Tooltip("Base power draw for life support, lighting, heating (kW).")]
        public float basePowerDrawKW = 4f;

        [Tooltip("Additional power per crew member housed (kW/crew).")]
        public float powerPerCrewKW = 1f;

        [Tooltip("Number of crew housed in this habitat (set by ModuleDefinition or Inspector).")]
        public int crewCount = 4;

        [Tooltip("If true, habitat stays on during power emergency (crew life support = critical).")]
        public bool isCriticalSystem = true;

        private void OnEnable()
        {
            if (ResourceSimulator.Instance != null)
                ResourceSimulator.Instance.RegisterConsumer(this);
        }

        private void OnDisable()
        {
            if (ResourceSimulator.Instance != null)
                ResourceSimulator.Instance.UnregisterConsumer(this);
        }

        // ── IPowerConsumer ────────────────────────────────────────────────────
        public float GetPowerConsumptionKW() => basePowerDrawKW + (powerPerCrewKW * crewCount);
        public bool IsCriticalSystem => isCriticalSystem;
    }
}
