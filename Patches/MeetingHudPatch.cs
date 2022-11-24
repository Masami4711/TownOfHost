using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    class CheckForEndVotingPatch
    {
        public static bool Prefix(MeetingHud __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            try
            {
                foreach (var pva in __instance.playerStates)
                {
                    if (pva == null) continue;
                    PlayerControl pc = Utils.GetPlayerById(pva.TargetPlayerId);
                    if (pc == null) continue;
                    //死んでいないディクテーターが投票済み
                    if (pc.Is(CustomRoles.Dictator) && pva.DidVote && pc.PlayerId != pva.VotedFor && pva.VotedFor < 253 && !pc.Data.IsDead)
                    {
                        var voteTarget = Utils.GetPlayerById(pva.VotedFor);
                        TryAddAfterMeetingDeathPlayers(pc.PlayerId, PlayerState.DeathReason.Suicide);
                        __instance.RpcVotingComplete(new MeetingHud.VoterState[]{ new ()
                        {
                            VoterId = pva.TargetPlayerId,
                            VotedForId = pva.VotedFor
                        }}, voteTarget.Data, false); //RPC
                        Logger.Info($"{voteTarget.GetNameWithRole()}を追放", "Dictator");
                        FollowingSuicideOnExile(pva.VotedFor);
                        RevengeOnExile(pva.VotedFor);
                        Logger.Info("ディクテーターによる強制会議終了", "Special Phase");
                        voteTarget.SetRealKiller(pc);
                        return true;
                    }
                }
                foreach (var ps in __instance.playerStates)
                {
                    //死んでいないプレイヤーが投票していない
                    if (!(ps.AmDead || ps.DidVote)) return false;
                }

                MeetingHud.VoterState[] states;
                GameData.PlayerInfo exiledPlayer = PlayerControl.LocalPlayer.Data;
                bool tie = false;

                List<MeetingHud.VoterState> statesList = new();
                for (var i = 0; i < __instance.playerStates.Length; i++)
                {
                    PlayerVoteArea ps = __instance.playerStates[i];
                    if (ps == null) continue;
                    Logger.Info(string.Format("{0,-2}{1}:{2,-3}{3}", ps.TargetPlayerId, Utils.PadRightV2($"({Utils.GetVoteName(ps.TargetPlayerId)})", 40), ps.VotedFor, $"({Utils.GetVoteName(ps.VotedFor)})"), "Vote");
                    var voter = Utils.GetPlayerById(ps.TargetPlayerId);
                    if (voter == null || voter.Data == null || voter.Data.Disconnected) continue;
                    if (Options.VoteMode.GetBool())
                    {
                        if (ps.VotedFor == 253 && !voter.Data.IsDead && //スキップ
                            !(Options.WhenSkipVoteIgnoreFirstMeeting.GetBool() && MeetingStates.FirstMeeting) && //初手会議を除く
                            !(Options.WhenSkipVoteIgnoreNoDeadBody.GetBool() && !MeetingStates.IsExistDeadBody) && //死体がない時を除く
                            !(Options.WhenSkipVoteIgnoreEmergency.GetBool() && MeetingStates.IsEmergencyMeeting) //緊急ボタンを除く
                            )
                        {
                            switch (Options.GetWhenSkipVote())
                            {
                                case VoteMode.Suicide:
                                    TryAddAfterMeetingDeathPlayers(ps.TargetPlayerId, PlayerState.DeathReason.Suicide);
                                    Logger.Info($"スキップしたため{voter.GetNameWithRole()}を自殺させました", "Vote");
                                    break;
                                case VoteMode.SelfVote:
                                    ps.VotedFor = ps.TargetPlayerId;
                                    Logger.Info($"スキップしたため{voter.GetNameWithRole()}に自投票させました", "Vote");
                                    break;
                                default:
                                    break;
                            }
                        }
                        if (ps.VotedFor == 254 && !voter.Data.IsDead)//無投票
                        {
                            switch (Options.GetWhenNonVote())
                            {
                                case VoteMode.Suicide:
                                    TryAddAfterMeetingDeathPlayers(ps.TargetPlayerId, PlayerState.DeathReason.Suicide);
                                    Logger.Info($"無投票のため{voter.GetNameWithRole()}を自殺させました", "Vote");
                                    break;
                                case VoteMode.SelfVote:
                                    ps.VotedFor = ps.TargetPlayerId;
                                    Logger.Info($"無投票のため{voter.GetNameWithRole()}に自投票させました", "Vote");
                                    break;
                                case VoteMode.Skip:
                                    ps.VotedFor = 253;
                                    Logger.Info($"無投票のため{voter.GetNameWithRole()}にスキップさせました", "Vote");
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    statesList.Add(new MeetingHud.VoterState()
                    {
                        VoterId = ps.TargetPlayerId,
                        VotedForId = ps.VotedFor
                    });
                    if (IsMayor(ps.TargetPlayerId))//Mayorの投票数
                    {
                        for (var i2 = 0; i2 < Options.MayorAdditionalVote.GetFloat(); i2++)
                        {
                            statesList.Add(new MeetingHud.VoterState()
                            {
                                VoterId = ps.TargetPlayerId,
                                VotedForId = ps.VotedFor
                            });
                        }
                    }
                }
                states = statesList.ToArray();

                var VotingData = __instance.CustomCalculateVotes();
                byte exileId = byte.MaxValue;
                int max = 0;
                Logger.Info("===追放者確認処理開始===", "Vote");
                foreach (var data in VotingData)
                {
                    Logger.Info($"{data.Key}({Utils.GetVoteName(data.Key)}):{data.Value}票", "Vote");
                    if (data.Value > max)
                    {
                        Logger.Info(data.Key + "番が最高値を更新(" + data.Value + ")", "Vote");
                        exileId = data.Key;
                        max = data.Value;
                        tie = false;
                    }
                    else if (data.Value == max)
                    {
                        Logger.Info(data.Key + "番が" + exileId + "番と同数(" + data.Value + ")", "Vote");
                        exileId = byte.MaxValue;
                        tie = true;
                    }
                    Logger.Info($"exileId: {exileId}, max: {max}票", "Vote");
                }

                Logger.Info($"追放者決定: {exileId}({Utils.GetVoteName(exileId)})", "Vote");

                if (Options.VoteMode.GetBool() && Options.WhenTie.GetBool() && tie)
                {
                    switch ((TieMode)Options.WhenTie.GetSelection())
                    {
                        case TieMode.Default:
                            exiledPlayer = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => info.PlayerId == exileId);
                            break;
                        case TieMode.All:
                            VotingData.DoIf(x => x.Key < 15 && x.Value == max, x =>
                            {
                                Main.AfterMeetingDeathPlayers.Add(x.Key, PlayerState.DeathReason.Vote);
                                Utils.GetPlayerById(x.Key).SetRealKiller(null);
                            });
                            exiledPlayer = null;
                            break;
                        case TieMode.Random:
                            exiledPlayer = GameData.Instance.AllPlayers.ToArray().OrderBy(_ => Guid.NewGuid()).FirstOrDefault(x => VotingData.TryGetValue(x.PlayerId, out int vote) && vote == max);
                            tie = false;
                            break;
                    }
                }
                else
                    exiledPlayer = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => !tie && info.PlayerId == exileId);
                if (exiledPlayer != null)
                    exiledPlayer.Object.SetRealKiller(null);

                //RPC
                if (AntiBlackout.OverrideExiledPlayer)
                {
                    __instance.RpcVotingComplete(states, null, true);
                    ExileControllerWrapUpPatch.AntiBlackout_LastExiled = exiledPlayer;
                }
                else __instance.RpcVotingComplete(states, exiledPlayer, tie); //通常処理
                if (Utils.GetPlayerById(exileId).Is(CustomRoles.Witch))
                {
                    var which = Utils.GetPlayerById(exileId);
                    foreach (var pc in PlayerControl.AllPlayerControls)
                        if (Main.SpelledPlayer.TryGetValue(pc.PlayerId, out var killer) && killer == which)
                            Main.SpelledPlayer.Remove(pc.PlayerId);
                }
                foreach (var kvp in Main.SpelledPlayer)
                {
                    if (Utils.GetPlayerById(kvp.Key) == null) continue;
                    TryAddAfterMeetingDeathPlayers(kvp.Key, PlayerState.DeathReason.Spell);
                    Utils.GetPlayerById(kvp.Key).SetRealKiller(kvp.Value);
                }
                Main.SpelledPlayer.Clear();
                ToughGuy.AfterMeetingDeath();


                FollowingSuicideOnExile(exileId);
                RevengeOnExile(exileId);

                //霊界用暗転バグ対処
                if (!AntiBlackout.OverrideExiledPlayer && exiledPlayer != null && Main.ResetCamPlayerList.Contains(exiledPlayer.PlayerId))
                    exiledPlayer.Object?.ResetPlayerCam(19f);

                return false;
            }
            catch (Exception ex)
            {
                Logger.SendInGame(string.Format(GetString("Error.MeetingException"), ex.Message), true);
                throw;
            }
        }
        public static bool IsMayor(byte id)
        {
            var player = PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == id).FirstOrDefault();
            return player != null && player.Is(CustomRoles.Mayor);
        }
        public static void TryAddAfterMeetingDeathPlayers(byte playerId, PlayerState.DeathReason deathReason)
        {
            if (Main.AfterMeetingDeathPlayers.TryAdd(playerId, deathReason))
            {
                FollowingSuicideOnExile(playerId);
                RevengeOnExile(playerId, deathReason);
            }
        }
        public static void FollowingSuicideOnExile(byte playerId)
        {
            var player = Utils.GetPlayerById(playerId);
            if (player == null) return;

            //Loversの後追い
            if (CustomRoles.Lovers.IsEnable() && !Main.isLoversDead && Main.LoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                FixedUpdatePatch.LoversSuicide(playerId, true);
        }
        public static void RevengeOnExile(byte playerId, PlayerState.DeathReason deathReason = PlayerState.DeathReason.Vote)
        {
            if (deathReason == PlayerState.DeathReason.Suicide) return;
            var player = Utils.GetPlayerById(playerId);
            var target = PickRevengeTarget(player);
            if (target == null) return;
            TryAddAfterMeetingDeathPlayers(target.PlayerId, PlayerState.DeathReason.Revenge);
            target.SetRealKiller(player);
            Logger.Info($"{player.GetNameWithRole()}の道連れ先:{target.GetNameWithRole()}", "MadmatesRevengeOnExile");
        }
        public static PlayerControl PickRevengeTarget(PlayerControl exiledplayer)//道連れ先選定
        {
            List<PlayerControl> TargetList = new();
            foreach (var candidate in PlayerControl.AllPlayerControls)
            {
                if (candidate == exiledplayer || candidate.Data.IsDead || Main.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId)) continue;
                switch (exiledplayer.GetCustomRole())
                {
                    //ここに道連れ役職を追加
                    default:
                        if (exiledplayer.Is(RoleType.Madmate) && Options.MadmateExileCrewmate.GetBool() //黒猫オプション
                        && !candidate.Is(RoleType.Impostor))
                            TargetList.Add(candidate);
                        break;
                }
            }
            if (TargetList == null || TargetList.Count == 0) return null;
            var rand = new System.Random();
            var target = TargetList[rand.Next(TargetList.Count)];
            Logger.Info($"{exiledplayer.GetNameWithRole()}の道連れ先:{target.GetNameWithRole()}", "PickRevengeTarget");
            return target;
        }
    }

    static class ExtendedMeetingHud
    {
        public static Dictionary<byte, int> CustomCalculateVotes(this MeetingHud __instance)
        {
            Logger.Info("CustomCalculateVotes開始", "Vote");
            Dictionary<byte, int> dic = new();
            //| 投票された人 | 投票された回数 |
            for (int i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea ps = __instance.playerStates[i];
                if (ps == null) continue;
                if (ps.VotedFor is not ((byte)252) and not byte.MaxValue and not ((byte)254))
                {
                    int VoteNum = 1;
                    if (CheckForEndVotingPatch.IsMayor(ps.TargetPlayerId)) VoteNum += Options.MayorAdditionalVote.GetInt();
                    //投票を1追加 キーが定義されていない場合は1で上書きして定義
                    dic[ps.VotedFor] = !dic.TryGetValue(ps.VotedFor, out int num) ? VoteNum : num + VoteNum;
                }
            }
            return dic;
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class MeetingHudStartPatch
    {
        public static void Prefix(MeetingHud __instance)
        {
            Logger.Info("------------会議開始------------", "Phase");
            ChatUpdatePatch.DoBlockChat = true;
            GameStates.AlreadyDied |= GameData.Instance.AllPlayers.ToArray().Any(x => x.IsDead);
            PlayerControl.AllPlayerControls.ToArray().Do(x => ReportDeadBodyPatch.WaitReport[x.PlayerId].Clear());
            Utils.NotifyRoles(isMeeting: true, NoCache: true);
            MeetingStates.MeetingCalled = true;
        }
        public static void Postfix(MeetingHud __instance)
        {
            SoundManager.Instance.ChangeMusicVolume(0f);
            if (Options.SyncButtonMode.GetBool())
            {
                Utils.SendMessage(string.Format(GetString("Message.SyncButtonLeft"), Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount));
                Logger.Info("緊急会議ボタンはあと" + (Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + "回使用可能です。", "SyncButtonMode");
            }
            if (AntiBlackout.OverrideExiledPlayer)
            {
                Utils.SendMessage(GetString("Warning.OverrideExiledPlayer"));
            }

            if (AmongUsClient.Instance.AmHost)
            {
                _ = new LateTask(() =>
                {
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        pc.RpcSetNameEx(pc.GetRealName(isMeeting: true));
                    }
                    ChatUpdatePatch.DoBlockChat = false;
                }, 3f, "SetName To Chat");
            }

            foreach (var pva in __instance.playerStates)
            {
                if (pva == null) continue;
                var seer = PlayerControl.LocalPlayer;
                var target = Utils.GetPlayerById(pva.TargetPlayerId);
                if (target == null) continue;
                var roleTextMeeting = UnityEngine.Object.Instantiate(pva.NameText);
                roleTextMeeting.transform.SetParent(pva.NameText.transform);
                roleTextMeeting.transform.localPosition = new Vector3(0f, -0.18f, 0f);
                roleTextMeeting.fontSize = 1.5f;
                roleTextMeeting.text = Utils.GetDisplayRoleText(seer, target);
                roleTextMeeting.gameObject.name = "RoleTextMeeting";
                roleTextMeeting.enableWordWrapping = false;
                roleTextMeeting.enabled = seer.KnowTargetRole(target);

                //会議画面での名前変更
                if (seer.KnowTargetRoleColor(target, true) && AmongUsClient.Instance.IsGameStarted)
                    pva.NameText.color = target.GetRoleColor();//名前の色を変更

                pva.NameText.text += Utils.GetDeathReasonText(seer, target) + Utils.GetTargetMark(seer, target, true);
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    class MeetingHudUpdatePatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.LeftControl))
            {
                __instance.playerStates.DoIf(x => x.HighlightedFX.enabled, x =>
                {
                    var player = Utils.GetPlayerById(x.TargetPlayerId);
                    player.RpcExileV2();
                    Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Execution;
                    Main.PlayerStates[player.PlayerId].SetDead();
                    Utils.SendMessage(string.Format(GetString("Message.Executed"), player.Data.PlayerName));
                    Logger.Info($"{player.GetNameWithRole()}を処刑しました", "Execution");
                });
            }
        }
    }
    [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetHighlighted))]
    class SetHighlightedPatch
    {
        public static bool Prefix(PlayerVoteArea __instance, bool value)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            if (!__instance.HighlightedFX) return false;
            __instance.HighlightedFX.enabled = value;
            return false;
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
    class MeetingHudOnDestroyPatch
    {
        public static void Postfix()
        {
            MeetingStates.FirstMeeting = false;
            Logger.Info("------------会議終了------------", "Phase");
            if (AmongUsClient.Instance.AmHost)
            {
                AntiBlackout.SetIsDead();
                PlayerControl.AllPlayerControls.ToArray().Do(pc => RandomSpawn.CustomNetworkTransformPatch.NumOfTP[pc.PlayerId] = 0);
            }
        }
    }
}