using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using GorillaLocomotion;
using UnityEngine;
using Valve.VR;
using UnityEngine.InputSystem;

namespace GorillaTagMods
{
    [BepInPlugin("com.ace.gorillatag.combinedmods", "Gorilla Tag Combined Mods", "1.2.0")]
    public class CombinedMods : BaseUnityPlugin
    {
        #region Speed Boost Settings
        private static class Defaults
        {
            public const float DefaultMultiplier = 1.2f;
            public const float DefaultMaxSpeed = 8f;
        }

        private Dictionary<string, BoostSettings> allSettings;
        private ConfigFile config;

        // Input action reference for the key press
        private InputAction toggleGuiAction;

        private bool showGUI = true;
        private Rect mainWindowRect = new Rect(20, 20, 450, 500);
        private readonly Dictionary<string, bool> expandedSections = new Dictionary<string, bool>();

        // Main GUI state
        private enum TabState { Predictions, SpeedBoost }
        private TabState currentTab = TabState.Predictions;

        // GUI Styles - will be initialized in OnGUI
        private GUIStyle windowStyle;
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle buttonStyle;
        private GUIStyle tabButtonStyle;
        private GUIStyle activeTabButtonStyle;
        private GUIStyle expandedButtonStyle;
        private GUIStyle sliderLabelStyle;
        private GUIStyle sliderValueStyle;
        private GUIStyle actionButtonStyle;
        private GUIStyle dividerStyle;
        private GUIStyle boxStyle;
        private GUIStyle scrollViewStyle;
        private GUIStyle toggleStyle;
        private bool stylesInitialized = false;

        // Color themes
        private Color primaryColor = new Color(0.2f, 0.6f, 1f);
        private Color secondaryColor = new Color(0.1f, 0.4f, 0.8f);
        private Color accentColor = new Color(0.8f, 0.3f, 1f);
        private Color backgroundColor = new Color(0.12f, 0.14f, 0.18f, 0.95f);
        private Color textColor = new Color(0.9f, 0.9f, 0.9f);
        private Color highlightColor = new Color(0.3f, 0.7f, 1f);

        // Scrolling
        private Vector2 scrollPosition;
        #endregion

        #region Prediction Mod Settings
        private float colorChangeSpeed = 0.8f;
        private static float predictionStrength = 0f;
        private static float smoothingFactor = 0.1f;
        private static Vector3 lastLeftPosition;
        private static Vector3 lastRightPosition;
        private static Vector3 playerVelocity;
        private static Vector3 lastLeftVelocity;
        private static Vector3 lastRightVelocity;
        private static float deltaTime;
        private bool showHelp = false;
        #endregion

        private void Awake()
        {
            config = new ConfigFile(Path.Combine(Paths.ConfigPath, "CombinedMods.cfg"), true);
            InitializeSettings();
            LoadSettings(); // Make sure to load saved settings at startup

            // Set up input action for GUI toggle
            toggleGuiAction = new InputAction("ToggleGUI", binding: "<Keyboard>/tab");
            toggleGuiAction.performed += ctx => ToggleGUIVisibility();
            toggleGuiAction.Enable();

            // Log initialization
            Debug.Log("Gorilla Tag Combined Mods initialized successfully!");
        }

        private void OnDestroy()
        {
            // Clean up input actions
            if (toggleGuiAction != null)
            {
                toggleGuiAction.Disable();
                toggleGuiAction.Dispose();
            }
        }

