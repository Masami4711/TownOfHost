using System.Collections.Generic;
using UnityEngine;

namespace TownOfHost
{
    public static class Outsider
    {
        private static readonly int Id = 3300;
        public static List<byte> playerIdList = new();

        private static OptionItem KillCooldown;
        private static OptionItem CanSeeImpostors;
        private static OptionItem KillCountToSeeImpostors;

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Outsider);
            KillCooldown = OptionItem.Create(Id + 10, TabGroup.ImpostorRoles, Color.white, "KillCooldown", 30f, 0f, 180f, 2.5f, Options.CustomRoleSpawnChances[CustomRoles.Outsider], format: OptionFormat.Seconds);
            CanSeeImpostors = OptionItem.Create(Id + 11, TabGroup.ImpostorRoles, Color.white, "OutsiderCanSeeImpostors", false, Options.CustomRoleSpawnChances[CustomRoles.Outsider]);
            KillCountToSeeImpostors = OptionItem.Create(Id + 12, TabGroup.ImpostorRoles, Color.white, "OutsiderKillCountToSeeImpostors", 1, 0, 10, 1, CanSeeImpostors, format: OptionFormat.Times);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte id)
        {
            playerIdList.Add(id);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
        public static bool KnowImpostor(PlayerControl seer, PlayerControl target)
            => seer.Is(CustomRoles.Outsider) && CanSeeImpostors.GetBool() && target.Is(RoleType.Impostor);
    }
}