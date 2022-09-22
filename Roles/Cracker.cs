using System;
using System.Collections.Generic;
using Hazel;
using UnityEngine;
using InnerNet;

namespace TownOfHost
{
    public static class Cracker
    {
        static readonly int Id = 3000;
        public static List<byte> playerIdList = new();
        public static bool IsForcedLightsOut = false;
        public static int CountsToFixComms = 0;
        public static bool IsForcedComms = false;
        private static int mapId = PlayerControl.GameOptions.MapId;
        public static CustomOption EnablePoweredLightsOut;
        public static CustomOption LightsOutMinimum;
        public static CustomOption EnablePoweredComms;
        public static CustomOption NormaToFixComms;
        public static CustomOption EnablePoweredO2;
        public static CustomOption EnablePoweredReactor;
        public static CustomOption CloseAllDoors;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Cracker);
            EnablePoweredLightsOut = CustomOption.Create(Id + 10, TabGroup.ImpostorRoles, Color.white, "CrackerEnablePoweredLightsOut", true, Options.CustomRoleSpawnChances[CustomRoles.Cracker]);
            LightsOutMinimum = CustomOption.Create(Id + 11, TabGroup.ImpostorRoles, Color.white, "CrackerLightsOutMinimum", 5, 0, 20, 1, EnablePoweredLightsOut);
            EnablePoweredComms = CustomOption.Create(Id + 12, TabGroup.ImpostorRoles, Color.white, "CrackerEnablePoweredComms", true, Options.CustomRoleSpawnChances[CustomRoles.Cracker]);
            NormaToFixComms = CustomOption.Create(Id + 13, TabGroup.ImpostorRoles, Color.white, "CrackerNormaToFixComms", 2, 2, 5, 1, EnablePoweredComms);
            EnablePoweredO2 = CustomOption.Create(Id + 14, TabGroup.ImpostorRoles, Color.white, "CrackerEnablePoweredO2", true, Options.CustomRoleSpawnChances[CustomRoles.Cracker]);
            EnablePoweredReactor = CustomOption.Create(Id + 15, TabGroup.ImpostorRoles, Color.white, "CrackerEnablePoweredReactor", true, Options.CustomRoleSpawnChances[CustomRoles.Cracker]);
            CloseAllDoors = CustomOption.Create(Id + 16, TabGroup.ImpostorRoles, Color.white, "CrackerCloseAllDoors", true, EnablePoweredReactor);
        }
        public static void Init()
        {
            playerIdList = new();
            IsForcedLightsOut = new();
            CountsToFixComms = new();
            IsForcedComms = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            IsForcedLightsOut = false;
            CountsToFixComms = 0;
            IsForcedComms = false;
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void PoweredSabotage(SystemTypes systemType, PlayerControl player, byte amount)
        {
            // Logger.Info($"Powered Sabotage by {Utils.GetNameWithRole(player.PlayerId)}", "Cracker");
            switch (systemType)
            {
                case SystemTypes.Sabotage: //停電
                    if (!EnablePoweredLightsOut.GetBool()) break;
                    if (amount != 7) break;
                    Logger.Info($"Powered Lights Out by {Utils.GetNameWithRole(player.PlayerId)}", "Cracker");
                    PoweredLightsOut();
                    break;
                case SystemTypes.Comms:
                    if (IsForcedComms) break;
                    if (!EnablePoweredComms.GetBool()) break;
                    if (amount != 128) break;
                    if (mapId == 1) break;
                    Logger.Info($"Powered Comms by {Utils.GetNameWithRole(player.PlayerId)}", "Cracker");
                    PoweredComms();
                    break;
                case SystemTypes.LifeSupp:
                    if (!EnablePoweredO2.GetBool()) break;
                    if (amount != 128) break;
                    Logger.Info($"Powered O2 by {Utils.GetNameWithRole(player.PlayerId)}", "Cracker");
                    PoweredO2();
                    break;
                case SystemTypes.Reactor:
                case SystemTypes.Laboratory:
                    if (!EnablePoweredReactor.GetBool()) break;
                    if (!(systemType == SystemTypes.Laboratory && mapId == 2 && amount == 128)
                        && !(systemType == SystemTypes.Reactor && mapId is 0 or 1 or 4 && amount == 128)) break;
                    Logger.Info($"Powered Reactor by {Utils.GetNameWithRole(player.PlayerId)}", "Cracker");
                    PoweredReactor();
                    break;
            }
        }
        public static void PoweredLightsOut()
        {
            IsForcedLightsOut = true;
            float delay = 2.0f;
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
            }, delay, "Powered Lights Out");

            new LateTask(() =>
            {
                IsForcedLightsOut = false;
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (!HasImpostorVision(player))
                    {
                        PlayerState.IsBlackOut[player.PlayerId] = false;
                        ExtendedPlayerControl.CustomSyncSettings(player);
                    }
                }
            }, delay + LightsOutMinimum.GetFloat(), "Powered Lights Out");
        }
        public static void PoweredComms()
        {
            CountsToFixComms = NormaToFixComms.GetInt() - 1;
        }
        public static void PoweredO2()
        {
            StartForcedComms();
        }
        public static void PoweredReactor()
        {
            StartForcedComms();
            if (CloseAllDoors.GetBool() && (mapId is 2 or 4)) CheckAndCloseAllDoors(mapId);
        }
        public static void StartForcedComms()
        {
            IsForcedComms = true;
            bool HostIsAlive = false;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.AmOwner && !pc.Data.IsDead)
                {
                    HostIsAlive = true;
                    break;
                }
            }
            if (HostIsAlive)
            {
                ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 128);
                foreach (var pc in PlayerControl.AllPlayerControls)
                    if (pc.Data.IsDead) FixForcedComms(pc);
            }
            else foreach (var pc in PlayerControl.AllPlayerControls)
                    if (!pc.Data.IsDead) CauseForcedComms(pc);
            new LateTask(() =>
            {
                Utils.NotifyRoles();
            }, 0.1f, "NotifyRoles");
        }
        public static void CauseForcedComms(PlayerControl pc)
        {
            if (!IsForcedComms) return;
            if (pc.AmOwner) return;
            MessageWriter SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, pc.GetClientId());
            SabotageFixWriter.Write((byte)SystemTypes.Comms);
            MessageExtensions.WriteNetObject(SabotageFixWriter, pc);
            SabotageFixWriter.Write((byte)128);
            AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);
        }
        public static void CheckAndFixAllForcedComms(bool NoCheck = false)
        {
            if (!IsForcedComms) return;
            if (!NoCheck && (Utils.IsActive(SystemTypes.LifeSupp) || Utils.IsActive(SystemTypes.Laboratory) || Utils.IsActive(SystemTypes.Reactor)))
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (!pc.Data.IsDead) continue;
                    foreach (PlayerTask task in pc.myTasks)
                    {
                        // Logger.Info($"{task.TaskType}", $"{pc.GetNameWithRole()}");
                        // if (task.TaskType == TaskTypes.FixComms)
                        // {
                        //     Logger.Info($"{pc.GetNameWithRole()}", "TaskTypes.FixComms");
                        //     break;
                        // }
                    }
                    FixForcedComms(pc);
                }
                return;
            }
            IsForcedComms = false;
            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 16);
            new LateTask(() =>
            {
                Utils.NotifyRoles();
            }, 0.1f, "NotifyRoles");
        }
        public static void FixForcedComms(PlayerControl pc)
        {
            if (!IsForcedComms) return;
            Logger.Info($"{pc.GetNameWithRole()}", "FixForcedComms");
            // if (pc.AmOwner)
            // {
            //     ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 16);
            //     foreach (var pc2 in PlayerControl.AllPlayerControls)
            //         if (!pc.Data.IsDead) CauseForcedComms(pc2);
            //     return;
            // }
            MessageWriter SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, pc.GetClientId());
            SabotageFixWriter.Write((byte)SystemTypes.Comms);
            MessageExtensions.WriteNetObject(SabotageFixWriter, pc);
            SabotageFixWriter.Write((byte)16);
            AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);
        }
        public static void CheckAndCloseAllDoors(int mapId)
        {
            if (mapId == 1) return;
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

            SystemTypes[][] Doors = { SkeldDoorRooms, null, PolusDoorRooms, SkeldDoorRooms, AirShipDoorRooms }; //Skeld, MiraHQ, Polus, Dleks, AirShip
            foreach (var doorRoom in Doors[mapId])
            {
                ShipStatus.Instance.CloseDoorsOfType(doorRoom);
            }
        }
        public static bool IsComms(PlayerControl player, bool Comms) => IsForcedComms ? !player.Data.IsDead : Comms && Utils.IsActive(SystemTypes.Comms);
        public static bool CheckAndBlockFixComms(PlayerControl player)
        {
            if (IsForcedComms) return true;
            if (CountsToFixComms == 0 || (!Options.MadmateCanFixComms.GetBool() && player.GetCustomRole().IsMadmate())) return false;
            Logger.Info($"{NormaToFixComms.GetInt() - CountsToFixComms}/{NormaToFixComms.GetInt()}回目の修理", "CheckAndBlockFixComms");
            CountsToFixComms = Math.Max(0, CountsToFixComms - 1);
            return true;
        }
        public static bool HasImpostorVision(PlayerControl player)
            => player.Data.IsDead
            || player.GetCustomRole().IsImpostor()
            || (player.GetCustomRole().IsMadmate() && Options.MadmateHasImpostorVision.GetBool())
            || player.Is(CustomRoles.EgoSchrodingerCat)
            || (player.Is(CustomRoles.Lighter) && player.GetPlayerTaskState().IsTaskFinished && Options.LighterTaskCompletedDisableLightOut.GetBool())
            || ((player.Is(CustomRoles.Jackal) || player.Is(CustomRoles.JSchrodingerCat)) && Options.JackalHasImpostorVision.GetBool());
    }
}