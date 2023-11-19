using System;
using System.Collections.Generic;
using AmongUs.GameOptions;

namespace TownOfHost.Roles.Core.Interfaces;

public interface IShapeshifter : IRoleType, IKiller
{
    RoleTypes IRoleType.RoleType => RoleTypes.Shapeshifter;
    void IRoleType.ApplyRoleOptions()
    {
        AURoleOptions.ShapeshifterCooldown = Math.Max(ShapeshifterCooldown, 1f);
        AURoleOptions.ShapeshifterDuration = ShapeshifterDuration;
        AURoleOptions.ShapeshifterLeaveSkin = ShapeshifterLeaveSkin;
    }

    public float ShapeshifterCooldown { get; }
    public float ShapeshifterDuration { get; }
    public bool ShapeshifterLeaveSkin { get; }
    public bool OnCheckShapeshift(PlayerControl target, bool animate) => true;
    public void OnShapeshift(PlayerControl target) { }
    public static readonly (float, float, bool) DefaultOptionValue = (255f, 1f, false);
    public static Dictionary<byte, bool> CheckShapeshift = new();
    public static Dictionary<byte, byte> ShapeshiftTarget = new();
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
