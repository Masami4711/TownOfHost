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
        public static CustomOption InsiderCanSeeAbilitiesOfImpostors;
        public static CustomOption InsiderCanSeeWholeRolesOfGhosts;
        public static CustomOption InsiderCanSeeMadmate;
        public static CustomOption InsiderCanSeeMadmateKillCount;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.Insider);
            InsiderCanSeeAbilitiesOfImpostors = CustomOption.Create(Id + 10, Color.white, "InsiderCanSeeAbilitiesOfImpostors", true, Options.CustomRoleSpawnChances[CustomRoles.Insider]);
            InsiderCanSeeWholeRolesOfGhosts = CustomOption.Create(Id + 11, Color.white, "InsiderCanSeeWholeRolesOfGhosts", false, Options.CustomRoleSpawnChances[CustomRoles.Insider]);
            InsiderCanSeeMadmate = CustomOption.Create(Id + 12, Color.white, "InsiderCanSeeMadmate", false, Options.CustomRoleSpawnChances[CustomRoles.Insider]);
            InsiderCanSeeMadmateKillCount = CustomOption.Create(Id + 13, Color.white, "InsiderCanSeeMadmateKillCount", 2, 0, 12, 1, InsiderCanSeeMadmate);
        }
        public static void Init()
        {
            IsKilledByInsider = new Dictionary<byte, PlayerControl>();
            playerIdList = new();
        }
        public static void Add(PlayerControl pc)
        {
            if (InsiderCanSeeMadmate.GetBool()) Logger.Info($"{pc.GetNameWithRole()} : 現在{InsiderKillCount(pc)}/{InsiderCanSeeMadmateKillCount.GetInt()}キル", "Insider");
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
                float Norma = InsiderCanSeeMadmateKillCount.GetInt();
                IsKilledByInsider.Add(target.PlayerId, killer);
                RpcInsiderKill(killer.PlayerId, target.PlayerId);
                if (InsiderCanSeeMadmate.GetBool()) Logger.Info($"{killer.GetNameWithRole()} : 現在{InsiderKillCount(killer)}/{Norma}キル", "Insider");
            }
            Utils.NotifyRoles();
        }
        public static bool InsiderKnowsOtherRole(PlayerControl Insider, PlayerControl Target)
        {
            if (!Insider.Is(CustomRoles.Insider)) return false;
            if (!GameStates.IsMeeting && !Insider.Data.IsDead && Target.Data.IsDead) return false;
            if (Insider == Target) return false;
            if (Insider.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool()) return false;
            if (InsiderCanSeeAbilitiesOfImpostors.GetBool() && Target.GetCustomRole().IsImpostor()) return true;
            if (Target.Data.IsDead)
                if (InsiderCanSeeWholeRolesOfGhosts.GetBool()) return true;
                else if (IsKilledByInsider.TryGetValue(Target.PlayerId, out var killer) && Insider == killer) return true;
            return false;
        }
        public static int InsiderKillCount(PlayerControl Insider)
        {
            int KillCount = 0;
            foreach (var target in PlayerControl.AllPlayerControls)
            {
                if (!IsKilledByInsider.TryGetValue(target.PlayerId, out var killer)) continue;
                if (Insider == killer) KillCount += 1;
            }
            return KillCount;
        }
        public static string AddProgressText(byte playerId, string ProgressText)
        {
            if (InsiderCanSeeMadmate.GetBool())
            {
                int KillCount = InsiderKillCount(Utils.GetPlayerById(playerId));
                int Norma = InsiderCanSeeMadmateKillCount.GetInt();
                if (KillCount < Norma) ProgressText += $"<color={Utils.GetRoleColorCode(CustomRoles.Impostor)}>({KillCount}/{Norma})</color>";
                else ProgressText += $" <color={Utils.GetRoleColorCode(CustomRoles.Impostor)}>★</color>";
            }
            return ProgressText;
        }
        public static bool InsiderKnowsMadmate(PlayerControl seer)
        {
            if (!seer.Is(CustomRoles.Insider) || !InsiderCanSeeMadmate.GetBool()) return false;
            int KillCount = InsiderKillCount(seer);
            return KillCount >= InsiderCanSeeMadmateKillCount.GetInt();

        }
    }
}