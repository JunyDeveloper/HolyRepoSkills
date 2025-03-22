using TMPro;
using UnityEngine;

namespace JP_RepoHolySkills
{
    public class Utility
    {
        /// <summary>
        /// Finds and returns the Skill Tier Container GameObject based on the index.
        /// For example, if index is 1, it will search for "SkillTier1Container".
        /// </summary>
        /// <param name="index">The skill tier index (e.g., 1 for SkillTier1Container).</param>
        /// <returns>The GameObject with the matching name, or null if not found.</returns>
        public static GameObject FindSkillTierContainer(int index)
        {
            string objectName = $"SkillTier{index}Container";
            return GameObject.Find(objectName);
        }

        /// <summary>
        /// Finds and returns the Skill Tier Description GameObject based on the index.
        /// For example, if index is 1, it will search for "SkillTier1Description".
        /// </summary>
        /// <param name="index">The skill tier index (e.g., 1 for SkillTier1Description).</param>
        /// <returns>The GameObject with the matching name, or null if not found.</returns>
        public static GameObject FindSkillTierDescription(int index)
        {
            string objectName = $"SkillTier{index}Description";
            return GameObject.Find(objectName);
        }

        /// <summary>
        /// Finds the Skill Tier Container with the given index and sets its Image component's color.
        /// </summary>
        /// <param name="index">The skill tier index (e.g., 1 for SkillTier1Container).</param>
        /// <param name="newColor">The new color to set on the Image component.</param>
        public static void SetSkillTierContainerColor(int index, Color newColor)
        {
            GameObject container = FindSkillTierContainer(index);
            if (container != null)
            {
                UnityEngine.UI.Image img = container.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    img.color = newColor;
                }
                else
                {
                    Debug.LogWarning($"SkillTier{index}Container does not have an Image component.");
                }
            }
            else
            {
                Debug.LogWarning($"SkillTier{index}Container not found in the scene.");
            }
        }

        /// <summary>
        /// Finds the Skill Tier Container with the given index and sets its TextMeshPro – Text (UI) component's text.
        /// </summary>
        /// <param name="index">The skill tier index (e.g., 1 for SkillTier1Container).</param>
        /// <param name="newText">The new text to set on the TextMeshPro component.</param>
        public static void SetSkillTierContainerText(int index, string newText)
        {
            GameObject container = FindSkillTierContainer(index);
            if (container != null)
            {
                TextMeshProUGUI textComponent = container.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = newText;
                }
                else
                {
                    Debug.LogWarning($"SkillTier{index}Container does not have a TextMeshProUGUI component.");
                }
            }
            else
            {
                Debug.LogWarning($"SkillTier{index}Container not found in the scene.");
            }
        }

        public static string FormatTotalExtractedHaul(int haulAmount)
        {
            // Format the haul with commas for readability
            string formattedHaul = haulAmount.ToString("N0");

            // White text label, cyan-blue value color
            return $"<b><color=#FFFFFF>Total Extracted Haul:</color> <color=#00FFFF>{formattedHaul}</color></b>";
        }


        public static void UpdateTotalExtractedHaulText(int extractedHaul)
        {
            GameObject haulTextObj = GameObject.Find("SkillTotalHaulText");
            if (haulTextObj != null)
            {
                TextMeshProUGUI textComp = haulTextObj.GetComponent<TextMeshProUGUI>();
                if (textComp != null)
                {
                    textComp.text = FormatTotalExtractedHaul(extractedHaul);
                    // Plugin.Logger.LogInfo($"Updated SkillTotalHaulText to: {textComp.text}");
                }
                else
                {
                    //  Plugin.Logger.LogWarning("GameObject 'SkillTotalHaulText' does not have a TextMeshProUGUI component.");
                }
            }
            else
            {
                // Plugin.Logger.LogWarning("GameObject 'SkillTotalHaulText' not found in the scene.");
            }
        }
    }
}
