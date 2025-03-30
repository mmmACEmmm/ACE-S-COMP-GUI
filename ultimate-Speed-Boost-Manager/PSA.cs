using System;
using BepInEx.Configuration;
using GorillaLocomotion;
using UnityEngine;

namespace GorillaTagMods
{
    public class SpeedManager
    {
        // Speed settings
        private float movementSpeed = 10f;

        // UI state
        private bool isVisible = true;
        private float guiFade = 1f;

        // Config reference
        private ConfigFile config;

        // Constructor
        public SpeedManager(ConfigFile config)
        {
            this.config = config;
        }

        public void LoadSettings()
        {
            movementSpeed = config.Bind("Speed", "MovementSpeed", 10f, "Forward movement speed").Value;
            isVisible = config.Bind("Speed", "IsVisible", true, "Whether Speed UI is visible").Value;
        }

        public void SaveSettings()
        {
            config.Bind("Speed", "MovementSpeed", 10f, "Forward movement speed").Value = movementSpeed;
            config.Bind("Speed", "IsVisible", true, "Whether Speed UI is visible").Value = isVisible;
        }

        public void ToggleVisibility()
        {
            isVisible = !isVisible;
        }

        public void DrawGUI(
            GUIStyle boxStyle,
            GUIStyle subHeaderStyle,
            GUIStyle buttonStyle,
            GUIStyle sliderLabelStyle,
            GUIStyle sliderValueStyle,
            GUIStyle actionButtonStyle)
        {
            // Calculate fade effect for the mini GUI
            if (!isVisible && guiFade > 0f)
            {
                guiFade -= Time.deltaTime;
            }
            else if (isVisible && guiFade < 1f)
            {
                guiFade += Time.deltaTime;
            }

            guiFade = Mathf.Clamp01(guiFade);

            // Main integrated settings UI
            GUILayout.BeginVertical(boxStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Movement Speed:", sliderLabelStyle);
            GUILayout.Label($"{movementSpeed:F1}", sliderValueStyle);
            GUILayout.EndHorizontal();

            movementSpeed = GUILayout.HorizontalSlider(movementSpeed, 1f, 20f);

            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Decrease", buttonStyle))
            {
                movementSpeed = Mathf.Max(1f, movementSpeed - 1f);
            }

            if (GUILayout.Button("Increase", buttonStyle))
            {
                movementSpeed = Mathf.Min(20f, movementSpeed + 1f);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset (10.0)", actionButtonStyle))
            {
                movementSpeed = 10f;
            }

            if (GUILayout.Button("Legit (6.7)", actionButtonStyle))
            {
                movementSpeed = 6.7f;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.Label("Press P to toggle mini speed bar visibility", subHeaderStyle);

            GUILayout.EndVertical();
        }

        public void DrawMiniGUI()
        {
            // Only draw if visible or fading
            if (!isVisible && guiFade <= 0f)
                return;

            // Apply fade effect
            GUI.color = new Color(1f, 1f, 1f, guiFade);

            // Draw the mini GUI at the top of the screen
            GUILayout.BeginHorizontal("Box");

            if (GUILayout.Button("<b> - </b>"))
            {
                movementSpeed -= 1f;
            }

            GUILayout.Label("<b>Speed: </b>" + movementSpeed.ToString("F1"));

            if (GUILayout.Button("<b> + </b>"))
            {
                movementSpeed += 1f;
            }

            if (GUILayout.Button("<b> Reset </b>"))
            {
                movementSpeed = 10f;
            }

            if (GUILayout.Button("<b> Legit </b>"))
            {
                movementSpeed = 6.7f;
            }

            GUILayout.EndHorizontal();

            // Reset color
            GUI.color = Color.white;
        }

        public void ApplySettings()
        {
            // Apply speed boost when right primary button is pressed
            if (GTPlayer.Instance != null && ControllerInputPoller.instance.rightControllerPrimaryButton)
            {
                GTPlayer.Instance.transform.position += GTPlayer.Instance.bodyCollider.transform.forward * movementSpeed * Time.deltaTime;
            }
        }
    }
}