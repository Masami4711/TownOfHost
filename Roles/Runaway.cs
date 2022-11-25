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
        public static bool IsEnable => playerIdList.Count > 0;
        public static bool CanUseVent(PlayerControl pc)
            => pc.Is(CustomRoles.Runaway) && pc.IsAlive()
            && (pc.GetPlayerTaskState().IsTaskFinished || pc.GetPlayerTaskState().CompletedTasksCount >= NumTasksToEscape.GetInt());
        public static void SetAbilityButton(HudManager __instance, PlayerControl player)
        {
            __instance.AbilityButton.ToggleVisible(CanUseVent(player));
            __instance.AbilityButton.OverrideText($"{GetString("DeathReason.Escape")}");
        }
        public static bool AnyEscapeWin => IsEnable && playerIdList.Any(x => IsEscapeWin(x));
        public static bool IsEscapeWin(byte playerId)
        {
            var state = Main.PlayerStates[playerId];
            return state.MainRole == CustomRoles.Runaway
            && state.deathReason == PlayerState.DeathReason.Escape
            && CustomWinnerHolder.WinnerTeam != CustomWinner.Crewmate
            && (CanEscapeWinWithoutTask.GetBool() || state.GetTaskState().IsTaskFinished);
        }
        public static bool IsAliveWin(byte playerId)
        {

            var state = Main.PlayerStates[playerId];
            return state.MainRole == CustomRoles.Runaway
            && !state.IsDead
            && CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate;
        }
        public static void OverrideCustomWinner()
        {
            Logger.Info($"{AnyEscapeWin}", "AnyEscapeWin");
            if (CustomWinnerHolder.WinnerTeam == CustomWinner.None && AnyEscapeWin)
                CustomWinnerHolder.WinnerTeam = CustomWinner.Runaway;
        }
        public static void SoloWin(List<PlayerControl> winner)
        {
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Runaway) return;
            winner.Clear();
            foreach (var id in playerIdList)
            {
                var pc = Utils.GetPlayerById(id);
                if (pc == null) continue;
                if (IsEscapeWin(pc.PlayerId))
                    winner.Add(pc);
            }
        }
        public static string GetSuffixText(PlayerControl pc, string fontSize = null)
        {
            if (!GameStates.IsInTask || !pc.Is(CustomRoles.Runaway)) return "";
            string text = "";
            if (CanUseVent(pc))
                text = GetString("RunawayReadyToEscape");
            else if (pc.Is(PlayerState.DeathReason.Escape) && !IsEscapeWin(pc.PlayerId))
                text = GetString("RunawayFinishTask");
            else return "";
            text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Runaway), text);
            if (fontSize != null)
                text = $"<size={fontSize}>" + text + "</size>";
            return text;
        }
        public static void OnEnterVent(PlayerControl pc, int ventId)
        {
            if (!AmongUsClient.Instance.AmHost || !AmongUsClient.Instance.IsGameStarted) return;
            pc?.MyPhysics?.RpcBootFromVent(ventId);
            if (!CanUseVent(pc)) return;
            new LateTask(() =>
            {
                Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Escape;
                Main.PlayerStates[pc.PlayerId].SetDead();
                pc.SetRealKiller(pc);
                pc.RpcExileV2();
                Utils.CustomSyncAllSettings();
                Utils.NotifyRoles();
            }, 0.25f, "Runaway Escape");

        }
    }
}