using System.Collections.Generic;
using System.Linq;
using Hazel;
using static TownOfHost.Options;

namespace TownOfHost
{
    public static class EagleEye
    {
        private static readonly int Id = 3500;
        private static List<byte> playerIdList = new();
        private static OptionItem NumAvailableVote;
        private static Dictionary<byte, List<byte>> TargetList = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.EagleEye);
            NumAvailableVote = IntegerOptionItem.Create(Id + 10, "EagleEyeNumAvailableVote", new(1, 5, 1), 1, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.EagleEye]);
        }
        public static void Init()
        {
            playerIdList = new();
            TargetList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            TargetList.Add(playerId, new List<byte>());
        }
        public static bool IsEnable => playerIdList.Count > 0;

        // 値取得の関数
        public static bool KnowTargetRole(PlayerControl seer, PlayerControl target)
            => playerIdList.Contains(seer.PlayerId)
            && TargetList.TryGetValue(seer.PlayerId, out var value)
            && value.Contains(target.PlayerId);

        // 各所で呼ばれる処理
        public static void OnCheckForEndVoting(MeetingHud __instance)
        {
            if (!IsEnable) return;
            foreach (var playerId in playerIdList)
            {
                var pc = Utils.GetPlayerById(playerId);
                if (!pc.IsAlive()) continue;
                var pva = __instance.playerStates.Where(x => x.TargetPlayerId == playerId).FirstOrDefault();
                if (pva == null) continue;

                var targetId = pva.VotedFor;
                if (targetId >= 253 || Utils.GetPlayerById(targetId) == null) continue;
                if (!__instance.CustomCalculateVotes().TryGetValue(targetId, out var numTargetVote)) continue;
                if (numTargetVote <= NumAvailableVote.GetInt())
                    AddTarget(playerId, targetId);
            }
        }
        private static void AddTarget(byte seerId, byte targetId)
        {
            TargetList[seerId].Add(targetId); // ターゲット設定

            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.AddEagleEyeTarget, SendOption.Reliable, -1);
            writer.Write(seerId);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte seerId = reader.ReadByte();
            byte targetId = reader.ReadByte();
            AddTarget(seerId, targetId);
        }
    }
}