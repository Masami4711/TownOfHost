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
        private static OptionItem CanWinOnEscape;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Runaway);
            CanWinOnEscape = OptionItem.Create(Id + 10, TabGroup.NeutralRoles, Color.white, "RunawayCanWinOnEscape", false, CustomRoleSpawnChances[CustomRoles.Runaway]);
            OverrideTasksData.Create(Id + 11, TabGroup.NeutralRoles, CustomRoles.Runaway);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void ApplyGameOptions(GameOptionsData opt, PlayerControl player) => opt.RoleOptions.EngineerCooldown = CanUseVent(player) ? 0f : 255f;
        public static bool CanUseVent(PlayerControl pc)
            => pc.Is(CustomRoles.Runaway) && pc.GetPlayerTaskState().IsTaskFinished;
        public static void SetHudActive(HudManager __instance, PlayerControl player)
        {
            __instance.AbilityButton.ToggleVisible(CanUseVent(player));
            __instance.AbilityButton.OverrideText($"{GetString("DeathReason.Escape")}");
        }
        public static bool AnyEscapeWin() => IsEnable() && PlayerControl.AllPlayerControls.ToArray().Any(x => IsEscapeWin(x));
        public static bool IsEscapeWin(PlayerControl pc)
            => pc.Is(CustomRoles.Runaway) && Main.PlayerStates[pc.PlayerId].deathReason == PlayerState.DeathReason.Escape;
        public static bool IsAliveWin(PlayerControl pc)
            => pc.Is(CustomRoles.Runaway) && pc.IsAlive() && CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate;
        public static void OnEnterVent(PlayerControl pc)
        {
            if (!AmongUsClient.Instance.AmHost || !AmongUsClient.Instance.IsGameStarted) return;
            if (!pc.Is(CustomRoles.Runaway) || !CanUseVent(pc)) return;

            pc.RpcExileV2();
            Main.PlayerStates[pc.PlayerId].SetDead();
            Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Escape;
            Utils.CustomSyncAllSettings();
            Utils.NotifyRoles();
        }
    }
}