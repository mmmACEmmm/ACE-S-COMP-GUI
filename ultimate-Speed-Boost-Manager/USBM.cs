using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using GorillaLocomotion;
using UnityEngine;
using Valve.VR;

namespace GorillaTagMods
{
    public class USBManager
    {
        // Default settings
        private static class Defaults
        {
            public const float DefaultMultiplier = 1.2f;
            public const float DefaultMaxSpeed = 8f;
        }

        // Settings dictionary
        private Dictionary<string, BoostSettings> allSettings;

        // Config reference
        private ConfigFile config;

        // Expanded sections tracking
        private Dictionary<string, bool> expandedSections = new Dictionary<string, bool>();

        // Scroll position tracking
        private Vector2 scrollPosition = Vector2.zero;

        // Constructor
        public USBManager(ConfigFile config)
        {
            this.config = config;
            InitializeSettings();
        }

        private void InitializeSettings()
        {
            allSettings = new Dictionary<string, BoostSettings>
            {
                {"Left Primary", LoadOrCreateSettings("LeftPrimary")},
                {"Left Secondary", LoadOrCreateSettings("LeftSecondary")},
                {"Left Grab", LoadOrCreateSettings("LeftGrab")},
                {"Left Joystick", LoadOrCreateSettings("LeftJoystick")},
                {"Right Primary", LoadOrCreateSettings("RightPrimary")},
                {"Right Secondary", LoadOrCreateSettings("RightSecondary")},
                {"Right Grab", LoadOrCreateSettings("RightGrab")},
                {"Right Joystick", LoadOrCreateSettings("RightJoystick")},
                {"Defaults", LoadOrCreateSettings("Defaults")}
            };
        }

        private BoostSettings LoadOrCreateSettings(string settingName)
        {
            float multiplier = config.Bind("Multipliers", settingName, Defaults.DefaultMultiplier, "Multiplier for " + settingName).Value;
            float maxSpeed = config.Bind("MaxSpeeds", settingName, Defaults.DefaultMaxSpeed, "Max speed for " + settingName).Value;
            return new BoostSettings(multiplier, maxSpeed);
        }

        public void SaveSettings()
        {
            foreach (var kvp in allSettings)
            {
                config.Bind("Multipliers", kvp.Key.Replace(" ", ""), Defaults.DefaultMultiplier, "Multiplier for " + kvp.Key).Value = kvp.Value.Multiplier;
                config.Bind("MaxSpeeds", kvp.Key.Replace(" ", ""), Defaults.DefaultMaxSpeed, "Max speed for " + kvp.Key).Value = kvp.Value.MaxSpeed;
            }
        }

        public void LoadSettings()
        {
            foreach (var kvp in allSettings)
            {
                string key = kvp.Key.Replace(" ", "");
                kvp.Value.Multiplier = config.Bind("Multipliers", key, Defaults.DefaultMultiplier, "Multiplier for " + kvp.Key).Value;
                kvp.Value.MaxSpeed = config.Bind("MaxSpeeds", key, Defaults.DefaultMaxSpeed, "Max speed for " + kvp.Key).Value;
            }
        }

        // Draw the GUI for the speed boost manager
        public Vector2 DrawGUI(
            GUIStyle boxStyle,
            GUIStyle scrollViewStyle,
            GUIStyle subHeaderStyle,
            GUIStyle dividerStyle,
            GUIStyle buttonStyle,
            GUIStyle expandedButtonStyle,
            GUIStyle sliderLabelStyle,
            GUIStyle sliderValueStyle,
            GUIStyle actionButtonStyle,
            Vector2 inScrollPosition)
        {
            // Begin scroll view with custom style
            GUILayout.BeginVertical(boxStyle);
            scrollPosition = GUILayout.BeginScrollView(inScrollPosition, scrollViewStyle, GUILayout.Height(260));

            // Group settings into categories
            GUILayout.Label("Left Controller", subHeaderStyle);
            DrawControlSetButton("Left Primary", allSettings["Left Primary"], buttonStyle, expandedButtonStyle,
                               boxStyle, sliderLabelStyle, sliderValueStyle);
            DrawControlSetButton("Left Secondary", allSettings["Left Secondary"], buttonStyle, expandedButtonStyle,
                               boxStyle, sliderLabelStyle, sliderValueStyle);
            DrawControlSetButton("Left Grab", allSettings["Left Grab"], buttonStyle, expandedButtonStyle,
                               boxStyle, sliderLabelStyle, sliderValueStyle);
            DrawControlSetButton("Left Joystick", allSettings["Left Joystick"], buttonStyle, expandedButtonStyle,
                               boxStyle, sliderLabelStyle, sliderValueStyle);

            GUILayout.Space(10);
            GUILayout.Box("", dividerStyle);

            GUILayout.Label("Right Controller", subHeaderStyle);
            DrawControlSetButton("Right Primary", allSettings["Right Primary"], buttonStyle, expandedButtonStyle,
                               boxStyle, sliderLabelStyle, sliderValueStyle);
            DrawControlSetButton("Right Secondary", allSettings["Right Secondary"], buttonStyle, expandedButtonStyle,
                               boxStyle, sliderLabelStyle, sliderValueStyle);
            DrawControlSetButton("Right Grab", allSettings["Right Grab"], buttonStyle, expandedButtonStyle,
                               boxStyle, sliderLabelStyle, sliderValueStyle);
            DrawControlSetButton("Right Joystick", allSettings["Right Joystick"], buttonStyle, expandedButtonStyle,
                               boxStyle, sliderLabelStyle, sliderValueStyle);

            GUILayout.Space(10);
            GUILayout.Box("", dividerStyle);

            GUILayout.Label("General", subHeaderStyle);
            DrawControlSetButton("Defaults", allSettings["Defaults"], buttonStyle, expandedButtonStyle,
                               boxStyle, sliderLabelStyle, sliderValueStyle);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            // Reset buttons
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset All to Defaults", actionButtonStyle))
            {
                ResetToDefaults();
            }
            GUILayout.EndHorizontal();

            // Return the current scroll position
            return scrollPosition;
        }

