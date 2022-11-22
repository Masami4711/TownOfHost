using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace TownOfHost
{
    public static class Outsider
    {
        private static readonly int Id = 3300;
        public static List<byte> playerIdList = new();

        private static OptionItem KillCooldown;
        private static OptionItem CanSeeImpostor;
        private static OptionItem KillCountToSeeImpostors;
        private static OptionItem CanSeeAllTeamImpostors;

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Outsider);
            KillCooldown = OptionItem.Create(Id + 10, TabGroup.ImpostorRoles, Color.white, "KillCooldown", 30f, 0f, 180f, 2.5f, Options.CustomRoleSpawnChances[CustomRoles.Outsider], format: OptionFormat.Seconds);
            CanSeeImpostor = OptionItem.Create(Id + 11, TabGroup.ImpostorRoles, Color.white, "OutsiderCanSeeImpostor", false, Options.CustomRoleSpawnChances[CustomRoles.Outsider]);
            KillCountToSeeImpostors = OptionItem.Create(Id + 12, TabGroup.ImpostorRoles, Color.white, "OutsiderKillCountToSeeImpostors", 1, 0, 10, 1, CanSeeImpostor, format: OptionFormat.Times);
            CanSeeAllTeamImpostors = OptionItem.Create(Id + 12, TabGroup.ImpostorRoles, Color.white, "OutsiderCanSeeAllTeamImpostors", false, CanSeeImpostor);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte id)
        {
            playerIdList.Add(id);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
        public static void SetHudActive(HudManager __instance, bool isActive, PlayerControl player)
        {
            if (player.Data.Role.Role != RoleTypes.GuardianAngel)
                __instance.KillButton.ToggleVisible(isActive && !player.Data.IsDead);
            __instance.SabotageButton.ToggleVisible(isActive);
            __instance.ImpostorVentButton.ToggleVisible(isActive);
            __instance.AbilityButton.ToggleVisible(false);
        }
        public static int KillCount(byte playerId)
            => Main.PlayerStates[playerId].GetKillCount(true);
        public static bool KnowImpostor(PlayerControl seer, PlayerControl target = null)
            => seer.Is(CustomRoles.Outsider) && CanSeeImpostor.GetBool()
            && KillCount(seer.PlayerId) >= KillCountToSeeImpostors.GetInt()
            && (target == null || target.Is(RoleType.Impostor, CanSeeAllTeamImpostors.GetBool()));
        public static bool KnowMadmate(PlayerControl seer, PlayerControl target)
            => KnowImpostor(seer) && CanSeeAllTeamImpostors.GetBool()
            && target.Is(RoleType.Madmate);
        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
            => killer.Is(CustomRoles.Outsider) && !KnowImpostor(killer, target);
        public static string GetKillCount(byte playerId)
        {
            if (!CanSeeImpostor.GetBool()) return "";
            return Utils.ColorString(Palette.ImpostorRed.ShadeColor(0.5f), KillCount(playerId) >= KillCountToSeeImpostors.GetInt() ? " ★" : $" ({KillCount(playerId)}/{KillCountToSeeImpostors.GetInt()})");
        }
    }
}