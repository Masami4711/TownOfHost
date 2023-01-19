using System.Collections.Generic;
using UnityEngine;
using static TownOfHost.Translator;
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

        //基本条件
        private static bool BasicRequirementToSeeTargetRole(PlayerControl seer, PlayerControl target)
        {
            if (seer == null || target == null || !IsEnable) return false;
            if (seer == target) return false; //自分自身
            if (!seer.IsAlive() && GhostCanSeeOtherRoles.GetBool()) return false; //幽霊で普通に見えるパターン
            return playerIdList.Contains(seer.PlayerId);
        }
        private static int KillCount(byte playerId)
            => Main.PlayerStates[playerId].GetKillCount(true);

        //役職を見る条件の関数
        public static bool KnowTargetRole(PlayerControl seer, PlayerControl target) //Insider能力で役職が分かるケースのみ
        {
            if (KnowDeadTargetRole(seer, target)) return true;
            if (BasicRequirementToSeeTargetRole(seer, target))
                return target.GetCustomRole().GetRoleType() switch
                {
                    RoleType.Impostor
                        => CanSeeImpostorAbilities.GetBool(),
                    RoleType.Madmate
                        => CanSeeMadmates.GetBool() && KillCount(seer.PlayerId) >= KillCountToSeeMadmates.GetInt(),
                    _ => false,
                };
            return false;
        }
        public static bool KnowDeadTargetRole(PlayerControl seer, PlayerControl target) //幽霊の役職が見えるケース
        {
            if (BasicRequirementToSeeTargetRole(seer, target) && !target.IsAlive())
                return CanSeeAllGhostsRoles.GetBool() //全員見える
                    || target.GetRealKiller() == seer; //自分でキルした相手
            return false;
        }
    }
}