using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Neutral;

public sealed class Runaway : RoleBase, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Runaway),
            player => new Runaway(player),
            CustomRoles.Runaway,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Neutral,
            51000,
            SetupOptionItem,
            "ra",
            "#708090"
        );
    public Runaway(PlayerControl player)
        : base(RoleInfo, player, () => player.IsAlive() ? HasTask.True : HasTask.False)
    {
        numTasksToEscape = optionNumTasksToEscape.GetInt();
        defaultEscapeCooldown = optionDefaultEscapeCooldown.GetInt();
        finalEscapeCooldown = optionFinalEscapeCooldown.GetInt();

        instances.Add(this);
    }
    public override void OnDestroy()
    {
        instances.Remove(this);
    }
    private static HashSet<Runaway> instances = new(1);
    private static OptionItem optionNumTasksToEscape;
    private static OptionItem optionDefaultEscapeCooldown;
    private static OptionItem optionFinalEscapeCooldown;
    private enum OptionName
    {
        RunawayNumTasksToEscape,
        RunawayDefaultEscapeCooldown,
        RunawayFinalEscapeCooldown
    }
    private static int numTasksToEscape;
    private static float defaultEscapeCooldown;
    private static float finalEscapeCooldown;

    private static void SetupOptionItem()
    {
        optionNumTasksToEscape = IntegerOptionItem.Create(RoleInfo, 10, OptionName.RunawayNumTasksToEscape, new(0, 15, 1), 4, false);
        optionDefaultEscapeCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.RunawayDefaultEscapeCooldown, new(0f, 180f, 2.5f), 20f, false);
        optionFinalEscapeCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.RunawayFinalEscapeCooldown, new(0f, 180f, 2.5f), 60f, false);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = escapeCooldown();
        AURoleOptions.EngineerInVentMaxTime = 0f;

        static float escapeCooldown()
        {
            int numAllPlayers = Utils.AllPlayersCount;
            int numAlivePlayers = Utils.AllAlivePlayersCount;
            int numDeadPlayers = numAllPlayers - numAlivePlayers;
            return (defaultEscapeCooldown * numAlivePlayers + finalEscapeCooldown * numDeadPlayers) / numAllPlayers;
        }
    }
    public override bool CanUseAbilityButton() => CanEnterVent();
    public override string GetAbilityButtonText() => Utils.GetDeathReason(CustomDeathReason.Escape);
    private bool CanEnterVent() =>
        Player.IsAlive() &&
        MyTaskState.HasCompletedEnoughCountOfTasks(numTasksToEscape);
    private bool IsAliveWin() =>
        Player.IsAlive() &&
        CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate;
    private bool IsEscapeWin() =>
        MyState.DeathReason == CustomDeathReason.Escape &&
        CustomWinnerHolder.WinnerTeam != CustomWinner.Crewmate;

    public override bool OnCompleteTask()
    {
        Player.MarkDirtySettings();
        return true;
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (CanEnterVent())
        {
            _ = new LateTask(escape, 0.25f, nameof(OnEnterVent));
        }
        return false;

        void escape()
        {
            if (!Main.isLoversDead && Main.LoversPlayers.Any(pc => pc.PlayerId == Player.PlayerId))
            {
                Main.isLoversDead = true;
                var lover = Main.LoversPlayers.FirstOrDefault(pc => pc.PlayerId != Player.PlayerId);
                Main.LoversPlayers.Clear();
                PlayerState.GetByPlayerId(lover.PlayerId).RemoveSubRole(CustomRoles.Lovers);
            }

            Player.RpcExileV2();
            MyState.DeathReason = CustomDeathReason.Escape;
            MyState.SetDead();
            Player.SetRealKiller(Player);
            Utils.MarkEveryoneDirtySettings();
            Utils.NotifyRoles();
        }
    }
    public override void AfterMeetingTasks()
    {
        if (Player.IsAlive())
        {
            Player.SyncSettings();
            Player.RpcResetAbilityCooldown();
        }
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (isForMeeting || !Player.IsAlive())
        {
            return "";
        }
        else
        {
            string suffixText = CanEnterVent() ?
                "RunawayReadyToEscape" :
                "RunawayCannotEscape";
            return Utils.ColorString(RoleInfo.RoleColor, GetString(suffixText));
        }
    }

    bool IAdditionalWinner.CheckWin(ref CustomRoles winnerRole)
    {
        if (CustomWinnerHolder.WinnerTeam == CustomWinner.Runaway)
        {
            return false;
        }
        else
        {
            return IsAliveWin() || IsEscapeWin();
        }
    }
    public static void CheckSoloWin()
    {
        foreach (var runaway in instances)
        {
            if (runaway?.Player is null || !runaway.IsEscapeWin()) continue;

            if (CustomWinnerHolder.WinnerTeam == CustomWinner.None)
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Runaway);
            }
            CustomWinnerHolder.WinnerIds.Add(runaway.Player.PlayerId);
        }
    }
}