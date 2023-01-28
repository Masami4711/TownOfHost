using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using static TownOfHost.Options;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class Runaway
    {
        private static readonly int Id = 51000;
        private static List<byte> playerIdList = new();
        private static OptionItem OptionNumTasksToEscape;
        private static OptionItem OptionDefaultEscapeCooldown;
        private static OptionItem OptionFinalEscapeCooldown;
        private static int NumTasksToEscape;
        private static float DefaultEscapeCooldown;
        private static float FinalEscapeCooldown;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Runaway);
            OptionNumTasksToEscape = IntegerOptionItem.Create(Id + 10, "RunawayNumTasksToEscape", new(0, 15, 1), 4, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Runaway]);
            OptionDefaultEscapeCooldown = FloatOptionItem.Create(Id + 11, "RunawayDefaultEscapeCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Runaway]);
            OptionFinalEscapeCooldown = FloatOptionItem.Create(Id + 12, "RunawayFinalEscapeCooldown", new(0f, 180f, 2.5f), 60f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Runaway]);
        }
        public static void Init()
        {
            playerIdList = new();

            NumTasksToEscape = OptionNumTasksToEscape.GetInt();
            DefaultEscapeCooldown = OptionDefaultEscapeCooldown.GetInt();
            FinalEscapeCooldown = OptionFinalEscapeCooldown.GetInt();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;

        //設定関連
        public static void ApplyGameOptions()
        {
            AURoleOptions.EngineerCooldown = EscapeCooldown();
            AURoleOptions.EngineerInVentMaxTime = 1f;
        }
        public static void SetAbilityButton(HudManager __instance, PlayerControl player)
        {
            __instance.AbilityButton.ToggleVisible(CanUseVent(player));
            __instance.AbilityButton.OverrideText(GetString("DeathReason.Escape"));
        }
        public static bool CanUseVent(PlayerControl pc)
            => pc.IsAlive() && playerIdList.Contains(pc.PlayerId)
            && (pc.GetPlayerTaskState().IsTaskFinished || pc.GetPlayerTaskState().CompletedTasksCount >= NumTasksToEscape);
        private static float EscapeCooldown()
        {
            int numAll = Utils.AllPlayersCount; //ゲーム的な人数
            int numAlive = Utils.AllAlivePlayersCount;
            int numDead = numAll - numAlive;
            // Alive : Deadで内分
            return (DefaultEscapeCooldown * numAlive + FinalEscapeCooldown * numDead) / numAll;
        }

        //勝利条件関連
        private static bool IsAliveWin(PlayerControl pc)
            => pc.IsAlive()
            && playerIdList.Contains(pc.PlayerId)
            && CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate;
        private static bool IsEscapeWin(PlayerControl pc)
            => pc != null && !pc.IsAlive()
            && pc.Is(PlayerState.DeathReason.Escape)
            && CustomWinnerHolder.WinnerTeam != CustomWinner.Crewmate;
        public static void SoloWin() //全滅 & 脱出
        {
            var winnerList = Main.AllPlayerControls.Where(pc => IsEscapeWin(pc));
            if (winnerList == null || winnerList.Count() == 0) return;
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Runaway);
            foreach (var pc in winnerList)
                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
        }
        public static void AdditionalWin(PlayerControl pc)
        {
            if (IsAliveWin(pc) || (IsEscapeWin(pc) && CustomWinnerHolder.WinnerTeam != CustomWinner.Runaway))
            {
                CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Runaway);
                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
            }
        }

        //内部で呼ばれるvoid関数
        private static void Escape(PlayerControl pc)
        {
            pc.RpcExileV2();
            Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Escape;
            Main.PlayerStates[pc.PlayerId].SetDead();
            pc.SetRealKiller(pc);
            Utils.MarkEveryoneDirtySettings();
            Utils.NotifyRoles();
        }
        //外部で呼ばれるvoid関数
        public static void OnEnterVent(PlayerControl pc, int ventId)
        {
            if (!AmongUsClient.Instance.AmHost || !AmongUsClient.Instance.IsGameStarted) return;
            if (!CanUseVent(pc)) return;
            new LateTask(() =>
            {
                pc?.MyPhysics?.RpcBootFromVent(ventId);
                Escape(pc);
            }, 0.25f, "Runaway Escape");
        }
        public static void AfterMeetingTasks() //クールダウンを設定
        {
            foreach (var id in playerIdList)
            {
                if (!Main.PlayerStates[id].IsDead)
                {
                    var pc = Utils.GetPlayerById(id);
                    pc?.SyncSettings();
                    pc?.RpcResetAbilityCooldown();
                }
            }
        }
    }
}