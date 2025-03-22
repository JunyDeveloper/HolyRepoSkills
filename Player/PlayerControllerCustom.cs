using System.Collections;
using System.Reflection;
using JP_RepoHolySkills.Skills;
using Photon.Pun;
using UnityEngine;

namespace JP_RepoHolySkills.Player
{
    internal class PlayerControllerCustom : MonoBehaviour
    {
        public PhotonView photonView;
        public HealSkill healSkill;
        public HolyAuraSkill holyAuraSkill;
        public HolyWallSkill holyWallSkill;
        public Coroutine boostSprintCoroutine;
        public Coroutine staminaRegenCoroutine;
        public Coroutine healthRegenCoroutine;
        public PlayerHealth playerHealth;

        void Start()
        {
            Plugin.Logger.LogInfo("PlayerControllerCustom: Start called.");
            photonView = GetComponent<PhotonView>();
            playerHealth = GetComponent<PlayerHealth>();

            // Add skill components and log their initialization.
            healSkill = gameObject.AddComponent<HealSkill>();
            Plugin.Logger.LogInfo("PlayerControllerCustom: HealSkill component added.");

            holyAuraSkill = gameObject.AddComponent<HolyAuraSkill>();
            Plugin.Logger.LogInfo("PlayerControllerCustom: HolyAuraSkill component added.");

            holyWallSkill = gameObject.AddComponent<HolyWallSkill>();
            Plugin.Logger.LogInfo("PlayerControllerCustom: HolyWallSkill component added.");
        }

        void Update()
        {
            // Only run logic if in level mode and this is our local instance.
            if (!SemiFunc.RunIsLevel() || !photonView.IsMine)
                return;
        }

        public void StartOwnerRPCSprintBoost(float speedModifier, float duration)
        {
            Plugin.Logger.LogInfo($"PlayerControllerCustom: Requesting sprint boost with modifier {speedModifier} for {duration} seconds.");
            photonView.RPC("StartSprintBoostCoroutine_RPC", photonView.Owner, speedModifier, duration);
        }

        [PunRPC]
        private void StartSprintBoostCoroutine_RPC(float speedModifier, float duration)
        {
            Plugin.Logger.LogInfo("PlayerControllerCustom: Starting sprint boost coroutine RPC.");
            StartBoostSprintCoroutine(speedModifier, duration);
        }

        private void StartBoostSprintCoroutine(float speedModifier, float duration)
        {
            if (boostSprintCoroutine != null)
            {
                StopCoroutine(boostSprintCoroutine);
                Plugin.Logger.LogInfo("PlayerControllerCustom: Stopped existing sprint boost coroutine.");
            }
            boostSprintCoroutine = StartCoroutine(BoostSprint(speedModifier, duration));
        }

