using System;
using BepInEx.Configuration;
using GorillaLocomotion;
using UnityEngine;

namespace GorillaTagMods
{
    public class PredictionManager
    {
        // Settings
        private float colorChangeSpeed = 0.8f;
        private static float predictionStrength = 0f;
        private static float smoothingFactor = 0.1f;

        // Tracking variables
        private static Vector3 lastLeftPosition;
        private static Vector3 lastRightPosition;
        private static Vector3 playerVelocity;
        private static Vector3 lastLeftVelocity;
        private static Vector3 lastRightVelocity;
        private static float deltaTime;

        // Config reference
        private ConfigFile config;

        // Constructor
        public PredictionManager(ConfigFile config)
        {
            this.config = config;

            // Initialize tracking vectors
            lastLeftPosition = Vector3.zero;
            lastRightPosition = Vector3.zero;
            lastLeftVelocity = Vector3.zero;
            lastRightVelocity = Vector3.zero;
        }

        public void LoadSettings()
        {
            // Load prediction settings
            predictionStrength = config.Bind("Prediction", "Strength", 0f, "Prediction strength").Value;
            smoothingFactor = config.Bind("Prediction", "SmoothingFactor", 0.1f, "Smoothing factor").Value;
        }

        public void SaveSettings()
        {
            // Save prediction settings
            config.Bind("Prediction", "Strength", predictionStrength, "Prediction strength").Value = predictionStrength;
            config.Bind("Prediction", "SmoothingFactor", smoothingFactor, "Smoothing factor").Value = smoothingFactor;
        }

        public void DrawGUI(
            GUIStyle headerStyle,
            GUIStyle subHeaderStyle,
            GUIStyle boxStyle,
            GUIStyle sliderLabelStyle,
            GUIStyle sliderValueStyle,
            GUIStyle buttonStyle,
            GUIStyle dividerStyle)
        {
            // Get rainbow color effect
            float time = Time.time * colorChangeSpeed;
            float red = Mathf.Abs(Mathf.Sin(time));
            float green = Mathf.Abs(Mathf.Sin(time + 2.094f));
            float blue = Mathf.Abs(Mathf.Sin(time + 4.188f));
            Color rainbowColor = new Color(red, green, blue);

            GUI.contentColor = rainbowColor;
            GUILayout.Label("Prediction Settings", headerStyle);
            GUI.contentColor = Color.white;

            GUILayout.Label("Configure prediction strength and presets", subHeaderStyle);

            GUILayout.Space(10);

            // Main settings in a box
            GUILayout.BeginVertical(boxStyle);

            // Prediction strength with value display
            GUILayout.BeginHorizontal();
            GUILayout.Label("Prediction Strength:", sliderLabelStyle, GUILayout.Width(150));
            GUILayout.Label($"{predictionStrength:F1}", sliderValueStyle);
            GUILayout.EndHorizontal();

            predictionStrength = GUILayout.HorizontalSlider(predictionStrength, 0f, 100f);

            // Smoothing factor with value display
            GUILayout.BeginHorizontal();
            GUILayout.Label("Smoothing Factor:", sliderLabelStyle, GUILayout.Width(150));
            GUILayout.Label($"{smoothingFactor:F2}", sliderValueStyle);
            GUILayout.EndHorizontal();

            smoothingFactor = GUILayout.HorizontalSlider(smoothingFactor, 0.01f, 0.5f);

            GUILayout.EndVertical();

            GUILayout.Space(15);

            // Presets section
            GUILayout.Label("Quick Presets", subHeaderStyle);

            // Preset buttons with improved styling
            GUILayout.BeginVertical(boxStyle);

            // First row
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Off", buttonStyle))
            {
                predictionStrength = 0f;
            }
            if (GUILayout.Button("Very Low", buttonStyle))
            {
                predictionStrength = 1.0f;
            }
            if (GUILayout.Button("Low", buttonStyle))
            {
                predictionStrength = 2.1f;
            }
            GUILayout.EndHorizontal();

            // Second row
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Pico", buttonStyle))
            {
                predictionStrength = 6f;
            }
            if (GUILayout.Button("RiftS", buttonStyle))
            {
                predictionStrength = 7.34f;
            }
            if (GUILayout.Button("Valve", buttonStyle))
            {
                predictionStrength = 4.92f;
            }
            GUILayout.EndHorizontal();

            // Third row
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Medium", buttonStyle))
            {
                predictionStrength = 15f;
            }
            if (GUILayout.Button("High", buttonStyle))
            {
                predictionStrength = 30f;
            }
            if (GUILayout.Button("Extreme", buttonStyle))
            {
                predictionStrength = 50f;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            // Status indicator
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Status:", GUILayout.Width(80));
            if (predictionStrength <= 0.01f)
            {
                GUI.contentColor = Color.gray;
                GUILayout.Label("OFF");
            }
            else if (predictionStrength < 5f)
            {
                GUI.contentColor = Color.green;
                GUILayout.Label("ACTIVE - Low");
            }
            else if (predictionStrength < 20f)
            {
                GUI.contentColor = Color.yellow;
                GUILayout.Label("ACTIVE - Medium");
            }
            else
            {
                GUI.contentColor = Color.red;
                GUILayout.Label("ACTIVE - High");
            }
            GUI.contentColor = Color.white;
            GUILayout.EndHorizontal();
        }

        public void ApplySettings()
        {
            if (GTPlayer.Instance == null) return;
            if (predictionStrength <= 0.01f) return;

            ApplyPrediction();
        }

        private static void ApplyPrediction()
        {
            deltaTime = Time.deltaTime;
            playerVelocity = GTPlayer.Instance.AveragedVelocity;
            Vector3 leftPosition = GTPlayer.Instance.leftControllerTransform.position;
            Vector3 rightPosition = GTPlayer.Instance.rightControllerTransform.position;
            Vector3 leftVelocity = ((leftPosition - lastLeftPosition) / deltaTime - playerVelocity) * smoothingFactor + lastLeftVelocity * (1f - smoothingFactor);
            Vector3 rightVelocity = ((rightPosition - lastRightPosition) / deltaTime - playerVelocity) * smoothingFactor + lastRightVelocity * (1f - smoothingFactor);
            GTPlayer.Instance.leftControllerTransform.position = PredictFuturePosition(GTPlayer.Instance.leftControllerTransform, leftVelocity);
            GTPlayer.Instance.rightControllerTransform.position = PredictFuturePosition(GTPlayer.Instance.rightControllerTransform, rightVelocity);
            lastLeftPosition = leftPosition;
            lastRightPosition = rightPosition;
            lastLeftVelocity = leftVelocity;
            lastRightVelocity = rightVelocity;
        }

        private static Vector3 PredictFuturePosition(Transform target, Vector3 velocity)
        {
            return target.position + velocity * (predictionStrength * 0.005f);
        }

        public void ResetSettings()
        {
            predictionStrength = 0f;
            smoothingFactor = 0.1f;
        }
    }
}