        private void InitializeGUIStyles()
        {
            // Only initialize once and only within OnGUI
            if (stylesInitialized) return;

            windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.normal.background = CreateColorTexture(backgroundColor);
            windowStyle.normal.textColor = textColor;
            windowStyle.fontStyle = FontStyle.Bold;
            windowStyle.fontSize = 16;
            windowStyle.alignment = TextAnchor.UpperCenter;
            windowStyle.padding = new RectOffset(15, 15, 25, 15);
            windowStyle.border = new RectOffset(10, 10, 10, 10);

            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 18;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = highlightColor;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.margin = new RectOffset(0, 0, 8, 12);

            subHeaderStyle = new GUIStyle(headerStyle);
            subHeaderStyle.fontSize = 14;
            subHeaderStyle.normal.textColor = textColor;
            subHeaderStyle.margin = new RectOffset(0, 0, 5, 8);

            // Tab button styles
            tabButtonStyle = new GUIStyle(GUI.skin.button);
            tabButtonStyle.normal.background = CreateColorTexture(new Color(0.15f, 0.15f, 0.2f, 0.8f));
            tabButtonStyle.hover.background = CreateColorTexture(new Color(0.25f, 0.25f, 0.3f, 0.9f));
            tabButtonStyle.normal.textColor = textColor;
            tabButtonStyle.fontSize = 16;
            tabButtonStyle.fontStyle = FontStyle.Bold;
            tabButtonStyle.alignment = TextAnchor.MiddleCenter;
            tabButtonStyle.margin = new RectOffset(5, 5, 0, 10);
            tabButtonStyle.padding = new RectOffset(10, 10, 8, 8);
            tabButtonStyle.border = new RectOffset(5, 5, 5, 5);

            activeTabButtonStyle = new GUIStyle(tabButtonStyle);
            activeTabButtonStyle.normal.background = CreateColorTexture(primaryColor);
            activeTabButtonStyle.hover.background = CreateColorTexture(new Color(primaryColor.r + 0.1f, primaryColor.g + 0.1f, primaryColor.b + 0.1f, 0.9f));
            activeTabButtonStyle.normal.textColor = Color.white;

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.background = CreateColorTexture(new Color(0.18f, 0.2f, 0.25f, 0.9f));
            buttonStyle.hover.background = CreateColorTexture(new Color(0.25f, 0.28f, 0.33f, 0.9f));
            buttonStyle.active.background = CreateColorTexture(primaryColor);
            buttonStyle.normal.textColor = textColor;
            buttonStyle.fontSize = 13;
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.margin = new RectOffset(5, 5, 3, 3);
            buttonStyle.padding = new RectOffset(8, 8, 6, 6);
            buttonStyle.border = new RectOffset(4, 4, 4, 4);

            expandedButtonStyle = new GUIStyle(buttonStyle);
            expandedButtonStyle.normal.background = CreateColorTexture(secondaryColor);
            expandedButtonStyle.hover.background = CreateColorTexture(new Color(secondaryColor.r + 0.1f, secondaryColor.g + 0.1f, secondaryColor.b + 0.1f, 0.9f));
            expandedButtonStyle.normal.textColor = Color.white;

            sliderLabelStyle = new GUIStyle(GUI.skin.label);
            sliderLabelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            sliderLabelStyle.fontSize = 12;
            sliderLabelStyle.margin = new RectOffset(10, 10, 5, 2);

            sliderValueStyle = new GUIStyle(sliderLabelStyle);
            sliderValueStyle.normal.textColor = highlightColor;
            sliderValueStyle.fontStyle = FontStyle.Bold;
            sliderValueStyle.alignment = TextAnchor.MiddleRight;

            actionButtonStyle = new GUIStyle(buttonStyle);
            actionButtonStyle.normal.background = CreateColorTexture(new Color(0.2f, 0.5f, 0.3f, 0.9f));
            actionButtonStyle.hover.background = CreateColorTexture(new Color(0.3f, 0.6f, 0.4f, 0.9f));
            actionButtonStyle.active.background = CreateColorTexture(new Color(0.4f, 0.7f, 0.5f, 0.9f));
            actionButtonStyle.fontSize = 14;

            dividerStyle = new GUIStyle();
            dividerStyle.normal.background = CreateColorTexture(new Color(0.3f, 0.3f, 0.4f, 0.5f));
            dividerStyle.margin = new RectOffset(0, 0, 8, 8);
            dividerStyle.fixedHeight = 1;

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = CreateColorTexture(new Color(0.15f, 0.17f, 0.2f, 0.8f));
            boxStyle.padding = new RectOffset(10, 10, 10, 10);
            boxStyle.margin = new RectOffset(5, 5, 5, 5);
            boxStyle.border = new RectOffset(3, 3, 3, 3);

            scrollViewStyle = new GUIStyle(GUI.skin.scrollView);
            scrollViewStyle.normal.background = CreateColorTexture(new Color(0.12f, 0.14f, 0.18f, 0.6f));

            toggleStyle = new GUIStyle(GUI.skin.toggle);
            toggleStyle.normal.textColor = textColor;
            toggleStyle.fontSize = 12;
            toggleStyle.margin = new RectOffset(5, 5, 5, 5);

            // Customize scroll bars
            GUI.skin.verticalScrollbar.normal.background = CreateColorTexture(new Color(0.2f, 0.22f, 0.25f, 0.8f));
            GUI.skin.verticalScrollbarThumb.normal.background = CreateColorTexture(new Color(0.3f, 0.35f, 0.4f, 0.8f));
            GUI.skin.horizontalScrollbar.normal.background = CreateColorTexture(new Color(0.2f, 0.22f, 0.25f, 0.8f));
            GUI.skin.horizontalScrollbarThumb.normal.background = CreateColorTexture(new Color(0.3f, 0.35f, 0.4f, 0.8f));

            // Customize sliders
            GUI.skin.horizontalSlider.normal.background = CreateColorTexture(new Color(0.2f, 0.22f, 0.25f, 0.8f));
            GUI.skin.horizontalSliderThumb.normal.background = CreateColorTexture(primaryColor);

            stylesInitialized = true;
        }

