using System;
using System.Collections.Generic;
using System.Text;

public static class ClassModConstants
{
    // Asset Keys
    public const string HOLY_AURA_ASSET = "HolyAura";
    public const string HOLY_AURA_BUFF_ASSET = "HolyAuraBuff";
    public const string HOLY_AURA_ICON_ASSET = "HolyAuraIcon";
    public const string HOLY_AURA_SFX_ASSET = "HolyAuraSFX";

    public const string HEAL_SKILL = "HealSkill";
    public const string HEAL_SKILL_ICON = "HealSkillIcon";
    public const string HEAL_SKILL_SFX = "HealSkillSFX";
    public const string HEAL_REVIVE_SKILL = "HealReviveSkill";
    public const string HEAL_REVIVE_SKILL_SFX = "HealReviveSkillSFX";

    public const string HOLY_WALL = "HolyWall";
    public const string HOLY_WALL_SFX = "HolyWallSFX";
    public const string HOLY_WALL_ICON = "HolyWallIcon";

    public const string SELECT_SKILL_UI = "SelectSkillUI";

    public const int HAUL_TIER_INCREMENT = 500000;

    // Layer Constants
    public const int PLAYER_LAYER = 11;

    // War Cries
    public static readonly string[] HOLY_WAR_CRIES =
    {
        "For the Emperor!",
        "Glory to His name!",
        "Burn the heretics!",
        "The Emperor wills it!",
        "Purge in His light!"
    };

    public static readonly string[] HEAL_WAR_CRIES =
    {
        "The Emperor restores us!",
        "His will strengthens us!",
        "Rise by His grace!",
        "No wound shall stop us!",
        "His mercy heals you!",
        "The Emperor protects!"
    };

    public static readonly string[] HOLY_WALL_WAR_CRIES =
    {
        "The Emperor’s shield holds!",
        "His light is our wall!",
        "None shall pass!",
        "Stand firm in His name!",
        "The righteous endure!",
        "Faith is our fortress!",
        "His will is our shield!",
        "This ground is sacred!",
        "Let them break upon us!",
        "The Emperor defends!"
    };

    public static readonly string HolyAuraDescription =
        "Grants <b><color=#00FF00>+40% movement speed</color></b> and <b><color=#00FF00>2% stamina regen/sec</color></b> based on the caster’s max stamina to all players within range.\r\n" +
        "Regen increases by <b><color=#00FF00>+2%</color></b> for each nearby player.\r\n" +
        "<b>Duration:</b> <b><color=#FFD700>4 seconds</color></b>\r\n" +
        "<b>Cooldown:</b> <b><color=#FF5555>90 seconds</color></b>";

    public static readonly string HealDescription =
        "Restores <b><color=#00FF00>10 HP</color></b> to all nearby allies and grants <b><color=#00FF00>2% HP regen/sec</color></b> based on the caster’s max health.\r\n" +
        "Regen increases by <b><color=#00FF00>+3%</color></b> for each nearby player.\r\n" +
        "<b>Duration:</b> <b><color=#FFD700>3 seconds</color></b>\r\n" +
        "<b>Cooldown:</b> <b><color=#FF5555>3 minutes</color></b>";


    public static readonly string HolyWallDescription =
        "Summons a <b><color=#00FF00>holy barrier</color></b> that blocks movement.\r\n" +
        "<b>Duration:</b> <b><color=#FFD700>3 seconds</color></b>\r\n" +
        "<b>Cooldown:</b> <b><color=#FF5555>90 seconds</color></b>";

    public static readonly string[] HolyAuraTierDescriptions = new string[]
    {
        "<b><color=#FFFFFF>Cast radius <color=#00FF00>doubled</color></color></b>\r\n<size=80%><b><color=#00FFFF>Requires 500,000 total haul extracted</color></b></size>",
        "<b><color=#FFFFFF>Stamina regen increased from <color=#00FF00>2%</color> to <color=#00FF00>4%</color></color></b>\r\n<size=80%><b><color=#00FFFF>Requires 1,000,000 total haul extracted</color></b></size>",
        "<b><color=#FFFFFF>Sprint duration increased from <color=#00FF00>4</color> to <color=#00FF00>8</color> seconds</color></b>\r\n<size=80%><b><color=#00FFFF>Requires 1,500,000 total haul extracted</color></b></size>",
        "<b><color=#FFFFFF>Regen duration increased from <color=#00FF00>4</color> to <color=#00FF00>8</color> seconds</color></b>\r\n<size=80%><b><color=#00FFFF>Requires 2,000,000 total haul extracted</color></b></size>"
    };

    public static readonly string[] HealSkillTierDescriptions = new string[]
    {
        "<b><color=#FFFFFF>Base heal increased from <color=#00FF00>10</color> to <color=#00FF00>20</color></color></b>\r\n<size=80%><b><color=#00FFFF>Requires 500,000 total haul extracted</color></b></size>",
        "<b><color=#FFFFFF>Healing radius <color=#00FF00>doubled</color></color></b>\r\n<size=80%><b><color=#00FFFF>Requires 1,000,000 total haul extracted</color></b></size>",
        "<b><color=#FFFFFF>Heal regen increased from <color=#00FF00>2%</color> to <color=#00FF00>4%</color></color></b>\r\n<size=80%><b><color=#00FFFF>Requires 1,500,000 total haul extracted</color></b></size>",
        "<b><color=#FFFFFF>Regen duration increased from <color=#00FF00>3</color> to <color=#00FF00>6</color> seconds</color></b>\r\n<size=80%><b><color=#00FFFF>Requires 2,000,000 total haul extracted</color></b></size>",
        "<b><color=#FFFFFF><color=#00FF00>Can revive</color> all nearby allies once per game</color></b>\r\n<size=80%><b><color=#00FFFF>Requires 2,500,000 total haul extracted</color></b></size>"
    };

    public static readonly string[] HolyWallSkillTierDescriptions = new string[]
    {
        "<b><color=#FFFFFF>Wall size increased by <color=#00FF00>2x</color></color></b>\r\n<size=80%><b><color=#00FFFF>Requires 500,000 total haul extracted</color></b></size>",
        "<b><color=#FFFFFF>Duration increased from <color=#00FF00>3</color> to <color=#00FF00>6</color> seconds</color></b>\r\n<size=80%><b><color=#00FFFF>Requires 1,000,000 total haul extracted</color></b></size>",
        "<b><color=#FFFFFF><color=#00FF00>20%</color> chance to spawn a stun grenade</color></b>\r\n<size=80%><b><color=#00FFFF>Requires 1,500,000 total haul extracted</color></b></size>",
        "<b><color=#FFFFFF><color=#00FF00>20%</color> chance to spawn a stun mine</color></b>\r\n<size=80%><b><color=#00FFFF>Requires 2,000,000 total haul extracted</color></b></size>"
    };


}
