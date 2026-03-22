using UnityEngine;

namespace MoonBase
{
    /// <summary>
    /// Drives the scene's directional "Sun" light from LunarSimulationClock's
    /// azimuth/elevation angles. Attach to the Sun GameObject alongside a Light.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LunarSkybox : MonoBehaviour
    {
        private Light _sun;
        private LunarSimulationClock _clock;

        private void Start()
        {
            _sun = GetComponent<Light>();
            _sun.color     = new Color(1f, 0.97f, 0.92f);
            _sun.intensity = 1.3f;
            RenderSettings.sun = _sun;

            _clock = LunarSimulationClock.Instance;
            if (_clock != null)
            {
                _clock.OnSimTick += OnSimTick;
                // Apply initial direction immediately
                OnSimTick(_clock.SunAzimuthDegrees, _clock.SunElevationDegrees);
            }
            else
            {
                Debug.LogWarning("[LunarSkybox] LunarSimulationClock.Instance not found — sun won't track simulation time.");
            }
        }

        private void OnDestroy()
        {
            if (_clock != null)
                _clock.OnSimTick -= OnSimTick;
        }

        /// <summary>
        /// Called each sim tick. Converts azimuth + elevation to a world-space
        /// direction and rotates the light to match.
        /// </summary>
        private void OnSimTick(float azimuthDeg, float elevationDeg)
        {
            float az = azimuthDeg  * Mathf.Deg2Rad;
            float el = elevationDeg * Mathf.Deg2Rad;

            // Standard spherical → Cartesian (Y-up, Z-north)
            Vector3 dir = new Vector3(
                Mathf.Sin(az) * Mathf.Cos(el),
                Mathf.Sin(el),
                Mathf.Cos(az) * Mathf.Cos(el)
            );

            // LookRotation toward -dir so the light shines from that direction
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(-dir);
        }
    }
}
