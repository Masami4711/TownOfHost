using System.Collections.Generic;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class Cracker
    {
        static readonly int Id = 3000;
        public static List<byte> playerIdList = new();
        public static bool IsPoweredLightsOut = false;
        public static List<byte> IsBlackOut = new();
        public static CustomOption EnablePoweredLightsOut;
        public static CustomOption LightsOutMinimum;
        public static CustomOption EnablePoweredComms;
        public static CustomOption EnablePoweredReactor;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.Cracker);
            EnablePoweredLightsOut = CustomOption.Create(Id + 10, Color.white, "EnablePoweredLightsOut", true, Options.CustomRoleSpawnChances[CustomRoles.Cracker]);
            LightsOutMinimum = CustomOption.Create(Id + 11, Color.white, "LightsOutMinimum", 5, 0, 20, 1, EnablePoweredLightsOut);
            EnablePoweredComms = CustomOption.Create(Id + 12, Color.white, "EnablePoweredComms", true, Options.CustomRoleSpawnChances[CustomRoles.Cracker]);
            EnablePoweredReactor = CustomOption.Create(Id + 13, Color.white, "EnablePoweredReactor", true, Options.CustomRoleSpawnChances[CustomRoles.Cracker]);
        }
        public static void Init()
        {
            playerIdList = new();
            IsPoweredLightsOut = new();
            IsBlackOut = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            IsPoweredLightsOut = false;
            IsBlackOut.Clear();
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void PoweredSabotage(SystemTypes systemType, PlayerControl player, byte amount)
        {
            // Logger.Info($"Powered Sabotage by {Utils.GetNameWithRole(player.PlayerId)}", "Cracker");
            int mapId = PlayerControl.GameOptions.MapId;

            switch (systemType)
            {
                case SystemTypes.Sabotage: //停電
                    if (amount != 7) break;
                    if (!EnablePoweredLightsOut.GetBool()) break;
                    Logger.Info($"Ready for Powered Lights Out by {Utils.GetNameWithRole(player.PlayerId)}", "Cracker");
                    PoweredLightsOut();
                    break;
                case SystemTypes.Comms:
                    if (amount != 128) break;
                    if (!EnablePoweredComms.GetBool()) break;
                    Logger.Info($"Powered Comms by {Utils.GetNameWithRole(player.PlayerId)}", "Cracker");
                    break;
                case SystemTypes.Reactor:
                case SystemTypes.Laboratory:
                    if (!(systemType == SystemTypes.Laboratory && mapId == 2 && amount == 128)
                        && !(systemType == SystemTypes.Reactor && mapId == 4 && amount == 128)) break;
                    if (!EnablePoweredReactor.GetBool()) break;
                    Logger.Info($"Powered Reactor by {Utils.GetNameWithRole(player.PlayerId)}", "Cracker");
                    CheckAndCloseAllDoors(mapId);
                    break;
                default:
                    break;
            }
        }
        public static bool HasImpostorVision(PlayerControl player)
            => player.Data.IsDead
            || player.GetCustomRole().IsImpostor()
            || (player.GetCustomRole().IsMadmate() && Options.MadmateHasImpostorVision.GetBool())
            || player.Is(CustomRoles.EgoSchrodingerCat)
            || (player.Is(CustomRoles.Lighter) && player.GetPlayerTaskState().IsTaskFinished && Options.LighterTaskCompletedDisableLightOut.GetBool())
            || ((player.Is(CustomRoles.Jackal) || player.Is(CustomRoles.JSchrodingerCat)) && Options.JackalHasImpostorVision.GetBool());
        public static void PoweredLightsOut()
        {
            IsPoweredLightsOut = true;
            new LateTask(() =>
            {
                if (Utils.IsActive(SystemTypes.Electrical))
                {
                    foreach (var player in PlayerControl.AllPlayerControls)
                    {
                        if (!HasImpostorVision(player))
                        {
                            PlayerState.IsBlackOut[player.PlayerId] = true;
                            ExtendedPlayerControl.CustomSyncSettings(player);
                        }
                    }
                }
            }, 2.0f, "Powered Lights Out");

            new LateTask(() =>
            {
                IsPoweredLightsOut = false;
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (!HasImpostorVision(player))
                    {
                        PlayerState.IsBlackOut[player.PlayerId] = false;
                        ExtendedPlayerControl.CustomSyncSettings(player);
                    }
                }
            }, LightsOutMinimum.GetFloat(), "Powered Lights Out");
        }
        public static void Comms()
        {

        }
        public static void CheckAndCloseAllDoors(int mapId)
        {
            if (mapId == 3) return;
            SystemTypes[] SkeldDoorRooms =
            {SystemTypes.Cafeteria,
            SystemTypes.Electrical,
            SystemTypes.LowerEngine,
            SystemTypes.MedBay,
            SystemTypes.Security,
            SystemTypes.Storage,
            SystemTypes.UpperEngine};

            SystemTypes[] PolusDoorRooms =
            {SystemTypes.Comms,
            SystemTypes.Electrical,
            SystemTypes.Laboratory,
            SystemTypes.LifeSupp,
            SystemTypes.Office,
            SystemTypes.Storage,
            SystemTypes.Weapons};

            SystemTypes[] AirShipDoorRooms =
            {SystemTypes.Brig,
            SystemTypes.Comms,
            SystemTypes.Kitchen,
            SystemTypes.MainHall,
            SystemTypes.Medical,
            SystemTypes.Records};

            SystemTypes[][] Doors = { SkeldDoorRooms, PolusDoorRooms, null, AirShipDoorRooms };
            foreach (var doorRoom in Doors[mapId - 1])
            {
                ShipStatus.Instance.CloseDoorsOfType(doorRoom);
            }
        }
    }
}