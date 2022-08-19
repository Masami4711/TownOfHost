using System.Collections.Generic;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class Insider
    {
        static readonly int Id = 2800;
        static List<byte> playerIdList = new();
        public static Dictionary<byte, PlayerControl> IsKilledByInsider = new();
        public static CustomOption CanSeeImpostorAbilities;
        public static CustomOption CanSeeWholeRolesOfGhosts;
        public static CustomOption CanSeeMadmates;
        public static CustomOption KillCountToSeeMadmates;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.Insider);
            CanSeeImpostorAbilities = CustomOption.Create(Id + 10, Color.white, "CanSeeImpostorAbilities", true, Options.CustomRoleSpawnChances[CustomRoles.Insider]);
            CanSeeWholeRolesOfGhosts = CustomOption.Create(Id + 11, Color.white, "CanSeeWholeRolesOfGhosts", false, Options.CustomRoleSpawnChances[CustomRoles.Insider]);
            CanSeeMadmates = CustomOption.Create(Id + 12, Color.white, "CanSeeMadmates", false, Options.CustomRoleSpawnChances[CustomRoles.Insider]);
            KillCountToSeeMadmates = CustomOption.Create(Id + 13, Color.white, "KillCountToSeeMadmates", 2, 0, 12, 1, CanSeeMadmates);
        }
        public static void Init()
        {
            IsKilledByInsider = new Dictionary<byte, PlayerControl>();
            playerIdList = new();
        }
        public static void Add(PlayerControl pc)
        {
            if (CanSeeMadmates.GetBool()) Logger.Info($"{pc.GetNameWithRole()} : 現在{KillCount(pc)}/{KillCountToSeeMadmates.GetInt()}キル", "Insider");
            playerIdList.Add(pc.PlayerId);
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void ReceiveRPC(MessageReader msg)
        {
            byte insiderId = msg.ReadByte();
            byte insiderTargetId = msg.ReadByte();
            IsKilledByInsider.Add(insiderTargetId, Utils.GetPlayerById(insiderId));
        }
        public static void RpcInsiderKill(byte insider, byte player)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.InsiderKill, Hazel.SendOption.Reliable, -1);
            writer.Write(insider);
            writer.Write(player);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            if (!IsKilledByInsider.ContainsKey(target.PlayerId) && !target.Is(CustomRoles.SchrodingerCat) && !(target.Is(CustomRoles.MadGuardian) && target.GetPlayerTaskState().IsTaskFinished))
            {
                float Norma = KillCountToSeeMadmates.GetInt();
                IsKilledByInsider.Add(target.PlayerId, killer);
                RpcInsiderKill(killer.PlayerId, target.PlayerId);
                if (CanSeeMadmates.GetBool()) Logger.Info($"{killer.GetNameWithRole()} : 現在{KillCount(killer)}/{Norma}キル", "Insider");
            }
            Utils.NotifyRoles();
        }
        public static bool KnowOtherRole(PlayerControl Insider, PlayerControl Target)
        {
            if (!Insider.Is(CustomRoles.Insider)) return false;
            if (!GameStates.IsMeeting && !Insider.Data.IsDead && Target.Data.IsDead) return false;
            if (Insider == Target) return false;
            if (Insider.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool()) return false;
            if (CanSeeImpostorAbilities.GetBool() && Target.GetCustomRole().IsImpostor()) return true;
            if (Target.Data.IsDead)
                if (CanSeeWholeRolesOfGhosts.GetBool()) return true;
                else if (IsKilledByInsider.TryGetValue(Target.PlayerId, out var killer) && Insider == killer) return true;
            return false;
        }
        public static int KillCount(PlayerControl Insider)
        {
            int KillCount = 0;
            foreach (var target in PlayerControl.AllPlayerControls)
            {
                if (!IsKilledByInsider.TryGetValue(target.PlayerId, out var killer)) continue;
                if (Insider == killer) KillCount += 1;
            }
            return KillCount;
        }
        public static string GetKillCount(byte playerId)
        {
            string ProgressText = "";
            if (CanSeeMadmates.GetBool())
            {
                int killCount = KillCount(Utils.GetPlayerById(playerId));
                int Norma = KillCountToSeeMadmates.GetInt();
                if (killCount < Norma) ProgressText += $"<color={Utils.GetRoleColorCode(CustomRoles.Impostor)}>({killCount}/{Norma})</color>";
                else ProgressText += $" <color={Utils.GetRoleColorCode(CustomRoles.Impostor)}>★</color>";
            }
            return ProgressText;
        }
        public static bool KnowMadmates(PlayerControl seer)
        {
            if (!seer.Is(CustomRoles.Insider) || !CanSeeMadmates.GetBool()) return false;
            int killCount = KillCount(seer);
            return killCount >= KillCountToSeeMadmates.GetInt();

        }
        public static bool KnowImpostorAbiliies(PlayerControl seer) => seer.Is(CustomRoles.Insider) && CanSeeImpostorAbilities.GetBool();
    }
}