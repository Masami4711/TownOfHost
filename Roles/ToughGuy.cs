using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class ToughGuy
    {
        private static readonly int Id = 21100;
        public static List<byte> playerIdList = new();

        private static Dictionary<byte, (PlayerControl, PlayerState.DeathReason)> WillDieAfterMeeting = new();

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.ToughGuy);
        }
        public static void Init()
        {
            playerIdList = new();
            WillDieAfterMeeting = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            WillDieAfterMeeting.Remove(playerId);
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static bool CanGuardDeath(PlayerControl pc)
        {
            if (WillDieAfterMeeting.TryGetValue(pc.PlayerId, out var result))
            {
                WillDieAfterMeeting.Remove(pc.PlayerId);
                Utils.NotifyRoles();
                return false;
            }
            return true;
        }
        public static bool CheckAndGuardKill(PlayerControl killer, PlayerControl target)
        {
            //そもそもキルじゃないパターン、ここでは処理しないパターン
            switch (killer.GetCustomRole())
            {
                case CustomRoles.Puppeteer:
                case CustomRoles.Sheriff:
                case CustomRoles.Arsonist:
                    return false;
                case CustomRoles.Witch:
                    if (killer.IsSpellMode()) return false;
                    break;
                case CustomRoles.Warlock:
                    if (!Main.CheckShapeshift[killer.PlayerId]) return false;
                    break;
            }
            //既に負傷している
            if (!CanGuardDeath(target)) return false;
            var deathReason = PlayerState.DeathReason.Kill;
            switch (killer.GetCustomRole())
            {
                case CustomRoles.BountyHunter: //キルが発生する前にここの処理をしないとバグる
                    BountyHunter.OnCheckMurder(killer, target);
                    break;
                case CustomRoles.SerialKiller:
                    SerialKiller.OnCheckMurder(killer, true);
                    break;
                case CustomRoles.Vampire:
                    deathReason = PlayerState.DeathReason.Bite;
                    break;
                default:
                    if (killer == target)
                    {
                        if (Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.Sniped)
                        {
                            deathReason = PlayerState.DeathReason.Sniped;
                            killer = Utils.GetPlayerById(Sniper.GetSniper(target.PlayerId));
                            break;
                        }
                    }
                    break;
            }
            killer.RpcGuardAndKill(target);
            WillDieAfterMeeting.Add(target.PlayerId, (killer, deathReason));
            Logger.Info($"{target.GetNameWithRole()}が{killer.GetNameWithRole()}に{deathReason}されて負傷", "WillDieAfterMeeting");
            Utils.NotifyRoles();
            Utils.CustomSyncAllSettings();
            return true;
        }
        public static bool CheckAndGuardSpecificKill(PlayerControl killer, PlayerControl target, PlayerState.DeathReason deathReason)
        {
            if (!(target.Is(CustomRoles.ToughGuy) && CanGuardDeath(target))) return false;
            WillDieAfterMeeting.Add(target.PlayerId, (killer, deathReason));
            Logger.Info($"{target.GetNameWithRole()}が{killer.GetNameWithRole()}に{deathReason}されて負傷", "CheckAndGuardPuppeteerKill");
            // killer.RpcGuardAndKill(target);
            Utils.NotifyRoles();
            Utils.CustomSyncAllSettings();
            return true;
        }
        public static void AfterMeetingDeath()
        {
            foreach (var kvp in WillDieAfterMeeting)
            {
                var playerId = kvp.Key;
                var deathReason = kvp.Value.Item2;
                if (!Main.PlayerStates[playerId].IsDead)
                {
                    Main.AfterMeetingDeathPlayers.TryAdd(playerId, deathReason);
                    Logger.Info($"{Utils.GetPlayerById(playerId).GetNameWithRole()}が{deathReason}で死亡", "ToughGuy");
                }
            }
            WillDieAfterMeeting.Clear();
        }
        public static string GetMark(PlayerControl seer, PlayerControl target)
        {
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.ToughGuy),
                (WillDieAfterMeeting.ContainsKey(target.PlayerId) && (seer.Data.IsDead
                || seer == WillDieAfterMeeting[target.PlayerId].Item1)) ? "＊" : "");
        }
    }
}