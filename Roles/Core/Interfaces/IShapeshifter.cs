using System;
using AmongUs.GameOptions;

namespace TownOfHost.Roles.Core.Interfaces;

public interface IShapeshifter
{
    public float ShapeshifterCooldown { get; }
    public float ShapeshifterDuration { get; }
    public bool ShapeshifterLeaveSkin { get; }
}
public static class IShapeshifterHelper
{
    public static void ApplyShapeshifterOptions(this RoleBase roleClass)
    {
        if (roleClass is IShapeshifter shapeshifter)
        {
            AURoleOptions.ShapeshifterCooldown = Math.Max(shapeshifter.ShapeshifterCooldown, 1f);
            AURoleOptions.ShapeshifterDuration = shapeshifter.ShapeshifterDuration;
            AURoleOptions.ShapeshifterLeaveSkin = shapeshifter.ShapeshifterLeaveSkin;
        }
        else
        {
            AURoleOptions.ShapeshifterCooldown = 255f;
            AURoleOptions.ShapeshifterDuration = 1f;
            AURoleOptions.ShapeshifterLeaveSkin = false;
        }
    }
}
