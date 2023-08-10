using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.AddOns.Crewmate;
using static TownOfHost.Translator;
using static TownOfHost.Utils;

namespace TownOfHost.Modules;

public static class RoleNameManager
{
    public static bool KnowOtherRoles(this PlayerControl seer, PlayerControl seen)
    {
        if (seer == seen)
        {
            return true;
        }
        else if (seen.Is(CustomRoles.GM))
        {
            return true;
        }
        else return Main.VisibleTasksCount && !seer.IsAlive() && Options.GhostCanSeeOtherRoles.GetBool();
    }
    public static bool KnowOtherTasks(this PlayerControl seer, PlayerControl seen)
    {
        if (seer == seen)
        {
            return true;
        }
        else if (seen.Is(CustomRoles.GM))
        {
            return true;
        }
        else return Main.VisibleTasksCount && !seer.IsAlive() && Options.GhostCanSeeOtherTasks.GetBool();
    }
    public static string GetRoleText(CustomRoles mainRole, List<CustomRoles> subRoles)
    {
        Color roleColor = GetRoleColor(mainRole);
        string prefix = "";
        string roleText = GetRoleName(mainRole);

        foreach (var subRole in subRoles)
        {
            if (subRole <= CustomRoles.NotAssigned) continue;
            switch (subRole)
            {
                case CustomRoles.LastImpostor:
                    prefix = GetRoleString("Last-");
                    break;
            }
        }

        return ColorString(roleColor, prefix + roleText);
    }
    private static string GetSubRoleMarks(List<CustomRoles> subRoles)
    {
        var sb = new StringBuilder(100);
        foreach (var subRole in subRoles)
        {
            if (subRole <= CustomRoles.NotAssigned) continue;
            switch (subRole)
            {
                case CustomRoles.Watcher:
                    sb.Append(Watcher.SubRoleMark);
                    break;
            }
        }
        return sb.ToString();
    }
    private static string GetTaskText(byte playerId, bool isComms = false)
    {
        var state = PlayerState.GetByPlayerId(playerId);
        var taskState = state?.taskState;
        if (state is null ||
            taskState is null ||
            !taskState.hasTasks)
        {
            return "";
        }

        Color color = Color.yellow;
        string completed = $"{taskState.CompletedTasksCount}";
        bool forRecompute = HasTasks(GetPlayerInfoById(playerId));

        if (isComms)
        {
            color = Color.gray;
            completed = "?";
        }
        else if (taskState.IsTaskFinished)
        {
            color = forRecompute ?
                Color.green :
                GetRoleColor(state.MainRole).ShadeColor(0.5f); //タスク完了後の色
        }
        else if (Workhorse.IsThisRole(playerId))
        {
            color = Workhorse.RoleColor;
        }
        else if (!forRecompute)
        {
            color = Color.white; //カウントされない人外は白色
        }

        return ColorString(color, $"({completed}/{taskState.AllTasksCount})");
    }
    public static string GetProgressText(byte playerId, bool comms = false)
    {
        var sb = new StringBuilder();
        var roleClass = CustomRoleManager.GetByPlayerId(playerId);

        sb.Append(roleClass?.GetProgressText(comms));
        sb.Append((roleClass as ISidekickable)?.NumSidekickLeftMark());

        return sb.ToString();
    }
    /// <summary>
    /// 対象のRoleNameを全て正確に表示
    /// </summary>
    /// <param name="playerId">見られる側のPlayerId</param>
    /// <returns>構築したRoleName</returns>
    public static string GetTrueRoleName(byte playerId, bool isComms = false)
    {
        var state = PlayerState.GetByPlayerId(playerId);
        var roleNameList = new List<string>
        {
            GetRoleText(state.MainRole, state.SubRoles),
            GetSubRoleMarks(state.SubRoles),
            GetTaskText(playerId, isComms),
            GetProgressText(playerId, isComms)
        };
        roleNameList.RemoveAll(x => string.IsNullOrWhiteSpace(x));

        return string.Join(' ', roleNameList);
    }
    public class RoleNameStringBuilder
    {
        public PlayerControl Seer { get; }
        public PlayerControl Seen { get; }
        public bool IsRoleEnable { get; set; } = false;
        public CustomRoles MainRole { get; set; }
        public List<CustomRoles> SubRoles { get; } = new();
        public bool IsTaskEnable { get; set; } = false;
        public Color RoleColor { get; set; } = Color.white;

