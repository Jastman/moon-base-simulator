using UnityEngine;
using System;
using MoonBase.UI;

namespace MoonBase.Core
{
    public class TimeController : MonoBehaviour
    {
        public event Action OnPaused;
        public event Action OnResumed;

        private float currentTimeScale = 1f;
        private bool isPaused = false;

        private float[] timeMultipliers = new float[] { 1f, 60f, 600f, 3600f, 86400f };

        private void Update()
        {
            HandleKeyInput();
        }

        private void HandleKeyInput()
        {
            // Check for time scale multiplier keys (1-5)
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SetTimeScale(1f);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SetTimeScale(60f);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SetTimeScale(600f);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                SetTimeScale(3600f);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                SetTimeScale(86400f);
            }

            // Check for pause/resume (Space)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TogglePause();
            }
        }

        public void SetTimeScale(float multiplier)
        {
            currentTimeScale = multiplier;
            if (!isPaused)
            {
                Time.timeScale = multiplier;
                UpdateTimeScaleUI(multiplier);
            }
        }

        private void TogglePause()
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        private void Pause()
        {
            isPaused = true;
            Time.timeScale = 0f;
            OnPaused?.Invoke();
            Debug.Log("Simulation paused");
        }

        private void Resume()
        {
            isPaused = false;
            Time.timeScale = currentTimeScale;
            OnResumed?.Invoke();
            Debug.Log($"Simulation resumed at {currentTimeScale}x speed");
        }

        private void UpdateTimeScaleUI(float multiplier)
        {
            string displayText = multiplier switch
            {
                1f => "1x Speed",
                60f => "60x Speed (1 min = 1 hr)",
                600f => "600x Speed",
                3600f => "3600x Speed (1 sec = 1 hr)",
                86400f => "86400x Speed (1 sec = 1 day)",
                _ => $"{multiplier}x Speed"
            };
        }

        public float GetCurrentTimeScale()
        {
            return currentTimeScale;
        }

        public bool IsPaused()
        {
            return isPaused;
        }
    }
}
