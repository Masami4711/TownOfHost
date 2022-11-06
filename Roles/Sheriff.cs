using System.Collections.Generic;
using Hazel;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class Sheriff
    {
        private static readonly int Id = 20400;
        public static List<byte> playerIdList = new();

        private static OptionItem KillCooldown;
        private static OptionItem MisfireKillsTarget;
        private static OptionItem ShotLimitOpt;
        private static OptionItem CanKillAllAlive;
        public static OptionItem CanKillMadmates;
        public static OptionItem CanKillNeutrals;
        public static OptionItem CanKillJester;
        public static OptionItem CanKillTerrorist;
        public static OptionItem CanKillOpportunist;
        public static OptionItem CanKillArsonist;
        public static OptionItem CanKillEgoist;
        public static OptionItem CanKillEgoSchrodingerCat;
        public static OptionItem CanKillExecutioner;
        public static OptionItem CanKillJackal;
        public static OptionItem CanKillJSchrodingerCat;

        public static Dictionary<byte, float> ShotLimit = new();
        public static Dictionary<byte, float> CurrentKillCooldown = new();
        public static readonly string[] KillOption =
        {
            "SheriffCanKillAll", "SheriffCanKillSeparately"
        };
        public static Dictionary<string, string> SheriffCanKillRole(CustomRoles role)
        {
            var roleName = Utils.GetRoleName(role);
            if (role == CustomRoles.EgoSchrodingerCat) roleName += GetString("In%team%", new Dictionary<string, string>() { { "%team%", Utils.GetRoleName(CustomRoles.Egoist) } });
            if (role == CustomRoles.JSchrodingerCat) roleName += GetString("In%team%", new Dictionary<string, string>() { { "%team%", Utils.GetRoleName(CustomRoles.Jackal) } });
            Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(role), roleName) } };
            return replacementDic;
        }
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Sheriff);
            KillCooldown = OptionItem.Create(Id + 10, TabGroup.CrewmateRoles, Color.white, "KillCooldown", 30, 0, 990, 1, Options.CustomRoleSpawnChances[CustomRoles.Sheriff], format: "Seconds");
            MisfireKillsTarget = OptionItem.Create(Id + 11, TabGroup.CrewmateRoles, Color.white, "SheriffMisfireKillsTarget", false, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            ShotLimitOpt = OptionItem.Create(Id + 12, TabGroup.CrewmateRoles, Color.white, "SheriffShotLimit", 15, 1, 15, 1, Options.CustomRoleSpawnChances[CustomRoles.Sheriff], format: "Times");
            CanKillAllAlive = OptionItem.Create(Id + 24, TabGroup.CrewmateRoles, Color.white, "SheriffCanKillAllAlive", true, Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillMadmates = OptionItem.Create(Id + 13, TabGroup.CrewmateRoles, Color.white, "SheriffCanKill%role%", true, Options.CustomRoleSpawnChances[CustomRoles.Sheriff], replacementDic: SheriffCanKillRole(CustomRoles.Madmate));
            CanKillNeutrals = OptionItem.Create(Id + 14, TabGroup.CrewmateRoles, Color.white, "SheriffCanKillNeutrals", KillOption, KillOption[0], Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
            CanKillJester = OptionItem.Create(Id + 15, TabGroup.CrewmateRoles, Color.white, "SheriffCanKill%role%", true, CanKillNeutrals, replacementDic: SheriffCanKillRole(CustomRoles.Jester));
            CanKillTerrorist = OptionItem.Create(Id + 16, TabGroup.CrewmateRoles, Color.white, "SheriffCanKill%role%", true, CanKillNeutrals, replacementDic: SheriffCanKillRole(CustomRoles.Terrorist));
            CanKillOpportunist = OptionItem.Create(Id + 17, TabGroup.CrewmateRoles, Color.white, "SheriffCanKill%role%", true, CanKillNeutrals, replacementDic: SheriffCanKillRole(CustomRoles.Opportunist));
            CanKillArsonist = OptionItem.Create(Id + 18, TabGroup.CrewmateRoles, Color.white, "SheriffCanKill%role%", true, CanKillNeutrals, replacementDic: SheriffCanKillRole(CustomRoles.Arsonist));
            CanKillEgoist = OptionItem.Create(Id + 19, TabGroup.CrewmateRoles, Color.white, "SheriffCanKill%role%", true, CanKillNeutrals, replacementDic: SheriffCanKillRole(CustomRoles.Egoist));
            CanKillEgoSchrodingerCat = OptionItem.Create(Id + 20, TabGroup.CrewmateRoles, Color.white, "SheriffCanKill%role%", true, CanKillNeutrals, replacementDic: SheriffCanKillRole(CustomRoles.EgoSchrodingerCat));
            CanKillExecutioner = OptionItem.Create(Id + 21, TabGroup.CrewmateRoles, Color.white, "SheriffCanKill%role%", true, CanKillNeutrals, replacementDic: SheriffCanKillRole(CustomRoles.Executioner));
            CanKillJackal = OptionItem.Create(Id + 22, TabGroup.CrewmateRoles, Color.white, "SheriffCanKill%role%", true, CanKillNeutrals, replacementDic: SheriffCanKillRole(CustomRoles.Jackal));
            CanKillJSchrodingerCat = OptionItem.Create(Id + 23, TabGroup.CrewmateRoles, Color.white, "SheriffCanKill%role%", true, CanKillNeutrals, replacementDic: SheriffCanKillRole(CustomRoles.JSchrodingerCat));
        }
        public static void Init()
        {
            playerIdList = new();
            ShotLimit = new();
            CurrentKillCooldown = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            CurrentKillCooldown.Add(playerId, KillCooldown.GetFloat());

            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);

            ShotLimit.TryAdd(playerId, ShotLimitOpt.GetFloat());
            Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{ShotLimit[playerId]}発", "Sheriff");
        }
        public static bool IsEnable => playerIdList.Count > 0;
        private static void SendRPC(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSheriffShotLimit, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(ShotLimit[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte SheriffId = reader.ReadByte();
            float Limit = reader.ReadSingle();
            if (ShotLimit.ContainsKey(SheriffId))
                ShotLimit[SheriffId] = Limit;
            else
                ShotLimit.Add(SheriffId, ShotLimitOpt.GetFloat());
        }
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? CurrentKillCooldown[id] : 0f;
        public static bool CanUseKillButton(byte playerId)
            => !Main.PlayerStates[playerId].IsDead
            && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
            && ShotLimit[playerId] > 0;

        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            ShotLimit[killer.PlayerId]--;
            Logger.Info($"{killer.GetNameWithRole()} : 残り{ShotLimit[killer.PlayerId]}発", "Sheriff");
            SendRPC(killer.PlayerId);
            if (!target.CanBeKilledBySheriff())
            {
                Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
                killer.RpcMurderPlayer(killer);
                if (MisfireKillsTarget.GetBool())
                {
                    if (ToughGuy.CheckAndGuardSpecificKill(killer, target, PlayerState.DeathReason.Kill))
                        return false;
                    return true;
                }
                return false;
            }
            SetKillCooldown(killer.PlayerId);
            return true;
        }
        public static string GetShotLimit(byte playerId) => Utils.ColorString(CanUseKillButton(playerId) ? Color.yellow : Color.white, ShotLimit.TryGetValue(playerId, out var shotLimit) ? $"({shotLimit})" : "Invalid");
        public static bool CanBeKilledBySheriff(this PlayerControl player)
        {
            var cRole = player.GetCustomRole();
            if (CanKillNeutrals.GetSelection() == 0 && cRole.IsNeutral()) return true;
            return cRole switch
            {
                CustomRoles.Jester => CanKillJester.GetBool(),
                CustomRoles.Terrorist => CanKillTerrorist.GetBool(),
                CustomRoles.Executioner => CanKillExecutioner.GetBool(),
                CustomRoles.Opportunist => CanKillOpportunist.GetBool(),
                CustomRoles.Arsonist => CanKillArsonist.GetBool(),
                CustomRoles.Egoist => CanKillEgoist.GetBool(),
                CustomRoles.EgoSchrodingerCat => CanKillEgoSchrodingerCat.GetBool(),
                CustomRoles.Jackal => CanKillJackal.GetBool(),
                CustomRoles.JSchrodingerCat => CanKillJSchrodingerCat.GetBool(),
                CustomRoles.SchrodingerCat => true,
                _ => cRole.GetRoleType() switch
                {
                    RoleType.Impostor => true,
                    RoleType.Madmate => CanKillMadmates.GetBool(),
                    _ => false,
                }
            };
        }
    }
}