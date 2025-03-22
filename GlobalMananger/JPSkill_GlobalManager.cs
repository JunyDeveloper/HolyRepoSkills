using ES3Internal; // Make sure ES3 is referenced in your project
using JP_RepoHolySkills.SkillSelector;
using Photon.Pun;
using TMPro;
using UnityEngine;

namespace JP_RepoHolySkills.GlobalMananger
{
    public class JPSkill_GlobalManager : MonoBehaviour
    {
        public int savedExtractionHaul = 0;
        public SelectableSkills selectedSkill = SelectableSkills.None;
        public static JPSkill_GlobalManager Instance;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Plugin.Logger.LogInfo("JPSkill_GlobalManager Awake: Instance set successfully.");
            }
            else
            {
                Plugin.Logger.LogWarning("JPSkill_GlobalManager Awake: An instance already exists!");
            }
        }

        void Start()
        {
            // Load the saved extraction haul from file.
            try
            {
                ES3Settings es3Settings = new ES3Settings("JPSkillRepo.es3", ES3.Location.File);
                savedExtractionHaul = Plugin.Instance.isInDebugMode ? 3000000 : ES3.Load<int>("accumulatedExtractionHaul", es3Settings);
                Plugin.Logger.LogInfo($"JPSkill_GlobalManager Start: Loaded savedExtractionHaul = {savedExtractionHaul}.");
            }
            catch (System.Exception e)
            {
                Plugin.Logger.LogError("JPSkill_GlobalManager Start: Failed to load savedExtractionHaul: " + e.Message);
            }

            // Retrieve the modded holy wall prefab.
            GameObject holyWallSkillPrefab;
            bool foundHolyWall = Plugin.AssetManager.TryGetValue(ClassModConstants.HOLY_WALL, out holyWallSkillPrefab);
            if (foundHolyWall && holyWallSkillPrefab != null)
            {
                Plugin.Logger.LogInfo($"JPSkill_GlobalManager Start: Found Holy Wall prefab: {holyWallSkillPrefab.name}.");
            }
            else
            {
                Plugin.Logger.LogWarning("JPSkill_GlobalManager Start: Holy Wall prefab not found in AssetManager.");
            }

            // Ensure Photon is fully initialized by checking the prefab pool.
            IPunPrefabPool existingPool = PhotonNetwork.PrefabPool;
            if (existingPool == null)
            {
                Plugin.Logger.LogInfo("JPSkill_GlobalManager Start: No existing Photon prefab pool found, using DefaultPool.");
                existingPool = new DefaultPool();
            }
            else
            {
                Plugin.Logger.LogInfo("JPSkill_GlobalManager Start: Existing Photon prefab pool found: " + existingPool.GetType().Name);
            }

            // Create a combined pool that wraps the existing one.
            CombinedPrefabPool combinedPool = new CombinedPrefabPool(existingPool);
            Plugin.Logger.LogInfo("JPSkill_GlobalManager Start: Created CombinedPrefabPool.");

            // Add your modded holy wall prefab using a unique key.
            combinedPool.AddModdedPrefab(ClassModConstants.HOLY_WALL, holyWallSkillPrefab);
            Plugin.Logger.LogInfo($"JPSkill_GlobalManager Start: Added modded Holy Wall prefab with key '{ClassModConstants.HOLY_WALL}'.");

            // Set Photon to use our combined prefab pool.
            PhotonNetwork.PrefabPool = combinedPool;
            Plugin.Logger.LogInfo("JPSkill_GlobalManager Start: PhotonNetwork.PrefabPool has been set to the CombinedPrefabPool.");
        }
    }


}
