using UnityEngine;
using System;

namespace MoonBase.Core
{
    /// <summary>
    /// Master simulation time controller for LUNAR OPS.
    ///
    /// Manages Mission Elapsed Time (MET), time scale, and derives
    /// real lunar south pole solar geometry from elapsed time.
    ///
    /// Solar model: At the lunar south pole (~89.9°S), the Sun circles
    /// the horizon at an elevation of roughly 1.5–6° depending on the
    /// Moon's axial tilt (1.54°) and libration. One full azimuth rotation
    /// takes one synodic lunar month: 29.530589 Earth days.
    ///
    /// Setup: Attach to a persistent GameObject (e.g., "SimulationClock").
    ///        No inspector references required; works standalone.
    ///        LunarSimulationClock.Instance is available after Awake.
    /// </summary>
    public class LunarSimulationClock : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        public static LunarSimulationClock Instance { get; private set; }

        // ── Constants ──────────────────────────────────────────────────────────
        /// <summary>Length of one synodic lunar month in Earth seconds.</summary>
        public const double LunarMonthSeconds = 29.530589 * 24.0 * 3600.0;

        /// <summary>Min sun elevation at south pole (degrees). Moon axial tilt floor.</summary>
        public const float SunElevationMin = 1.5f;

        /// <summary>Max sun elevation at south pole (degrees). Peak libration.</summary>
        public const float SunElevationMax = 6.0f;

        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Time Scale")]
        [Tooltip("Current simulation time multiplier. 1 = real time, 100 = fast, 8640 = 1 day per 10 sec.")]
        [Range(0f, 100000f)]
        public float timeScale = 100f;

        [Tooltip("Maximum allowed time scale. Clamped in SetTimeScale().")]
        public float maxTimeScale = 100000f;

        [Header("Starting Conditions")]
        [Tooltip("Mission Elapsed Time at scene start, in seconds. 0 = mission begin.")]
        public double startingMETSeconds = 0.0;

        [Tooltip("Starting sun azimuth offset in degrees (0 = north). " +
                 "Use to set what time of lunar day you start on.")]
        [Range(0f, 360f)]
        public float startingAzimuthOffset = 0f;

        // ── Runtime Properties ─────────────────────────────────────────────────
        /// <summary>Mission Elapsed Time in seconds (simulated).</summary>
        public double METSeconds { get; private set; }

        /// <summary>Current lunar day number (0-indexed from mission start).</summary>
        public int LunarDayNumber { get; private set; }

        /// <summary>
        /// Normalized progress through current lunar day (0 = day start, 1 = day end).
        /// A full revolution of the Sun around the horizon = 0 to 1.
        /// </summary>
        public float TimeOfLunarDay { get; private set; }

        /// <summary>Sun azimuth in degrees (0 = north, 90 = east, 180 = south, 270 = west).</summary>
        public float SunAzimuthDegrees { get; private set; }

        /// <summary>
        /// Sun elevation above the horizon in degrees.
        /// Oscillates between SunElevationMin and SunElevationMax over one lunar month,
        /// driven by the Moon's libration cycle.
        /// </summary>
        public float SunElevationDegrees { get; private set; }

        /// <summary>True when the sun is above the local horizon (elevation > 0).</summary>
        public bool IsLunarDay => SunElevationDegrees > 0f;

        // ── Events ─────────────────────────────────────────────────────────────
        /// <summary>Fired at the start of each new lunar day (azimuth crosses 0).</summary>
        public event Action<int> OnLunarDayStart;

        /// <summary>
        /// Fired when sun dips to SunElevationMin — effectively "night" for high-latitude
        /// shadowed regions (not a true night at the pole, but minimum illumination).
        /// </summary>
        public event Action OnLunarNightStart;

        /// <summary>Fired whenever time scale changes. Provides new scale value.</summary>
        public event Action<float> OnTimeScaleChanged;

        /// <summary>Fired every simulation tick with current MET. Useful for subscribers
        /// that need to update on sim time rather than real time.</summary>
        public event Action<double> OnSimTick;