        private IEnumerator BoostSprint(float speedModifier, float duration)
        {
            // Use reflection to retrieve necessary fields from PlayerController.
            FieldInfo playerNameField = typeof(PlayerController).GetField("playerName", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo originalSprintField = typeof(PlayerController).GetField("playerOriginalSprintSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo originalMoveField = typeof(PlayerController).GetField("playerOriginalMoveSpeed", BindingFlags.NonPublic | BindingFlags.Instance);

            string playerName = (string)playerNameField.GetValue(PlayerController.instance);
            float originalSprint = (float)originalSprintField.GetValue(PlayerController.instance);
            float originalMove = (float)originalMoveField.GetValue(PlayerController.instance);

            Plugin.Logger.LogInfo($"{playerName}: Sprint boost starting. Original Walk: {PlayerController.instance.MoveSpeed}, Original Sprint: {PlayerController.instance.SprintSpeed}");

            // Calculate new speeds.
            float newWalkSpeed = PlayerController.instance.MoveSpeed * speedModifier;
            float newSprintSpeed = PlayerController.instance.SprintSpeed * speedModifier;
            Plugin.Logger.LogInfo($"{playerName}: New Walk: {newWalkSpeed}, New Sprint: {newSprintSpeed}");

            // Set boosted speeds.
            PlayerController.instance.MoveSpeed = newWalkSpeed;
            PlayerController.instance.SprintSpeed = newSprintSpeed;

            yield return new WaitForSeconds(duration);

            // Reset speeds after duration.
            PlayerController.instance.MoveSpeed = originalMove;
            PlayerController.instance.SprintSpeed = originalSprint;
            Plugin.Logger.LogInfo($"{playerName}: Sprint boost ended. Speeds reset to Walk: {PlayerController.instance.MoveSpeed}, Sprint: {PlayerController.instance.SprintSpeed}");
        }

        [PunRPC]
        private void SyncMoveSpeed(float walkSpeed, float sprintSpeed)
        {
            PlayerController.instance.MoveSpeed = walkSpeed;
            PlayerController.instance.SprintSpeed = sprintSpeed;
            Plugin.Logger.LogInfo($"PlayerControllerCustom: Synchronized move speeds: Walk = {walkSpeed}, Sprint = {sprintSpeed}");
        }

        public void StartOwnerRPCRegenStamina(float regenAmount, float duration)
        {
            Plugin.Logger.LogInfo($"PlayerControllerCustom: Requesting stamina regeneration: {regenAmount} per tick for {duration} seconds.");
            photonView.RPC("StartRegenStamina_RPC", photonView.Owner, regenAmount, duration);
        }

        [PunRPC]
        private void StartRegenStamina_RPC(float regenAmount, float duration)
        {
            Plugin.Logger.LogInfo("PlayerControllerCustom: Starting stamina regen RPC.");
            StartRegenStaminaCoroutine(regenAmount, duration);
        }

        private void StartRegenStaminaCoroutine(float regenAmount, float duration)
        {
            if (staminaRegenCoroutine != null)
            {
                StopCoroutine(staminaRegenCoroutine);
                Plugin.Logger.LogInfo("PlayerControllerCustom: Stopped existing stamina regen coroutine.");
            }
            staminaRegenCoroutine = StartCoroutine(RegenStamina(regenAmount, duration));
        }

        private IEnumerator RegenStamina(float regenAmount, float duration)
        {
            float elapsedTime = 0f;
            float tickInterval = 1f;

            while (elapsedTime < duration)
            {
                PlayerController.instance.EnergyCurrent = Mathf.Min(PlayerController.instance.EnergyCurrent + regenAmount, PlayerController.instance.EnergyStart);
                Plugin.Logger.LogInfo($"PlayerControllerCustom: Regenerating stamina. Current: {PlayerController.instance.EnergyCurrent}");
                elapsedTime += tickInterval;
                yield return new WaitForSeconds(tickInterval);
            }
        }

        public void StartOwnerRPCHealthRegen(int regenAmount, float duration)
        {
            Plugin.Logger.LogInfo($"PlayerControllerCustom: Requesting health regeneration: {regenAmount} per tick for {duration} seconds.");
            photonView.RPC("StartHealthRegen_RPC", photonView.Owner, regenAmount, duration);
        }

        [PunRPC]
        private void StartHealthRegen_RPC(int regenAmount, float duration)
        {
            Plugin.Logger.LogInfo("PlayerControllerCustom: Starting health regen RPC.");
            StartHealthRegenCoroutine(regenAmount, duration);
        }

        private void StartHealthRegenCoroutine(int regenAmount, float duration)
        {
            if (healthRegenCoroutine != null)
            {
                StopCoroutine(healthRegenCoroutine);
                Plugin.Logger.LogInfo("PlayerControllerCustom: Stopped existing health regen coroutine.");
            }
            healthRegenCoroutine = StartCoroutine(RegenHealth(regenAmount, duration));
        }

        private IEnumerator RegenHealth(int regenAmount, float duration)
        {
            FieldInfo healthField = typeof(PlayerHealth).GetField("health", BindingFlags.NonPublic | BindingFlags.Instance);
            int initialHealth = (int)healthField.GetValue(playerHealth);
            Plugin.Logger.LogInfo($"PlayerControllerCustom: Initial health is {initialHealth}.");

            float elapsedTime = 0f;
            float tickInterval = 1f;

            while (elapsedTime < duration)
            {
                playerHealth.Heal(regenAmount);
                int updatedHealth = (int)healthField.GetValue(playerHealth);
                Plugin.Logger.LogInfo($"PlayerControllerCustom: Health regenerated to {updatedHealth}.");
                photonView.RPC("SyncHealth", RpcTarget.OthersBuffered, updatedHealth);
                elapsedTime += tickInterval;
                yield return new WaitForSeconds(tickInterval);
            }
        }

        public void StartOwnerHealRPC(int amount)
        {
            Plugin.Logger.LogInfo($"PlayerControllerCustom: Requesting heal of {amount} HP.");
            photonView.RPC("Heal_RPC", photonView.Owner, amount);
        }

        [PunRPC]
        private void Heal_RPC(int amount)
        {
            Plugin.Logger.LogInfo($"PlayerControllerCustom: Heal_RPC received with amount {amount}.");
            Heal(amount);
        }

        public void Heal(int amount)
        {
            playerHealth.Heal(amount);
            FieldInfo healthField = typeof(PlayerHealth).GetField("health", BindingFlags.NonPublic | BindingFlags.Instance);
            if (healthField != null)
            {
                int updatedHealth = (int)healthField.GetValue(playerHealth);
                photonView.RPC("SyncHealth", RpcTarget.OthersBuffered, updatedHealth);
                Plugin.Logger.LogInfo($"PlayerControllerCustom: Healed. Updated health is {updatedHealth}.");
            }
            else
            {
                Plugin.Logger.LogWarning("PlayerControllerCustom: Field 'health' not found on PlayerHealth.");
            }
        }

        [PunRPC]
        private void SyncHealth(int updatedHealth)
        {
            FieldInfo healthField = typeof(PlayerHealth).GetField("health", BindingFlags.NonPublic | BindingFlags.Instance);
            if (healthField != null)
            {
                healthField.SetValue(playerHealth, updatedHealth);
                Plugin.Logger.LogInfo($"PlayerControllerCustom: Synchronized health to {updatedHealth}.");
            }
            else
            {
                Plugin.Logger.LogWarning("PlayerControllerCustom: Field 'health' not found on PlayerHealth in SyncHealth RPC.");
            }
        }
    }
}
