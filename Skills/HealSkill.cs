using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JP_RepoHolySkills.GlobalMananger; // (Consider renaming this namespace if "GlobalManager" was intended.)
using JP_RepoHolySkills.Player;
using JP_RepoHolySkills.SkillSelector;
using Photon.Pun;
using UnityEngine;
using Random = UnityEngine.Random;

namespace JP_RepoHolySkills.Skills
{
    public class HealSkill : MonoBehaviour
    {
        #region Configurable Fields
        // Base heal values.
        public int baseHealAmount = 10;
        public float healRange = 4.5f; // Base heal range.
        public float healthRegenPercentage = 0.02f; // 2% base regen.
        public float regenDuration = 3f; // Base regen duration.

        // Heal UI fields.
        public GameObject healIconInstance;
        public Color healUICooldownStartColor = new Color(1f, 1f, 1f, 0f); // Fully transparent.
        public Color healUICooldownEndColor = Color.white;                // Transitional color.
        public Color healUIColor = new Color(0f, 1f, 0f, 1f);               // Final UI color.
        public float cooldownDuration = 180f; // Duration of the cooldown.
        #endregion

        #region Private Fields
        private PhotonView pv;
        private bool isOnCooldown;
        #endregion

        #region Constants (Extraction Haul Thresholds)
        private const string CANVAS_NAME = "Game Hud";
        private const int UI_LAYER = 5;
        private static readonly Vector3 UI_SCALE = new Vector3(13f, 13f, 13f);
        private const float UI_VERTICAL_OFFSET = 40f;
        private const float SOUND_VOLUME = 0.2f;

        // Extraction haul thresholds.
        private const int BASE_HEAL_UPGRADE_THRESHOLD = 500000;      // ≥500,000: base heal becomes 20.
        private const int DOUBLE_HEAL_RADIUS_THRESHOLD = 1000000;      // ≥1,000,000: heal range doubles.
        private const int HEAL_REGEN_INCREASE_THRESHOLD = 1500000;     // ≥1,500,000: regen percentage becomes 4%.
        private const int REGEN_DURATION_INCREASE_THRESHOLD = 2000000;   // ≥2,000,000: regen duration increases to 6.
        private const int REVIVAL_THRESHOLD = 2500000;                 // ≥2,500,000: can revive everyone (once per game).
        #endregion

        #region State Fields
        private bool hasRevivedAll = false;
        #endregion

        void Start()
        {
            pv = GetComponent<PhotonView>();
            Plugin.Logger.LogInfo("HealSkill: Started.");

            // Set a shorter cooldown when in debug mode.
            if (Plugin.Instance.isInDebugMode)
            {
                cooldownDuration = 1f;
            }
        }

        void Update()
        {
            if (!ShouldProcessInput() || JPSkill_GlobalManager.Instance.selectedSkill != SelectableSkills.Heal)
                return;

            SetupUIIfNeeded();

            if (Input.GetKeyDown(Plugin.ActivateSkillHotkey.Value.MainKey) && !isOnCooldown)
            {
                ActivateHealSkill();
            }
        }

        #region Input & UI Helpers

        private bool ShouldProcessInput()
        {
            if (!SemiFunc.RunIsLevel() || !pv.IsMine)
                return false;
            if (IsChatActive())
                return false;
            return true;
        }

        private bool IsChatActive()
        {
            FieldInfo chatActiveField = typeof(ChatManager)
                .GetField("chatActive", BindingFlags.NonPublic | BindingFlags.Instance);
            if (chatActiveField == null)
            {
                //  Plugin.Logger.LogWarning("IsChatActive: 'chatActive' field not found in ChatManager.");
                return false;
            }
            return (bool)chatActiveField.GetValue(ChatManager.instance);
        }

        private void SetupUIIfNeeded()
        {
            if (JPSkill_GlobalManager.Instance.selectedSkill == SelectableSkills.Heal && healIconInstance == null)
            {
                Plugin.Logger.LogInfo("HealSkill: Rendering Heal UI for selected Heal skill.");
                RenderHealUI();
            }
        }
        #endregion

