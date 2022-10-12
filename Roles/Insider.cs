using System.Collections.Generic;
using Hazel;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class Insider
    {
        static readonly int Id = 2800;
        static List<byte> playerIdList = new();
        public static Dictionary<byte, PlayerControl> IsKilledByInsider = new();
        private static CustomOption CanSeeImpostorAbilities;
        private static CustomOption CanSeeAllGhostsRoles;
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
            CanSeeAllGhostsRoles = CustomOption.Create(Id + 11, TabGroup.ImpostorRoles, Color.white, "InsiderCanSeeAllGhostsRoles", false, Options.CustomRoleSpawnChances[CustomRoles.Insider]);
            CanSeeMadmates = CustomOption.Create(Id + 12, TabGroup.ImpostorRoles, Color.white, "InsiderCanSeeMadmates", false, Options.CustomRoleSpawnChances[CustomRoles.Insider]);
            KillCountToSeeMadmates = CustomOption.Create(Id + 13, TabGroup.ImpostorRoles, Color.white, "InsiderKillCountToSeeMadmates", 2, 0, 12, 1, CanSeeMadmates);
        }
        public static void Init()
        {
            IsKilledByInsider = new Dictionary<byte, PlayerControl>();
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
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
            if (IsKilledByInsider.ContainsKey(target.PlayerId)) return;
            float Norma = KillCountToSeeMadmates.GetInt();
            IsKilledByInsider.Add(target.PlayerId, killer);
            RpcInsiderKill(killer.PlayerId, target.PlayerId);
            if (CanSeeMadmates.GetBool()) Logger.Info($"{killer.GetNameWithRole()} : 現在{KillCount(killer)}/{Norma}キル", "Insider");
            Utils.NotifyRoles();
        }
        public static bool KnowImpostorAbiliies(PlayerControl seer) => seer.Is(CustomRoles.Insider) && CanSeeImpostorAbilities.GetBool();
        public static bool KnowOtherRole(PlayerControl Insider, PlayerControl target) //Insider能力で役職が分かるケースのみ
        {
            if (!Insider.Is(CustomRoles.Insider)) return false; //Insider以外
            if (!GameStates.IsMeeting && !Insider.Data.IsDead && target.Data.IsDead) return false; //タスクフェーズでTargetが死んでいる
            if (Insider == target) return false; //自分自身
            if (Insider.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool()) return false; //幽霊で普通に見えるパターン
            // ここまで前提条件
            if (CanSeeImpostorAbilities.GetBool() && target.Is(RoleType.Impostor)) return true; //味方インポスターのケース
            if (KnowMadmates(Insider) && target.Is(RoleType.Madmate)) return true; //マッドメイトが分かるケース
            return KnowGhostRole(Insider, target);
        }
        public static bool KnowGhostRole(PlayerControl Insider, PlayerControl target) //幽霊の役職が見えるケース
        {
            if (!Insider.Is(CustomRoles.Insider)) return false; //Insider以外
            if (!GameStates.IsMeeting && !Insider.Data.IsDead && target.Data.IsDead) return false; //タスクフェーズでTargetが死んでいる
            if (Insider == target) return false; //自分自身
            if (Insider.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool()) return false; //幽霊で普通に見えるパターン
            // ここまで前提条件
            if (!target.Data.IsDead) return false;
            if (CanSeeAllGhostsRoles.GetBool()) return true; //全員見える
            else if (IsKilledByInsider.TryGetValue(target.PlayerId, out var killer) && Insider == killer) return true; //自分でキルした相手
            return false;
        }
        public static bool KnowMadmates(PlayerControl seer)
        {
            if (!seer.Is(CustomRoles.Insider) || !CanSeeMadmates.GetBool()) return false;
            int killCount = KillCount(seer);
            return killCount >= KillCountToSeeMadmates.GetInt();
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
                if (killCount < Norma) ProgressText += Utils.ColorString(Palette.ImpostorRed, $"({killCount}/{Norma})");
                else ProgressText += Utils.ColorString(Palette.ImpostorRed, " ★");
            }
            return ProgressText;
        }
        public static string GetRoleText(PlayerControl target, string TargetTaskText, string fontSize)
        {
            if (DisableTaskText(target)) TargetTaskText = "";
            switch (target.GetCustomRole()) //本人には表示しないケースのみ
            {
                case CustomRoles.FireWorks:
                    TargetTaskText += FireWorks.GetFireWorksCount(target.PlayerId);
                    break;
                case CustomRoles.Witch:
                    TargetTaskText += Utils.ColorString(Palette.ImpostorRed, $" {GetString(target.IsSpellMode() ? "WitchModeSpell" : "WitchModeKill")}");
                    break;
            }
            var Role = RoleTextData(target);
            var RoleText = $"<size={fontSize}>{Utils.ColorString(Role.Item2, Role.Item1)}{TargetTaskText}</size>\r\n";
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
        //=> Utils.HasTasks(pc.Data) && AssassinAndMarin.IsEnable(); && !Marin.HasTasks.GetBool();

        public static string GetOtherImpostorMarks(PlayerControl Insider, PlayerControl target)
        {
            if (!KnowImpostorAbiliies(Insider)) return "";
            string Mark = "";
            foreach (var seer in PlayerControl.AllPlayerControls)
            {
                if (!seer.Is(RoleType.Impostor)) continue;
                switch (seer.GetCustomRole())
                {
                    case CustomRoles.BountyHunter:
                        Mark += BountyHunter.GetTargetMark(seer, target);
                        break;
                    case CustomRoles.EvilTracker:
                        Mark += EvilTracker.GetTargetMark(seer, target);
                        break;
                    case CustomRoles.Puppeteer:
                        Mark += Utils.GetPuppeteerMark(seer, target);
                        break;
                    case CustomRoles.Vampire:
                        Mark += Utils.GetVampireMark(seer, target);
                        break;
                    case CustomRoles.Warlock:
                        Mark += Utils.GetWarlockMark(seer, target);
                        break;
                }
            }
            return Mark;
        }
    }
}