using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class NekoKabocha
    {
        static readonly int Id = 3200;
        static List<byte> playerIdList = new();
        public static CustomOption RevengeCrewmate;
        public static CustomOption RevengeNeutral;
        public static CustomOption RevengeImpostor;
        public static CustomOption RevengeExile;
        public static CustomOption RandomRevengeIncludeTeamImpostor;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.NekoKabocha);
            RevengeCrewmate = CustomOption.Create(Id + 10, TabGroup.ImpostorRoles, Color.white, "NekoKabochaRevengeCrewmate", true, Options.CustomRoleSpawnChances[CustomRoles.NekoKabocha]);
            RevengeNeutral = CustomOption.Create(Id + 11, TabGroup.ImpostorRoles, Color.white, "NekoKabochaRevengeNeutral", true, Options.CustomRoleSpawnChances[CustomRoles.NekoKabocha]);
            RevengeImpostor = CustomOption.Create(Id + 12, TabGroup.ImpostorRoles, Color.white, "NekoKabochaRevengeImpostor", true, Options.CustomRoleSpawnChances[CustomRoles.NekoKabocha]);
            RevengeExile = CustomOption.Create(Id + 13, TabGroup.ImpostorRoles, Color.white, "NekoKabochaRevengeExile", false, Options.CustomRoleSpawnChances[CustomRoles.NekoKabocha]);
            RandomRevengeIncludeTeamImpostor = CustomOption.Create(Id + 14, TabGroup.ImpostorRoles, Color.white, "RandomRevengeIncludeTeamImpostor", true, RevengeExile);
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
        public static void RevengeOnKill(PlayerControl killer, PlayerControl target)
        {
            Logger.Info(target?.Data?.PlayerName + "はNekoKabochaだった", "MurderPlayer");
            var deathReason = PlayerState.GetDeathReason(target.PlayerId);
            bool NoRevenge = PlayerControl.AllPlayerControls.ToArray().All(x => x == killer || x == target || x.Data.IsDead);
            if (killer == target || NoRevenge) return;
            if ((killer.GetCustomRole().IsCrewmate() && RevengeCrewmate.GetBool())
            || (killer.GetCustomRole().IsNeutral() && RevengeNeutral.GetBool())
            || (killer.GetCustomRole().IsImpostor() && RevengeImpostor.GetBool()))
            {
                PlayerState.SetDeathReason(killer.PlayerId, PlayerState.DeathReason.Revenge);
                killer.RpcMurderPlayer(killer);
                Logger.Info($"{target.GetNameWithRole()}が:{killer.GetNameWithRole()}を道連れにしました", "NekoKabocha");
            }
        }
        public static void RevengeOnExile(byte playerId)
        {
            if (!RevengeExile.GetBool()) return;
            var nekokabocha = Utils.GetPlayerById(playerId);
            var target = PickRevengeTarget(nekokabocha);
            if (target == null) return;
            // Main.AfterMeetingDeathPlayers.TryAdd(target.PlayerId, PlayerState.DeathReason.Revenge);
            CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(target.PlayerId, PlayerState.DeathReason.Revenge);
            Logger.Info($"{nekokabocha.GetNameWithRole()}の道連れ先:{target.GetNameWithRole()}", "NekoKabocha");
        }
        public static PlayerControl PickRevengeTarget(PlayerControl exiledplayer)//道連れ先選定
        {
            List<PlayerControl> TargetList = new();
            foreach (var candidate in PlayerControl.AllPlayerControls)
            {
                if (candidate == exiledplayer || candidate.Data.IsDead || Main.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId)) continue;
                if (!candidate.GetCustomRole().IsImpostorTeam() || RandomRevengeIncludeTeamImpostor.GetBool())
                    TargetList.Add(candidate);
            }
            if (TargetList == null) return null;
            var rand = new System.Random();
            var target = TargetList[rand.Next(TargetList.Count)];
            Logger.Info($"{exiledplayer.GetNameWithRole()}の道連れ先:{target.GetNameWithRole()}", "PickRevengeTarget");
            return target;
        }
    }
}