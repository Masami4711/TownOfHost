using System.Collections.Generic;
using System.Text;
using Hazel;
using UnityEngine;
using AmongUs.GameOptions;
using TownOfHost.Roles.Core;

using static TownOfHost.Modules.MeetingVoteManager;

namespace TownOfHost.Roles.Impostor
{
    public sealed class EagleEye : RoleBase
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(EagleEye),
                player => new EagleEye(player),
                CustomRoles.EagleEye,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                3500,
                null,
                "ee"
            );
        public EagleEye(PlayerControl player)
            : base(RoleInfo, player)
        { }
        private HashSet<byte> targetIdList;

        public override void Add()
        {
            targetIdList = new(15);
        }
        private void AddTargetId(byte targetId)
        {
            if (targetIdList.Add(targetId) && AmongUsClient.Instance.AmHost)
            {
                SendRPC(targetId);
            }
        }
        private void SendRPC(byte targetId)
        {
            using var sender = CreateSender(CustomRPC.EagleEyeAddTarget);
            sender.Writer.Write(targetId);
        }

        public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
        {
            if (rpcType != CustomRPC.EagleEyeAddTarget) return;

            byte targetId = reader.ReadByte();
            AddTargetId(targetId);
        }
        public override void OnVotingComplete(VoteData voteData, VoteResult voteResult)
        {
            byte voteTargetId = voteData.VotedFor;
            if (voteTargetId is Skip or NoVote)
            {
                return;
            }
            int targetVotedCount = voteResult.VotedCounts.TryGetValue(voteTargetId, out int count) ? count : 0;

            if (targetVotedCount == voteData.NumVotes)
            {
                Logger.Info($"{Player.GetNameWithRole()} => {Utils.GetPlayerById(voteTargetId).GetNameWithRole()} : {targetVotedCount} Votes", nameof(OnVotingComplete));
                AddTargetId(voteTargetId);
            }
        }
        public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText)
        {
            enabled |= targetIdList.Contains(seen.PlayerId);
        }
        public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
        {
            seen ??= seer;
            var mark = new StringBuilder(20);

            // 死亡したLoversのマーク追加
            if (seen.Is(CustomRoles.Lovers) && !seer.Is(CustomRoles.Lovers) && targetIdList.Contains(seen.PlayerId))
                mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♡"));

            return mark.ToString();
        }
    }
}