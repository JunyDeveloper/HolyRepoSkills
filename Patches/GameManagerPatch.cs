using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using JP_RepoHolySkills.GlobalMananger;
using JP_RepoHolySkills.MapToolControllerCustoms;
using JP_RepoHolySkills.SkillSelector;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JP_RepoHolySkills.Patches
{
    [HarmonyPatch(typeof(GameManager))]
    internal class GameManagerPatch
    {
        private static bool _hasPatched = false; // Ensure patch only runs once

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void Awake(GameManager __instance)
        {
            if (_hasPatched)
            {
                Plugin.Logger.LogInfo("GameManagerPatch Awake: Already patched, skipping further patching.");
                return;
            }

            _hasPatched = true;
            Plugin.Logger.LogInfo("GameManagerPatch Awake: Running patch for the first time.");

            // Add SkillSelectorController component to the GameManager's GameObject.
            __instance.gameObject.AddComponent<SkillSelectorController>();
            Plugin.Logger.LogInfo("GameManagerPatch Awake: SkillSelectorController added to GameManager.");

            // Create and add JPSkill_GlobalManager component on a new GameObject.
            Plugin.Logger.LogInfo("GameManagerPatch Awake: Creating JPSkill_GlobalManagerGameObject...");
            GameObject jpsGlobalManagerGO = new GameObject("JPSkill_GlobalManagerGameObject");
            jpsGlobalManagerGO.AddComponent<JPSkill_GlobalManager>();
            Plugin.Logger.LogInfo("GameManagerPatch Awake: JPSkill_GlobalManager added to JPSkill_GlobalManagerGameObject.");
        }
    }
}