        private Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        #region Speed Boost Methods
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

        private void SaveSettings()
        {
            foreach (var kvp in allSettings)
            {
                config.Bind("Multipliers", kvp.Key.Replace(" ", ""), kvp.Value.Multiplier, "Multiplier for " + kvp.Key).Value = kvp.Value.Multiplier;
                config.Bind("MaxSpeeds", kvp.Key.Replace(" ", ""), kvp.Value.MaxSpeed, "Max speed for " + kvp.Key).Value = kvp.Value.MaxSpeed;
            }

            // Save prediction settings
            config.Bind("Prediction", "Strength", predictionStrength, "Prediction strength").Value = predictionStrength;
            config.Bind("Prediction", "SmoothingFactor", smoothingFactor, "Smoothing factor").Value = smoothingFactor;
            config.Bind("UI", "ActiveTab", (int)currentTab, "Active tab index").Value = (int)currentTab;
            config.Bind("UI", "ShowGUI", showGUI, "Show GUI").Value = showGUI;

            config.Save();
            Debug.Log("Settings saved successfully!");
        }

        private void LoadSettings()
        {
            foreach (var kvp in allSettings)
            {
                string key = kvp.Key.Replace(" ", "");
                kvp.Value.Multiplier = config.Bind("Multipliers", key, Defaults.DefaultMultiplier, "Multiplier for " + kvp.Key).Value;
                kvp.Value.MaxSpeed = config.Bind("MaxSpeeds", key, Defaults.DefaultMaxSpeed, "Max speed for " + kvp.Key).Value;
            }

            // Load prediction settings
            predictionStrength = config.Bind("Prediction", "Strength", 0f, "Prediction strength").Value;
            smoothingFactor = config.Bind("Prediction", "SmoothingFactor", 0.1f, "Smoothing factor").Value;
            currentTab = (TabState)config.Bind("UI", "ActiveTab", 0, "Active tab index").Value;

            Debug.Log("Settings loaded successfully!");
        }

        private void DrawMainWindow(int windowID)
        {
            // Top title bar with version and help button
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Gorilla Tag Mods v1.2.0", headerStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(showHelp ? "×" : "?", GUILayout.Width(30), GUILayout.Height(30)))
            {
                showHelp = !showHelp;
            }
            GUILayout.EndHorizontal();

            // Help section if toggled
            if (showHelp)
            {
                GUILayout.BeginVertical(boxStyle);
                GUILayout.Label("Quick Help:", subHeaderStyle);
                GUILayout.Label("• Press TAB to show/hide this GUI");
                GUILayout.Label("• Use the tabs to switch between mods");
                GUILayout.Label("• Each tab has its own settings");
                GUILayout.Label("• Remember to SAVE your settings!");
                GUILayout.EndVertical();

                // Draw divider
                GUILayout.Box("", dividerStyle);
            }

            // Tab buttons at the top
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Predictions", currentTab == TabState.Predictions ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(40), GUILayout.Width(210)))
            {
                currentTab = TabState.Predictions;
            }

            if (GUILayout.Button("Speed Boost", currentTab == TabState.SpeedBoost ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(40), GUILayout.Width(210)))
            {
                currentTab = TabState.SpeedBoost;
            }

            GUILayout.EndHorizontal();

            // Draw divider
            GUILayout.Box("", dividerStyle);

            // Content based on active tab
            if (currentTab == TabState.Predictions)
            {
                DrawPredictionContent();
            }
            else
            {
                DrawSpeedBoostContent();
            }

            // Save & Hide buttons at the bottom
            GUILayout.FlexibleSpace();
            GUILayout.Box("", dividerStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Settings", actionButtonStyle, GUILayout.Height(35)))
            {
                SaveSettings();
            }

            if (GUILayout.Button("Hide GUI", buttonStyle, GUILayout.Height(35)))
            {
                showGUI = false;
            }
            GUILayout.EndHorizontal();

            // Status bar
            GUILayout.Box("Press TAB to toggle this GUI anytime", subHeaderStyle);

            GUI.DragWindow(new Rect(0, 0, 10000, 30));
        }

        private void DrawSpeedBoostContent()
        {
            GUILayout.Label("Ultimate Speed Boost Manager", headerStyle);
            GUILayout.Label("Configure speed settings for different buttons", subHeaderStyle);

            // Begin scroll view with custom style
            GUILayout.BeginVertical(boxStyle);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, scrollViewStyle, GUILayout.Height(260));

            // Group settings into categories
            GUILayout.Label("Left Controller", subHeaderStyle);
            DrawControlSetButton("Left Primary", allSettings["Left Primary"]);
            DrawControlSetButton("Left Secondary", allSettings["Left Secondary"]);
            DrawControlSetButton("Left Grab", allSettings["Left Grab"]);
            DrawControlSetButton("Left Joystick", allSettings["Left Joystick"]);

            GUILayout.Space(10);
            GUILayout.Box("", dividerStyle);

            GUILayout.Label("Right Controller", subHeaderStyle);
            DrawControlSetButton("Right Primary", allSettings["Right Primary"]);
            DrawControlSetButton("Right Secondary", allSettings["Right Secondary"]);
            DrawControlSetButton("Right Grab", allSettings["Right Grab"]);
            DrawControlSetButton("Right Joystick", allSettings["Right Joystick"]);

            GUILayout.Space(10);
            GUILayout.Box("", dividerStyle);

            GUILayout.Label("General", subHeaderStyle);
            DrawControlSetButton("Defaults", allSettings["Defaults"]);

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
        }

