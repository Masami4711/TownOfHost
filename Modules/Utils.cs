using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AmongUs.Data;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class Utils
    {
        public static bool IsActive(SystemTypes type)
        {
            //Logger.Info($"SystemTypes:{type}", "IsActive");
            int mapId = PlayerControl.GameOptions.MapId;
            switch (type)
            {
                case SystemTypes.Electrical:
                    {
                        var SwitchSystem = ShipStatus.Instance.Systems[type].Cast<SwitchSystem>();
                        return SwitchSystem != null && SwitchSystem.IsActive;
                    }
                case SystemTypes.Reactor:
                    {
                        if (mapId == 2) return false;
                        else if (mapId == 4)
                        {
                            var HeliSabotageSystem = ShipStatus.Instance.Systems[type].Cast<HeliSabotageSystem>();
                            return HeliSabotageSystem != null && HeliSabotageSystem.IsActive;
                        }
                        else
                        {
                            var ReactorSystemType = ShipStatus.Instance.Systems[type].Cast<ReactorSystemType>();
                            return ReactorSystemType != null && ReactorSystemType.IsActive;
                        }
                    }
                case SystemTypes.Laboratory:
                    {
                        if (mapId != 2) return false;
                        var ReactorSystemType = ShipStatus.Instance.Systems[type].Cast<ReactorSystemType>();
                        return ReactorSystemType != null && ReactorSystemType.IsActive;
                    }
                case SystemTypes.LifeSupp:
                    {
                        if (mapId is 2 or 4) return false;
                        var LifeSuppSystemType = ShipStatus.Instance.Systems[type].Cast<LifeSuppSystemType>();
                        return LifeSuppSystemType != null && LifeSuppSystemType.IsActive;
                    }
                case SystemTypes.Comms:
                    {
                        if (mapId == 1)
                        {
                            var HqHudSystemType = ShipStatus.Instance.Systems[type].Cast<HqHudSystemType>();
                            return HqHudSystemType != null && HqHudSystemType.IsActive;
                        }
                        else
                        {
                            var HudOverrideSystemType = ShipStatus.Instance.Systems[type].Cast<HudOverrideSystemType>();
                            return HudOverrideSystemType != null && HudOverrideSystemType.IsActive;
                        }
                    }
                default:
                    return false;
            }
        }
        public static void SetVision(this GameOptionsData opt, PlayerControl player, bool HasImpVision)
        {
            if (HasImpVision)
            {
                opt.CrewLightMod = opt.ImpostorLightMod;
                if (IsActive(SystemTypes.Electrical))
                    opt.CrewLightMod *= 5;
                return;
            }
            else
            {
                opt.ImpostorLightMod = opt.CrewLightMod;
                if (IsActive(SystemTypes.Electrical))
                    opt.ImpostorLightMod /= 5;
                return;
            }
        }
        //誰かが死亡したときのメソッド
        public static void TargetDies(PlayerControl killer, PlayerControl target)
        {
            if (!target.Data.IsDead || GameStates.IsMeeting) return;
            foreach (var seer in PlayerControl.AllPlayerControls)
            {
                if (!KillFlashCheck(killer, target, seer)) continue;
                seer.KillFlash();
            }
        }
        public static bool KillFlashCheck(PlayerControl killer, PlayerControl target, PlayerControl seer)
        {
            if (seer.Is(CustomRoles.GM)) return true;
            if (seer.Data.IsDead || killer == seer || target == seer) return false;
            return seer.GetCustomRole() switch
            {
                CustomRoles.EvilTracker => EvilTracker.KillFlashCheck(killer, target),
                CustomRoles.Seer => true,
                _ => seer.Is(RoleType.Madmate) && Options.MadmateCanSeeKillFlash.GetBool(),
            };
        }
        public static void KillFlash(this PlayerControl player)
        {
            //キルフラッシュ(ブラックアウト+リアクターフラッシュ)の処理
            bool ReactorCheck = false; //リアクターフラッシュの確認
            if (PlayerControl.GameOptions.MapId == 2) ReactorCheck = IsActive(SystemTypes.Laboratory);
            else ReactorCheck = IsActive(SystemTypes.Reactor);

            var Duration = Options.KillFlashDuration.GetFloat();
            if (ReactorCheck) Duration += 0.2f; //リアクター中はブラックアウトを長くする

            //実行
            Main.PlayerStates[player.PlayerId].IsBlackOut = true; //ブラックアウト
            if (player.PlayerId == 0)
            {
                FlashColor(new(1f, 0f, 0f, 0.5f));
                if (Constants.ShouldPlaySfx()) RPC.PlaySound(player.PlayerId, Sounds.KillSound);
            }
            else if (!ReactorCheck) player.ReactorFlash(0f); //リアクターフラッシュ
            ExtendedPlayerControl.CustomSyncSettings(player);
            new LateTask(() =>
            {
                Main.PlayerStates[player.PlayerId].IsBlackOut = false; //ブラックアウト解除
                ExtendedPlayerControl.CustomSyncSettings(player);
            }, Options.KillFlashDuration.GetFloat(), "RemoveKillFlash");
        }
        public static void BlackOut(this GameOptionsData opt, bool IsBlackOut)
        {
            opt.ImpostorLightMod = Main.DefaultImpostorVision;
            opt.CrewLightMod = Main.DefaultCrewmateVision;
            if (IsBlackOut)
            {
                opt.ImpostorLightMod = 0.0f;
                opt.CrewLightMod = 0.0f;
            }
            return;
        }
        ///<summary>
        ///最終結果にも表示する役職名
        ///</summary>
        public static string GetDisplayRoleName(byte playerId)
        {
            string RoleText = "Invalid Role";
            Color RoleColor = Color.red;

            var mainRole = Main.PlayerStates[playerId].MainRole;
            var SubRoles = Main.PlayerStates[playerId].SubRoles;
            RoleText = GetRoleName(mainRole);
            if (mainRole.IsImpostor() && mainRole != CustomRoles.LastImpostor && IsLastImpostor(playerId))
            {
                RoleText = GetRoleString("Last-") + RoleText;
            }
            RoleColor = GetRoleColor(mainRole);

            return ColorString(RoleColor, RoleText);
        }
        public static string GetRoleName(CustomRoles role)
        {
            return GetRoleString(Enum.GetName(typeof(CustomRoles), role));
        }
        public static string GetDeathReason(PlayerState.DeathReason status)
        {
            return GetString("DeathReason." + Enum.GetName(typeof(PlayerState.DeathReason), status));
        }
        public static Color GetRoleColor(CustomRoles role)
        {
            if (!Main.roleColors.TryGetValue(role, out var hexColor)) hexColor = "#ffffff";
            ColorUtility.TryParseHtmlString(hexColor, out Color c);
            return c;
        }
        public static string GetRoleColorCode(CustomRoles role)
        {
            if (!Main.roleColors.TryGetValue(role, out var hexColor)) hexColor = "#ffffff";
            return hexColor;
        }
        public static string GetVitalText(byte playerId, bool RealKillerColor = false)
        {
            var state = Main.PlayerStates[playerId];
            string deathReason = state.IsDead ? GetString("DeathReason." + state.deathReason) : GetString("Alive");
            if (RealKillerColor)
            {
                var KillerId = state.GetRealKiller();
                Color color = KillerId != byte.MaxValue ? Main.PlayerColors[KillerId] : GetRoleColor(CustomRoles.Doctor);
                deathReason = ColorString(color, deathReason);
            }
            return deathReason;
        }
        public static (string, Color) GetRoleTextHideAndSeek(RoleTypes oRole, CustomRoles hRole)
        {
            string text = "Invalid";
            Color color = Color.red;
            switch (oRole)
            {
                case RoleTypes.Impostor:
                case RoleTypes.Shapeshifter:
                    text = "Impostor";
                    color = Palette.ImpostorRed;
                    break;
                default:
                    switch (hRole)
                    {
                        case CustomRoles.Crewmate:
                            text = "Crewmate";
                            color = Color.white;
                            break;
                        case CustomRoles.HASFox:
                            text = "Fox";
                            color = Color.magenta;
                            break;
                        case CustomRoles.HASTroll:
                            text = "Troll";
                            color = Color.green;
                            break;
                    }
                    break;
            }
            return (text, color);
        }

        public static bool HasTasks(GameData.PlayerInfo p, bool ForRecompute = true)
        {
            if (GameStates.IsLobby) return false;
            //Tasksがnullの場合があるのでその場合タスク無しとする
            if (p.Tasks == null) return false;
            if (p.Role == null) return false;

            var hasTasks = true;
            var States = Main.PlayerStates[p.PlayerId];
            if (p.Disconnected) hasTasks = false;
            if (p.Role.IsImpostor)
                hasTasks = false; //タスクはCustomRoleを元に判定する
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                if (p.IsDead) hasTasks = false;
                if (States.MainRole is CustomRoles.HASFox or CustomRoles.HASTroll) hasTasks = false;
            }
            else
            {
                if (p.IsDead && Options.GhostIgnoreTasks.GetBool()) hasTasks = false;
                var role = States.MainRole;
                switch (role)
                {
                    case CustomRoles.GM:
                    case CustomRoles.Madmate:
                    case CustomRoles.SKMadmate:
                    case CustomRoles.Sheriff:
                    case CustomRoles.Arsonist:
                    case CustomRoles.Egoist:
                    case CustomRoles.Jackal:
                    case CustomRoles.Jester:
                    case CustomRoles.Opportunist:
                        hasTasks = false;
                        break;
                    case CustomRoles.MadGuardian:
                    case CustomRoles.MadSnitch:
                    case CustomRoles.Terrorist:
                        if (ForRecompute)
                            hasTasks = false;
                        break;
                    case CustomRoles.Executioner:
                        if (Executioner.ChangeRolesAfterTargetKilled.GetSelection() == 0)
                            hasTasks = !ForRecompute;
                        else hasTasks = false;
                        break;
                    case CustomRoles.Runaway:
                        if (p.IsDead && (Main.PlayerStates[p.PlayerId].deathReason != PlayerState.DeathReason.Escape || ForRecompute))
                            hasTasks = false;
                        break;
                    default:
                        if (role.IsImpostor() || role.IsKilledSchrodingerCat()) hasTasks = false;
                        break;
                }

                foreach (var subRole in States.SubRoles)
                    switch (subRole)
                    {
                        case CustomRoles.Lovers:
                            //ラバーズがクルー陣営の場合タスクを付与しない
                            if (role.IsCrewmate())
                                hasTasks = false;
                            break;
                    }
            }
            return hasTasks;
        }
        public static string GetProgressText(PlayerControl pc)
        {
            if (!Main.playerVersion.ContainsKey(0)) return ""; //ホストがMODを入れていなければ未記入を返す
            var taskState = pc.GetPlayerTaskState();
            var Comms = false;
            if (taskState.hasTasks)
            {
                foreach (PlayerTask task in PlayerControl.LocalPlayer.myTasks)
                    if (task.TaskType == TaskTypes.FixComms)
                    {
                        Comms = true;
                        break;
                    }
            }
            return GetProgressText(pc.PlayerId, Comms);
        }
        public static string GetProgressText(byte playerId, bool comms = false)
        {
            if (!Main.playerVersion.ContainsKey(0)) return ""; //ホストがMODを入れていなければ未記入を返す
            string ProgressText = "";
            var role = Main.PlayerStates[playerId].MainRole;
            switch (role)
            {
                case CustomRoles.Arsonist:
                    var doused = GetDousedPlayerCount(playerId);
                    ProgressText = ColorString(GetRoleColor(CustomRoles.Arsonist).ShadeColor(0.25f), $"({doused.Item1}/{doused.Item2})");
                    break;
                case CustomRoles.Sheriff:
                    ProgressText += Sheriff.GetShotLimit(playerId);
                    break;
                case CustomRoles.Sniper:
                    ProgressText += Sniper.GetBulletCount(playerId);
                    break;
                case CustomRoles.TimeThief:
                    ProgressText += TimeThief.GetDecreacedTime(playerId);
                    break;
                case CustomRoles.EvilTracker:
                    ProgressText += EvilTracker.GetMarker(playerId);
                    break;
                case CustomRoles.Insider:
                    ProgressText += Insider.GetKillCount(playerId);
                    break;
                case CustomRoles.Outsider:
                    ProgressText += Outsider.GetKillCount(playerId);
                    break;
                default:
                    //タスクテキスト
                    var taskState = Main.PlayerStates?[playerId].GetTaskState();
                    if (taskState.hasTasks)
                    {
                        Color TextColor = Color.yellow;
                        var info = GetPlayerInfoById(playerId);
                        var TaskCompleteColor = HasTasks(info) ? Color.green : GetRoleColor(role).ShadeColor(0.5f); //タスク完了後の色
                        var NonCompleteColor = HasTasks(info) ? Color.yellow : Color.white; //カウントされない人外は白色
                        var NormalColor = taskState.IsTaskFinished ? TaskCompleteColor : NonCompleteColor;
                        TextColor = comms ? Color.gray : NormalColor;
                        string Completed = comms ? "?" : $"{taskState.CompletedTasksCount}";
                        ProgressText = ColorString(TextColor, $"({Completed}/{taskState.AllTasksCount})");
                    }
                    break;
            }
            if (GetPlayerById(playerId).CanMakeMadmate()) ProgressText += ColorString(Palette.ImpostorRed.ShadeColor(0.5f), $" [{Options.CanMakeMadmateCount.GetInt() - Main.SKMadmateNowCount}]");

            return ProgressText;
        }
        public static void ShowActiveSettingsHelp(byte PlayerId = byte.MaxValue)
        {
            SendMessage(GetString("CurrentActiveSettingsHelp") + ":", PlayerId);
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                SendMessage(GetString("HideAndSeekInfo"), PlayerId);
                if (CustomRoles.HASFox.IsEnable()) { SendMessage(GetRoleName(CustomRoles.HASFox) + GetString("HASFoxInfoLong"), PlayerId); }
                if (CustomRoles.HASTroll.IsEnable()) { SendMessage(GetRoleName(CustomRoles.HASTroll) + GetString("HASTrollInfoLong"), PlayerId); }
            }
            else
            {
                if (Options.DisableDevices.GetBool()) { SendMessage(GetString("DisableDevicesInfo"), PlayerId); }
                if (Options.SyncButtonMode.GetBool()) { SendMessage(GetString("SyncButtonModeInfo"), PlayerId); }
                if (Options.SabotageTimeControl.GetBool()) { SendMessage(GetString("SabotageTimeControlInfo"), PlayerId); }
                if (Options.RandomMapsMode.GetBool()) { SendMessage(GetString("RandomMapsModeInfo"), PlayerId); }
                if (Options.IsStandardHAS) { SendMessage(GetString("StandardHASInfo"), PlayerId); }
                if (Options.EnableGM.GetBool()) { SendMessage(GetRoleName(CustomRoles.GM) + GetString("GMInfoLong"), PlayerId); }
                foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
                {
                    if (role is CustomRoles.HASFox or CustomRoles.HASTroll) continue;
                    if (role.IsEnable() && !role.IsVanilla()) SendMessage(GetRoleName(role) + GetString(Enum.GetName(typeof(CustomRoles), role) + "InfoLong"), PlayerId);
                }
                if (Options.EnableLastImpostor.GetBool()) { SendMessage(GetRoleName(CustomRoles.LastImpostor) + GetString("LastImpostorInfoLong"), PlayerId); }
            }
            if (Options.NoGameEnd.GetBool()) { SendMessage(GetString("NoGameEndInfo"), PlayerId); }
        }
        public static void ShowActiveSettings(byte PlayerId = byte.MaxValue)
        {
            var mapId = PlayerControl.GameOptions.MapId;
            if (Options.HideGameSettings.GetBool() && PlayerId != byte.MaxValue)
            {
                SendMessage(GetString("Message.HideGameSettings"), PlayerId);
                return;
            }
            var text = "";
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                text = GetString("Roles") + ":";
                if (CustomRoles.HASFox.IsEnable()) text += String.Format("\n{0}:{1}", GetRoleName(CustomRoles.HASFox), CustomRoles.HASFox.GetCount());
                if (CustomRoles.HASTroll.IsEnable()) text += String.Format("\n{0}:{1}", GetRoleName(CustomRoles.HASTroll), CustomRoles.HASTroll.GetCount());
                SendMessage(text, PlayerId);
                text = GetString("Settings") + ":";
                text += GetString("HideAndSeek");
            }
            else
            {
                text = GetString("Settings") + ":";
                foreach (var role in Options.CustomRoleCounts)
                {
                    if (!role.Key.IsEnable()) continue;
                    text += $"\n【{GetRoleName(role.Key)}×{role.Key.GetCount()}】\n";
                    ShowChildrenSettings(Options.CustomRoleSpawnChances[role.Key], ref text);
                    text = text.RemoveHtmlTags();
                }
                foreach (var opt in OptionItem.Options.Where(x => x.Enabled && x.Parent == null && x.Id >= 80000 && !x.IsHidden(Options.CurrentGameMode)))
                {
                    if (opt.Name is "KillFlashDuration" or "RoleAssigningAlgorithm")
                        text += $"\n【{opt.GetName(true)}: {opt.GetString()}】\n";
                    else
                        text += $"\n【{opt.GetName(true)}】\n";
                    ShowChildrenSettings(opt, ref text);
                    text = text.RemoveHtmlTags();
                }
            }
            SendMessage(text, PlayerId);
        }
        public static void CopyCurrentSettings()
        {
            var text = "";
            if (Options.HideGameSettings.GetBool() && !AmongUsClient.Instance.AmHost)
            {
                ClipboardHelper.PutClipboardString(GetString("Message.HideGameSettings"));
                return;
            }
            text += $"━━━━━━━━━━━━【{GetString("Roles")}】━━━━━━━━━━━━";
            foreach (var role in Options.CustomRoleCounts)
            {
                if (!role.Key.IsEnable()) continue;
                text += $"\n【{GetRoleName(role.Key)}×{role.Key.GetCount()}】\n";
                ShowChildrenSettings(Options.CustomRoleSpawnChances[role.Key], ref text);
                text = text.RemoveHtmlTags();
            }
            text += $"━━━━━━━━━━━━【{GetString("Settings")}】━━━━━━━━━━━━";
            foreach (var opt in OptionItem.Options.Where(x => x.Enabled && x.Parent == null && x.Id >= 80000 && !x.IsHidden(Options.CurrentGameMode)))
            {
                if (opt.Name == "KillFlashDuration")
                    text += $"\n【{opt.GetName(true)}: {opt.GetString()}】\n";
                else
                    text += $"\n【{opt.GetName(true)}】\n";
                ShowChildrenSettings(opt, ref text);
                text = text.RemoveHtmlTags();
            }
            text += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";
            ClipboardHelper.PutClipboardString(text);
        }
        public static void ShowActiveRoles(byte PlayerId = byte.MaxValue)
        {
            if (Options.HideGameSettings.GetBool() && PlayerId != byte.MaxValue)
            {
                SendMessage(GetString("Message.HideGameSettings"), PlayerId);
                return;
            }
            var text = GetString("Roles") + ":";
            text += string.Format("\n{0}:{1}", GetRoleName(CustomRoles.GM), Options.EnableGM.GetString().RemoveHtmlTags());
            foreach (CustomRoles role in Enum.GetValues(typeof(CustomRoles)))
            {
                if (role is CustomRoles.HASFox or CustomRoles.HASTroll) continue;
                if (role.IsEnable()) text += string.Format("\n{0}:{1}x{2}", GetRoleName(role), $"{role.GetChance() * 100}%", role.GetCount());
            }
            SendMessage(text, PlayerId);
        }
        public static void ShowChildrenSettings(OptionItem option, ref string text, int deep = 0)
        {
            foreach (var opt in option.Children.Select((v, i) => new { Value = v, Index = i + 1 }))
            {
                if (opt.Value.Name == "Maximum") continue; //Maximumの項目は飛ばす
                if (opt.Value.Name == "DisableSkeldDevices" && !Options.IsActiveSkeld) continue;
                if (opt.Value.Name == "DisableMiraHQDevices" && !Options.IsActiveMiraHQ) continue;
                if (opt.Value.Name == "DisablePolusDevices" && !Options.IsActivePolus) continue;
                if (opt.Value.Name == "DisableAirshipDevices" && !Options.IsActiveAirship) continue;
                if (opt.Value.Name == "PolusReactorTimeLimit" && !Options.IsActivePolus) continue;
                if (opt.Value.Name == "AirshipReactorTimeLimit" && !Options.IsActiveAirship) continue;
                if (deep > 0)
                {
                    text += string.Concat(Enumerable.Repeat("┃", Mathf.Max(deep - 1, 0)));
                    text += opt.Index == option.Children.Count ? "┗ " : "┣ ";
                }
                text += $"{opt.Value.GetName(true)}: {opt.Value.GetString()}\n";
                if (opt.Value.Enabled) ShowChildrenSettings(opt.Value, ref text, deep + 1);
            }
        }
        public static void ShowLastResult(byte PlayerId = byte.MaxValue)
        {
            if (AmongUsClient.Instance.IsGameStarted)
            {
                SendMessage(GetString("CantUse.lastroles"), PlayerId);
                return;
            }
            var text = GetString("LastResult") + ":";
            List<byte> cloneRoles = new(Main.PlayerStates.Keys);
            text += $"\n{SetEverythingUpPatch.LastWinsText}\n";
            foreach (var id in Main.winnerList)
            {
                text += $"\n★ " + EndGamePatch.SummaryText[id].RemoveHtmlTags();
                cloneRoles.Remove(id);
            }
            foreach (var id in cloneRoles)
            {
                text += $"\n　 " + EndGamePatch.SummaryText[id].RemoveHtmlTags();
            }
            SendMessage(text, PlayerId);
            SendMessage(EndGamePatch.KillLog, PlayerId);
        }


        public static string GetSubRolesText(byte id, bool disableColor = false)
        {
            var SubRoles = Main.PlayerStates[id].SubRoles;
            if (SubRoles.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var role in SubRoles)
            {
                if (role == CustomRoles.NotAssigned) continue;

                var RoleText = disableColor ? GetRoleName(role) : ColorString(GetRoleColor(role), GetRoleName(role));
                sb.Append($"</color> + {RoleText}");
            }

            return sb.ToString();
        }

        public static void ShowHelp()
        {
            SendMessage(
                GetString("CommandList")
                + $"\n/winner - {GetString("Command.winner")}"
                + $"\n/lastresult - {GetString("Command.lastresult")}"
                + $"\n/rename - {GetString("Command.rename")}"
                + $"\n/now - {GetString("Command.now")}"
                + $"\n/h now - {GetString("Command.h_now")}"
                + $"\n/h roles {GetString("Command.h_roles")}"
                + $"\n/h addons {GetString("Command.h_addons")}"
                + $"\n/h modes {GetString("Command.h_modes")}"
                + $"\n/dump - {GetString("Command.dump")}"
                );

        }
        public static void CheckTerroristWin(GameData.PlayerInfo Terrorist)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            var taskState = GetPlayerById(Terrorist.PlayerId).GetPlayerTaskState();
            if (taskState.IsTaskFinished && (!Main.PlayerStates[Terrorist.PlayerId].IsSuicide() || Options.CanTerroristSuicideWin.GetBool())) //タスクが完了で（自殺じゃない OR 自殺勝ちが許可）されていれば
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.Terrorist))
                    {
                        if (Main.PlayerStates[pc.PlayerId].deathReason == PlayerState.DeathReason.Vote)
                        {
                            //追放された場合は生存扱い
                            Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.etc;
                            //生存扱いのためSetDeadは必要なし
                        }
                        else
                        {
                            //キルされた場合は自爆扱い
                            Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
                        }
                    }
                    else if (!pc.Data.IsDead)
                    {
                        //生存者は爆死
                        pc.SetRealKiller(Terrorist.Object);
                        pc.RpcMurderPlayer(pc);
                        Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                        Main.PlayerStates[pc.PlayerId].SetDead();
                    }
                }
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Terrorist);
                CustomWinnerHolder.WinnerIds.Add(Terrorist.PlayerId);
            }
        }
        public static void SendMessage(string text, byte sendTo = byte.MaxValue, string title = "")
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (title == "") title = "<color=#aaaaff>" + GetString("DefaultSystemMessageTitle") + "</color>";
            Main.MessagesToSend.Add((text.RemoveHtmlTags(), sendTo, title));
        }
        public static void ApplySuffix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            string name = DataManager.player.Customization.Name;
            if (Main.nickName != "") name = Main.nickName;
            if (AmongUsClient.Instance.IsGameStarted)
            {
                if (Options.ColorNameMode.GetBool() && Main.nickName == "") name = Palette.GetColorName(PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId);
            }
            else
            {
                if (AmongUsClient.Instance.IsGamePublic)
                    name = $"<color={Main.ModColor}>TownOfHost v{Main.PluginVersion}</color>\r\n" + name;
                switch (Options.GetSuffixMode())
                {
                    case SuffixModes.None:
                        break;
                    case SuffixModes.TOH:
                        name += $"\r\n<color={Main.ModColor}>TOH v{Main.PluginVersion}</color>";
                        break;
                    case SuffixModes.Streaming:
                        name += $"\r\n<color={Main.ModColor}>{GetString("SuffixMode.Streaming")}</color>";
                        break;
                    case SuffixModes.Recording:
                        name += $"\r\n<color={Main.ModColor}>{GetString("SuffixMode.Recording")}</color>";
                        break;
                    case SuffixModes.RoomHost:
                        name += $"\r\n<color={Main.ModColor}>{GetString("SuffixMode.RoomHost")}</color>";
                        break;
                    case SuffixModes.OriginalName:
                        name += $"\r\n<color={Main.ModColor}>{DataManager.player.Customization.Name}</color>";
                        break;
                }
            }
            if (name != PlayerControl.LocalPlayer.name && PlayerControl.LocalPlayer.CurrentOutfitType == PlayerOutfitType.Default) PlayerControl.LocalPlayer.RpcSetName(name);
        }
        public static PlayerControl GetPlayerById(int PlayerId)
        {
            return PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == PlayerId).FirstOrDefault();
        }
        public static GameData.PlayerInfo GetPlayerInfoById(int PlayerId) =>
            GameData.Instance.AllPlayers.ToArray().Where(info => info.PlayerId == PlayerId).FirstOrDefault();
        public static void NotifyRoles(bool isMeeting = false, PlayerControl SpecifySeer = null, bool NoCache = false, bool ForceLoop = false)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (PlayerControl.AllPlayerControls == null) return;

            var caller = new System.Diagnostics.StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string callerClassName = callerMethod.DeclaringType.FullName;
            TownOfHost.Logger.Info("NotifyRolesが" + callerClassName + "." + callerMethodName + "から呼び出されました", "NotifyRoles");
            HudManagerPatch.NowCallNotifyRolesCount++;
            HudManagerPatch.LastSetNameDesyncCount = 0;

            var seerList = PlayerControl.AllPlayerControls;
            if (SpecifySeer != null)
            {
                seerList = new();
                seerList.Add(SpecifySeer);
            }
            //seer:ここで行われた変更を見ることができるプレイヤー
            //target:seerが見ることができる変更の対象となるプレイヤー
            foreach (var seer in seerList)
            {
                if (seer.IsModClient()) continue;
                string fontSize = "1.5";
                if (isMeeting && (seer.GetClient().PlatformData.Platform.ToString() == "Playstation" || seer.GetClient().PlatformData.Platform.ToString() == "Switch")) fontSize = "70%";
                Logger.Info("NotifyRoles-Loop1-" + seer.GetNameWithRole() + ":START", "NotifyRoles");
                //Loop1-bottleのSTART-END間でKeyNotFoundException
                //seerが落ちているときに何もしない
                if (seer.Data.Disconnected) continue;

                //Markとは違い、改行してから追記されます。
                string SelfSuffix = "";

                switch (seer.GetCustomRole())
                {
                    case CustomRoles.BountyHunter:
                        if (BountyHunter.GetTarget(seer) != null)
                        {
                            string BountyTargetName = BountyHunter.GetTarget(seer).GetRealName(isMeeting);
                            SelfSuffix = $"<size={fontSize}>Target:{BountyTargetName}</size>";
                        }
                        break;
                    case CustomRoles.EvilTracker:
                        SelfSuffix = EvilTracker.UtilsGetTargetArrow(isMeeting, seer);
                        break;
                    case CustomRoles.FireWorks:
                        SelfSuffix = FireWorks.GetStateText(seer);
                        break;
                    case CustomRoles.Witch:
                        SelfSuffix = "Mode:" + GetString(seer.IsSpellMode() ? "WitchModeSpell" : "WitchModeKill");
                        break;
                    case CustomRoles.Snitch:
                        if (seer.KnowImpostor() && Options.SnitchEnableTargetArrow.GetBool() && !isMeeting)
                        {
                            foreach (var arrow in Main.targetArrows)
                            {
                                //自分用の矢印で対象が死んでない時
                                if (arrow.Key.Item1 == seer.PlayerId && !Main.PlayerStates[arrow.Key.Item2].IsDead)
                                    SelfSuffix += arrow.Value;
                            }
                        }
                        break;
                    case CustomRoles.Runaway:
                        SelfSuffix += Runaway.GetSuffixText(seer, fontSize);
                        break;
                }

                //seerの役職名とSelfTaskTextとseerのプレイヤー名とSelfMarkを合成
                string SelfRoleName = $"<size={fontSize}>{GetTargetRoleText(seer, seer, isMeeting)}{GetTargetTaskText(seer, seer, isMeeting)}</size>";
                string SelfName = GetDisplayRealName(seer, seer, isMeeting) + GetDeathReasonText(seer, seer) + GetTargetMark(seer, seer, isMeeting);
                SelfName = SelfRoleName + "\r\n" + SelfName;
                if (SelfSuffix != "") SelfName += "\r\n " + SelfSuffix;
                if (!isMeeting) SelfName += "\r\n";

                //適用
                seer.RpcSetNamePrivate(SelfName, true, force: NoCache);

                //seerが死んでいる場合など、必要なときのみ第二ループを実行する
                if (seer.Data.IsDead //seerが死んでいる
                    || seer.KnowImpostor() //seerがインポスターを知っている状態
                    || seer.Is(RoleType.Impostor) //seerがインポスター
                    || seer.Is(RoleType.Madmate) //seerがインポスター
                    || seer.Is(CustomRoles.EgoSchrodingerCat) //seerがエゴイストのシュレディンガーの猫
                    || seer.Is(CustomRoles.JSchrodingerCat) //seerがJackal陣営のシュレディンガーの猫
                    || NameColorManager.Instance.GetDataBySeer(seer.PlayerId).Count > 0 //seer視点用の名前色データが一つ以上ある
                    || seer.Is(CustomRoles.Arsonist)
                    || seer.Is(CustomRoles.Lovers)
                    || Main.SpelledPlayer != null || Main.SpelledPlayer.Count > 0
                    || seer.Is(CustomRoles.Executioner)
                    || seer.Is(CustomRoles.Doctor) //seerがドクター
                    || seer.IsNeutralKiller() //seerがキル出来る第三陣営
                    || IsActive(SystemTypes.Electrical)
                    || IsActive(SystemTypes.Comms)
                    || NoCache
                    || ForceLoop
                )
                {
                    foreach (var target in PlayerControl.AllPlayerControls)
                    {
                        //targetがseer自身の場合は何もしない
                        if (target == seer || target.Data.Disconnected) continue;
                        Logger.Info("NotifyRoles-Loop2-" + target.GetNameWithRole() + ":START", "NotifyRoles");

                        string TargetRoleText = GetTargetRoleText(seer, target, isMeeting) + GetTargetTaskText(seer, target, isMeeting);
                        if (TargetRoleText != "") TargetRoleText = $"<size={fontSize}>{TargetRoleText}</size>\r\n";
                        string TargetPlayerName = GetDisplayRealName(seer, target, isMeeting);
                        string TargetDeathReason = GetDeathReasonText(seer, target);
                        string TargetMark = GetTargetMark(seer, target, isMeeting);

                        //全てのテキストを合成します。
                        string TargetName = $"{TargetRoleText}{TargetPlayerName}{TargetDeathReason}{TargetMark}";

                        //適用
                        target.RpcSetNamePrivate(TargetName, true, seer, force: NoCache);

                        Logger.Info("NotifyRoles-Loop2-" + target.GetNameWithRole() + ":END", "NotifyRoles");
                    }
                }
                Logger.Info("NotifyRoles-Loop1-" + seer.GetNameWithRole() + ":END", "NotifyRoles");
            }
        }
        public static void CustomSyncAllSettings()
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                pc.CustomSyncSettings();
            }
        }
        public static void AfterMeetingTasks()
        {
            BountyHunter.AfterMeetingTasks();
            SerialKiller.AfterMeetingTasks();
        }

        public static void ChangeInt(ref int ChangeTo, int input, int max)
        {
            var tmp = ChangeTo * 10;
            tmp += input;
            ChangeTo = Math.Clamp(tmp, 0, max);
        }
        public static void CountAliveImpostors()
        {
            int AliveImpostorCount = 0;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                CustomRoles pc_role = pc.GetCustomRole();
                if (pc_role.IsImpostor() && !Main.PlayerStates[pc.PlayerId].IsDead) AliveImpostorCount++;
            }
            if (Main.AliveImpostorCount == AliveImpostorCount) return;
            TownOfHost.Logger.Info("生存しているインポスター:" + AliveImpostorCount + "人", "CountAliveImpostors");
            Main.AliveImpostorCount = AliveImpostorCount;
            if (Options.EnableLastImpostor.GetBool() && AliveImpostorCount == 1)
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.IsLastImpostor() && pc.Is(CustomRoles.Impostor))
                    {
                        pc.RpcSetCustomRole(CustomRoles.LastImpostor);
                        break;
                    }
                }
                NotifyRoles();
                CustomSyncAllSettings();
            }
        }
        public static bool IsLastImpostor(byte playerId)
        { //キルクールを変更するインポスター役職は省く
            var role = Main.PlayerStates[playerId].MainRole;
            return role.IsImpostor() &&
                !Main.PlayerStates[playerId].IsDead &&
                Options.CurrentGameMode != CustomGameMode.HideAndSeek &&
                Options.EnableLastImpostor.GetBool() &&
                role is not CustomRoles.Vampire or CustomRoles.BountyHunter or CustomRoles.SerialKiller &&
                Main.AliveImpostorCount == 1;
        }
        public static string GetVoteName(byte num)
        {
            string name = "invalid";
            var player = GetPlayerById(num);
            if (num < 15 && player != null) name = player?.GetNameWithRole();
            if (num == 253) name = "Skip";
            if (num == 254) name = "None";
            if (num == 255) name = "Dead";
            return name;
        }
        public static string PadRightV2(this object text, int num)
        {
            int bc = 0;
            var t = text.ToString();
            foreach (char c in t) bc += Encoding.GetEncoding("UTF-8").GetByteCount(c.ToString()) == 1 ? 1 : 2;
            return t?.PadRight(Mathf.Max(num - (bc - t.Length), 0));
        }
        public static void DumpLog()
        {
            string t = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
            string filename = $"{System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}/TownOfHost-v{Main.PluginVersion}-{t}.log";
            FileInfo file = new(@$"{System.Environment.CurrentDirectory}/BepInEx/LogOutput.log");
            file.CopyTo(@filename);
            System.Diagnostics.Process.Start(@$"{System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}");
            if (PlayerControl.LocalPlayer != null)
                HudManager.Instance?.Chat?.AddChat(PlayerControl.LocalPlayer, "デスクトップにログを保存しました。バグ報告チケットを作成してこのファイルを添付してください。");
        }
        public static (int, int) GetDousedPlayerCount(byte playerId)
        {
            int doused = 0, all = 0; //学校で習った書き方
                                     //多分この方がMain.isDousedでforeachするより他のアーソニストの分ループ数少なくて済む
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null ||
                    pc.Data.IsDead ||
                    pc.Data.Disconnected ||
                    pc.PlayerId == playerId
                ) continue; //塗れない人は除外 (死んでたり切断済みだったり あとアーソニスト自身も)

                all++;
                if (Main.isDoused.TryGetValue((playerId, pc.PlayerId), out var isDoused) && isDoused)
                    //塗れている場合
                    doused++;
            }

            return (doused, all);
        }
        public static string SummaryTexts(byte id, bool disableColor = true)
        {
            var RolePos = TranslationController.Instance.currentLanguage.languageID == SupportedLangs.English ? 47 : 37;
            string summary = $"{ColorString(Main.PlayerColors[id], Main.AllPlayerNames[id])}<pos=22%> {GetProgressText(id)}</pos><pos=29%> {GetVitalText(id)}</pos><pos={RolePos}%> {GetDisplayRoleName(id)}{GetSubRolesText(id)}</pos>";
            return disableColor ? summary.RemoveHtmlTags() : summary;
        }
        public static string RemoveHtmlTags(this string str) => Regex.Replace(str, "<[^>]*?>", "");
        public static bool CanMafiaKill()
        {
            if (Main.PlayerStates == null) return false;
            //マフィアを除いた生きているインポスターの人数  Number of Living Impostors excluding mafia
            int LivingImpostorsNum = 0;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.IsAlive() && !pc.Is(CustomRoles.Mafia) && pc.Is(RoleType.Impostor, false))
                    LivingImpostorsNum++;
            }

            return LivingImpostorsNum <= 0;
        }
        ///<summary>
        ///seerから見たtargetの役職表示　誤認させる場合はここで書き換え
        ///</summary>
        public static string GetTargetRoleText(PlayerControl seer, PlayerControl target, bool isMeeting)
        {
            string RoleText = seer.KnowTargetRole(target) ? GetDisplayRoleName(target.PlayerId) : "";
            switch (seer.GetCustomRole())
            {
                case CustomRoles.EvilTracker:
                    if (isMeeting && EvilTracker.IsTrackTarget(seer, target))
                        RoleText = EvilTracker.GetArrowAndLastRoom(seer, target);
                    break;
                case CustomRoles.Insider:
                    if (Insider.KnowOtherRole(seer, target))
                        RoleText = Insider.GetRoleText(target);
                    break;
            }
            return RoleText;
        }
        ///<summary>
        ///seerから見たtargetのタスク表示　誤認させる場合はここで書き換え
        ///</summary>
        public static string GetTargetTaskText(PlayerControl seer, PlayerControl target, bool isMeeting)
        {
            string TaskText = seer.KnowTargetRole(target) ? GetProgressText(target) : "";
            switch (seer.GetCustomRole())
            {
                case CustomRoles.Insider:
                    if (Insider.KnowOtherRole(seer, target))
                        TaskText = Insider.GetTaskText(target);
                    break;
            }
            if (TaskText != "") TaskText = " " + TaskText;
            return TaskText;
        }
        public static string GetDisplayRealName(PlayerControl seer, PlayerControl target, bool isMeeting)
        {
            string Name = target.GetRealName(isMeeting);
            //イントロに変更
            if (!seer.IsModClient() && seer == target && !isMeeting && MeetingStates.FirstMeeting && Options.ChangeNameToRoleInfo.GetBool())
                Name = target.GetRoleInfo();
            //役職ごとに書き換え
            switch (target.GetCustomRole())
            {
                case CustomRoles.Arsonist:
                    if (seer == target && target.IsDouseDone())
                        Name = ColorString(GetRoleColor(CustomRoles.Arsonist), GetString("EnterVentToWin"));
                    break;
            }
            //名前に色付け
            if (seer.KnowTargetRoleColor(target, isMeeting))
                Name = ColorString(target.GetRoleColor(), Name);
            //通信障害でのカムフラージュ
            if (IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool() && !isMeeting)
                Name = $"<size=0%>{Name}</size>";
            //NameColorManager準拠の処理
            var ncd = NameColorManager.Instance.GetData(seer.PlayerId, target.PlayerId);
            if (ncd.color != null) Name = ncd.OpenTag + Name + ncd.CloseTag;
            return Name;
        }
        public static string GetTargetMark(PlayerControl seer, PlayerControl target, bool isMeeting)
        {
            string Mark = "";
            foreach (var subRole in target.GetCustomSubRoles())
            {
                switch (subRole)
                {
                    case CustomRoles.Lovers:
                        //ハートマークを付ける(相手に) || 霊界からラバーズ視認
                        if (seer == target || seer.Is(CustomRoles.Lovers) || (seer.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool()))
                            Mark += ColorString(GetRoleColor(CustomRoles.Lovers), "♡");
                        break;
                }
            }
            switch (target.GetCustomRole())
            {
                case CustomRoles.MadSnitch:
                    if (target.KnowSpecificImpostor(seer, true) && Options.MadSnitchCanAlsoBeExposedToImpostor.GetBool())
                        Mark += ColorString(Palette.ImpostorRed, "★");
                    break;
                case CustomRoles.Snitch:
                    //タスク完了直前のSnitchにマークを表示
                    if (seer.KnowSnitch(target))
                        Mark += ColorString(GetRoleColor(CustomRoles.Snitch), "★");
                    break;
                case CustomRoles.ToughGuy:
                    Mark += ToughGuy.GetMark(seer, target);
                    break;
            }

            switch (seer.GetCustomRole())
            {
                case CustomRoles.EvilTracker:
                    Mark += EvilTracker.GetTargetMark(seer, target);
                    break;
                case CustomRoles.Insider:
                    Mark += Insider.GetOtherImpostorMarks(seer, target);
                    break;
                case CustomRoles.Puppeteer:
                    Mark += GetPuppeteerMark(seer, target);
                    break;
                case CustomRoles.Vampire:
                    Mark += GetVampireMark(seer, target);
                    break;
                case CustomRoles.Warlock:
                    Mark += GetWarlockMark(seer, target);
                    break;
                case CustomRoles.Arsonist:
                    if (seer.IsDousedPlayer(target))
                        Mark += ColorString(GetRoleColor(CustomRoles.Arsonist), "▲");
                    else if (!isMeeting && Main.ArsonistTimer.TryGetValue(seer.PlayerId, out var value) && value.Item1 == target)
                        Mark += ColorString(GetRoleColor(CustomRoles.Arsonist), "△");
                    break;
                case CustomRoles.Executioner:
                    Mark += Executioner.TargetMark(seer, target);
                    break;
            }

            if (CustomRoles.Snitch.IsEnable() && !isMeeting) //人外からスニッチへの矢印とマーク
            {
                if (seer.AmOwner)
                {
                    var found = false;
                    var update = false;
                    var arrows = "";
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    { //全員分ループ
                        if (pc.Data.IsDead || pc.Data.Disconnected || !target.KnowSnitch(pc)) continue; //(スニッチ以外 || 死者 || 切断者)に用はない
                        found = true;
                        //矢印表示しないならこれ以上は不要
                        if (!Options.SnitchEnableTargetArrow.GetBool()) break;
                        update = FixedUpdatePatch.CheckArrowUpdate(target, pc, update, false);
                        arrows += Main.targetArrows[(target.PlayerId, pc.PlayerId)];
                    }
                    if (found && target.AmOwner)
                        Mark += ColorString(GetRoleColor(CustomRoles.Snitch), "★" + arrows); //Snitch警告を表示
                    if (AmongUsClient.Instance.AmHost && seer.PlayerId != target.PlayerId && update)
                    {
                        //更新があったら非Modに通知
                        NotifyRoles(SpecifySeer: target);
                    }
                }
                else if (seer == target)
                {
                    var arrows = "";
                    bool found = false;
                    foreach (var arrow in Main.targetArrows)
                    {
                        var pc = GetPlayerById(arrow.Key.Item2);
                        if (arrow.Key.Item1 == seer.PlayerId && pc != null && pc.IsAlive() && seer.KnowSnitch(pc))
                        {
                            found = true;
                            //自分用の矢印で対象が死んでない時
                            arrows += arrow.Value;
                        }
                    }
                    if (found)
                        Mark += ColorString(GetRoleColor(CustomRoles.Snitch), "★" + arrows);
                }
            }

            if (Main.SpelledPlayer.ContainsKey(target.PlayerId) && isMeeting)
                Mark += ColorString(Palette.ImpostorRed, "†");

            if (Sniper.IsEnable())
                Mark += Sniper.GetShotNotify(seer.PlayerId);

            return Mark;
        }
        public static string GetDeathReasonText(PlayerControl seer, PlayerControl target)
            => seer.KnowDeathReason(target) ? ColorString(GetRoleColor(CustomRoles.Scientist), $"({GetVitalText(target.PlayerId)})") : "";
        public static string GetPuppeteerMark(PlayerControl seer, PlayerControl target)
            => (GameStates.IsInTask && Main.PuppeteerList.TryGetValue(target.PlayerId, out var puppeteerId) && puppeteerId == seer.PlayerId) ? ColorString(Palette.ImpostorRed, "◆") : "";
        public static void FlashColor(Color color, float duration = 1f)
        {
            var hud = DestroyableSingleton<HudManager>.Instance;
            if (hud.FullScreen == null) return;
            var obj = hud.transform.FindChild("FlashColor_FullScreen")?.gameObject;
            if (obj == null)
            {
                obj = GameObject.Instantiate(hud.FullScreen.gameObject, hud.transform);
                obj.name = "FlashColor_FullScreen";
            }
            hud.StartCoroutine(Effects.Lerp(duration, new Action<float>((t) =>
            {
                obj.SetActive(t != 1f);
                obj.GetComponent<SpriteRenderer>().color = new(color.r, color.g, color.b, Mathf.Clamp01((-2f * Mathf.Abs(t - 0.5f) + 1) * color.a)); //アルファ値を0→目標→0に変化させる
            })));
        }
        public static string GetVampireMark(PlayerControl seer, PlayerControl target)
        {
            string TargetMark = "";
            if (GameStates.IsInTask && Main.BitPlayers.TryGetValue(target.PlayerId, out var vampire) && vampire.Item1 == seer.PlayerId)
                TargetMark += ColorString(Palette.ImpostorRed, "×");
            return TargetMark;
        }
        public static string GetWarlockMark(PlayerControl seer, PlayerControl target)
        {
            string TargetMark = "";
            if (Main.CursedPlayers.TryGetValue(seer.PlayerId, out var cursedPlayer) && cursedPlayer == target)
                TargetMark += ColorString(Palette.ImpostorRed, "＊");
            return TargetMark;
        }

        public static Sprite LoadSprite(string path, float pixelsPerUnit = 1f)
        {
            Sprite sprite = null;
            try
            {
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
                var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                using MemoryStream ms = new();
                stream.CopyTo(ms);
                ImageConversion.LoadImage(texture, ms.ToArray());
                sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), new(0.5f, 0.5f), pixelsPerUnit);
            }
            catch
            {
                Logger.Error($"\"{path}\"の読み込みに失敗しました。", "LoadImage");
            }
            return sprite;
        }
        public static string ColorString(Color32 color, string str) => $"<color=#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}>{str}</color>";
        /// <summary>
        /// Darkness:１の比率で黒色と元の色を混ぜる。マイナスだと白色と混ぜる。
        /// </summary>
        public static Color ShadeColor(this Color color, float Darkness = 0)
        {
            bool IsDarker = Darkness >= 0; //黒と混ぜる
            if (!IsDarker) Darkness = -Darkness;
            float Weight = IsDarker ? 0 : Darkness; //黒/白の比率
            float R = (color.r + Weight) / (Darkness + 1);
            float G = (color.g + Weight) / (Darkness + 1);
            float B = (color.b + Weight) / (Darkness + 1);
            return new Color(R, G, B, color.a);
        }

        /// <summary>
        /// 乱数の簡易的なヒストグラムを取得する関数
        /// <params name="nums">生成した乱数を格納したint配列</params>
        /// <params name="scale">ヒストグラムの倍率 大量の乱数を扱う場合、この値を下げることをお勧めします。</params>
        /// </summary>
        public static string WriteRandomHistgram(int[] nums, float scale = 1.0f)
        {
            int[] countData = new int[nums.Max() + 1];
            foreach (var num in nums)
            {
                if (0 <= num) countData[num]++;
            }
            StringBuilder sb = new();
            for (int i = 0; i < countData.Length; i++)
            {
                // 倍率適用
                countData[i] = (int)(countData[i] * scale);

                // 行タイトル
                sb.AppendFormat("{0:D2}", i).Append(" : ");

                // ヒストグラム部分
                for (int j = 0; j < countData[i]; j++)
                    sb.Append('|');

                // 改行
                sb.Append('\n');
            }

            // その他の情報
            sb.Append("最大数 - 最小数: ").Append(countData.Max() - countData.Min());

            return sb.ToString();
        }
        public static string GetRoomName(this SystemTypes roomId) => DestroyableSingleton<TranslationController>.Instance.GetString(roomId);
    }
}