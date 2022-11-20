using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using UnityEngine;
using static TownOfHost.Options;

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
        public static bool AnyEscapeWin() => IsEnable() && PlayerControl.AllPlayerControls.ToArray().Any(x => IsEscapeWin(x));
        public static bool IsEscapeWin(PlayerControl pc)
            => pc.Is(CustomRoles.Runaway) && Main.PlayerStates[pc.PlayerId].deathReason == PlayerState.DeathReason.Escape;
        public static bool IsAliveWin(PlayerControl pc)
            => pc.Is(CustomRoles.Runaway) && pc.IsAlive() && CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate;
    }
}