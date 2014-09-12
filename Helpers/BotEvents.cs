using System;
using System.Collections.Generic;
using System.Threading;
using QuestTools.ProfileTags;
using QuestTools.ProfileTags.Complex;
using Zeta.Bot;
using Zeta.Common;
using Zeta.Game;

namespace QuestTools.Helpers
{
    public class BotEvents
    {
        public static DateTime LastBotStart { get; set; }
        public static int GameCount { get; set; }
        public static DateTime LastJoinedGame { get; set; }
        public static DateTime LastProfileReload { get; set; }

        internal static void WireUp()
        {
            BotMain.OnStart += BotMain_OnStart;
            GameEvents.OnPlayerDied += GameEvents_OnPlayerDied;
            GameEvents.OnGameChanged += GameEvents_OnGameChanged;
            GameEvents.OnGameJoined += GameEvents_OnGameJoined;
            GameEvents.OnWorldChanged += GameEvents_OnWorldChanged;
        }

        internal static void UnWire()
        {
            BotMain.OnStart -= BotMain_OnStart;
            GameEvents.OnPlayerDied -= GameEvents_OnPlayerDied;
            GameEvents.OnGameChanged -= GameEvents_OnGameChanged;
            GameEvents.OnGameJoined -= GameEvents_OnGameJoined;
            GameEvents.OnWorldChanged -= GameEvents_OnWorldChanged;
        }

        private static void BotMain_OnStart(IBot bot)
        {
            Logger.Log("Bot is Starting");
            LastBotStart = DateTime.UtcNow;
            PositionCache.Cache = new HashSet<Vector3>();
            ReloadProfileTag.LastReloadLoopQuestStep = "";
            ReloadProfileTag.QuestStepReloadLoops = 0;

            UseOnceTag.UseOnceIDs.Clear();
            UseOnceTag.UseOnceCounter.Clear();

            Death.DeathCount = 0;
            Death.LastDeathTime = DateTime.MinValue;
        }

        private static void GameEvents_OnGameChanged(object sender, EventArgs e)
        {
            UseOnceTag.UseOnceIDs.Clear();
            UseOnceTag.UseOnceCounter.Clear();
        }
        private static void GameEvents_OnGameJoined(object sender, EventArgs e)
        {
            LastJoinedGame = DateTime.UtcNow;
            Logger.Debug("LastJoinedGame is {0}", LastJoinedGame);
            GameCount++;
            Death.DeathCount = 0;
            Death.LastDeathTime = DateTime.MinValue;
            LoadOnceTag.UsedProfiles.Clear();
        }
        private static void GameEvents_OnPlayerDied(object sender, EventArgs e)
        {
            Death.DeathCount++;
            Death.LastDeathTime = DateTime.UtcNow;

            Logger.Log("Player died! Position={0} QuestId={1} StepId={2} WorldId={3}",
                ZetaDia.Me.Position, ZetaDia.CurrentQuest.QuestSNO, ZetaDia.CurrentQuest.StepId, ZetaDia.CurrentWorldId);

            if (Death.MaxDeathsAllowed <= 0)
                return;

            if (Death.DeathCount < Death.MaxDeathsAllowed)
                return;

            Logger.Log("You have died too many times. Now restarting the game.");
            ProfileManager.Load(ProfileManager.CurrentProfile.Path);
            ZetaDia.Service.Party.LeaveGame(true);

            // This is bad, we shouldn't do this :(
            Thread.Sleep(12000);
        }
        private static void GameEvents_OnWorldChanged(object sender, EventArgs e)
        {
            PositionCache.Cache = new HashSet<Vector3>();
        }
    }
}
