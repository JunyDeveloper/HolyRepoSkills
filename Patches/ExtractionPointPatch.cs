﻿using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JP_RepoHolySkills.GlobalMananger;
using UnityEngine;

namespace JP_RepoHolySkills.Patches
{
    [HarmonyPatch(typeof(ExtractionPoint))]
    internal class ExtractionPointPatch
    {
        [HarmonyPatch("StateComplete")]
        [HarmonyPrefix]
        public static void StateComplete(ExtractionPoint __instance)
        {
            // If the J key is pressed, modify the haul goal.
            /*   if (Input.GetKeyDown(KeyCode.J))
               {
                   Plugin.Logger.LogInfo("ExtractionPointPatch: J key pressed. Modifying haulGoal.");
                   Plugin.Logger.LogInfo("ExtractionPointPatch: Instance = " + __instance);
                   __instance.haulGoal = 4;
               }*/

            // No processing in shop mode.
            if (SemiFunc.RunIsShop())
            {
                // Plugin.Logger.LogInfo("ExtractionPointPatch: In shop mode. Skipping further processing.");
                return;
            }

            // Access the private field "stateStart" to check if the extraction is complete.
            FieldInfo stateStartField = typeof(ExtractionPoint).GetField("stateStart", BindingFlags.NonPublic | BindingFlags.Instance);
            if (stateStartField == null)
            {
                //Plugin.Logger.LogWarning("ExtractionPointPatch: Field 'stateStart' not found in ExtractionPoint.");
                return;
            }

            bool stateStart = (bool)stateStartField.GetValue(__instance);
            // Plugin.Logger.LogInfo("ExtractionPointPatch: stateStart value = " + stateStart);
            if (!stateStart)
            {
                // Plugin.Logger.LogInfo("ExtractionPointPatch: stateStart is false. Extraction processing already completed. Skipping.");
                return;
            }

            // Read the current extraction haul.
            FieldInfo extractionHaulField = typeof(ExtractionPoint).GetField("extractionHaul", BindingFlags.NonPublic | BindingFlags.Instance);
            if (extractionHaulField == null)
            {
                // Plugin.Logger.LogWarning("ExtractionPointPatch: Field 'extractionHaul' not found in ExtractionPoint.");
                return;
            }

            int extractionHaul = (int)extractionHaulField.GetValue(__instance);
            Plugin.Logger.LogInfo("ExtractionPointPatch: extractionHaul value = " + extractionHaul);

            // Save the accumulated extraction haul using ES3.
            ES3Settings es3Settings = new ES3Settings("JPSkillRepo.es3", ES3.Location.File);
            try
            {
                int prevSavedExtractionHaul = ES3.Load<int>("accumulatedExtractionHaul", es3Settings);
                Plugin.Logger.LogInfo("ExtractionPointPatch: Loaded previous accumulated extraction haul: " + prevSavedExtractionHaul);

                int newSavedExtractionHaul = checked(prevSavedExtractionHaul + extractionHaul);
                Plugin.Logger.LogInfo("ExtractionPointPatch: New accumulated extraction haul = " + newSavedExtractionHaul);

                ES3.Save("accumulatedExtractionHaul", newSavedExtractionHaul, es3Settings);
                Plugin.Logger.LogInfo("ExtractionPointPatch: Accumulated extraction haul saved successfully.");

                JPSkill_GlobalManager.Instance.savedExtractionHaul = newSavedExtractionHaul;
            }
            catch (System.Exception e)
            {
                Plugin.Logger.LogError("ExtractionPointPatch: Failed to load & save new extraction haul value: " + e.Message);
            }
        }
    }
}
