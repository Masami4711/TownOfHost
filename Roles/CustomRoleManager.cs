using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;

namespace TownOfHost.Roles;

public static class CustomRoleManager
{
    public static Type[] AllRolesClassType;
    public static List<SimpleRoleInfo> AllRolesInfo = new(Enum.GetValues(typeof(CustomRoles)).Length);
    public static List<RoleBase> AllActiveRoles = new(Enum.GetValues(typeof(CustomRoles)).Length);

    public static SimpleRoleInfo GetRoleInfo(this CustomRoles role) => AllRolesInfo.ToArray().Where(info => info.RoleName == role).FirstOrDefault();
    public static RoleBase GetRoleClass(this PlayerControl player) => GetByPlayerId(player.PlayerId);
    public static RoleBase GetByPlayerId(byte playerId) => AllActiveRoles.ToArray().Where(roleClass => roleClass.Player.PlayerId == playerId).FirstOrDefault();
    public static void Do<T>(this List<T> list, Action<T> action) => list.ToArray().Do(action);
    // == CheckMurder関連処理 ==
    public static void OnCheckMurder(PlayerControl attemptKiller, PlayerControl attemptTarget)
        => OnCheckMurder(attemptKiller, attemptTarget, attemptKiller, attemptTarget);
    public static void OnCheckMurder(PlayerControl attemptKiller, PlayerControl attemptTarget, PlayerControl appearanceKiller, PlayerControl appearanceTarget)
    {
        List<(int order, IEnumerator<int> method, RoleBase role)> methods = new();
        CheckMurderInfo info = new(attemptKiller, attemptTarget, appearanceKiller, appearanceTarget);
        foreach (var role in AllActiveRoles)
        {
            var m = role.OnCheckMurder(attemptKiller, attemptTarget, info);
            if (m != null)
                methods.Add((0, m, role));
        }

        while (methods.Count > 0)
        {
            var pair = methods.OrderByDescending(pair => pair.order).FirstOrDefault();
            methods.Remove(pair);
            (_, var method, var role) = pair;
            if (method == null) continue;

            try
            {
                // MoveNext() のタイミングで初めて処理が実行される
                // true: 次の処理順がyield returnされた (= まだ別の処理がある)
                // false: yield breakされた (= もう処理がない)
                if (method.MoveNext())
                {
                    methods.Add((method.Current, method, role));
                }
            }
            // 例外発生時: プレイヤー名とorderと例外内容を出力してスキップ
            catch (Exception ex)
            {
                var handler = Logger.Handler("CustomRoleManager.OnCheckMurder");
                handler.Error($"OnCheckMurder関数内でエラーが発生しました (player: {role.Player.name}, order: {pair.order})");
                handler.Error($"killer: {attemptKiller.name}, target: {attemptTarget.name}");
                handler.Exception(ex);
            }


            if (info.IsAborted) break;
        }
        if (!info.IsCanceled)
        {
            var (killer, target) = info.AppearanceTuple;
            killer.RpcMurderPlayer(target);
        }
    }
    public class CheckMurderInfo
    {
        /// <summary>実際にキルを行ったプレイヤー 不変</summary>
        public PlayerControl AttemptKiller { get; }
        /// <summary>Killerが実際にキルを行おうとしたプレイヤー 不変</summary>
        public PlayerControl AttemptTarget { get; }
        /// <summary>見た目上でキルを行うプレイヤー 可変</summary>
        public PlayerControl AppearanceKiller { get; set; }
        /// <summary>見た目上でキルされるプレイヤー 可変</summary>
        public PlayerControl AppearanceTarget { get; set; }

        // 分解用 (killer, target) = info.AttemptTuple; のような記述でkillerとtargetをまとめて取り出せる
        public (PlayerControl killer, PlayerControl target) AttemptTuple => (AttemptKiller, AttemptTarget);
        public (PlayerControl killer, PlayerControl target) AppearanceTuple => (AppearanceKiller, AppearanceTarget);

        /// <summary><para>この値がtrueの状態で終了すると、実際のキルが行われません。</para>
        /// <para>他のRoleBaseのCheckMurder処理は通常通り行われます。</para></summary>
        public bool IsCanceled { get; set; } = false;
        /// <summary><para>この値がtrueの状態でyield return/breakが行われると、以降の他のRoleBaseのCheckMurderの処理が行われなくなります。</para>
        /// <para>実際のキルが行われるかどうかはIsCanceledの値に依存します。</para></summary>
        public bool IsAborted { get; set; } = false;

        public CheckMurderInfo(PlayerControl killer, PlayerControl target)
        {
            AttemptKiller = AppearanceKiller = killer;
            AttemptTarget = AttemptTarget = target;
        }
        public CheckMurderInfo(PlayerControl attemptKiller, PlayerControl attemptTarget, PlayerControl appearanceKiller, PlayerControl appearancetarget)
        {
            AttemptKiller = attemptKiller;
            AttemptTarget = attemptTarget;
            AppearanceKiller = appearanceKiller;
            AppearanceTarget = appearancetarget;
        }
    }
    // ==/CheckMurder関連処理 ==
    public static void Initialize()
    {
        AllRolesInfo.Do(role => role.IsEnable = role.RoleName.IsEnable());
        AllActiveRoles.Clear();
    }
    public static void CreateInstance()
    {
        foreach (var info in AllRolesInfo)
        {
            if (!info.IsEnable) continue;

            var infoType = info.ClassType;
            var type = AllRolesClassType.Where(x => x == infoType).FirstOrDefault();
            foreach (var pc in Main.AllPlayerControls.Where(x => x.GetCustomRole() == info.RoleName).ToArray())
                Activator.CreateInstance(type, new object[] { pc });
        }
    }
}
public enum CustomRoles
{
    //Default
    Crewmate = 0,
    //Impostor(Vanilla)
    Impostor,
    Shapeshifter,
    //Impostor
    BountyHunter,
    EvilWatcher,
    FireWorks,
    Mafia,
    SerialKiller,
    ShapeMaster,
    Sniper,
    Vampire,
    Witch,
    Warlock,
    Mare,
    Puppeteer,
    TimeThief,
    EvilTracker,
    //Madmate
    MadGuardian,
    Madmate,
    MadSnitch,
    SKMadmate,
    MSchrodingerCat,//インポスター陣営のシュレディンガーの猫
    //両陣営
    Watcher,
    //Crewmate(Vanilla)
    Engineer,
    GuardianAngel,
    Scientist,
    //Crewmate
    Bait,
    Lighter,
    Mayor,
    NiceWatcher,
    SabotageMaster,
    Sheriff,
    Snitch,
    SpeedBooster,
    Trapper,
    Dictator,
    Doctor,
    Seer,
    TimeManager,
    CSchrodingerCat,//クルー陣営のシュレディンガーの猫
    //Neutral
    Arsonist,
    Egoist,
    EgoSchrodingerCat,//エゴイスト陣営のシュレディンガーの猫
    Jester,
    Opportunist,
    SchrodingerCat,//無所属のシュレディンガーの猫
    Terrorist,
    Executioner,
    Jackal,
    JSchrodingerCat,//ジャッカル陣営のシュレディンガーの猫
    //HideAndSeek
    HASFox,
    HASTroll,
    //GM
    GM,
    // Sub-roll after 500
    NotAssigned = 500,
    LastImpostor,
    Lovers,
    Workhorse,
}
public enum CustomRoleTypes
{
    Crewmate,
    Impostor,
    Neutral,
    Madmate
}