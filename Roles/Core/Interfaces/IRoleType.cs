using System;
using AmongUs.GameOptions;

namespace TownOfHost.Roles.Core.Interfaces;

public interface IRoleType
{
    public RoleTypes RoleType { get; }
    public void ApplyRoleOptions();
}
