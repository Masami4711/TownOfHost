using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using static TownOfHost.Options;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class Runaway
    {
        private static readonly int Id = 51000;
        private static List<byte> playerIdList = new();
        private static OptionItem NumTasksToEscape;
        private static OptionItem DefaultEscapeCooldown;
        private static OptionItem FinalEscapeCooldown;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Runaway);
            NumTasksToEscape = IntegerOptionItem.Create(Id + 10, "RunawayNumTasksToEscape", new(0, 15, 1), 4, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Runaway]);
            DefaultEscapeCooldown = FloatOptionItem.Create(Id + 11, "RunawayDefaultEscapeCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Runaway]);
            FinalEscapeCooldown = FloatOptionItem.Create(Id + 12, "RunawayFinalEscapeCooldown", new(0f, 180f, 2.5f), 60f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Runaway]);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
    }
}