        private void DrawControlSetButton(
            string label,
            BoostSettings settings,
            GUIStyle buttonStyle,
            GUIStyle expandedButtonStyle,
            GUIStyle boxStyle,
            GUIStyle sliderLabelStyle,
            GUIStyle sliderValueStyle)
        {
            // Initialize section state if needed
            if (!expandedSections.ContainsKey(label))
            {
                expandedSections[label] = false;
            }

            // Use the appropriate style based on expanded state
            GUIStyle currentButtonStyle = expandedSections[label] ? expandedButtonStyle : buttonStyle;

            GUILayout.BeginVertical(boxStyle);

            // Button with arrow indicator
            string buttonText = expandedSections[label] ? "▼ " + label : "► " + label;
            if (GUILayout.Button(buttonText, currentButtonStyle))
            {
                expandedSections[label] = !expandedSections[label];
            }

            if (expandedSections[label])
            {
                DrawControlSetSliders(label, settings, sliderLabelStyle, sliderValueStyle, buttonStyle);
            }

            GUILayout.EndVertical();
            GUILayout.Space(3);
        }

        private void DrawControlSetSliders(
            string label,
            BoostSettings settings,
            GUIStyle sliderLabelStyle,
            GUIStyle sliderValueStyle,
            GUIStyle buttonStyle)
        {
            // Max Speed slider with value display
            GUILayout.BeginHorizontal();
            GUILayout.Label("Max Speed:", sliderLabelStyle, GUILayout.Width(100));
            GUILayout.Label($"{settings.MaxSpeed:F2}", sliderValueStyle);
            GUILayout.EndHorizontal();

            settings.MaxSpeed = GUILayout.HorizontalSlider(settings.MaxSpeed, 0f, 20f);

            // Multiplier slider with value display
            GUILayout.BeginHorizontal();
            GUILayout.Label("Multiplier:", sliderLabelStyle, GUILayout.Width(100));
            GUILayout.Label($"{settings.Multiplier:F2}", sliderValueStyle);
            GUILayout.EndHorizontal();

            settings.Multiplier = GUILayout.HorizontalSlider(settings.Multiplier, 0f, 3f);

            // Quick preset buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Default", buttonStyle, GUILayout.Width(80)))
            {
                settings.MaxSpeed = Defaults.DefaultMaxSpeed;
                settings.Multiplier = Defaults.DefaultMultiplier;
            }
            if (GUILayout.Button("Low", buttonStyle, GUILayout.Width(60)))
            {
                settings.MaxSpeed = 5f;
                settings.Multiplier = 0.8f;
            }
            if (GUILayout.Button("Medium", buttonStyle, GUILayout.Width(80)))
            {
                settings.MaxSpeed = 10f;
                settings.Multiplier = 1.5f;
            }
            if (GUILayout.Button("High", buttonStyle, GUILayout.Width(60)))
            {
                settings.MaxSpeed = 15f;
                settings.Multiplier = 2.0f;
            }
            GUILayout.EndHorizontal();
        }

        public void ApplySettings()
        {
            if (GTPlayer.Instance == null) return;

            BoostSettings activeSettings = GetActiveBoostSettings();
            GTPlayer.Instance.maxJumpSpeed = activeSettings.MaxSpeed;
            GTPlayer.Instance.jumpMultiplier = activeSettings.Multiplier;

            // Update active state
            foreach (var settings in allSettings.Values)
            {
                settings.IsActive = false;
            }
            activeSettings.IsActive = true;
        }

        private BoostSettings GetActiveBoostSettings()
        {
            if (SteamVR_Actions.gorillaTag_LeftJoystickClick.state) return allSettings["Left Joystick"];
            if (SteamVR_Actions.gorillaTag_RightJoystickClick.state) return allSettings["Right Joystick"];
            if (ControllerInputPoller.instance.leftControllerPrimaryButton) return allSettings["Left Primary"];
            if (ControllerInputPoller.instance.leftControllerSecondaryButton) return allSettings["Left Secondary"];
            if (ControllerInputPoller.instance.leftGrab) return allSettings["Left Grab"];
            if (ControllerInputPoller.instance.rightControllerPrimaryButton) return allSettings["Right Primary"];
            if (ControllerInputPoller.instance.rightControllerSecondaryButton) return allSettings["Right Secondary"];
            if (ControllerInputPoller.instance.rightGrab) return allSettings["Right Grab"];

            return allSettings["Defaults"];
        }

        public void ResetToDefaults()
        {
            foreach (var kvp in allSettings)
            {
                kvp.Value.Multiplier = Defaults.DefaultMultiplier;
                kvp.Value.MaxSpeed = Defaults.DefaultMaxSpeed;
            }
        }

        public class BoostSettings
        {
            public float Multiplier { get; set; }
            public float MaxSpeed { get; set; }

            // Added to track active state
            public bool IsActive { get; set; }

            public BoostSettings(float multiplier, float maxSpeed)
            {
                Multiplier = multiplier;
                MaxSpeed = maxSpeed;
                IsActive = false;
            }
        }
    }
}