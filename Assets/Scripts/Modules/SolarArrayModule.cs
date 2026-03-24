using UnityEngine;
using MoonBase.Core;

namespace MoonBase.Modules
{
    /// <summary>
    /// Solar Array module — generates power proportional to sun efficiency.
    /// Attach alongside BaseModule on the SolarArray prefab.
    /// Automatically registers/unregisters with ResourceSimulator.
    /// </summary>
    [RequireComponent(typeof(BaseModule))]
    public class SolarArrayModule : MonoBehaviour, IPowerProducer
    {
        [Header("Solar Array Config")]
        [Tooltip("Peak output at full sun (kW). Typical 10kW per large panel array.")]
        public float peakOutputKW = 10f;

        [Tooltip("Minimum output even in full lunar night (RTG contribution or battery trickle). Usually 0.")]
        public float minimumOutputKW = 0f;

        // Cache the BaseModule for faster access
        private BaseModule baseModule;

        private void Awake()
        {
            baseModule = GetComponent<BaseModule>();
        }

        private void OnEnable()
        {
            if (ResourceSimulator.Instance != null)
                ResourceSimulator.Instance.RegisterProducer(this);
        }

        private void OnDisable()
        {
            if (ResourceSimulator.Instance != null)
                ResourceSimulator.Instance.UnregisterProducer(this);
        }

        // ── IPowerProducer ────────────────────────────────────────────────────
        /// <summary>
        /// Returns current output in kW. sunEfficiency 0-1 where 1 = full sun.
        /// At lunar south pole, sun stays near horizon so efficiency is always partial.
        /// </summary>
        public float GetCurrentOutputKW(float sunEfficiency)
        {
            return minimumOutputKW + (peakOutputKW - minimumOutputKW) * sunEfficiency;
        }
    }
}
