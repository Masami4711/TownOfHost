using System.Collections.Generic;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class Mayor : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Mayor),
            player => new Mayor(player),
            CustomRoles.Mayor,
            () => OptionHasPortableButton.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20200,
            SetupOptionItem,
            "#204d42",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Mayor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        AdditionalVote = OptionAdditionalVote.GetInt();
        HasPortableButton = OptionHasPortableButton.GetBool();
        NumOfUseButton = OptionNumOfUseButton.GetInt();

        LeftButtonCount = NumOfUseButton;
    }

    private static OptionItem OptionAdditionalVote;
    private static OptionItem OptionHasPortableButton;
    private static OptionItem OptionNumOfUseButton;
    enum OptionName
    {
        MayorAdditionalVote,
        MayorHasPortableButton,
        MayorNumOfUseButton,
    }
    public static int AdditionalVote;
    public static bool HasPortableButton;
    public static int NumOfUseButton;

    public int LeftButtonCount;
    private static void SetupOptionItem()
    {
        OptionAdditionalVote = IntegerOptionItem.Create(RoleInfo, 10, OptionName.MayorAdditionalVote, new(1, 99, 1), 1, false)
            .SetValueFormat(OptionFormat.Votes);
        OptionHasPortableButton = BooleanOptionItem.Create(RoleInfo, 11, OptionName.MayorHasPortableButton, false, false);
        OptionNumOfUseButton = IntegerOptionItem.Create(RoleInfo, 12, OptionName.MayorNumOfUseButton, new(1, 99, 1), 1, false, OptionHasPortableButton)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        Logger.Warn($"{LeftButtonCount} <= 0", "Mayor.ApplyGameOptions");
        AURoleOptions.EngineerCooldown =
            LeftButtonCount <= 0
            ? 255f
            : opt.GetInt(Int32OptionNames.EmergencyCooldown);
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override bool OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        if (reporter.Is(CustomRoles.Mayor) && target == null) //ボタン
            LeftButtonCount--;

        return true;
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (LeftButtonCount > 0)
        {
            var user = physics.myPlayer;
            physics.RpcBootFromVent(ventId);
            user?.ReportDeadBody(null);
        }

        return false;
    }
    public override bool OnCheckForEndVoting(ref List<MeetingHud.VoterState> statesList, PlayerVoteArea pva)
    {
        for (var i = 0; i < AdditionalVote; i++)
        {
            statesList.Add(new MeetingHud.VoterState()
            {
                VoterId = pva.TargetPlayerId,
                VotedForId = pva.VotedFor
            });
        }
        return true;
    }
    public override void AfterMeetingTasks()
    {
        if (HasPortableButton)
            Player.RpcResetAbilityCooldown();
    }
}