        #region Skill Activation

        private void ActivateHealSkill()
        {
            Plugin.Logger.LogInfo("HealSkill: Activated by local player.");
            isOnCooldown = true;
            TriggerWarCry();
            pv.RPC("PlayHealSkillSFX_RPC", RpcTarget.All, transform.position);

            // Compute effective values.
            int effectiveBaseHeal;
            float effectiveHealRange, effectiveRegenPercentage, effectiveRegenDuration, healParticleScaleMultiplier;
            bool allowRevival;
            ComputeEffectiveValues(out effectiveBaseHeal, out effectiveHealRange, out effectiveRegenPercentage,
                                   out effectiveRegenDuration, out allowRevival, out healParticleScaleMultiplier);

            // Play particle effect with scale multiplier.
            pv.RPC("PlayHealSkillParticles_RPC", RpcTarget.All, transform.position, healParticleScaleMultiplier);
            Plugin.Logger.LogInfo("HealSkill: SFX and particle effects triggered.");

            // Count additional players for healing multiplier.
            int additionalPlayersCount = CountAdditionalPlayers(effectiveHealRange);

            // Process revivals based on death head positions.
            ProcessRevival(effectiveHealRange, allowRevival);

            // Calculate healing multiplier.
            float healingMultiplier = 1f + additionalPlayersCount * 0.03f;
            Plugin.Logger.LogInfo($"HealSkill: Healing multiplier is {healingMultiplier} ({additionalPlayersCount} additional players in range).");

            // Process healing on targets.
            ProcessHealing(effectiveHealRange, effectiveBaseHeal, healingMultiplier, effectiveRegenPercentage, effectiveRegenDuration);

            // Mark revival as used if applicable.
            if (allowRevival)
            {
                hasRevivedAll = true;
            }

            pv.RPC("PlayHealReviveSFX_RPC", RpcTarget.All);

            if (healIconInstance != null)
            {
                StartCoroutine(HealCooldownCount());
            }
            else
            {
                Plugin.Logger.LogWarning("HealSkill: healIconInstance is null; cannot start cooldown transparency effect.");
            }
        }

        private void ComputeEffectiveValues(out int effectiveBaseHeal, out float effectiveHealRange, out float effectiveRegenPercentage,
                                            out float effectiveRegenDuration, out bool allowRevival, out float healParticleScaleMultiplier)
        {
            int extractionHaul = JPSkill_GlobalManager.Instance.savedExtractionHaul;
            effectiveBaseHeal = extractionHaul >= BASE_HEAL_UPGRADE_THRESHOLD ? 20 : baseHealAmount;
            effectiveHealRange = extractionHaul >= DOUBLE_HEAL_RADIUS_THRESHOLD ? healRange * 2f : healRange;
            effectiveRegenPercentage = extractionHaul >= HEAL_REGEN_INCREASE_THRESHOLD ? 0.04f : healthRegenPercentage;
            effectiveRegenDuration = extractionHaul >= REGEN_DURATION_INCREASE_THRESHOLD ? 6f : regenDuration;
            allowRevival = extractionHaul >= REVIVAL_THRESHOLD;
            healParticleScaleMultiplier = extractionHaul >= DOUBLE_HEAL_RADIUS_THRESHOLD ? 2f : 1f;
        }

