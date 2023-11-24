using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

using static TownOfHost.Modules.MeetingVoteManager;

namespace TownOfHost.Roles.Neutral;
public sealed class Duelist : RoleBase, IKiller, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Duelist),
            player => new Duelist(player),
            CustomRoles.Duelist,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            51800,
            SetupOptionItem,
            "du",
            "#efbb37",
            isDesyncImpostor: true,
            assignInfo: new(CustomRoles.Duelist, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(2, 14, 2),
                AssignUnitRoles = new CustomRoles[2] { CustomRoles.Duelist, CustomRoles.Duelist }
            }
        );
    public Duelist(PlayerControl player)
        : base(RoleInfo, player)
    {
        killCooldown = optionKillCooldown.GetFloat();
        canEnterVent = optionCanEnterVent.GetBool();
        MakeDuelistMatchUp();
        checkedPlayerIds = new(14);
        Player.AddDoubleTrigger();
    }

    private static OptionItem optionKillCooldown;
    private static OptionItem optionCanEnterVent;
    private static float killCooldown;
    private static bool canEnterVent;
    private static HashSet<(byte, byte)> duelistIdPairs = new(7);
    private HashSet<byte> checkedPlayerIds;
    private static void SetupOptionItem()
    {
        optionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        optionCanEnterVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, false, false);
    }

    float IKiller.CalculateKillCooldown() => killCooldown;
    bool IKiller.CanUseSabotageButton() => false;
    bool IKiller.CanUseImpostorVentButton() => canEnterVent;
    void IKiller.OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!Is(info.AttemptKiller) || info.IsSuicide) return;

        var (killer, target) = info.AttemptTuple;

        if (!checkedPlayerIds.Contains(target.PlayerId) &&
            !killer.CheckDoubleTrigger(target, () => { AddCheckedPlayerId(target.PlayerId); }))
        {
            info.DoKill = false;
            return;
        }

        if (target.PlayerId == GetEnemyId())
        {
            killer.ResetKillCooldown();
        }
        else
        {
            PlayerState.GetByPlayerId(killer.PlayerId).DeathReason = CustomDeathReason.Misfire;
            killer.RpcMurderPlayer(killer);
            info.DoKill = false;
        }
    }
    bool IAdditionalWinner.CheckWin(ref CustomRoles winnerRole)
    {
        if (!Player.IsAlive())
        {
            return false;
        }

        var enemy = Utils.GetPlayerById(GetEnemyId());
        return !enemy.IsAlive();
    }

    private void MakeDuelistMatchUp()
    {
        var duelistIds = CustomRoleManager.AllActiveRoles
            .Where(kvp => kvp.Value is Duelist)
            .Select(kvp => kvp.Key)
            .OrderBy(_ => Guid.NewGuid());

        duelistIdPairs = duelistIds
            .Chunk(2)
            .Where(array => array.Length == 2)
            .Select(array => (array[0], array[1]))
            .ToHashSet();
    }
    private byte GetEnemyId()
    {
        foreach (var (first, second) in duelistIdPairs)
        {
            if (Player.PlayerId == first)
            {
                return second;
            }
            else if (Player.PlayerId == second)
            {
                return first;
            }
        }
        return 255;
    }
    private void AddCheckedPlayerId(byte playerId)
    {
        if (checkedPlayerIds.Add(playerId) &&
            AmongUsClient.Instance.AmHost)
        {
            using var sender = CreateSender(CustomRPC.DuelistAddTarget);
            sender.Writer.Write(playerId);
        }
    }

    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.DuelistAddTarget) return;

        byte playerId = reader.ReadByte();
        _ = checkedPlayerIds.Add(playerId);
    }
    public override void OnVotingComplete(VoteData voteData, VoteResult voteResult)
    {
        if (!Player.IsAlive()) return;

        byte voteTargetId = voteData.VotedFor;
        if (voteTargetId < 15)
        {
            AddCheckedPlayerId(voteTargetId);
            return;
        }

        var candidateIds = Main.AllAlivePlayerControls
            .Select(pc => pc.PlayerId)
            .Where(id =>
                id != Player.PlayerId &&
                id != GetEnemyId() &&
                !checkedPlayerIds.Contains(id));

        if (candidateIds.Any())
        {
            byte targetId = candidateIds.MinBy(_ => Guid.NewGuid());
            AddCheckedPlayerId(targetId);
        }
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!checkedPlayerIds.Contains(seen.PlayerId)) return "";

        string mark = GetEnemyId() == seen.PlayerId ?
            "★" :
            "×";
        return Utils.ColorString(RoleInfo.RoleColor, mark);
    }
}