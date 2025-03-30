using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using GorillaNetworking;
using UnityEngine;

namespace GorillaTagMods
{
    public class PitGeoManager
    {
        // Settings
        private float groundBoostMultiplier = 1f;
        private float wallBoostMultiplier = 1f;
        private float doubleWallBoostMultiplier = 1f;
        private float slipWallBoostMultiplier = 1f;

        // UI visibility
        private bool isVisible = false;

        // Config reference
        private ConfigFile config;

        // Constructor
        public PitGeoManager(ConfigFile config)
        {
            this.config = config;
            LoadSettings();

            // Apply settings on startup
            ApplyGroundBoost();
            ApplyWallBoost();
            ApplyDoubleWallBoost();
            ApplySlipWallBoost();
        }

        public void LoadSettings()
        {
            // Load from PlayerPrefs for compatibility with original mod
            groundBoostMultiplier = PlayerPrefs.GetFloat("GroundBoostMultiplier", 1f);
            wallBoostMultiplier = PlayerPrefs.GetFloat("WallBoostMultiplier", 1f);
            doubleWallBoostMultiplier = PlayerPrefs.GetFloat("DoubleWallBoostMultiplier", 1f);
            slipWallBoostMultiplier = PlayerPrefs.GetFloat("SlipWallBoostMultiplier", 1f);

            // Also load from config
            groundBoostMultiplier = config.Bind("PitGeo", "GroundBoost", groundBoostMultiplier, "Ground boost multiplier").Value;
            wallBoostMultiplier = config.Bind("PitGeo", "WallBoost", wallBoostMultiplier, "Wall boost multiplier").Value;
            doubleWallBoostMultiplier = config.Bind("PitGeo", "DoubleWallBoost", doubleWallBoostMultiplier, "Double wall boost multiplier").Value;
            slipWallBoostMultiplier = config.Bind("PitGeo", "SlipWallBoost", slipWallBoostMultiplier, "Slip wall boost multiplier").Value;
            isVisible = config.Bind("PitGeo", "IsVisible", false, "Whether PitGeo UI is visible").Value;
        }

        public void SaveSettings()
        {
            // Save to PlayerPrefs for compatibility with original mod
            PlayerPrefs.SetFloat("GroundBoostMultiplier", groundBoostMultiplier);
            PlayerPrefs.SetFloat("WallBoostMultiplier", wallBoostMultiplier);
            PlayerPrefs.SetFloat("DoubleWallBoostMultiplier", doubleWallBoostMultiplier);
            PlayerPrefs.SetFloat("SlipWallBoostMultiplier", slipWallBoostMultiplier);
            PlayerPrefs.Save();

            // Also save to config
            config.Bind("PitGeo", "GroundBoost", 1f, "Ground boost multiplier").Value = groundBoostMultiplier;
            config.Bind("PitGeo", "WallBoost", 1f, "Wall boost multiplier").Value = wallBoostMultiplier;
            config.Bind("PitGeo", "DoubleWallBoost", 1f, "Double wall boost multiplier").Value = doubleWallBoostMultiplier;
            config.Bind("PitGeo", "SlipWallBoost", 1f, "Slip wall boost multiplier").Value = slipWallBoostMultiplier;
            config.Bind("PitGeo", "IsVisible", false, "Whether PitGeo UI is visible").Value = isVisible;
        }

        public void ToggleVisibility()
        {
            isVisible = !isVisible;
        }

