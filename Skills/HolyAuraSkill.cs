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
    public class HolyAuraSkill : MonoBehaviour
    {
        #region Configurable Fields
        // Skill effect parameters.
        public float sprintBoostMultiplier = 1.4f;
        public int auraFinalScaleValue = 8; // Target scale value for the aura effect.
        public float auraExpansionTime = 0.3f;
        public float sprintBoostDuration = 4f;
        public float staminaRegenDuration = 4f;
        public float staminaRegenPercentage = 0.02f;
        public float auraCooldownDuration = 90f;
        public float baseAuraRange = 6.60f; // Base radius for buff application.
        #endregion

        #region UI Fields
        public GameObject auraIconInstance;
        public Color auraUICooldownStartColor = new Color(1f, 1f, 1f, 0f); // Fully transparent.
        public Color auraUICooldownEndColor = Color.white;                // Transitional color during cooldown.
        public Color auraUIColor = new Color(0f, 1f, 0f, 1f);       // Final UI color when cooldown finishes.
        #endregion

        #region Private Fields
        private PhotonView pv;
        #endregion

        #region Constants
        private const string CANVAS_NAME = "Game Hud";
        private const int UI_LAYER = 5;
        private static readonly Vector3 UI_SCALE = new Vector3(13f, 13f, 13f);
        private const float UI_VERTICAL_OFFSET = 40f;
        private static readonly Vector3 BUFF_PARTICLES_OFFSET = new Vector3(0f, 1f, 0f);
        private const float SOUND_VOLUME = 0.2f;
        #endregion

        #region State Fields
        private bool isAuraOnCooldown;
        private float currentAuraCooldown;
        private bool hasUIBeenSetup;
        #endregion

        void Start()
        {
            Plugin.Logger.LogInfo("HolyAuraSkill Start: Initializing skill.");
            isAuraOnCooldown = false;
            pv = GetComponent<PhotonView>();

            // Set a shorter cooldown if in debug mode.
            if (Plugin.Instance.isInDebugMode)
            {
                auraCooldownDuration = 1f;
            }

            Plugin.Logger.LogInfo("HolyAuraSkill Start: Initialization complete.");
        }

        void Update()
        {
            if (!ShouldProcessInput() || JPSkill_GlobalManager.Instance.selectedSkill != SelectableSkills.HolyAura)
                return;

            SetupUIIfNeeded();

            // Improved input condition: Activate skill on R key press when not on cooldown or if in debug mode.
            if (Input.GetKeyDown(Plugin.ActivateSkillHotkey.Value.MainKey) && !isAuraOnCooldown)
            {
                ActivateAuraSkill();
            }
        }

        #region Input & UI Helpers

        private bool ShouldProcessInput()
        {
            if (!SemiFunc.RunIsLevel() || !pv.IsMine)
                return false;

            if (IsChatActive())
            {
                // Plugin.Logger.LogInfo("HolyAuraSkill Update: Chat is active, skipping input.");
                return false;
            }
            return true;
        }

        private bool IsChatActive()
        {
            FieldInfo chatActiveField = typeof(ChatManager)
                .GetField("chatActive", BindingFlags.NonPublic | BindingFlags.Instance);
            if (chatActiveField == null)
            {
                // Plugin.Logger.LogWarning("IsChatActive: Unable to find 'chatActive' field on ChatManager.");
                return false;
            }
            return (bool)chatActiveField.GetValue(ChatManager.instance);
        }

        private void SetupUIIfNeeded()
        {
            if (JPSkill_GlobalManager.Instance.selectedSkill == SelectableSkills.HolyAura && !hasUIBeenSetup)
            {
                Plugin.Logger.LogInfo("HolyAuraSkill Update: HolyAura skill selected; setting up UI.");
                SpawnAuraUI();
            }
        }
        #endregion

        #region Skill Activation

        private void ActivateAuraSkill()
        {
            Plugin.Logger.LogInfo("HolyAuraSkill Update: R key pressed, activating Holy Aura skill.");
            isAuraOnCooldown = true;
            pv.RPC("ActivateAuraSFX_RPC", RpcTarget.All, transform.position);
            StartCoroutine(AuraCooldownCount());
            TypeWarCry();

            // Compute a scale multiplier for the spawned aura based on extraction haul.
            float extractionHaul = JPSkill_GlobalManager.Instance.savedExtractionHaul;
            float auraScaleMultiplier = extractionHaul >= 500000f ? 2f : 1f;

            // Expand the aura effect on all clients with the appropriate scale.
            pv.RPC("ExpandAura_RPC", RpcTarget.All, transform.position, auraScaleMultiplier);
            Plugin.Logger.LogInfo("HolyAuraSkill Update: ExpandAura_RPC called with scale multiplier " + auraScaleMultiplier + ".");

            ApplyBuffsToPlayers();
        }

        private void ApplyBuffsToPlayers()
        {
            float extractionHaul = JPSkill_GlobalManager.Instance.savedExtractionHaul;
            // Determine effective aura range.
            float effectiveAuraRange = baseAuraRange;
            if (extractionHaul >= 500000f)
                effectiveAuraRange *= 2f;

            // Count additional players (excluding caster) within effective aura range.
            int additionalPlayers = 0;
            List<PlayerAvatar> allAvatars = SemiFunc.PlayerGetAll();
            foreach (PlayerAvatar avatar in allAvatars)
            {
                if (avatar == null || avatar.gameObject == this.gameObject)
                    continue;
                if (Vector3.Distance(transform.position, avatar.transform.position) <= effectiveAuraRange)
                {
                    FieldInfo deadField = typeof(PlayerAvatar).GetField("deadSet", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (deadField != null)
                    {
                        bool isDead = (bool)deadField.GetValue(avatar);
                        if (!isDead)
                        {
                            additionalPlayers++;
                        }
                    }
                }
            }

            // Base stamina regen percentage: 2% normally, 4% if extraction haul >= 1,000,000.
            float baseRegenPct = extractionHaul >= 1000000f ? 0.04f : 0.02f;
            // Increase by 2% per additional player.
            float effectiveRegenPct = baseRegenPct + additionalPlayers * 0.02f;

            // Adjust sprint boost duration: 8 seconds if extraction haul >= 1,500,000.
            float effectiveSprintDuration = extractionHaul >= 1500000f ? 8f : sprintBoostDuration;
            // Adjust regen duration: 8 seconds if extraction haul >= 2,000,000.
            float effectiveRegenDuration = extractionHaul >= 2000000f ? 8f : staminaRegenDuration;

            Plugin.Logger.LogInfo($"HolyAuraSkill: {additionalPlayers} additional players in range; effective regen percentage is {effectiveRegenPct * 100}%.");

            // Apply buffs to each player in range.
            foreach (PlayerAvatar avatar in allAvatars)
            {
                if (avatar == null)
                {
                    Plugin.Logger.LogWarning("HolyAuraSkill: Encountered null player avatar. Skipping.");
                    continue;
                }
                float distance = Vector3.Distance(transform.position, avatar.transform.position);
                if (distance > effectiveAuraRange)
                {
                    Plugin.Logger.LogInfo($"HolyAuraSkill: '{avatar.name}' is outside the effective aura range ({effectiveAuraRange}). Skipping.");
                    continue;
                }

                Plugin.Logger.LogInfo($"HolyAuraSkill: Applying buffs to '{avatar.name}'.");

                PlayerControllerCustom controller = avatar.GetComponent<PlayerControllerCustom>();
                int avatarViewID = avatar.GetComponent<PhotonView>().ViewID;
                //pv.RPC("SpawnAuraBuffParticles_RPC", RpcTarget.All, avatarViewID);
                //Plugin.Logger.LogInfo($"HolyAuraSkill: Buff particles spawned for '{avatar.name}' (ViewID: {avatarViewID}).");

                controller.StartOwnerRPCSprintBoost(sprintBoostMultiplier, effectiveSprintDuration);
                controller.StartOwnerRPCRegenStamina(PlayerController.instance.EnergyStart * effectiveRegenPct, effectiveRegenDuration);
            }
        }
        #endregion

        #region UI and Cooldown

        public void SpawnAuraUI()
        {
            Plugin.Logger.LogInfo("SpawnAuraUI: Setting up Holy Aura UI.");
            hasUIBeenSetup = true;
            if (Plugin.AssetManager.TryGetValue(ClassModConstants.HOLY_AURA_ICON_ASSET, out GameObject auraIconPrefab))
            {
                GameObject canvas = GameObject.Find(CANVAS_NAME);
                if (canvas == null)
                {
                    Plugin.Logger.LogWarning("SpawnAuraUI: 'Game Hud' canvas not found!");
                    return;
                }
                auraIconInstance = Instantiate(auraIconPrefab, transform.position, Quaternion.identity);
                auraIconInstance.layer = UI_LAYER;
                auraIconInstance.transform.localScale = UI_SCALE;
                auraIconInstance.transform.SetParent(canvas.transform, false);
                float halfCanvasHeight = canvas.GetComponent<RectTransform>().rect.height / 2;
                auraIconInstance.transform.localPosition = new Vector3(0f, UI_VERTICAL_OFFSET - halfCanvasHeight, 0f);
                Plugin.Logger.LogInfo("SpawnAuraUI: Holy Aura UI added to canvas at position: " + auraIconInstance.transform.localPosition);

                if (!isAuraOnCooldown)
                {
                    SpriteRenderer sr = auraIconInstance.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = auraUIColor;
                        Plugin.Logger.LogInfo("SpawnAuraUI: UI color set to auraUIColor.");
                    }
                    else
                    {
                        Plugin.Logger.LogWarning("SpawnAuraUI: No SpriteRenderer found on Holy Aura UI prefab.");
                    }
                }
            }
            else
            {
                Plugin.Logger.LogWarning("SpawnAuraUI: Holy Aura icon asset not found!");
            }
        }

        private IEnumerator AuraCooldownCount()
        {
            Plugin.Logger.LogInfo("AuraCooldownCount: Cooldown started.");
            currentAuraCooldown = 0f;
            SpriteRenderer auraSprite = auraIconInstance.GetComponent<SpriteRenderer>();
            auraSprite.color = Color.white;

            while (currentAuraCooldown < auraCooldownDuration)
            {
                Color lerpedColor = Color.Lerp(auraUICooldownStartColor, auraUICooldownEndColor, currentAuraCooldown / auraCooldownDuration);
                // Use sprite's color property consistently.
                auraSprite.color = lerpedColor;
                currentAuraCooldown += Time.deltaTime;

                // Plugin.Logger.LogInfo("currentAuraCooldown: " + currentAuraCooldown);
                yield return null;
            }

            auraSprite.color = auraUIColor;
            isAuraOnCooldown = false;
            Plugin.Logger.LogInfo("AuraCooldownCount: Cooldown finished. UI color reset.");
        }
        #endregion

        #region RPC and Effect Methods

        [PunRPC]
        void ActivateAuraSFX_RPC(Vector3 position)
        {
            Plugin.Logger.LogInfo("ActivateAuraSFX_RPC: Called at position " + position);
            ActivateAuraSFX(position);
        }

        private void ActivateAuraSFX(Vector3 position)
        {
            if (Plugin.AssetManager.TryGetValue(ClassModConstants.HOLY_AURA_SFX_ASSET, out GameObject auraSFXPrefab))
            {
                AudioSource audioSrc = auraSFXPrefab.GetComponent<AudioSource>();
                if (audioSrc != null)
                {
                    audioSrc.volume = SOUND_VOLUME;
                }
                Instantiate(auraSFXPrefab, position, Quaternion.identity);
                Plugin.Logger.LogInfo("ActivateAuraSFX: Sound effect instantiated at " + position);
            }
            else
            {
                Plugin.Logger.LogWarning("ActivateAuraSFX: Holy Aura SFX asset not found!");
            }
        }

        // Updated RPC to accept a scale multiplier.
        [PunRPC]
        public void ExpandAura_RPC(Vector3 position, float scaleMultiplier)
        {
            Plugin.Logger.LogInfo("ExpandAura_RPC: Spawning Holy Aura effect at " + position + " with scale multiplier " + scaleMultiplier);
            if (Plugin.AssetManager.TryGetValue(ClassModConstants.HOLY_AURA_ASSET, out GameObject auraPrefab))
            {
                StartCoroutine(ExpandAuraEffect(auraPrefab, position, scaleMultiplier));
            }
            else
            {
                Plugin.Logger.LogWarning("ExpandAura_RPC: Holy Aura asset not found!");
            }
        }

        // Modified to apply the scale multiplier to the spawned aura.
        private IEnumerator ExpandAuraEffect(GameObject auraPrefab, Vector3 position, float scaleMultiplier)
        {
            Plugin.Logger.LogInfo("ExpandAuraEffect: Instantiating Holy Aura effect.");
            GameObject spawnedAura = Instantiate(auraPrefab, position, Quaternion.identity);
            float elapsed = 0f;
            Vector3 initialScale = Vector3.zero;
            Vector3 targetScale = new Vector3(auraFinalScaleValue, auraFinalScaleValue, auraFinalScaleValue) * scaleMultiplier;

            while (elapsed < auraExpansionTime)
            {
                spawnedAura.transform.localScale = Vector3.Lerp(initialScale, targetScale, elapsed / auraExpansionTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
            spawnedAura.transform.localScale = targetScale;
            Plugin.Logger.LogInfo("ExpandAuraEffect: Final scale set to " + targetScale);

            ParticleSystem ps = spawnedAura.GetComponent<ParticleSystem>();
            float effectDuration = ps != null ? ps.main.duration + 1.5f : 1.5f;
            Plugin.Logger.LogInfo("ExpandAuraEffect: Waiting " + effectDuration + " seconds before destroying the aura effect.");
            yield return new WaitForSeconds(effectDuration);

            Plugin.Logger.LogInfo("ExpandAuraEffect: Destroying Holy Aura effect.");
            if (spawnedAura != null)
            {
                Destroy(spawnedAura);
            }
        }

        [PunRPC]
        public void TypeWarCry_RPC() // Optional RPC version if needed.
        {
            TypeWarCry();
        }

        public void TypeWarCry()
        {
            Color auraGold = new Color(1f, 0.85f, 0.45f, 1f);
            ChatManager.instance.PossessChatScheduleStart(10);
            int randomIndex = Random.Range(0, ClassModConstants.HOLY_WAR_CRIES.Length);
            string warCry = ClassModConstants.HOLY_WAR_CRIES[randomIndex];
            ChatManager.instance.PossessChat(ChatManager.PossessChatID.SelfDestruct, warCry, 1.5f, auraGold);
            ChatManager.instance.PossessChatScheduleEnd();
            Plugin.Logger.LogInfo($"TypeWarCry: War cry '{warCry}' triggered.");
        }

        [PunRPC]
        public void SpawnAuraBuffParticles_RPC(int avatarViewID)
        {
            PhotonView avatarPV = PhotonView.Find(avatarViewID);
            if (avatarPV != null)
            {
                GameObject avatar = avatarPV.gameObject;
                if (Plugin.AssetManager.TryGetValue(ClassModConstants.HOLY_AURA_BUFF_ASSET, out GameObject buffParticlesPrefab))
                {
                    Vector3 spawnPos = avatar.transform.position + BUFF_PARTICLES_OFFSET;
                    Quaternion spawnRot = Quaternion.identity;
                    Instantiate(buffParticlesPrefab, spawnPos, spawnRot);
                    GameObject particles = Instantiate(buffParticlesPrefab, spawnPos, spawnRot, avatar.transform);
                    Plugin.Logger.LogInfo($"SpawnAuraBuffParticles_RPC: Buff particles spawned and attached to '{avatar.name}' (ViewID: {avatarViewID}).");
                }
                else
                {
                    Plugin.Logger.LogWarning("SpawnAuraBuffParticles_RPC: Buff particles asset not found!");
                }
            }
            else
            {
                Plugin.Logger.LogWarning("SpawnAuraBuffParticles_RPC: Avatar with ViewID " + avatarViewID + " not found.");
            }
        }
        #endregion
    }
}
