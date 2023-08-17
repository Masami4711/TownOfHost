using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
namespace TownOfHost.Roles.Crewmate;
public sealed class Bait : RoleBase, ISecretRole
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Bait),
            player => new Bait(player),
            CustomRoles.Bait,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20000,
            null,
            "ba",
            "#00f7ff"
        );
    public Bait(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (target.Is(CustomRoles.Bait) && !info.IsSuicide)
            _ = new LateTask(() => killer.CmdReportDeadBody(target.Data), 0.15f, "Bait Self Report");
    }

    public CustomRoles ReplaceRole => CustomRoles.Crewmate;

    public bool DoReplace(PlayerControl player)
    {
        return player.IsAlive() && player.GetPlayerTaskState().CompletedTasksCount < 3;
    }
}