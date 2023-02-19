using System.Collections.Generic;
using System.Linq;
using static TownOfHost.Options;

namespace TownOfHost
{
    public static class NekoKabocha
    {
        private static readonly int Id = 3200;
        private static List<byte> playerIdList = new();
        private static OptionItem OptionRevengeCrewmate;
        private static OptionItem OptionRevengeImpostor;
        private static OptionItem OptionRevengeNeutral;
        private static OptionItem OptionRevengeOnExile;
        private static OptionItem OptionRevengeOnEveryKill;

        private static bool RevengeCrewmate;
        private static bool RevengeImpostor;
        private static bool RevengeNeutral;
        private static bool RevengeOnExile;
        private static bool RevengeOnEveryKill;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.NekoKabocha);
            OptionRevengeCrewmate = BooleanOptionItem.Create(Id + 10, "NekoKabochaRevengeCrewmate", true, TabGroup.ImpostorRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.NekoKabocha]);
            OptionRevengeImpostor = BooleanOptionItem.Create(Id + 11, "NekoKabochaRevengeImpostor", true, TabGroup.ImpostorRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.NekoKabocha]);
            OptionRevengeNeutral = BooleanOptionItem.Create(Id + 12, "NekoKabochaRevengeNeutral", true, TabGroup.ImpostorRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.NekoKabocha]);
            OptionRevengeOnExile = BooleanOptionItem.Create(Id + 13, "NekoKabochaRevengeOnExile", false, TabGroup.ImpostorRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.NekoKabocha]);
            OptionRevengeOnEveryKill = BooleanOptionItem.Create(Id + 14, "NekoKabochaRevengeOnEveryKill", false, TabGroup.ImpostorRoles, false)
                .SetParent(OptionRevengeOnExile);
        }
        public static void Init()
        {
            playerIdList = new();

            RevengeCrewmate = OptionRevengeCrewmate.GetBool();
            RevengeImpostor = OptionRevengeImpostor.GetBool();
            RevengeNeutral = OptionRevengeNeutral.GetBool();
            RevengeOnExile = OptionRevengeOnExile.GetBool();
            RevengeOnEveryKill = OptionRevengeOnEveryKill.GetBool();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        private static bool CanRevengeTarget(PlayerControl target)
            => target.GetCustomRole().GetCustomRoleTypes() switch
            {
                CustomRoleTypes.Crewmate or CustomRoleTypes.Madmate => RevengeCrewmate,
                CustomRoleTypes.Impostor => RevengeImpostor,
                CustomRoleTypes.Neutral => RevengeNeutral,
                _ => false,
            };
        public static bool IsRevengeTarget(PlayerControl target, PlayerState.DeathReason deathReason)
            => RevengeOnExile
            && (RevengeOnEveryKill || deathReason == PlayerState.DeathReason.Vote)
            && CanRevengeTarget(target);

        public static void OnMurderPlayer(PlayerControl killer, PlayerControl target)
        {
            if (!playerIdList.Contains(target.PlayerId)) return;
            if (Main.AllAlivePlayerControls.Count(pc => pc != target) <= 1) return;

            var realKiller = target.GetRealKiller() ?? killer;
            if (realKiller != target)
            {
                if (CanRevengeTarget(realKiller))
                    target.RevengeOnMurder(realKiller);
            }
            else if (RevengeOnEveryKill)
            {
                var revengeTargetArray = Main.AllAlivePlayerControls.Where(pc => CanRevengeTarget(pc)).ToArray();
                var count = revengeTargetArray.Count();
                if (count > 0)
                {
                    var revengeTarget = revengeTargetArray[IRandom.Instance.Next(count)];
                    target.RevengeOnMurder(revengeTarget);
                }
            }
        }
        private static void RevengeOnMurder(this PlayerControl nekoKabocha, PlayerControl target)
        {
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Revenge;
            target.SetRealKiller(nekoKabocha);
            target.RpcMurderPlayer(target);
            Logger.Info($"{nekoKabocha.GetNameWithRole()} revenges on {target.GetNameWithRole()}", "NekoKabocha.RevengeOnKill");
        }
    }
}