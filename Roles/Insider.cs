using System.Text;
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

        private static Dictionary<CustomRoles, CustomRoles> ReplacementMainRolesDictionary = new()
        {
            // { CustomRoles.Marin, CustomRoles.Crewmate }
        };
        private static List<CustomRoles> IgnoreSubRolesList = new()
        {
            CustomRoles.NotAssigned
        };

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
        public static string GetTargetRoleName(byte playerId)
        {
            var TextData = GetTargetRoleTextData(playerId);
            return Utils.ColorString(TextData.Item2, TextData.Item1);
        }
        public static (string, Color) GetTargetRoleTextData(byte playerId)
        {
            string RoleText = "Invalid Role";
            Color RoleColor = Color.red;

            var mainRole = Main.PlayerStates[playerId].MainRole;
            if (ReplacementMainRolesDictionary.TryGetValue(mainRole, out var newMainRole))
                mainRole = newMainRole;
            RoleText = Utils.GetRoleName(mainRole);
            RoleColor = Utils.GetRoleColor(mainRole);

            (RoleText, RoleColor) = Utils.AddSubRoleText(playerId, RoleText, RoleColor, IgnoreSubRolesList);
            return (RoleText, RoleColor);
        }
        public static string GetProgressText(PlayerControl pc)
        {
            StringBuilder ProgressText = new();
            switch (pc.GetCustomRole()) //本人には表示しないケースのみ
            {
                case CustomRoles.FireWorks:
                    ProgressText.Append(FireWorks.GetFireWorksCount(pc.PlayerId));
                    break;
                case CustomRoles.Witch:
                    ProgressText.Append(Utils.ColorString(Palette.ImpostorRed.ShadeColor(0.5f), GetString(Witch.IsSpellMode(pc.PlayerId) ? "WitchModeSpell" : "WitchModeKill")));
                    break;
                default:
                    if (pc.Is(RoleType.Impostor))
                        ProgressText.Append(Utils.GetProgressText(pc));
                    break;
            }
            if (ProgressText.Length != 0)
                ProgressText.Insert(0, " "); //空じゃなければ空白を追加
            return ProgressText.ToString();
        }
        public static string GetKillCount(byte playerId)
        {
            if (!CanSeeMadmates.GetBool()) return "";

            int killCount = KillCount(playerId);
            int Norma = KillCountToSeeMadmates.GetInt();
            if (killCount < Norma) return Utils.ColorString(Palette.ImpostorRed.ShadeColor(0.5f), $"({killCount}/{Norma})");
            else return Utils.ColorString(Palette.ImpostorRed.ShadeColor(0.5f), " ★");
        }
        public static string GetOtherImpostorMarks(PlayerControl insider, PlayerControl target, bool isMeeting)
        {
            if (!playerIdList.Contains(insider.PlayerId) || !CanSeeImpostorAbilities.GetBool()) return "";
            StringBuilder Mark = new();
            foreach (var seer in Main.AllPlayerControls)
            {
                switch (seer.GetCustomRole())
                {
                    case CustomRoles.BountyHunter:
                        Mark.Append(BountyHunter.GetTargetMark(seer, target));
                        break;
                    case CustomRoles.EvilTracker:
                        Mark.Append(EvilTracker.GetTargetMark(seer, target));
                        break;
                }
            }
            return Mark.ToString();
        }
    }
}