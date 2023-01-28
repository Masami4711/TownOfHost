using System.Collections.Generic;
using System.Linq;
using Hazel;
using static TownOfHost.Options;

namespace TownOfHost
{
    public static class EagleEye
    {
        private static readonly int Id = 3500;
        private static List<byte> playerIdList = new();
        private static OptionItem NumAvailableVote;
        private static Dictionary<byte, List<byte>> TargetList = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.EagleEye);
            NumAvailableVote = IntegerOptionItem.Create(Id + 10, "EagleEyeNumAvailableVote", new(1, 5, 1), 1, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.EagleEye]);
        }
        public static void Init()
        {
            playerIdList = new();
            TargetList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            TargetList.Add(playerId, new List<byte>());
        }
        public static bool IsEnable => playerIdList.Count > 0;
    }
}