        public void DrawGUI(
            GUIStyle boxStyle,
            GUIStyle scrollViewStyle,
            GUIStyle subHeaderStyle,
            GUIStyle dividerStyle,
            GUIStyle buttonStyle,
            GUIStyle sliderLabelStyle,
            GUIStyle sliderValueStyle,
            GUIStyle actionButtonStyle)
        {
            GUILayout.BeginVertical(boxStyle);

            // Ground Boost
            GUILayout.BeginHorizontal();
            GUILayout.Label("Ground Boost:", sliderLabelStyle);
            GUILayout.Label($"{groundBoostMultiplier:F1}", sliderValueStyle);
            GUILayout.EndHorizontal();

            float newGroundBoost = GUILayout.HorizontalSlider(groundBoostMultiplier, 1f, 2f);
            if (newGroundBoost != groundBoostMultiplier)
            {
                groundBoostMultiplier = newGroundBoost;
                ApplyGroundBoost();
            }

            GUILayout.Space(10);

            // Wall Boost
            GUILayout.BeginHorizontal();
            GUILayout.Label("Wall Boost:", sliderLabelStyle);
            GUILayout.Label($"{wallBoostMultiplier:F1}", sliderValueStyle);
            GUILayout.EndHorizontal();

            float newWallBoost = GUILayout.HorizontalSlider(wallBoostMultiplier, 1f, 2f);
            if (newWallBoost != wallBoostMultiplier)
            {
                wallBoostMultiplier = newWallBoost;
                ApplyWallBoost();
            }

            GUILayout.Space(10);

            // Double Wall Boost
            GUILayout.BeginHorizontal();
            GUILayout.Label("Double Wall Boost:", sliderLabelStyle);
            GUILayout.Label($"{doubleWallBoostMultiplier:F1}", sliderValueStyle);
            GUILayout.EndHorizontal();

            float newDoubleWallBoost = GUILayout.HorizontalSlider(doubleWallBoostMultiplier, 1f, 2f);
            if (newDoubleWallBoost != doubleWallBoostMultiplier)
            {
                doubleWallBoostMultiplier = newDoubleWallBoost;
                ApplyDoubleWallBoost();
            }

            GUILayout.Space(10);

            // Slip Wall Boost
            GUILayout.BeginHorizontal();
            GUILayout.Label("Slip Wall Boost:", sliderLabelStyle);
            GUILayout.Label($"{slipWallBoostMultiplier:F1}", sliderValueStyle);
            GUILayout.EndHorizontal();

            float newSlipWallBoost = GUILayout.HorizontalSlider(slipWallBoostMultiplier, 1f, 2f);
            if (newSlipWallBoost != slipWallBoostMultiplier)
            {
                slipWallBoostMultiplier = newSlipWallBoost;
                ApplySlipWallBoost();
            }

            GUILayout.Space(15);

            // Reset and room joining buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset All", actionButtonStyle))
            {
                ResetSettings();
            }

            if (GUILayout.Button("Join LUCIO Room", actionButtonStyle))
            {
                PhotonNetworkController.Instance.AttemptToJoinSpecificRoom("LUCIO", 0);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        public void ApplySettings()
        {
            // This is called every frame to keep settings updated
            // The individual Apply methods are called when sliders change
        }

        private void ApplyGroundBoost()
        {
            GameObject ground = GameObject.Find("pit ground");
            if (ground != null)
            {
                GorillaSurfaceOverride surfaceOverride = ground.GetComponent<GorillaSurfaceOverride>();
                if (surfaceOverride != null)
                {
                    surfaceOverride.extraVelMaxMultiplier = groundBoostMultiplier;
                    surfaceOverride.extraVelMultiplier = groundBoostMultiplier;
                }
            }
        }

        private void ApplyWallBoost()
        {
            GameObject wall = GameObject.Find("pitregularwallV2");
            if (wall != null)
            {
                GorillaSurfaceOverride surfaceOverride = wall.GetComponent<GorillaSurfaceOverride>();
                if (surfaceOverride != null)
                {
                    surfaceOverride.extraVelMaxMultiplier = wallBoostMultiplier;
                    surfaceOverride.extraVelMultiplier = wallBoostMultiplier;
                }
            }
        }

        private void ApplyDoubleWallBoost()
        {
            GameObject verticalWall1 = GameObject.Find("verticalwall (1)");
            GameObject verticalWall2 = GameObject.Find("verticalwall");
            if (verticalWall1 != null)
            {
                GorillaSurfaceOverride surfaceOverride = verticalWall1.GetComponent<GorillaSurfaceOverride>();
                if (surfaceOverride != null)
                {
                    surfaceOverride.extraVelMaxMultiplier = doubleWallBoostMultiplier;
                    surfaceOverride.extraVelMultiplier = doubleWallBoostMultiplier;
                }
            }

            if (verticalWall2 != null)
            {
                GorillaSurfaceOverride surfaceOverride = verticalWall2.GetComponent<GorillaSurfaceOverride>();
                if (surfaceOverride != null)
                {
                    surfaceOverride.extraVelMaxMultiplier = doubleWallBoostMultiplier;
                    surfaceOverride.extraVelMultiplier = doubleWallBoostMultiplier;
                }
            }
        }

        private void ApplySlipWallBoost()
        {
            GameObject slipWall = GameObject.Find("pit lower slippery wall");
            if (slipWall != null)
            {
                GorillaSurfaceOverride surfaceOverride = slipWall.GetComponent<GorillaSurfaceOverride>();
                if (surfaceOverride != null)
                {
                    surfaceOverride.extraVelMaxMultiplier = slipWallBoostMultiplier;
                    surfaceOverride.extraVelMultiplier = slipWallBoostMultiplier;
                }
            }
        }

        private void ResetSettings()
        {
            groundBoostMultiplier = 1f;
            wallBoostMultiplier = 1f;
            doubleWallBoostMultiplier = 1f;
            slipWallBoostMultiplier = 1f;

            ApplyGroundBoost();
            ApplyWallBoost();
            ApplyDoubleWallBoost();
            ApplySlipWallBoost();

            SaveSettings();
        }
    }
}