using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using JP_RepoHolySkills.Player;

namespace JP_RepoHolySkills.Patches
{
    [HarmonyPatch(typeof(PlayerAvatar))]
    internal class PlayerAvatarPatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        public static void Awake(PlayerAvatar __instance)
        {
            Plugin.Logger.LogInfo("PlayerAvatarPatch awake");
            __instance.gameObject.AddComponent<PlayerControllerCustom>();
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static void Update(PlayerAvatar __instance)
        {
            // Plugin.Logger.LogInfo($"PlayerControllerPatch update | {__instance == null}");
            // PlayerControllerCustom playerControllerCustom = __instance.gameObject.AddComponent<PlayerControllerCustom>();
        }
    }
}
