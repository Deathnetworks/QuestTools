using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using QuestTools.ProfileTags;
using QuestTools.ProfileTags.Complex;
using Zeta.Bot.Profile;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.TreeSharp;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools.Helpers
{
    public static class RiftTrial
    {
        public static bool InProgress;
        public static int CurrentWave;

        private static bool _countedWave;
        private static bool _lastCheckBelowThreshold;
        private static bool _finished;
        private static bool _isAborting;

        public static void PulseRiftTrial()
        {
            int maxWave = QuestToolsSettings.Instance.TrialRiftMaxLevel;

            if (!ZetaDia.IsInGame || !ZetaDia.Actors.Me.IsValid || !ZetaDia.ActInfo.IsValid)
                return;

            var quest = ZetaDia.ActInfo.ActiveQuests.FirstOrDefault(q => q.QuestSNO == 405695);

            if (quest == null || ZetaDia.IsInTown || ZetaDia.WorldInfo.SNOId != 405684 || !QuestToolsSettings.Instance.EnableTrialRiftMaxLevel)
            {
                InProgress = false;
                CurrentWave = 0;
                _countedWave = false;
                _lastCheckBelowThreshold = false;
                _finished = false;

                if (_isAborting)
                {
                    SetCombatAllowed(true);
                    _isAborting = false;
                }

                return;
            }

            if (quest.State == QuestState.InProgress && quest.QuestStep != 9)
            {
                if (!InProgress)
                    InProgress = true;

                if (!_countedWave && quest.QuestMeter >= 0.95f)
                {
                    _countedWave = true;
                    CurrentWave = CurrentWave + 1;
                    Logger.Log("Starting Wave: {0}", CurrentWave);
                }

                if (_lastCheckBelowThreshold && quest.QuestMeter >= 0.95)
                {
                    _countedWave = false;
                }

                _lastCheckBelowThreshold = quest.QuestMeter < 0.95f && quest.QuestMeter >= 0;
            }

            if (CurrentWave >= maxWave && !_isAborting)
            {
                Logger.Log("Reached Wave {0} Disabling Combat", maxWave);

                SetCombatAllowed(false);

                var endTrialSequence = new List<ProfileBehavior>
                {
                    new AsyncSafeMoveTo
                    {
                        PathPrecision = 5,
                        PathPointLimit = 250,
                        X = 393,
                        Y = 237,
                        Z = -11
                    },
                    new AsyncTownPortalTag(),
                    new AsyncCompositeTag()
                    {
                        IsDoneDelegate = ret => Zeta.Bot.ConditionParser.IsActiveQuestAndStep(405695,9),
                        BehaviorDelegate = new Action(ret =>
                        {
                            Logger.Log("Waiting for Trial to Finish...");
                            return RunStatus.Success;
                        })
                    }
                };
                BotBehaviorQueue.Queue(endTrialSequence);

                _isAborting = true;

            }


        }

        private static void SetCombatAllowed(bool allowed)
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name.ToLower().StartsWith("trinity"));
            Type t = asm.GetType("Trinity.Combat.Abilities.CombatBase");
            var pi = t.GetProperty("IsCombatAllowed", BindingFlags.Public | BindingFlags.Static);
            pi.SetValue(null, allowed, null);

            //if(TrinityApi.SetProperty("Trinity.Combat.Abilities.CombatBase", "IsCombatAllowed", allowed));
            Logger.Log("Turning Combat {0}", allowed ? "On" : "Off");
        }

    }
}
