using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JP_RepoHolySkills.GlobalMananger;
using JP_RepoHolySkills.Patches;
using Photon.Pun;
using UnityEngine;

namespace JP_RepoHolySkills
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        public static Plugin Instance;
        public static new ManualLogSource Logger;
        public static Dictionary<string, GameObject> AssetManager;

        public static ConfigEntry<KeyboardShortcut> SkillPageHotkey;
        public static ConfigEntry<KeyboardShortcut> ActivateSkillHotkey;

        public GameObject stunGrenadePrefab;
        public GameObject shockwaveGrenadePrefab;

        // All skills are available to use & all skill cooldowns are set to 1
        public bool isInDebugMode = true;

        private void Awake()
        {
            // Use the base logger from BepInEx.
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            if (Instance == null)
            {
                Instance = this;
                Logger.LogInfo("Plugin: Instance set successfully.");
            }
            else
            {
                Logger.LogWarning("Plugin: Multiple Plugin instances detected.");
            }

            SkillPageHotkey = Config.Bind(
                "Keybinds",
                "OpenSkillSelectionPage",
                new KeyboardShortcut(KeyCode.P),
                "The key used to open the skill selection page. Use Unity KeyCode names: https://docs.unity3d.com/ScriptReference/KeyCode.html"
            );

            ActivateSkillHotkey = Config.Bind(
                "Keybinds",
                "ActivateSkill",
                new KeyboardShortcut(KeyCode.R),
                "The key used to activate the currently selected skill. Use Unity KeyCode names: https://docs.unity3d.com/ScriptReference/KeyCode.html"
            );

            // NetcodeWeaver();

            // Determine the directory where this DLL is located.
            string dllDir = Path.GetDirectoryName(this.Info.Location);
            Logger.LogInfo($"Plugin: DLL directory is {dllDir}");

            // Load the asset bundle.
            string assetBundlePath = Path.Combine(dllDir, "jp_repoholyskillsprefabs");
            AssetBundle assetBundle = AssetBundle.LoadFromFile(assetBundlePath);
            AssetManager = new Dictionary<string, GameObject>();

            if (assetBundle == null)
            {
                Logger.LogError("Plugin: Unable to load asset bundle from " + assetBundlePath);
            }
            else
            {
                Logger.LogInfo("Plugin: Asset bundle loaded successfully.");
            }

            // Load each asset from the bundle and log the result.
            GameObject holyAuraAsset = LoadAssetFromAssetBundleAndLogInfo<GameObject>(assetBundle, ClassModConstants.HOLY_AURA_ASSET);
            GameObject holyAuraBuffAsset = LoadAssetFromAssetBundleAndLogInfo<GameObject>(assetBundle, ClassModConstants.HOLY_AURA_BUFF_ASSET);
            GameObject holyAuraIconAsset = LoadAssetFromAssetBundleAndLogInfo<GameObject>(assetBundle, ClassModConstants.HOLY_AURA_ICON_ASSET);
            GameObject holyAuraSFXAsset = LoadAssetFromAssetBundleAndLogInfo<GameObject>(assetBundle, ClassModConstants.HOLY_AURA_SFX_ASSET);

            GameObject healSkillIconAsset = LoadAssetFromAssetBundleAndLogInfo<GameObject>(assetBundle, ClassModConstants.HEAL_SKILL_ICON);
            GameObject healSkillSFXAsset = LoadAssetFromAssetBundleAndLogInfo<GameObject>(assetBundle, ClassModConstants.HEAL_SKILL_SFX);
            GameObject healSkillAsset = LoadAssetFromAssetBundleAndLogInfo<GameObject>(assetBundle, ClassModConstants.HEAL_SKILL);
            GameObject healReviveSkillAsset = LoadAssetFromAssetBundleAndLogInfo<GameObject>(assetBundle, ClassModConstants.HEAL_REVIVE_SKILL);
            GameObject healReviveSkillSFXAsset = LoadAssetFromAssetBundleAndLogInfo<GameObject>(assetBundle, ClassModConstants.HEAL_REVIVE_SKILL_SFX);

            GameObject holyWallSkillAsset = LoadAssetFromAssetBundleAndLogInfo<GameObject>(assetBundle, ClassModConstants.HOLY_WALL);
            GameObject holyWallSFXAsset = LoadAssetFromAssetBundleAndLogInfo<GameObject>(assetBundle, ClassModConstants.HOLY_WALL_SFX);
            GameObject holyWallIconAsset = LoadAssetFromAssetBundleAndLogInfo<GameObject>(assetBundle, ClassModConstants.HOLY_WALL_ICON);
            GameObject selectSkillAsset = LoadAssetFromAssetBundleAndLogInfo<GameObject>(assetBundle, ClassModConstants.SELECT_SKILL_UI);

            // Populate the asset manager dictionary.
            AssetManager.Add(ClassModConstants.HOLY_AURA_ASSET, holyAuraAsset);
            AssetManager.Add(ClassModConstants.HOLY_AURA_BUFF_ASSET, holyAuraBuffAsset);
            AssetManager.Add(ClassModConstants.HOLY_AURA_ICON_ASSET, holyAuraIconAsset);
            AssetManager.Add(ClassModConstants.HOLY_AURA_SFX_ASSET, holyAuraSFXAsset);
            AssetManager.Add(ClassModConstants.HEAL_SKILL, healSkillAsset);
            AssetManager.Add(ClassModConstants.HEAL_SKILL_ICON, healSkillIconAsset);
            AssetManager.Add(ClassModConstants.HEAL_SKILL_SFX, healSkillSFXAsset);
            AssetManager.Add(ClassModConstants.HEAL_REVIVE_SKILL, healReviveSkillAsset);
            AssetManager.Add(ClassModConstants.HEAL_REVIVE_SKILL_SFX, healReviveSkillSFXAsset);
            AssetManager.Add(ClassModConstants.HOLY_WALL, holyWallSkillAsset);
            AssetManager.Add(ClassModConstants.HOLY_WALL_ICON, holyWallIconAsset);
            AssetManager.Add(ClassModConstants.HOLY_WALL_SFX, holyWallSFXAsset);
            AssetManager.Add(ClassModConstants.SELECT_SKILL_UI, selectSkillAsset);
            Logger.LogInfo("Plugin: AssetManager populated successfully.");

            // Apply Harmony patches.
            harmony.PatchAll(typeof(PlayerControllerPatch));
            harmony.PatchAll(typeof(MapToolControllerPatch));
            harmony.PatchAll(typeof(ExtractionPointPatch));
            harmony.PatchAll(typeof(PlayerAvatarPatch));
            harmony.PatchAll(typeof(GameManagerPatch));
            Logger.LogInfo("Plugin: All Harmony patches applied successfully.");

            Logger.LogInfo("Plugin: Awake completed.");
        }

        void Start()
        {
            // Optionally add further initialization here.
        }

        private static void NetcodeWeaver()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
            Logger.LogInfo("Plugin: NetcodeWeaver executed.");
        }

        private T LoadAssetFromAssetBundleAndLogInfo<T>(AssetBundle bundle, string assetName) where T : UnityEngine.Object
        {
            T loadedAsset = bundle.LoadAsset<T>(assetName);
            if (loadedAsset == null)
            {
                Logger.LogError($"Plugin: {assetName} asset failed to load.");
            }
            else
            {
                Logger.LogInfo($"Plugin: {assetName} asset successfully loaded.");
            }
            return loadedAsset;
        }
    }
}
