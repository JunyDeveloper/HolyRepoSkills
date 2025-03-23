using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using JP_RepoHolySkills.GlobalMananger;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JP_RepoHolySkills.SkillSelector
{
    public enum SelectableSkills
    {
        HolyWall,
        HolyAura,
        Heal,
        None
    }

    public class SkillSelectorController : MonoBehaviour
    {
        public string currentSkillDescription = "";
        public bool hasSetupSkillUI = false;
        public bool isSelectSkillUIShowing = false;
        GameObject spawnedSelectSkillUI;
        public static SkillSelectorController Instance;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Plugin.Logger.LogInfo("SkillSelectorController Awake: Instance set successfully.");
            }
            else
            {
                Plugin.Logger.LogWarning("SkillSelectorController Awake: Duplicate instance detected!");
            }
        }

        void Start()
        {
            Plugin.Logger.LogInfo("SkillSelectorController Start: Initializing Skill Selector UI.");
            SetupSelectSkillUI();
        }

        void Update()
        {
            // Check chat status via reflection.
            FieldInfo chatActiveField = typeof(ChatManager).GetField("chatActive", BindingFlags.NonPublic | BindingFlags.Instance);
            bool isChatOpen = (bool)chatActiveField.GetValue(ChatManager.instance);

            if (isChatOpen)
            {
                return;
            }

            // Optionally skip input in a level.
            if (SemiFunc.RunIsLevel() || SemiFunc.RunIsShop())
            {
                return;
            }

            Utility.UpdateTotalExtractedHaulText(JPSkill_GlobalManager.Instance.savedExtractionHaul);

            // Toggle the Skill Selector UI when P is pressed.
            if (Input.GetKeyDown(Plugin.SkillPageHotkey.Value.MainKey))
            {
                Plugin.Logger.LogInfo("SkillSelectorController Update: P key pressed to toggle Skill Selector UI.");

                // If spawnedSelectSkillUI is null, reset the flag and try to reinitialize.
                if (spawnedSelectSkillUI == null)
                {
                    Plugin.Logger.LogWarning("SkillSelectorController Update: spawnedSelectSkillUI is null. Attempting to reinitialize UI.");
                    hasSetupSkillUI = false;
                    SetupSelectSkillUI();
                }

                if (spawnedSelectSkillUI != null)
                {
                    if (isSelectSkillUIShowing)
                    {
                        Plugin.Logger.LogInfo("SkillSelectorController Update: Hiding Skill Selector UI.");
                        spawnedSelectSkillUI.SetActive(false);
                    }
                    else
                    {
                        Plugin.Logger.LogInfo("SkillSelectorController Update: Showing Skill Selector UI.");
                        spawnedSelectSkillUI.SetActive(true);
                        AutoSelectSkillUI();
                    }
                    isSelectSkillUIShowing = !isSelectSkillUIShowing;
                }
                else
                {
                    Plugin.Logger.LogError("SkillSelectorController Update: Failed to initialize Skill Selector UI; spawnedSelectSkillUI is still null.");
                }
            }

            if (isSelectSkillUIShowing)
            {
                HandleSkillSeletorMenuInput();
            }
        }

        public void HandleSkillSeletorMenuInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SelectHolyAuraSkill();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SelectHealSkill();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SelectHolyWallSkill();
            }
        }

        private void AutoSelectSkillUI()
        {
            if (JPSkill_GlobalManager.Instance.selectedSkill == SelectableSkills.HolyAura)
            {
                SelectHolyAuraSkill();
            }
            else if (JPSkill_GlobalManager.Instance.selectedSkill == SelectableSkills.Heal)
            {
                SelectHealSkill();
            }
            else if (JPSkill_GlobalManager.Instance.selectedSkill == SelectableSkills.HolyWall)
            {
                SelectHolyWallSkill();
            }
        }

        private void SelectHolyAuraSkill()
        {
            Plugin.Logger.LogInfo("SkillSelectorController: Alpha1 pressed - Selecting HolyAura skill.");
            JPSkill_GlobalManager.Instance.selectedSkill = SelectableSkills.HolyAura;
            currentSkillDescription = ClassModConstants.HolyAuraDescription;
            SetSkillPointers(auraEnabled: true, healEnabled: false, wallEnabled: false);
            UpdateSkillDescriptionText(currentSkillDescription);

            // For HolyAura, assume 5 tiers.
            ClearSkillTierDescriptions(5);
            UpdateSkillTierContainers(5);
            UpdateSkillTierDescriptions(ClassModConstants.HolyAuraTierDescriptions);
        }

        private void SelectHealSkill()
        {
            Plugin.Logger.LogInfo("SkillSelectorController: Alpha2 pressed - Selecting Heal skill.");
            JPSkill_GlobalManager.Instance.selectedSkill = SelectableSkills.Heal;
            currentSkillDescription = ClassModConstants.HealDescription;
            SetSkillPointers(auraEnabled: false, healEnabled: true, wallEnabled: false);
            UpdateSkillDescriptionText(currentSkillDescription);

            // For Heal, assume 5 tiers.
            ClearSkillTierDescriptions(5);
            UpdateSkillTierContainers(5);
            UpdateSkillTierDescriptions(ClassModConstants.HealSkillTierDescriptions);
        }

        private void SelectHolyWallSkill()
        {
            Plugin.Logger.LogInfo("SkillSelectorController: Alpha3 pressed - Selecting Holy Wall skill.");
            JPSkill_GlobalManager.Instance.selectedSkill = SelectableSkills.HolyWall;
            currentSkillDescription = ClassModConstants.HolyWallDescription;
            SetSkillPointers(auraEnabled: false, healEnabled: false, wallEnabled: true);
            UpdateSkillDescriptionText(currentSkillDescription);

            ClearSkillTierDescriptions(5);
            UpdateSkillTierContainers(5);
            UpdateSkillTierDescriptions(ClassModConstants.HolyWallSkillTierDescriptions);
        }

        // Helper method to update the skill pointers safely.
        private void SetSkillPointers(bool auraEnabled, bool healEnabled, bool wallEnabled)
        {
            GameObject auraPointer = GameObject.Find("HolyAuraPointer");
            if (auraPointer != null)
            {
                RawImage auraImg = auraPointer.GetComponent<RawImage>();
                if (auraImg != null)
                    auraImg.enabled = auraEnabled;
            }
            else
            {
                Plugin.Logger.LogWarning("HolyAuraPointer not found.");
            }

            GameObject healPointer = GameObject.Find("HealPointer");
            if (healPointer != null)
            {
                RawImage healImg = healPointer.GetComponent<RawImage>();
                if (healImg != null)
                    healImg.enabled = healEnabled;
            }
            else
            {
                Plugin.Logger.LogWarning("HealPointer not found.");
            }

            GameObject wallPointer = GameObject.Find("HolyWallPointer");
            if (wallPointer != null)
            {
                RawImage wallImg = wallPointer.GetComponent<RawImage>();
                if (wallImg != null)
                    wallImg.enabled = wallEnabled;
            }
            else
            {
                Plugin.Logger.LogWarning("HolyWallPointer not found.");
            }
        }

        // Helper method to update the main skill description text safely.
        private void UpdateSkillDescriptionText(string description)
        {
            GameObject descObj = GameObject.Find("JP_SkillDescriptionText");
            if (descObj != null)
            {
                TextMeshProUGUI textComp = descObj.GetComponent<TextMeshProUGUI>();
                if (textComp != null)
                {
                    textComp.text = description;
                }
                else
                {
                    Plugin.Logger.LogWarning("JP_SkillDescriptionText does not have a TextMeshProUGUI component.");
                }
            }
            else
            {
                Plugin.Logger.LogWarning("JP_SkillDescriptionText not found.");
            }
        }

        // Clears the text of all skill tier description GameObjects up to the given number of tiers.
        private void ClearSkillTierDescriptions(int numberOfTiers)
        {
            for (int tier = 1; tier <= numberOfTiers; tier++)
            {
                GameObject tierDescObj = JP_RepoHolySkills.Utility.FindSkillTierDescription(tier);
                if (tierDescObj != null)
                {
                    TextMeshProUGUI textComp = tierDescObj.GetComponent<TextMeshProUGUI>();
                    if (textComp != null)
                    {
                        textComp.text = "";
                        Plugin.Logger.LogInfo($"SkillTier{tier}Description text cleared.");
                    }
                    else
                    {
                        Plugin.Logger.LogWarning($"SkillTier{tier}Description GameObject does not have a TextMeshProUGUI component.");
                    }
                }
                else
                {
                    Plugin.Logger.LogWarning($"SkillTier{tier}Description not found in the scene.");
                }
            }
        }

        // Updates the color of the skill tier container images up to the given number of tiers.
        private void UpdateSkillTierContainers(int numberOfTiers)
        {
            for (int tier = 1; tier <= numberOfTiers; tier++)
            {
                if (JPSkill_GlobalManager.Instance.savedExtractionHaul >= tier * ClassModConstants.HAUL_TIER_INCREMENT)
                {
                    JP_RepoHolySkills.Utility.SetSkillTierContainerColor(tier, new Color(0.5235849f, 1f, 0.5453033f, 0.3921569f));
                }
                else
                {
                    JP_RepoHolySkills.Utility.SetSkillTierContainerColor(tier, new Color(1f, 1f, 1f, 0.4f));
                }
            }
        }

        // Updates the text of the skill tier description GameObjects using the provided descriptions array.
        private void UpdateSkillTierDescriptions(string[] descriptions)
        {
            for (int tier = 1; tier <= descriptions.Length; tier++)
            {
                GameObject tierDescObj = JP_RepoHolySkills.Utility.FindSkillTierDescription(tier);
                if (tierDescObj != null)
                {
                    TextMeshProUGUI textComp = tierDescObj.GetComponent<TextMeshProUGUI>();
                    if (textComp != null)
                    {
                        textComp.text = ""; // Clear existing text.
                        textComp.text = descriptions[tier - 1];
                        Plugin.Logger.LogInfo($"SkillTier{tier}Description text set successfully.");
                    }
                    else
                    {
                        Plugin.Logger.LogWarning($"SkillTier{tier}Description GameObject does not have a TextMeshProUGUI component.");
                    }
                }
                else
                {
                    Plugin.Logger.LogWarning($"SkillTier{tier}Description not found in the scene.");
                }
            }
        }

        public void SetupSelectSkillUI()
        {
            // Allow reinitialization if the UI is missing.
            if (hasSetupSkillUI && spawnedSelectSkillUI != null)
            {
                Plugin.Logger.LogInfo("SkillSelectorController: SetupSelectSkillUI already executed. Skipping.");
                return;
            }
            hasSetupSkillUI = true;
            Plugin.Logger.LogInfo("SkillSelectorController: Setting up Skill Selector UI.");

            if (Plugin.AssetManager.TryGetValue(ClassModConstants.SELECT_SKILL_UI, out GameObject selectSkillUI))
            {
                GameObject canvas = GameObject.Find("Game Hud");
                if (canvas == null)
                {
                    Plugin.Logger.LogWarning("SkillSelectorController: 'Game Hud' canvas not found.");
                    return;
                }
                spawnedSelectSkillUI = Instantiate(selectSkillUI, this.transform.position, Quaternion.identity);
                spawnedSelectSkillUI.SetActive(false);
                spawnedSelectSkillUI.layer = 5;
                spawnedSelectSkillUI.transform.SetParent(canvas.transform, false);
                // Position the UI correctly on the canvas.
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                float canvasWidth = canvasRect.rect.width;
                Vector3 uiPosition = new Vector3(canvasWidth - (canvasWidth / 2) - 150f, -30f, 0f);
                spawnedSelectSkillUI.transform.localPosition = uiPosition;
                Plugin.Logger.LogInfo("SkillSelectorController: UI setup complete, added to canvas at local position " + uiPosition);
            }
            else
            {
                Plugin.Logger.LogWarning("SkillSelectorController: SELECT_SKILL_UI asset not found in AssetManager!");
            }
        }
    }
}
