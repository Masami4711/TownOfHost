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
        private static CustomOption CanSeeImpostorAbilities;
        private static CustomOption CanSeeWholeRolesOfGhosts;
        private static CustomOption CanSeeMadmates;
        private static CustomOption KillCountToSeeMadmates;
        static Dictionary<CustomRoles, CustomRoles> ReplaceRoles = new()
        {
            // {CustomRoles.Marin, CustomRoles.Crewmate}
        };
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Insider);
            CanSeeImpostorAbilities = CustomOption.Create(Id + 10, TabGroup.ImpostorRoles, Color.white, "InsiderCanSeeImpostorAbilities", true, Options.CustomRoleSpawnChances[CustomRoles.Insider]);
            CanSeeWholeRolesOfGhosts = CustomOption.Create(Id + 11, TabGroup.ImpostorRoles, Color.white, "InsiderCanSeeWholeRolesOfGhosts", false, Options.CustomRoleSpawnChances[CustomRoles.Insider]);
            CanSeeMadmates = CustomOption.Create(Id + 12, TabGroup.ImpostorRoles, Color.white, "InsiderCanSeeMadmates", false, Options.CustomRoleSpawnChances[CustomRoles.Insider]);
            KillCountToSeeMadmates = CustomOption.Create(Id + 13, TabGroup.ImpostorRoles, Color.white, "InsiderKillCountToSeeMadmates", 2, 0, 12, 1, CanSeeMadmates);
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
        public static bool KnowOtherRole(PlayerControl Insider, PlayerControl Target) //Insider能力で役職が分かるケースのみ
        {
            if (!Insider.Is(CustomRoles.Insider)) return false; //Insider以外
            if (!GameStates.IsMeeting && !Insider.Data.IsDead && Target.Data.IsDead) return false; //タスクフェーズでTargetが死んでいる
            if (Insider == Target) return false; //自分自身
            if (Insider.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool()) return false; //幽霊で普通に見えるパターン
            if (CanSeeImpostorAbilities.GetBool() && Target.GetCustomRole().IsImpostor()) return true; //味方インポスターのケース
            if (Target.Data.IsDead) //幽霊の役職が見えるケース
                if (CanSeeWholeRolesOfGhosts.GetBool()) return true; //全員見える
                else if (IsKilledByInsider.TryGetValue(Target.PlayerId, out var killer) && Insider == killer) return true; //自分でキルした相手
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
        public static string GetRoleText(PlayerControl target, string TargetTaskText, string fontSize)
        {
            if (DisableTaskText(target)) TargetTaskText = "";
            var Role = RoleTextData(target);
            var RoleText = $"<size={fontSize}>{Helpers.ColorString(Role.Item2, Role.Item1)}{TargetTaskText}</size>\r\n";
            return RoleText;
        }
        public static (string, Color) RoleTextData(PlayerControl target)
        {
            var RoleName = target.GetRoleName();
            var RoleColor = target.GetRoleColor();
            if (ReplaceRoles.TryGetValue(target.GetCustomRole(), out var newRole))
            {
                RoleName = Utils.GetRoleName(newRole);
                RoleColor = Utils.GetRoleColor(newRole);
            }
            return (RoleName, RoleColor);
        }
        public static bool DisableTaskText(PlayerControl pc) => false;
        //=> Utils.HasTasks(pc.Data) && CustomRoles.Marin.IsEnable() && !Marin.HasTasks.GetBool();
    }
}