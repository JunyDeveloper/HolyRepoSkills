using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using JP_RepoHolySkills.Player;
using UnityEngine;

namespace JP_RepoHolySkills.Patches
{
    // Player controller only exist on the owner so not what I'm looking for if I'm targeting all players
    [HarmonyPatch(typeof(PlayerController))]
    internal class PlayerControllerPatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        public static void Awake(PlayerController __instance)
        {
            //Plugin.Logger.LogInfo("PlayerControllerPatch awake");
            //PlayerControllerCustom playerControllerCustom = __instance.gameObject.AddComponent<PlayerControllerCustom>();
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static void Update(PlayerController __instance)
        {
            // Plugin.Logger.LogInfo($"PlayerControllerPatch update | {__instance == null}");
            // PlayerControllerCustom playerControllerCustom = __instance.gameObject.AddComponent<PlayerControllerCustom>();
        }
    }
}
