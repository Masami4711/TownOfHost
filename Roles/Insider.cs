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
        private static OptionItem CanSeeImpostorAbilities;
        private static OptionItem CanSeeAllGhostsRoles;
        private static OptionItem CanSeeMadmates;
        private static OptionItem KillCountToSeeMadmates;
        private static OptionItem CanSeeOutsider;
        static Dictionary<CustomRoles, CustomRoles> ReplaceRoles = new()
        {
            // {CustomRoles.Marin, CustomRoles.Crewmate}
        };
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Insider);
            CanSeeImpostorAbilities = OptionItem.Create(Id + 10, TabGroup.ImpostorRoles, Color.white, "InsiderCanSeeImpostorAbilities", true, Options.CustomRoleSpawnChances[CustomRoles.Insider]);
            CanSeeAllGhostsRoles = OptionItem.Create(Id + 11, TabGroup.ImpostorRoles, Color.white, "InsiderCanSeeAllGhostsRoles", false, Options.CustomRoleSpawnChances[CustomRoles.Insider]);
            CanSeeMadmates = OptionItem.Create(Id + 12, TabGroup.ImpostorRoles, Color.white, "InsiderCanSeeMadmates", false, Options.CustomRoleSpawnChances[CustomRoles.Insider]);
            KillCountToSeeMadmates = OptionItem.Create(Id + 13, TabGroup.ImpostorRoles, Color.white, "InsiderKillCountToSeeMadmates", 2, 0, 12, 1, CanSeeMadmates);
            CanSeeOutsider = OptionItem.Create(Id + 14, TabGroup.ImpostorRoles, Color.white, "InsiderCanSeeOutsider", false, CanSeeMadmates);
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
        public static bool KnowImpostorAbiliies(PlayerControl seer) => seer.Is(CustomRoles.Insider) && CanSeeImpostorAbilities.GetBool();
        public static bool KnowOtherRole(PlayerControl Insider, PlayerControl target) //Insider能力で役職が分かるケースのみ
        {
            if (!Insider.Is(CustomRoles.Insider)) return false; //Insider以外
            if (!GameStates.IsMeeting && !Insider.Data.IsDead && target.Data.IsDead) return false; //タスクフェーズでTargetが死んでいる
            if (Insider == target) return false; //自分自身
            if (Insider.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool()) return false; //幽霊で普通に見えるパターン
            // ここまで前提条件
            switch (target.GetCustomRole().GetRoleType())
            {
                case RoleType.Impostor:
                    if ((CanSeeImpostorAbilities.GetBool() && target.Is(RoleType.Impostor, false)) || KnowOutsider(Insider, target))
                        return true;
                    break;
                case RoleType.Madmate:
                    if (KnowMadmates(Insider))
                        return true;
                    break;
            }
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
            else if (target.GetRealKiller() == Insider) return true; //自分でキルした相手
            return false;
        }
        public static bool KnowMadmates(PlayerControl seer)
            => seer.Is(CustomRoles.Insider) && CanSeeMadmates.GetBool()
            && KillCount(seer) >= KillCountToSeeMadmates.GetInt();
        public static bool KnowOutsider(PlayerControl Insider, PlayerControl Outsider)
        => KnowMadmates(Insider) && CanSeeOutsider.GetBool() && Outsider.Is(CustomRoles.Outsider);
        public static int KillCount(PlayerControl Insider)
            => Main.PlayerStates[Insider.PlayerId].GetKillCount(true);
        public static string GetKillCount(byte playerId)
        {
            string ProgressText = "";
            if (CanSeeMadmates.GetBool())
            {
                int killCount = KillCount(Utils.GetPlayerById(playerId));
                int Norma = KillCountToSeeMadmates.GetInt();
                if (killCount < Norma) ProgressText += Utils.ColorString(Palette.ImpostorRed.ShadeColor(0.5f), $"({killCount}/{Norma})");
                else ProgressText += Utils.ColorString(Palette.ImpostorRed.ShadeColor(0.5f), "★");
            }
            return ProgressText;
        }
        public static string GetRoleText(PlayerControl target)
        {
            var RoleName = Utils.GetSelfRoleName(target.PlayerId);
            var RoleColor = target.GetRoleColor();
            if (ReplaceRoles.TryGetValue(target.GetCustomRole(), out var newRole))
            {
                RoleName = Utils.GetRoleName(newRole);
                RoleColor = Utils.GetRoleColor(newRole);
            }
            return Utils.ColorString(RoleColor, RoleName);
        }
        public static string GetTaskText(PlayerControl target)
        {
            if (DisableTaskText(target)) return "";
            return target.GetCustomRole() switch //本人には表示しないケースのみ
            {
                CustomRoles.FireWorks => FireWorks.GetFireWorksCount(target.PlayerId),
                CustomRoles.Witch => Utils.ColorString(Palette.ImpostorRed.ShadeColor(0.5f), $" {GetString(target.IsSpellMode() ? "WitchModeSpell" : "WitchModeKill")}"),
                _ => Utils.GetProgressText(target),
            };
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