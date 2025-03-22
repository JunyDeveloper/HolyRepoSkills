using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JP_RepoHolySkills.GlobalMananger;
using JP_RepoHolySkills.SkillSelector;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace JP_RepoHolySkills.Skills
{
    public class HolyWallSkill : MonoBehaviour
    {
        public PhotonView photonView;
        public float skillDuration = 3f;      // Base wall duration.
        public float cooldownDuration = 180f; // Cooldown duration in seconds.

        // --- UI Fields for Holy Wall ---
        public GameObject holyWallIconInstance;
        public Color wallUICooldownStartColor = new Color(1f, 1f, 1f, 0f); // Fully transparent.
        public Color wallUICooldownEndColor = Color.white;                // Transitional color.
        public Color wallUIColor = new Color(0f, 1f, 0f, 1f);         // Final UI color when ready.

        // --- UI & Layout Constants ---
        private const string CANVAS_NAME = "Game Hud";
        private const int UI_LAYER = 5;
        private static readonly Vector3 UI_SCALE = new Vector3(13f, 13f, 13f);
        private const float UI_VERTICAL_OFFSET = 40f;

        // --- Cooldown State ---
        private bool isWallOnCooldown;
        private float currentWallCooldown;
        private bool wallUISetup;

        // --- Other Constants ---
        private const float GRENADE_SPAWN_CHANCE = 0.20f;
        private const float MINE_SPAWN_CHANCE = 0.20f;
        private const float FORWARD_OFFSET = 2f;
        private const float UP_OFFSET = 1f;
        private const float ADDITIONAL_SPAWN_DISTANCE = 1f;

        // Extraction haul thresholds.
        private const float EXTRACTION_THRESHOLD_SCALE = 500000f;    // ≥500,000: Increase wall size 2x.
        private const float EXTRACTION_THRESHOLD_DURATION = 1000000f; // ≥1,000,000: Wall duration increases to 6 sec.
        private const float EXTRACTION_THRESHOLD_GRENADE = 1500000f;   // ≥1,500,000: 20% chance to spawn stun grenade.
        private const float EXTRACTION_THRESHOLD_MINE = 2000000f;      // ≥2,000,000: 20% chance to spawn stun mine.

        void Start()
        {
            Plugin.Logger.LogInfo("HolyWallSkill: Initializing skill.");
            photonView = GetComponent<PhotonView>();
            isWallOnCooldown = false;
            wallUISetup = false;

            // For debugging, override cooldown if in debug mode.
            if (Plugin.Instance.isInDebugMode)
            {
                cooldownDuration = 1f;
            }

            Plugin.Logger.LogInfo("HolyWallSkill: Initialization complete.");
        }

        void Update()
        {
            if (!CanActivateSkill() || JPSkill_GlobalManager.Instance.selectedSkill != SelectableSkills.HolyWall)
                return;

            SetupUIIfNeeded();

            // Add parentheses for clarity.
            if (!isWallOnCooldown && Input.GetKeyDown(Plugin.ActivateSkillHotkey.Value.MainKey))
            {
                ActivateHolyWallSkill();
            }
        }

        private bool CanActivateSkill()
        {
            // Ensure proper level and ownership.
            if (!SemiFunc.RunIsLevel() || !photonView.IsMine)
                return false;

            // Prevent activation if chat is active.
            FieldInfo chatActiveField = typeof(ChatManager)
                .GetField("chatActive", BindingFlags.NonPublic | BindingFlags.Instance);
            bool isChatOpen = (bool)chatActiveField.GetValue(ChatManager.instance);
            return !isChatOpen;
        }

        private void SetupUIIfNeeded()
        {
            // If Holy Wall is selected and UI isn’t set up, spawn it.
            if (JPSkill_GlobalManager.Instance.selectedSkill == SelectableSkills.HolyWall && !wallUISetup)
            {
                SpawnHolyWallUI();
            }
        }

        public void SpawnHolyWallUI()
        {
            Plugin.Logger.LogInfo("SpawnHolyWallUI: Setting up Holy Wall UI.");
            wallUISetup = true;
            if (Plugin.AssetManager.TryGetValue(ClassModConstants.HOLY_WALL_ICON, out GameObject wallIconPrefab))
            {
                GameObject canvas = GameObject.Find(CANVAS_NAME);
                if (canvas == null)
                {
                    Plugin.Logger.LogWarning("SpawnHolyWallUI: 'Game Hud' canvas not found!");
                    return;
                }
                holyWallIconInstance = Instantiate(wallIconPrefab, transform.position, Quaternion.identity);
                holyWallIconInstance.layer = UI_LAYER;
                holyWallIconInstance.transform.localScale = UI_SCALE;
                holyWallIconInstance.transform.SetParent(canvas.transform, false);
                float halfCanvasHeight = canvas.GetComponent<RectTransform>().rect.height / 2;
                holyWallIconInstance.transform.localPosition = new Vector3(0f, UI_VERTICAL_OFFSET - halfCanvasHeight, 0f);
                Plugin.Logger.LogInfo("SpawnHolyWallUI: Holy Wall UI added at position: " + holyWallIconInstance.transform.localPosition);

                SpriteRenderer sr = holyWallIconInstance.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = wallUIColor;
                    Plugin.Logger.LogInfo("SpawnHolyWallUI: UI color set to wallUIColor.");
                }
                else
                {
                    Plugin.Logger.LogWarning("SpawnHolyWallUI: No SpriteRenderer found on Holy Wall UI prefab.");
                }
            }
            else
            {
                Plugin.Logger.LogWarning("SpawnHolyWallUI: Holy Wall icon asset not found!");
            }
        }

        private void ActivateHolyWallSkill()
        {
            Plugin.Logger.LogInfo("HolyWallSkill: F key pressed, activating Holy Wall skill.");
            isWallOnCooldown = true;
            StartCoroutine(WallCooldownCount());

            // Play sound effect.
            PlayHolyWallSFX();

            // Ensure Holy Wall asset exists.
            if (!Plugin.AssetManager.TryGetValue(ClassModConstants.HOLY_WALL, out GameObject holyWallAsset))
            {
                Plugin.Logger.LogWarning("HolyWallSkill: Holy Wall asset not found!");
                return;
            }
            Plugin.Logger.LogInfo("HolyWallSkill: Holy Wall asset found. Instantiating...");

            // Calculate spawn position and rotation.
            Vector3 spawnPosition = transform.position + transform.forward * FORWARD_OFFSET + Vector3.up * UP_OFFSET;
            Quaternion spawnRotation = transform.rotation;

            // Instantiate via PhotonNetwork.
            GameObject spawnedWall = PhotonNetwork.Instantiate(ClassModConstants.HOLY_WALL, spawnPosition, spawnRotation);

            // Adjust scale based on extraction haul.
            AdjustHolyWallScale(spawnedWall);

            // Determine effective duration.
            float effectiveDuration = GetEffectiveDuration();

            // Shield has control of it's own destruction since if player dies the wall stays up forever
            /*StartCoroutine(RemoveShieldInSeconds(spawnedWall, effectiveDuration));*/
            TimedDestroyer destroyer = spawnedWall.AddComponent<TimedDestroyer>();
            destroyer.lifeTime = effectiveDuration;


            // Attempt to spawn additional objects.
            TryRequestGrenadeSpawn();
            TryRequestMineSpawn();

            // Trigger a war cry.
            int randomWarcryIndex = Random.Range(0, ClassModConstants.HOLY_WALL_WAR_CRIES.Length);
            string warCry = ClassModConstants.HOLY_WALL_WAR_CRIES[randomWarcryIndex];
            Color holyBlue = new Color(0.5f, 0.7f, 1f, 1f);
            ChatManager.instance.PossessChatScheduleStart(10);
            ChatManager.instance.PossessChat(ChatManager.PossessChatID.SelfDestruct, warCry, 1.5f, holyBlue);
            ChatManager.instance.PossessChatScheduleEnd();
            Plugin.Logger.LogInfo($"HolyWallSkill: War cry triggered: {warCry}");
        }

        private IEnumerator WallCooldownCount()
        {
            Plugin.Logger.LogInfo("WallCooldownCount: Cooldown started.");
            currentWallCooldown = 0f;

            // Ensure holyWallIconInstance and its SpriteRenderer are not null.
            if (holyWallIconInstance == null)
            {
                Plugin.Logger.LogWarning("WallCooldownCount: holyWallIconInstance is null. Exiting cooldown coroutine.");
                yield break;
            }

            SpriteRenderer wallSprite = holyWallIconInstance.GetComponent<SpriteRenderer>();
            if (wallSprite == null)
            {
                Plugin.Logger.LogWarning("WallCooldownCount: SpriteRenderer not found on holyWallIconInstance. Exiting cooldown coroutine.");
                yield break;
            }

            wallSprite.color = Color.white;

            while (currentWallCooldown < cooldownDuration)
            {
                Color lerpedColor = Color.Lerp(wallUICooldownStartColor, wallUICooldownEndColor, currentWallCooldown / cooldownDuration);
                // Use the sprite's color property directly.
                wallSprite.color = lerpedColor;
                currentWallCooldown += Time.deltaTime;
                yield return null;
            }

            wallSprite.color = wallUIColor;
            isWallOnCooldown = false;
            Plugin.Logger.LogInfo("WallCooldownCount: Cooldown finished. UI color reset.");
        }

        private void PlayHolyWallSFX()
        {
            if (Plugin.AssetManager.TryGetValue(ClassModConstants.HOLY_WALL_SFX, out GameObject holyWallSFXPrefab))
            {
                AudioSource audioSrc = holyWallSFXPrefab.GetComponent<AudioSource>();
                if (audioSrc != null)
                {
                    audioSrc.volume = 0.2f;
                }
                Instantiate(holyWallSFXPrefab, transform.position, Quaternion.identity);
                Plugin.Logger.LogInfo("HolyWallSkill: Holy Wall SFX played.");
            }
            else
            {
                Plugin.Logger.LogWarning("HolyWallSkill: HolyWallSFX asset not found!");
            }
        }

        private void AdjustHolyWallScale(GameObject spawnedWall)
        {
            float scaleMultiplier = GetScaleMultiplier();
            Vector3 newScale = spawnedWall.transform.localScale;
            newScale.x *= scaleMultiplier;
            newScale.y *= scaleMultiplier;

            int wallViewID = spawnedWall.GetComponent<PhotonView>().ViewID;
            photonView.RPC("SetHolyWallScale_RPC", RpcTarget.All, wallViewID, newScale);
            Plugin.Logger.LogInfo($"HolyWallSkill: Holy Wall instantiated at {spawnedWall.transform.position} with scale multiplier: {scaleMultiplier}.");
        }

        private float GetScaleMultiplier()
        {
            float extractionHaul = GetPlayerExtractionHaul();
            return extractionHaul >= EXTRACTION_THRESHOLD_SCALE ? 2f : 1.5f;
        }

        private float GetPlayerExtractionHaul()
        {
            return JPSkill_GlobalManager.Instance.savedExtractionHaul;
        }

        private float GetEffectiveDuration()
        {
            float extractionHaul = GetPlayerExtractionHaul();
            return extractionHaul >= EXTRACTION_THRESHOLD_DURATION ? 6f : skillDuration;
        }

        private void TryRequestGrenadeSpawn()
        {
            float extractionHaul = GetPlayerExtractionHaul();
            if (extractionHaul >= EXTRACTION_THRESHOLD_GRENADE)
            {
                if (Random.value <= GRENADE_SPAWN_CHANCE)
                {
                    Vector3 spawnPos = transform.position + transform.forward * ADDITIONAL_SPAWN_DISTANCE + Vector3.up * UP_OFFSET;
                    Quaternion spawnRot = transform.rotation;
                    photonView.RPC("RequestGrenadeSpawn", RpcTarget.MasterClient, spawnPos, spawnRot);
                    Plugin.Logger.LogInfo("HolyWallSkill: Grenade spawn requested.");
                }
                else
                {
                    Plugin.Logger.LogInfo("HolyWallSkill: Grenade spawn chance failed.");
                }
            }
            else
            {
                Plugin.Logger.LogInfo("HolyWallSkill: Extraction haul below threshold for grenade spawn.");
            }
        }

        private void TryRequestMineSpawn()
        {
            float extractionHaul = GetPlayerExtractionHaul();
            if (extractionHaul >= EXTRACTION_THRESHOLD_MINE)
            {
                if (Random.value <= MINE_SPAWN_CHANCE)
                {
                    Vector3 spawnPos = transform.position + transform.forward * ADDITIONAL_SPAWN_DISTANCE + Vector3.up * UP_OFFSET;
                    Quaternion spawnRot = transform.rotation;
                    photonView.RPC("RequestMineSpawn", RpcTarget.MasterClient, spawnPos, spawnRot);
                    Plugin.Logger.LogInfo("HolyWallSkill: Mine spawn requested.");
                }
                else
                {
                    Plugin.Logger.LogInfo("HolyWallSkill: Mine spawn chance failed.");
                }
            }
            else
            {
                Plugin.Logger.LogInfo("HolyWallSkill: Extraction haul below threshold for mine spawn.");
            }
        }

        [PunRPC]
        public void RequestGrenadeSpawn(Vector3 spawnPos, Quaternion spawnRot)
        {
            if (!PhotonNetwork.IsMasterClient)
                return;
            GameObject grenade = PhotonNetwork.Instantiate("Items/Item Grenade Stun", spawnPos, spawnRot);
            Plugin.Logger.LogInfo($"HolyWallSkill: Grenade spawned at {spawnPos} with rotation {spawnRot.eulerAngles}");
        }

        [PunRPC]
        public void RequestMineSpawn(Vector3 spawnPos, Quaternion spawnRot)
        {
            if (!PhotonNetwork.IsMasterClient)
                return;
            GameObject mine = PhotonNetwork.Instantiate("Items/Item Mine Stun", spawnPos, spawnRot);
            Plugin.Logger.LogInfo($"HolyWallSkill: Mine spawned at {spawnPos} with rotation {spawnRot.eulerAngles}");
        }

        [PunRPC]
        void SetHolyWallScale_RPC(int spawnedWallViewID, Vector3 newScale)
        {
            PhotonView wallPV = PhotonView.Find(spawnedWallViewID);
            if (wallPV != null)
            {
                GameObject spawnedWall = wallPV.gameObject;
                spawnedWall.transform.localScale = newScale;
                Plugin.Logger.LogInfo("HolyWallSkill: Scale updated for Holy Wall.");
            }
            else
            {
                Plugin.Logger.LogWarning("HolyWallSkill: Holy Wall not found for scaling update!");
            }
        }

        /*  private IEnumerator RemoveShieldInSeconds(GameObject spawnedWall, float seconds)
          {
              yield return new WaitForSeconds(seconds);
              if (spawnedWall != null)
              {
                  PhotonNetwork.Destroy(spawnedWall);
                  Plugin.Logger.LogInfo("HolyWallSkill: Holy Wall destroyed after duration.");
              }
          }*/
    }
}