        // ── Private ────────────────────────────────────────────────────────────
        private int previousLunarDay = -1;
        private bool wasLowElevation = false;

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[LunarSimulationClock] Duplicate instance destroyed.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            METSeconds = startingMETSeconds;
            RecalculateSolarPosition();
            previousLunarDay = LunarDayNumber;
        }

        private void Update()
        {
            if (timeScale <= 0f) return;

            double deltaSim = Time.deltaTime * timeScale;
            METSeconds += deltaSim;

            RecalculateSolarPosition();
            FireEvents();

            OnSimTick?.Invoke(METSeconds);
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Sets simulation time scale. Clamped to [0, maxTimeScale].
        /// Fires OnTimeScaleChanged.
        /// </summary>
        public void SetTimeScale(float scale)
        {
            float clamped = Mathf.Clamp(scale, 0f, maxTimeScale);
            if (Mathf.Approximately(clamped, timeScale)) return;
            timeScale = clamped;
            OnTimeScaleChanged?.Invoke(timeScale);
            Debug.Log($"[LunarSimulationClock] Time scale: {timeScale:F0}x");
        }

        /// <summary>
        /// Pauses simulation (sets time scale to 0).
        /// Call SetTimeScale() to resume.
        /// </summary>
        public void Pause() => SetTimeScale(0f);

        /// <summary>
        /// Jumps MET to a specific value in seconds without firing transition events.
        /// Useful for load/restore from save.
        /// </summary>
        public void SetMET(double metSeconds)
        {
            METSeconds = Math.Max(0.0, metSeconds);
            RecalculateSolarPosition();
            previousLunarDay = LunarDayNumber;
        }

        /// <summary>
        /// Formats MET as a human-readable mission time string.
        /// Format: "DDd HHh MMm SSs"
        /// </summary>
        public string GetMETString()
        {
            double total = METSeconds;
            int days  = (int)(total / 86400.0);
            int hours = (int)((total % 86400.0) / 3600.0);
            int mins  = (int)((total % 3600.0) / 60.0);
            int secs  = (int)(total % 60.0);
            return $"MET {days:D3}d {hours:D2}h {mins:D2}m {secs:D2}s";
        }

        /// <summary>
        /// Returns sun azimuth and elevation at the lunar south pole for a given MET.
        /// Static utility — can be called without an instance.
        /// </summary>
        public static (float azimuth, float elevation) GetSolarPositionAtMET(
            double metSeconds, float azimuthOffset = 0f)
        {
            double monthFraction = (metSeconds % LunarMonthSeconds) / LunarMonthSeconds;
            float azimuth = ((float)(monthFraction * 360.0) + azimuthOffset) % 360f;

            // Elevation oscillates sinusoidally over one month
            // (simplified model of libration; real data would use SPICE or DE430)
            float elevAngle = (float)(monthFraction * 2.0 * Math.PI);
            float elevation = SunElevationMin + (SunElevationMax - SunElevationMin)
                              * (0.5f + 0.5f * Mathf.Sin(elevAngle));

            return (azimuth, elevation);
        }

        // ── Private Helpers ────────────────────────────────────────────────────
        private void RecalculateSolarPosition()
        {
            var (az, el) = GetSolarPositionAtMET(METSeconds, startingAzimuthOffset);
            SunAzimuthDegrees   = az;
            SunElevationDegrees = el;

            double monthFraction = (METSeconds % LunarMonthSeconds) / LunarMonthSeconds;
            TimeOfLunarDay  = (float)monthFraction;
            LunarDayNumber  = (int)(METSeconds / LunarMonthSeconds);
        }

        private void FireEvents()
        {
            // Day rollover
            if (LunarDayNumber != previousLunarDay)
            {
                previousLunarDay = LunarDayNumber;
                OnLunarDayStart?.Invoke(LunarDayNumber);
                Debug.Log($"[LunarSimulationClock] Lunar day {LunarDayNumber} started. MET: {GetMETString()}");
            }

            // Minimum elevation crossing (proxy for "worst illumination" moment)
            bool isLowNow = SunElevationDegrees <= SunElevationMin + 0.05f;
            if (isLowNow && !wasLowElevation)
            {
                OnLunarNightStart?.Invoke();
            }
            wasLowElevation = isLowNow;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var (az, el) = GetSolarPositionAtMET(METSeconds, startingAzimuthOffset);
            float elevRad = el * Mathf.Deg2Rad;
            float azimRad = az * Mathf.Deg2Rad;
            Vector3 sunDir = new Vector3(
                Mathf.Sin(azimRad) * Mathf.Cos(elevRad),
                Mathf.Sin(elevRad),
                Mathf.Cos(azimRad) * Mathf.Cos(elevRad)
            ).normalized;
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, sunDir * 100f);
            Gizmos.DrawSphere(transform.position + sunDir * 100f, 3f);
        }
#endif
    }
}
