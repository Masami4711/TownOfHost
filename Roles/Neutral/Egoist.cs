using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral;
public sealed class Egoist : RoleBase, ISidekickable, IShapeshifter, ISchrodingerCatOwner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Egoist),
            player => new Egoist(player),
            CustomRoles.Egoist,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Neutral,
            50600,
            SetupOptionItem,
            "eg",
            "#5600ff",
            canMakeMadmate: () => OptionCanCreateMadmate.GetBool(),
            countType: CountTypes.Impostor,
            assignInfo: new RoleAssignInfo(CustomRoles.Egoist, CustomRoleTypes.Neutral)
            {
                AssignRoleType = CustomRoleTypes.Impostor,
                IsInitiallyAssignableCallBack =
                    () => Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors) > 1,
                AssignCountRule = new(1, 1, 1)
            }
        );
    public Egoist(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        CanCreateMadmate = OptionCanCreateMadmate.GetBool();
    }

    static OptionItem OptionKillCooldown;
    static OptionItem OptionCanCreateMadmate;

    private static float KillCooldown;
    public static bool CanCreateMadmate;
    private static PlayerControl egoist;

    public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Egoist;

    float IShapeshifter.ShapeshifterCooldown => Main.RealOptionsData.GetFloat(FloatOptionNames.ShapeshifterCooldown);
    float IShapeshifter.ShapeshifterDuration => Main.RealOptionsData.GetFloat(FloatOptionNames.ShapeshifterDuration);
    bool IShapeshifter.ShapeshifterLeaveSkin => Main.RealOptionsData.GetBool(BoolOptionNames.ShapeshifterLeaveSkin);

    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCanCreateMadmate = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanCreateMadmate, false, false);
    }
    public override void Add()
    {
        foreach (var impostor in Main.AllPlayerControls.Where(pc => pc.Is(CustomRoleTypes.Impostor)))
        {
            NameColorManager.Add(impostor.PlayerId, Player.PlayerId);
        }
        egoist = Player;
    }
    public override void OnDestroy()
    {
        egoist = null;
    }
    public float CalculateKillCooldown() => KillCooldown;
    public bool CanUseSabotageButton() => true;
    public static bool CheckWin()
    {
        if (Main.AllAlivePlayerControls.All(p => !p.Is(RoleTypes.Impostor)) &&
            egoist.IsAlive()) //インポスター全滅でエゴイストが生存
        {
            Win();
            return true;
        }

        return false;
    }
    private static void Win()
    {
        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Egoist);
        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Egoist);
    }
    public bool CanMakeSidekick() => CanCreateMadmate;
    public void ApplySchrodingerCatOptions(IGameOptions option)
    {
        option.SetVision(true);
    }
}