        private int CountAdditionalPlayers(float effectiveHealRange)
        {
            int count = 0;
            List<PlayerAvatar> allAvatars = SemiFunc.PlayerGetAll();
            foreach (PlayerAvatar avatar in allAvatars)
            {
                if (avatar == null || avatar.gameObject == this.gameObject)
                    continue;
                if (Vector3.Distance(transform.position, avatar.transform.position) <= effectiveHealRange)
                {
                    FieldInfo deadField = typeof(PlayerAvatar).GetField("deadSet", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (deadField != null)
                    {
                        bool isDead = (bool)deadField.GetValue(avatar);
                        if (!isDead)
                        {
                            count++;
                        }
                    }
                }
            }
            return count;
        }

        private void ProcessRevival(float effectiveHealRange, bool allowRevival)
        {
            // Get all death heads.
            List<PlayerAvatar> allAvatars = SemiFunc.PlayerGetAll();
            List<PlayerDeathHead> deathHeads = new List<PlayerDeathHead>();
            foreach (PlayerAvatar avatar in allAvatars)
            {
                if (avatar != null && avatar.playerDeathHead != null)
                {
                    deathHeads.Add(avatar.playerDeathHead);
                }
            }

            // Revive any target whose death head is in range.
            foreach (PlayerDeathHead deathHead in deathHeads)
            {
                if (Vector3.Distance(transform.position, deathHead.transform.position) > effectiveHealRange)
                {
                    continue;
                }
                PlayerAvatar target = deathHead.playerAvatar;
                FieldInfo deadField = typeof(PlayerAvatar).GetField("deadSet", BindingFlags.NonPublic | BindingFlags.Instance);
                if (deadField == null)
                {
                    Plugin.Logger.LogWarning("HealSkill: 'deadSet' field not found in PlayerAvatar");
                    continue;
                }
                bool isDead = (bool)deadField.GetValue(target);

                if (isDead)
                {
                    if (allowRevival && !hasRevivedAll)
                    {
                        Plugin.Logger.LogInfo($"HealSkill: '{target.name}' is dead. Reviving...");
                        target.Revive();
                        Plugin.Logger.LogInfo($"HealSkill: '{target.name}' revived successfully.");
                        pv.RPC("PlayHealReviveParticles_RPC", RpcTarget.All, target.photonView.ViewID);
                    }
                    else
                    {
                        Plugin.Logger.LogInfo($"HealSkill: '{target.name}' is dead, but revival is not allowed or already used.");
                    }
                }
            }
        }

        private void ProcessHealing(float effectiveHealRange, int effectiveBaseHeal, float healingMultiplier, float effectiveRegenPercentage, float effectiveRegenDuration)
        {
            List<PlayerAvatar> allAvatars = SemiFunc.PlayerGetAll();
            foreach (PlayerAvatar target in allAvatars)
            {
                if (target == null)
                {
                    Plugin.Logger.LogWarning("HealSkill: Encountered a null player avatar. Skipping.");
                    continue;
                }

                float distance = Vector3.Distance(transform.position, target.transform.position);
                Plugin.Logger.LogInfo($"HealSkill: Checking '{target.name}' at distance {distance:F2}.");
                if (distance > effectiveHealRange)
                {
                    Plugin.Logger.LogInfo($"HealSkill: '{target.name}' is outside the effective heal range ({effectiveHealRange}). Skipping.");
                    continue;
                }

                PlayerHealth targetHealth = target.GetComponent<PlayerHealth>();
                if (targetHealth == null)
                {
                    Plugin.Logger.LogWarning($"HealSkill: '{target.name}' has no PlayerHealth component. Skipping.");
                    continue;
                }
                Plugin.Logger.LogInfo($"HealSkill: Found PlayerHealth for '{target.name}'.");

                FieldInfo maxHealthField = typeof(PlayerHealth).GetField("maxHealth", BindingFlags.NonPublic | BindingFlags.Instance);
                if (maxHealthField == null)
                {
                    Plugin.Logger.LogWarning($"HealSkill: 'maxHealth' field not found in PlayerHealth for '{target.name}'. Skipping.");
                    continue;
                }
                int maxHealth = (int)maxHealthField.GetValue(targetHealth);
                Plugin.Logger.LogInfo($"HealSkill: '{target.name}' max health is {maxHealth}.");

                PlayerControllerCustom targetController = target.GetComponent<PlayerControllerCustom>();
                if (targetController == null)
                {
                    Plugin.Logger.LogWarning($"HealSkill: No PlayerControllerCustom component found on '{target.name}'. Skipping.");
                    continue;
                }
                FieldInfo deadField = typeof(PlayerAvatar).GetField("deadSet", BindingFlags.NonPublic | BindingFlags.Instance);
                if (deadField == null)
                {
                    Plugin.Logger.LogWarning($"HealSkill: 'deadSet' field not found in PlayerAvatar for '{target.name}'. Skipping.");
                    continue;
                }
                bool isDead = (bool)deadField.GetValue(target);

                if (!isDead)
                {
                    int finalHealAmount = (int)(effectiveBaseHeal * healingMultiplier);
                    Plugin.Logger.LogInfo($"HealSkill: Healing '{target.name}' with {finalHealAmount} health (base: {effectiveBaseHeal}, multiplier: {healingMultiplier}).");
                    targetController.StartOwnerHealRPC(finalHealAmount);

                    int regenAmount = (int)(maxHealth * effectiveRegenPercentage);
                    Plugin.Logger.LogInfo($"HealSkill: Starting health regeneration for '{target.name}' with {regenAmount} health over {effectiveRegenDuration} seconds.");
                    targetController.StartOwnerRPCHealthRegen(regenAmount, effectiveRegenDuration);
                }
            }
        }

        private void TriggerWarCry()
        {
            int randomWarcryIndex = Random.Range(0, ClassModConstants.HEAL_WAR_CRIES.Length);
            string warCry = ClassModConstants.HEAL_WAR_CRIES[randomWarcryIndex];
            Plugin.Logger.LogInfo($"HealSkill: Selected warcry: {warCry}");
            Color healingGreen = new Color(0.5f, 1f, 0.5f, 1f);
            ChatManager.instance.PossessChatScheduleStart(10);
            ChatManager.instance.PossessChat(ChatManager.PossessChatID.SelfDestruct, warCry, 1.5f, healingGreen);
            ChatManager.instance.PossessChatScheduleEnd();
        }
        #endregion

        #region RPC and Effect Methods

        [PunRPC]
        public void PlayHealReviveParticles_RPC(int targetViewID)
        {
            PhotonView targetPV = PhotonView.Find(targetViewID);
            if (targetPV == null)
            {
                Plugin.Logger.LogWarning("HealSkill: Could not find player for heal revive particles.");
                return;
            }
            GameObject targetPlayer = targetPV.gameObject;
            Vector3 playerPosition = targetPlayer.transform.position;

            if (Plugin.AssetManager.TryGetValue(ClassModConstants.HEAL_REVIVE_SKILL, out GameObject healRevivePrefab))
            {
                Plugin.Logger.LogInfo($"HealSkill: Spawning revive particle effect at {playerPosition}.");
                GameObject particles = Instantiate(healRevivePrefab, playerPosition, Quaternion.identity);
                Plugin.Logger.LogInfo($"HealSkill: Revive particles spawned for '{targetPlayer.name}' at {particles.transform.position}.");
                particles.transform.SetParent(targetPlayer.transform, true);
                Destroy(particles, 5f);
            }
            else
            {
                Plugin.Logger.LogWarning("HealSkill: HealReviveSkill particle asset not found!");
            }
        }

        [PunRPC]
        public void PlayHealReviveSFX_RPC()
        {
            if (Plugin.AssetManager.TryGetValue(ClassModConstants.HEAL_REVIVE_SKILL_SFX, out GameObject healReviveSFX))
            {
                AudioSource audioSource = healReviveSFX.GetComponent<AudioSource>();
                if (audioSource != null)
                    audioSource.volume = SOUND_VOLUME;
                Instantiate(healReviveSFX, transform.position, Quaternion.identity);
                Plugin.Logger.LogInfo("HealSkill: Heal Revive Skill SFX played.");
            }
            else
            {
                Plugin.Logger.LogWarning("HealSkill: HealReviveSkillSFX asset not found!");
            }
        }

        [PunRPC]
        public void PlayHealSkillSFX_RPC(Vector3 position)
        {
            PlayHealSkillSFX(position);
        }

        public void PlayHealSkillSFX(Vector3 position)
        {
            if (Plugin.AssetManager.TryGetValue(ClassModConstants.HEAL_SKILL_SFX, out GameObject healSFXPrefab))
            {
                AudioSource audioSource = healSFXPrefab.GetComponent<AudioSource>();
                if (audioSource != null)
                    audioSource.volume = SOUND_VOLUME;
                Instantiate(healSFXPrefab, position, Quaternion.identity);
                Plugin.Logger.LogInfo("HealSkill: HealSkillSFX played at position " + position);
            }
            else
            {
                Plugin.Logger.LogWarning("HealSkill: HealSkillSFX asset not found!");
            }
        }

        [PunRPC]
        public void PlayHealSkillParticles_RPC(Vector3 position, float scaleMultiplier)
        {
            PlayHealSkillParticles(position, scaleMultiplier);
        }

        public void PlayHealSkillParticles(Vector3 position, float scaleMultiplier)
        {
            if (Plugin.AssetManager.TryGetValue(ClassModConstants.HEAL_SKILL, out GameObject healParticlesPrefab))
            {
                GameObject particles = Instantiate(healParticlesPrefab, position, Quaternion.identity);
                particles.transform.localScale *= scaleMultiplier;
                Plugin.Logger.LogInfo("HealSkill: HealSkillParticles played at position " + position + " with scale multiplier " + scaleMultiplier);
            }
            else
            {
                Plugin.Logger.LogWarning("HealSkill: HealSkillParticles asset not found!");
            }
        }
        #endregion

        #region UI and Cooldown

        public void RenderHealUI()
        {
            if (Plugin.AssetManager.TryGetValue(ClassModConstants.HEAL_SKILL_ICON, out GameObject healSkillIcon))
            {
                GameObject canvas = GameObject.Find(CANVAS_NAME);
                if (canvas == null)
                {
                    Plugin.Logger.LogWarning("HealSkill: 'Game Hud' canvas not found!");
                    return;
                }
                healIconInstance = Instantiate(healSkillIcon, transform.position, Quaternion.identity);
                healIconInstance.layer = UI_LAYER;
                healIconInstance.transform.localScale = UI_SCALE;
                healIconInstance.transform.SetParent(canvas.transform, false);
                float halfCanvasHeight = canvas.GetComponent<RectTransform>().rect.height / 2;
                healIconInstance.transform.localPosition = new Vector3(0f, UI_VERTICAL_OFFSET - halfCanvasHeight, 0f);
                Plugin.Logger.LogInfo("HealSkill: Heal UI rendered on canvas at position " + healIconInstance.transform.localPosition);

                SpriteRenderer sr = healIconInstance.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = healUIColor;
                    Plugin.Logger.LogInfo("SpawnHolyWallUI: UI color set to wallUIColor.");
                }
                else
                {
                    Plugin.Logger.LogWarning("SpawnHolyWallUI: No SpriteRenderer found on Holy Wall UI prefab.");
                }
            }
            else
            {
                Plugin.Logger.LogWarning("HealSkill: HealSkillIcon asset not found!");
            }
        }

        private IEnumerator HealCooldownCount()
        {
            Plugin.Logger.LogInfo("HealSkill: Heal cooldown started.");
            float currentCooldown = 0f;
            SpriteRenderer sr = healIconInstance.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                Plugin.Logger.LogWarning("HealSkill: No SpriteRenderer found on healIconInstance.");
                yield break;
            }

            sr.color = healUICooldownStartColor;
            Plugin.Logger.LogInfo("HealSkill: Initial UI color set.");

            while (currentCooldown < cooldownDuration)
            {
                float lerpRatio = currentCooldown / cooldownDuration;
                sr.color = Color.Lerp(healUICooldownStartColor, healUICooldownEndColor, lerpRatio);
                currentCooldown += Time.deltaTime;
                yield return null;
            }

            sr.color = healUIColor;
            isOnCooldown = false;
            Plugin.Logger.LogInfo("HealSkill: Cooldown finished. UI color set to base color.");
        }
        #endregion
    }
}