        private void DrawPredictionContent()
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

        private void DrawControlSetButton(string label, BoostSettings settings)
        {
            expandedSections.TryAdd(label, false);

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
                DrawControlSetSliders(label, settings);
            }

            GUILayout.EndVertical();
            GUILayout.Space(3);
        }

        private void DrawControlSetSliders(string label, BoostSettings settings)
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

        private void ApplyBoostSettings()
        {
            if (GTPlayer.Instance == null) return;

            BoostSettings activeSettings = GetActiveBoostSettings();
            GTPlayer.Instance.maxJumpSpeed = activeSettings.MaxSpeed;
            GTPlayer.Instance.jumpMultiplier = activeSettings.Multiplier;
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

        private void ResetToDefaults()
        {
            foreach (var kvp in allSettings)
            {
                kvp.Value.Multiplier = Defaults.DefaultMultiplier;
                kvp.Value.MaxSpeed = Defaults.DefaultMaxSpeed;
            }
            predictionStrength = 0f;
            smoothingFactor = 0.1f;
            SaveSettings();
        }

        private void ToggleGUIVisibility()
        {
            showGUI = !showGUI;
            Debug.Log($"GUI visibility toggled. ShowGUI: {showGUI}");
        }
        #endregion

        #region Prediction Mod Methods
        private static void ApplyPrediction()
        {
            if (GTPlayer.Instance == null) return;
            if (predictionStrength <= 0.01f) return;

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
        #endregion

        private void OnGUI()
        {
            if (!showGUI) return;

            // Initialize styles within OnGUI to avoid errors
            InitializeGUIStyles();

            // Draw main tabbed window with drop shadow effect
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.Box(new Rect(mainWindowRect.x + 5, mainWindowRect.y + 5, mainWindowRect.width, mainWindowRect.height), "");
            GUI.color = Color.white;

            mainWindowRect = GUILayout.Window(0, mainWindowRect, DrawMainWindow, "", windowStyle);
        }

        private void Update()
        {
            // Check for GUI toggle with Tab key (handled by input system)

            // Apply speed boost settings when appropriate
            if (GTPlayer.Instance != null)
            {
                ApplyBoostSettings();
            }

            // Apply prediction if enabled
            if (predictionStrength > 0.01f)
            {
                ApplyPrediction();
            }
        }

        // Draw a nice animated background on the window
        private void DrawWindowBackground(Rect windowRect)
        {
            // Draw a subtle animated background pattern
            float time = Time.time * 0.3f;
            Color bgColor1 = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0.4f);
            Color bgColor2 = new Color(backgroundColor.r + 0.05f, backgroundColor.g + 0.05f, backgroundColor.b + 0.05f, 0.4f);

            int gridSize = 20;
            int offsetX = (int)(Mathf.Sin(time) * 5);
            int offsetY = (int)(Mathf.Cos(time) * 5);

            for (int x = 0; x < windowRect.width; x += gridSize)
            {
                for (int y = 0; y < windowRect.height; y += gridSize)
                {
                    if ((x + y) % (gridSize * 2) == 0)
                    {
                        GUI.DrawTexture(
                            new Rect(windowRect.x + x + offsetX, windowRect.y + y + offsetY, gridSize, gridSize),
                            CreateColorTexture(bgColor1)
                        );
                    }
                    else
                    {
                        GUI.DrawTexture(
                            new Rect(windowRect.x + x + offsetX, windowRect.y + y + offsetY, gridSize, gridSize),
                            CreateColorTexture(bgColor2)
                        );
                    }
                }
            }
        }

        private class BoostSettings
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