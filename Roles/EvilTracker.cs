using System.Collections.Generic;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class EvilTracker
    {
        private static readonly int Id = 2900;
        public static List<byte> playerIdList = new();

        public static CustomOption CanSeeKillFlash;
        public static CustomOption CanResetTargetAfterMeeting;

        public static Dictionary<byte, PlayerControl> Target = new();
        public static Dictionary<byte, bool> CanSetTarget = new();

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.EvilTracker);
            CanSeeKillFlash = CustomOption.Create(Id + 10, Color.white, "EvilTrackerCanSeeKillFlash", true, Options.CustomRoleSpawnChances[CustomRoles.EvilTracker]);
            CanResetTargetAfterMeeting = CustomOption.Create(Id + 11, Color.white, "EvilTrackerResetTargetAfterMeeting", true, Options.CustomRoleSpawnChances[CustomRoles.EvilTracker]);
        }
        public static void Init()
        {
            playerIdList = new();
            Target = new();
            CanSetTarget = new();
        }
        public static void Add(PlayerControl pc)
        {
            playerIdList.Add(pc.PlayerId);
            Target.Add(pc.PlayerId, null);
            CanSetTarget.Add(pc.PlayerId, true);
            // RemoveTargetKey(pc.PlayerId);
        }
        public static bool IsEnable()
        {
            return playerIdList.Count > 0;
        }
        // public static void RPCSetTarget(MessageReader reader)
        // {
        //     byte TrackingId = reader.ReadByte();
        //     var tracking = Utils.GetPlayerById(TrackingId);
        //     if (tracking != null) Target[TrackingId] = tracking;
        // }

        // public static void RPCRemoveTarget(MessageReader reader)
        // {
        //     byte TrackerId = reader.ReadByte();
        //     Target.Remove(TrackerId);
        // }
        public static void ApplyGameOptions(GameOptionsData opt)
        {
            opt.RoleOptions.ShapeshifterCooldown = 5f;
            opt.RoleOptions.ShapeshifterDuration = 1f;
        }
        // public static void SendTarget(byte EvilTrackerId, byte targetId)
        // {
        //     MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetEvilTrackerTarget, Hazel.SendOption.Reliable, -1);
        //     writer.Write(targetId);
        //     AmongUsClient.Instance.FinishRpcImmediately(writer);
        // }
        // public static void RemoveTargetKey(byte EvilTrackerId)
        // {
        //     MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RemoveEvilTrackerTarget, Hazel.SendOption.Reliable, -1);
        //     writer.Write(EvilTrackerId);
        //     AmongUsClient.Instance.FinishRpcImmediately(writer);
        // }
        public static PlayerControl GetTarget(this PlayerControl player)
        {
            if (player == null) return null;
            if (Target == null) Target = new Dictionary<byte, PlayerControl>();
            if (!Target.TryGetValue(player.PlayerId, out var target))
            {
                Target.Add(player.PlayerId, null);
                target = player.RemoveTarget();
            }
            return target;
        }
        public static PlayerControl RemoveTarget(this PlayerControl player)
        {
            if (!AmongUsClient.Instance.AmHost/* && AmongUsClient.Instance.GameMode != GameModes.FreePlay*/) return null;
            Target[player.PlayerId] = null;
            Logger.Info($"プレイヤー{player.GetNameWithRole()}のターゲットを削除", "EvilTracker");
            // RemoveTargetKey(player.PlayerId);
            return Target[player.PlayerId];
        }
        public static bool KillFlashCheck(PlayerControl killer, PlayerState.DeathReason deathReason)
        {
            if (!CanSeeKillFlash.GetBool()) return false;
            else //インポスターによるキルかどうかの判別
            {
                switch (deathReason) //死因での判別
                {
                    case PlayerState.DeathReason.Bite
                        or PlayerState.DeathReason.Sniped
                        or PlayerState.DeathReason.Bombed:
                        return true;
                    case PlayerState.DeathReason.Suicide
                        or PlayerState.DeathReason.LoversSuicide
                        or PlayerState.DeathReason.Misfire
                        or PlayerState.DeathReason.Torched:
                        return false;
                    default:
                        bool PuppeteerCheck = CustomRoles.Puppeteer.IsEnable() && !killer.GetCustomRole().IsImpostor() && Main.PuppeteerList.ContainsKey(killer.PlayerId);
                        return killer.GetCustomRole().IsImpostor() || PuppeteerCheck; //インポスターのノーマルキル || パペッティアキル
                }
            }
        }
        public static string GetMarker(byte playerId) => Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), CanSetTarget[playerId] ? "◁" : "");
        public static string GetTargetMark(PlayerControl seer, PlayerControl target) => Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), Target[seer.PlayerId] == target ? "◀" : "");
        public static string UtilsGetTargetArrow(bool isMeeting, PlayerControl seer)
        {
            //ミーティング以外では矢印表示
            if (isMeeting) return "";
            string SelfSuffix = "";
            foreach (var arrow in Main.targetArrows)
            {
                var target = Utils.GetPlayerById(arrow.Key.Item2);
                bool EvilTrackerTarget = Target[seer.PlayerId] == target;
                if (arrow.Key.Item1 == seer.PlayerId && !target.Data.IsDead && (target.GetCustomRole().IsImpostor() || EvilTrackerTarget))
                    if (EvilTrackerTarget) SelfSuffix += Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Crewmate), arrow.Value);
                    else SelfSuffix += arrow.Value;
            }
            return SelfSuffix;
        }
        public static string PCGetTargetArrow(PlayerControl seer, PlayerControl target)
        {
            var update = false;
            string Suffix = "";
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                bool EvilTrackerTarget = Target[target.PlayerId] == pc;
                bool foundCheck =
                    pc != target && (pc.GetCustomRole().IsImpostor() || EvilTrackerTarget);

                //発見対象じゃ無ければ次
                if (!foundCheck) continue;

                update = FixedUpdatePatch.CheckArrowUpdate(target, pc, update, pc.GetCustomRole().IsImpostor());
                var key = (target.PlayerId, pc.PlayerId);
                var arrow = Main.targetArrows[key];
                if (EvilTrackerTarget) arrow = Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Crewmate), arrow);
                if (target.AmOwner)
                {
                    //MODなら矢印表示
                    Suffix += arrow;
                }
            }
            if (AmongUsClient.Instance.AmHost && seer.PlayerId != target.PlayerId && update)
            {
                //更新があったら非Modに通知
                Utils.NotifyRoles(SpecifySeer: target);
            }
            return Suffix;
        }
        public static void SetMarker(PlayerControl pc)
        {
            if (CanResetTargetAfterMeeting.GetBool()) CanSetTarget[pc.PlayerId] = true;
        }
        public static void Shapeshift(PlayerControl shapeshifter, PlayerControl target, bool shapeshifting)
        {
            if (CanSetTarget[shapeshifter.PlayerId] && shapeshifting)
            {
                if (!target.Data.IsDead && !target.GetCustomRole().IsImpostor())
                {
                    Target[shapeshifter.PlayerId] = target;
                    CanSetTarget[shapeshifter.PlayerId] = false;
                    // SendTarget(shapeshifter.PlayerId, target.PlayerId);
                    Logger.Info($"{shapeshifter.GetNameWithRole()}のターゲットを{Target[shapeshifter.PlayerId].GetNameWithRole()}に設定", "EvilTrackerTarget");
                }
                Utils.CustomSyncAllSettings();
                Utils.NotifyRoles();
            }
        }
        public static void FixedUpdate()
        {
            bool DoNotifyRoles = false;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (!pc.Is(CustomRoles.EvilTracker)) continue;
                var target = pc.GetTarget();
                //EvilTrackerのターゲット削除
                if (pc != target && target != null && (target.Data.IsDead || target.Data.Disconnected))
                {
                    Target[pc.PlayerId] = null;
                    pc.RemoveTarget();
                    Logger.Info($"{pc.GetNameWithRole()}のターゲットが無効だったため、ターゲットを削除しました", "EvilTracker");
                    DoNotifyRoles = true;
                }
            }
            if (DoNotifyRoles) Utils.NotifyRoles();
        }
    }
}