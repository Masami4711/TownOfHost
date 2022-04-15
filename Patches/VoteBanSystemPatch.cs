using Hazel;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;

namespace TownOfHost
{
    [HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.AddVote))]
    class AddVotePatch
    {
        public static bool Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (__instance.isNiceguesser() || __instance.isEvilguesser())
            {
                if (main.GuesserShootingItems[__instance.PlayerId].Item1 == 0)
                {
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (!pc.Data.IsDead && !pc.Data.Disconnected)
                        {
                            main.GuesserShootingItems[__instance.PlayerId] = (1, pc.PlayerId, target.PlayerId, 0);
                            Utils.NotifyRoles();
                            Logger.info("ゲッサーのターゲット：" + $"{target.getRealName()}");
                            break;
                        }
                    }
                    return false;
                }
                if (main.GuesserShootingItems[__instance.PlayerId].Item1 == 1)
                {
                    if (target.PlayerId == __instance.PlayerId)
                    {
                        main.GuesserShootingItems[__instance.PlayerId] = (0, __instance.PlayerId, __instance.PlayerId, 0);
                        Utils.NotifyRoles();
                        Logger.info("キャンセル");
                        return false;
                    }
                    if (target.PlayerId == main.GuesserShootingItems[__instance.PlayerId].Item2)
                    {
                        main.GuesserShootingItems[__instance.PlayerId] = (2, main.GuesserShootingItems[__instance.PlayerId].Item2, main.GuesserShootingItems[__instance.PlayerId].Item3, 0);
                        Utils.NotifyRoles();
                        Logger.info("shoot");
                        return false;
                    }
                    if (target.PlayerId == main.GuesserShootingItems[__instance.PlayerId].Item3)
                    {
                        Utils.NotifyRoles();
                        Logger.info("投票");
                        return true;
                    }
                }
                if (main.GuesserShootingItems[__instance.PlayerId].Item1 == 2)
                {
                    int RoleKey = main.GuesserShootingItems[__instance.PlayerId].Item4;
                    if (target.PlayerId == __instance.PlayerId)
                    {
                        main.GuesserShootingItems[__instance.PlayerId] = (0, __instance.PlayerId, __instance.PlayerId, 0);
                        Utils.NotifyRoles();
                        Logger.info("キャンセル");
                        return false;
                    }
                    if (target.PlayerId == main.GuesserShootingItems[__instance.PlayerId].Item2)
                    {
                        main.GuesserShootingItems[__instance.PlayerId] = (2, main.GuesserShootingItems[__instance.PlayerId].Item2, main.GuesserShootingItems[__instance.PlayerId].Item3, RoleKey++);
                        Utils.NotifyRoles();
                        Logger.info("ロールの変更");
                        return false;
                    }
                    if (target.PlayerId == main.GuesserShootingItems[__instance.PlayerId].Item3)
                    {
                        var targetg = Utils.getPlayerById(main.GuesserShootingItems[__instance.PlayerId].Item3);
                        var killer = Utils.getPlayerById(__instance.PlayerId);
                        if (main.GuesserRoles[RoleKey] == target.getCustomRole())
                        {
                            targetg.RpcMurderPlayer(killer);
                            PlayerState.setDeathReason(targetg.PlayerId, PlayerState.DeathReason.Kill);
                            main.GuesserShootingItems[__instance.PlayerId] = (0, __instance.PlayerId, __instance.PlayerId, 0);
                            Logger.info("キル成功");
                            Utils.NotifyRoles();
                            return false;
                        }
                        if (main.GuesserRoles[RoleKey] != target.getCustomRole())
                        {
                            killer.RpcMurderPlayer(killer);
                            PlayerState.setDeathReason(__instance.PlayerId, PlayerState.DeathReason.Suicide);
                            main.GuesserShootingItems[__instance.PlayerId] = (0, __instance.PlayerId, __instance.PlayerId, 0);
                            Logger.info("キル失敗");
                            Utils.NotifyRoles();
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}