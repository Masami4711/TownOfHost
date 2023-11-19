using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Vanilla;

public sealed class Shapeshifter : RoleBase, IImpostor, IShapeshifter, ISidekickable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(Shapeshifter),
            player => new Shapeshifter(player),
            RoleTypes.Shapeshifter,
            canMakeMadmate: true
        );
    public Shapeshifter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    float IShapeshifter.ShapeshifterCooldown => Main.RealOptionsData.GetFloat(FloatOptionNames.ShapeshifterCooldown);
    float IShapeshifter.ShapeshifterDuration => Main.RealOptionsData.GetFloat(FloatOptionNames.ShapeshifterDuration);
    bool IShapeshifter.ShapeshifterLeaveSkin => Main.RealOptionsData.GetBool(BoolOptionNames.ShapeshifterLeaveSkin);
}
