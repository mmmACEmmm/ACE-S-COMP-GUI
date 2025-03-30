using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using GorillaLocomotion;
using GorillaNetworking;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GorillaTagMods
{
    [BepInPlugin("com.ace.gorillatag.combinedmods", "ace's comp gui", "1.2.1")]
    public class CombinedMods : BaseUnityPlugin
    {
        // Instance references to other components
        public static CombinedMods Instance { get; private set; }
        private USBManager speedBoostManager;
        private PredictionManager predictionManager;
        private PitGeoManager pitGeoManager;
        private SpeedManager speedManager;

        // GUI state
        private bool showGUI = true;
        private Rect mainWindowRect = new Rect(20, 20, 450, 500);
        private Dictionary<string, bool> expandedSections = new Dictionary<string, bool>();

        // Main GUI state
        private enum TabState { Predictions, SpeedBoost, PitGeo, Speed }
        private TabState currentTab = TabState.Predictions;

        // Input action reference for the key press
        private InputAction toggleGuiAction;

        // Scroll view position
        private Vector2 scrollPosition;
        private bool showHelp = false;

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

        // Configuration
        private ConfigFile config;

        private void Awake()
        {
            // Set singleton instance
            Instance = this;

            // Initialize config
            config = new ConfigFile(Path.Combine(Paths.ConfigPath, "CombinedMods.cfg"), true);

            // Initialize managers
            speedBoostManager = new USBManager(config);
            predictionManager = new PredictionManager(config);
            pitGeoManager = new PitGeoManager(config);
            speedManager = new SpeedManager(config);

            // Load settings
            LoadSettings();

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

        private void LoadSettings()
        {
            // Load general UI settings
            currentTab = (TabState)config.Bind("UI", "ActiveTab", 0, "Active tab index").Value;
            showGUI = config.Bind("UI", "ShowGUI", true, "Show GUI").Value;

            // Call load on managers
            speedBoostManager.LoadSettings();
            predictionManager.LoadSettings();
            pitGeoManager.LoadSettings();
            speedManager.LoadSettings();
        }

        private void SaveSettings()
        {
            // Save general UI settings
            config.Bind("UI", "ActiveTab", (int)currentTab, "Active tab index").Value = (int)currentTab;
            config.Bind("UI", "ShowGUI", showGUI, "Show GUI").Value = showGUI;

            // Call save on managers
            speedBoostManager.SaveSettings();
            predictionManager.SaveSettings();
            pitGeoManager.SaveSettings();
            speedManager.SaveSettings();

            config.Save();
            Debug.Log("Settings saved successfully!");
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

        private void DrawMainWindow(int windowID)
        {
            // Top title bar with version and help button
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Ace's COMP GUI v1.2.1", headerStyle);
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

            // Tab buttons at the top - now with 4 tabs
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Predictions", currentTab == TabState.Predictions ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(40), GUILayout.Width(105)))
            {
                currentTab = TabState.Predictions;
            }

            if (GUILayout.Button("USBM", currentTab == TabState.SpeedBoost ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(40), GUILayout.Width(105)))
            {
                currentTab = TabState.SpeedBoost;
            }

            if (GUILayout.Button("PitGeo", currentTab == TabState.PitGeo ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(40), GUILayout.Width(105)))
            {
                currentTab = TabState.PitGeo;
            }

            if (GUILayout.Button("PSA", currentTab == TabState.Speed ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(40), GUILayout.Width(105)))
            {
                currentTab = TabState.Speed;
            }

            GUILayout.EndHorizontal();

            // Draw divider
            GUILayout.Box("", dividerStyle);

            // Content based on active tab
            if (currentTab == TabState.Predictions)
            {
                DrawPredictionTab();
            }
            else if (currentTab == TabState.SpeedBoost)
            {
                DrawSpeedBoostTab();
            }
            else if (currentTab == TabState.PitGeo)
            {
                DrawPitGeoTab();
            }
            else if (currentTab == TabState.Speed)
            {
                DrawSpeedTab();
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

        private void DrawSpeedBoostTab()
        {
            GUILayout.Label("Ultimate Speed Boost Manager", headerStyle);
            GUILayout.Label("Configure speed settings for different buttons", subHeaderStyle);

            // Use the speedBoostManager to draw its UI
            scrollPosition = speedBoostManager.DrawGUI(
                boxStyle,
                scrollViewStyle,
                subHeaderStyle,
                dividerStyle,
                buttonStyle,
                expandedButtonStyle,
                sliderLabelStyle,
                sliderValueStyle,
                actionButtonStyle,
                scrollPosition
            );
        }

        private void DrawPredictionTab()
        {
            // Use the predictionManager to draw its UI
            predictionManager.DrawGUI(
                headerStyle,
                subHeaderStyle,
                boxStyle,
                sliderLabelStyle,
                sliderValueStyle,
                buttonStyle,
                dividerStyle
            );
        }

        private void DrawPitGeoTab()
        {
            GUILayout.Label("Pit Geometry Boost Settings", headerStyle);
            GUILayout.Label("Enhance movement on different pit surfaces", subHeaderStyle);

            // Use the pitGeoManager to draw its UI
            pitGeoManager.DrawGUI(
                boxStyle,
                scrollViewStyle,
                subHeaderStyle,
                dividerStyle,
                buttonStyle,
                sliderLabelStyle,
                sliderValueStyle,
                actionButtonStyle
            );
        }

        private void DrawSpeedTab()
        {
            GUILayout.Label("Speed Modifier", headerStyle);
            GUILayout.Label("Adjust your forward speed while holding the right primary button", subHeaderStyle);

            // Use the speedManager to draw its UI
            speedManager.DrawGUI(
                boxStyle,
                subHeaderStyle,
                buttonStyle,
                sliderLabelStyle,
                sliderValueStyle,
                actionButtonStyle
            );
        }

        private void ToggleGUIVisibility()
        {
            showGUI = !showGUI;
            Debug.Log($"GUI visibility toggled. ShowGUI: {showGUI}");
        }

        private void OnGUI()
        {
            if (!showGUI)
            {
                // Even if main GUI is hidden, still draw mini speed bar if enabled
                speedManager.DrawMiniGUI();
                return;
            }

            // Initialize styles within OnGUI to avoid errors
            InitializeGUIStyles();

            // Draw mini speed bar if enabled
            speedManager.DrawMiniGUI();

            // Draw main tabbed window with drop shadow effect
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.Box(new Rect(mainWindowRect.x + 5, mainWindowRect.y + 5, mainWindowRect.width, mainWindowRect.height), "");
            GUI.color = Color.white;

            mainWindowRect = GUILayout.Window(0, mainWindowRect, DrawMainWindow, "", windowStyle);
        }

        private void Update()
        {
            // Apply changes from all managers
            if (GTPlayer.Instance != null)
            {
                speedBoostManager.ApplySettings();
                predictionManager.ApplySettings();
                pitGeoManager.ApplySettings();
                speedManager.ApplySettings();
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
    }
}