        private static readonly LogHandler logger = Logger.Handler(nameof(MeetingVoteManager));

        private RoleNameStringBuilder(PlayerControl seer, PlayerControl seen)
        {
            Seer = seer;
            Seen = seen;
            IsRoleEnable = Seer.KnowOtherRoles(Seen);
            IsTaskEnable = Seer.KnowOtherTasks(Seen);
        }
        public void Deconstruct(
            out string roleText,
            out string subRoleMark,
            out string taskText,
            out string progressText
        )
        {
            roleText = GetRoleText(MainRole, SubRoles);
            subRoleMark = GetSubRoleMarks(SubRoles);
            taskText = GetTaskText(Seen.PlayerId);
            progressText = GetProgressText();
        }
        // public void Deconstruct(out string progressText, out bool isEnable)
        // {
        //
        // }
        public override string ToString()
        {
            var (roleText, subRoleMark, taskText, progressText) = this;
        }
        /// <summary>
        /// seerが自分であるときのseenのRoleName + sb
        /// </summary>
        /// <param name="seer">見る側</param>
        /// <param name="seen">見られる側</param>
        /// <returns>RoleName + ProgressTextを表示するか、構築する色とテキスト(bool, Color, string)</returns>
        public static (bool enabled, string text) GetRoleNameAndProgressTextData(PlayerControl seer, PlayerControl seen = null)
        {
            var roleName = GetDisplayRoleName(seer, seen);
            var progressText = GetProgressText(seer, seen);
            var text = roleName + (roleName == "" ? "" : " ") + progressText;
            return (text != "", text);
        }
        /// <summary>
        /// GetDisplayRoleNameDataからRoleNameを構築
        /// </summary>
        /// <param name="seer">見る側</param>
        /// <param name="seen">見られる側</param>
        /// <returns>構築されたRoleName</returns>
        private static string GetDisplayRoleName(PlayerControl seer, PlayerControl seen = null)
        {
            seen ??= seer;
            //デフォルト値
            bool enabled = seer == seen
                        || seen.Is(CustomRoles.GM)
                        || (Main.VisibleTasksCount && !seer.IsAlive() && Options.GhostCanSeeOtherRoles.GetBool());
            var (roleColor, roleText) = RoleNameManager.GetTrueRoleNameData(seen.PlayerId);

            //seen側による変更
            seen.GetRoleClass()?.OverrideRoleNameAsSeen(seer, ref enabled, ref roleColor, ref roleText);

            //seer側による変更
            seer.GetRoleClass()?.OverrideRoleNameAsSeer(seen, ref enabled, ref roleColor, ref roleText);

            return enabled ? ColorString(roleColor, roleText) : "";
        }

        private static string GetProgressText(PlayerControl seer, PlayerControl seen = null)
        {
            seen ??= seer;
            var comms = false;
            foreach (PlayerTask task in PlayerControl.LocalPlayer.myTasks)
            {
                if (task.TaskType == TaskTypes.FixComms)
                {
                    comms = true;
                    break;
                }
            }
            bool enabled = seer == seen
                        || (Main.VisibleTasksCount && !seer.IsAlive() && Options.GhostCanSeeOtherTasks.GetBool());
            string text = RoleNameManager.GetProgressText(seen.PlayerId, comms);

            //seer側による変更
            seer.GetRoleClass()?.OverrideProgressTextAsSeer(seen, ref enabled, ref text);

            return enabled ? text : "";
        }
    }
}
