using System.Collections.Generic;
using static TownOfHost.Options;

namespace TownOfHost
{
    public static class Insider
    {
        private static readonly int Id = 2800;
        private static List<byte> playerIdList = new();
        private static OptionItem CanSeeImpostorAbilities;
        private static OptionItem CanSeeAllGhostsRoles;
        private static OptionItem CanSeeMadmates;
        private static OptionItem KillCountToSeeMadmates;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Insider);
            CanSeeImpostorAbilities = BooleanOptionItem.Create(Id + 10, "InsiderCanSeeImpostorAbilities", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Insider]);
            CanSeeAllGhostsRoles = BooleanOptionItem.Create(Id + 11, "InsiderCanSeeAllGhostsRoles", false, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Insider]);
            CanSeeMadmates = BooleanOptionItem.Create(Id + 12, "InsiderCanSeeMadmates", false, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Insider]);
            KillCountToSeeMadmates = IntegerOptionItem.Create(Id + 13, "InsiderKillCountToSeeMadmates", new(0, 12, 1), 2, TabGroup.ImpostorRoles, false).SetParent(CanSeeMadmates);
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