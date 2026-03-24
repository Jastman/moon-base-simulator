using UnityEngine;
using MoonBase.Core;

namespace MoonBase.Modules
{
    /// <summary>
    /// ISRU Mining Rig — extracts water ice from permanently shadowed regions.
    /// Consumes significant power, produces water for life support and rocket fuel.
    /// Attach alongside BaseModule on the MiningRig prefab.
    /// </summary>
    [RequireComponent(typeof(BaseModule))]
    public class MiningRigModule : MonoBehaviour, IPowerConsumer, IWaterProducer
    {
        [Header("Mining Config")]
        [Tooltip("Power consumption for the drilling and electrolysis system (kW).")]
        public float powerConsumptionKW = 8f;

        [Tooltip("Water extraction rate in liters per simulated day when operational.")]
        public float extractionRateLPerDay = 200f;

        [Tooltip("0-1 efficiency multiplier based on local ice concentration. " +
                 "Set by IceDepositManager if present, otherwise defaults to 1.")]
        [Range(0f, 1f)]
        public float iceConcentration = 1f;

        [Tooltip("Mining is not a critical system — shed during power emergency.")]
        public bool isCriticalSystem = false;

        private void OnEnable()
        {
            if (ResourceSimulator.Instance != null)
            {
                ResourceSimulator.Instance.RegisterConsumer(this);
                ResourceSimulator.Instance.RegisterWaterProducer(this);
            }
        }

        private void OnDisable()
        {
            if (ResourceSimulator.Instance != null)
            {
                ResourceSimulator.Instance.UnregisterConsumer(this);
                ResourceSimulator.Instance.UnregisterWaterProducer(this);
            }
        }

        // ── IPowerConsumer ────────────────────────────────────────────────────
        public float GetPowerConsumptionKW() => powerConsumptionKW;
        public bool IsCriticalSystem => isCriticalSystem;

        // ── IWaterProducer ────────────────────────────────────────────────────
        public float GetExtractionRateLPerDay() => extractionRateLPerDay * iceConcentration;
    }
}
