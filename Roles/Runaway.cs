using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using static TownOfHost.Options;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class Runaway
    {
        private static readonly int Id = 51000;
        public static List<byte> playerIdList = new();
        public static byte WinnerID;
        private static OptionItem NumTasksToEscape;
        private static OptionItem CanEscapeWinWithoutTask;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Runaway);
            NumTasksToEscape = OptionItem.Create(Id + 10, TabGroup.NeutralRoles, Color.white, "RunawayNumTasksToEscape", 3, 0, 15, 1, CustomRoleSpawnChances[CustomRoles.Runaway]);
            CanEscapeWinWithoutTask = OptionItem.Create(Id + 11, TabGroup.NeutralRoles, Color.white, "RunawayCanEscapeWinWithoutTask", false, CustomRoleSpawnChances[CustomRoles.Runaway]);
        }
        public static void Init()
            => playerIdList = new();
        public static void Add(byte playerId)
            => playerIdList.Add(playerId);
        public static bool IsEnable() => playerIdList.Count > 0;
        public static bool CanUseVent(PlayerControl pc)
            => pc.Is(CustomRoles.Runaway) && pc.IsAlive()
            && (pc.GetPlayerTaskState().IsTaskFinished || pc.GetPlayerTaskState().CompletedTasksCount >= NumTasksToEscape.GetInt());
        public static void SetHudActive(HudManager __instance, PlayerControl player)
        {
            __instance.AbilityButton.ToggleVisible(CanUseVent(player));
            __instance.AbilityButton.OverrideText($"{GetString("DeathReason.Escape")}");
        }
        public static bool AnyEscapeWin() => IsEnable() && PlayerControl.AllPlayerControls.ToArray().Any(x => IsEscapeWin(x));
        public static bool IsEscapeWin(PlayerControl pc)
            => pc.Is(CustomRoles.Runaway)
            && Main.PlayerStates[pc.PlayerId].deathReason == PlayerState.DeathReason.Escape
            && CustomWinnerHolder.WinnerTeam != CustomWinner.Crewmate
            && (CanEscapeWinWithoutTask.GetBool() || pc.GetPlayerTaskState().IsTaskFinished);
        public static bool IsAliveWin(PlayerControl pc)
            => pc.Is(CustomRoles.Runaway)
            && pc.IsAlive()
            && CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate;
        public static string GetSuffixText(PlayerControl pc, string fontSize = null)
        {
            if (!GameStates.IsInTask || !CanUseVent(pc)) return "";
            string text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Runaway), GetString("RunawaySuffixText"));
            if (fontSize != null)
                text = $"<size={fontSize}>" + text + "</size>";
            return text;
        }
        public static void OnEnterVent(PlayerControl pc)
        {
            if (!AmongUsClient.Instance.AmHost || !AmongUsClient.Instance.IsGameStarted) return;
            if (!pc.Is(CustomRoles.Runaway) || !CanUseVent(pc)) return;

            pc.RpcExileV2();
            Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Escape;
            Main.PlayerStates[pc.PlayerId].SetDead();
            Utils.CustomSyncAllSettings();
            Utils.NotifyRoles();
        }
    }
}