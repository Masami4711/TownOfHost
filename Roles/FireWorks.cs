using System.Collections.Generic;
using Hazel;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class Bomber
    {
        public enum BomberState
        {
            Initial = 1,
            SettingBomber = 2,
            WaitTime = 4,
            ReadyFire = 8,
            FireEnd = 16,
            CanUseKill = Initial | FireEnd
        }
        static readonly int Id = 1700;

        static CustomOption BomberCount;
        static CustomOption BomberRadius;

        static Dictionary<byte, int> nowBomberCount = new();
        static Dictionary<byte, List<Vector3>> bomberPosition = new();
        static Dictionary<byte, BomberState> state = new();
        static Dictionary<byte, int> bomberBombKill = new();

        static int bomberCount = 1;
        static float bomberRadius = 1;

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.Bomber);
            BomberCount = CustomOption.Create(Id + 10, Color.white, "BomberMaxCount", 1f, 1f, 3f, 1f, Options.CustomRoleSpawnChances[CustomRoles.Bomber]);
            BomberRadius = CustomOption.Create(Id + 11, Color.white, "BomberRadius", 1f, 0.5f, 3f, 0.5f, Options.CustomRoleSpawnChances[CustomRoles.Bomber]);
        }

        public static void Init()
        {
            nowBomberCount = new();
            bomberPosition = new();
            state = new();
            bomberBombKill = new();
            bomberCount = BomberCount.GetInt();
            bomberRadius = BomberRadius.GetFloat();
        }

        public static void Add(byte playerId)
        {
            nowBomberCount[playerId] = bomberCount;
            bomberPosition[playerId] = new();
            state[playerId] = BomberState.Initial;
            bomberBombKill[playerId] = 0;
        }

        public static void SendRPC(byte playerId)
        {
            Logger.Info($"Player{playerId}:SendRPC", "Bomber");
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SendBomberState, Hazel.SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(nowBomberCount[playerId]);
            writer.Write((int)state[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void ReceiveRPC(MessageReader msg)
        {
            var playerId = msg.ReadByte();
            nowBomberCount[playerId] = msg.ReadInt32();
            state[playerId] = (BomberState)msg.ReadInt32();
            Logger.Info($"Player{playerId}:ReceiveRPC", "Bomber");
        }

        public static bool CanUseKillButton(PlayerControl pc)
        {
            Logger.Info($"Bomber CanUseKillButton", "Bomber");
            if (pc.Data.IsDead) return false;
            var canUse = false;
            if ((state[pc.PlayerId] & BomberState.CanUseKill) != 0)
            {
                canUse = true;
            }
            Logger.Info($"CanUseKillButton:{canUse}", "Bomber");
            return canUse;
        }

        public static void ShapeShiftState(PlayerControl pc, bool shapeshifting)
        {
            Logger.Info($"Bomber ShapeShift", "Bomber");
            if (pc == null || pc.Data.IsDead || !shapeshifting) return;
            switch (state[pc.PlayerId])
            {
                case BomberState.Initial:
                case BomberState.SettingBomber:
                    Logger.Info("爆弾を一個設置", "Bomber");
                    bomberPosition[pc.PlayerId].Add(pc.transform.position);
                    nowBomberCount[pc.PlayerId]--;
                    if (nowBomberCount[pc.PlayerId] == 0)
                        state[pc.PlayerId] = Main.AliveImpostorCount <= 1 ? BomberState.ReadyFire : BomberState.WaitTime;
                    else
                        state[pc.PlayerId] = BomberState.SettingBomber;
                    break;
                case BomberState.ReadyFire:
                    Logger.Info("爆弾を爆破", "Bomber");
                    bool suicide = false;
                    foreach (PlayerControl target in PlayerControl.AllPlayerControls)
                    {
                        if (target.Data.IsDead) continue;

                        foreach (var pos in bomberPosition[pc.PlayerId])
                        {
                            var dis = Vector2.Distance(pos, target.transform.position);
                            if (dis > bomberRadius) continue;

                            if (target == pc)
                            {
                                //自分は後回し
                                suicide = true;
                            }
                            else
                            {
                                PlayerState.SetDeathReason(target.PlayerId, PlayerState.DeathReason.Bombed);
                                target.RpcMurderPlayer(target);
                            }
                        }
                    }
                    if (suicide)
                    {
                        PlayerState.SetDeathReason(pc.PlayerId, PlayerState.DeathReason.Suicide);
                        pc.RpcMurderPlayer(pc);
                    }
                    state[pc.PlayerId] = BomberState.FireEnd;
                    break;
                default:
                    break;
            }
            SendRPC(pc.PlayerId);
            Utils.NotifyRoles();
        }

        public static string GetStateText(PlayerControl pc, bool isLocal = true)
        {
            string retText = "";
            if (pc == null || pc.Data.IsDead) return retText;

            if (state[pc.PlayerId] == BomberState.WaitTime && Main.AliveImpostorCount <= 1)
            {
                Logger.Info("爆破準備OK", "Bomber");
                state[pc.PlayerId] = BomberState.ReadyFire;
                SendRPC(pc.PlayerId);
                Utils.NotifyRoles();
            }
            switch (state[pc.PlayerId])
            {
                case BomberState.Initial:
                case BomberState.SettingBomber:
                    retText = string.Format(GetString("BomberPutPhase"), nowBomberCount[pc.PlayerId]);
                    break;
                case BomberState.WaitTime:
                    retText = GetString("BomberWaitPhase");
                    break;
                case BomberState.ReadyFire:
                    retText = GetString("BomberReadyFirePhase");
                    break;
                case BomberState.FireEnd:
                    break;
            }
            return retText;
        }
    }
}