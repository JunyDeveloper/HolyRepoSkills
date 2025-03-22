using System.Reflection;
using HarmonyLib;
using JP_RepoHolySkills.MapToolControllerCustoms;
using JP_RepoHolySkills.SkillSelector;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JP_RepoHolySkills.Patches
{
    [HarmonyPatch(typeof(MapToolController))]
    internal class MapToolControllerPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        public static void Start(MapToolController __instance)
        {
            Plugin.Logger.LogInfo("MapToolControllerPatch start");
            __instance.gameObject.AddComponent<MapToolControllerCustom>();
        }
    }
}
