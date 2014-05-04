using System;
using System.IO;
using System.Text.RegularExpressions;
using QuestTools.Helpers;
using Zeta.Bot;
using Zeta.Bot.Navigation;
using Zeta.Bot.Profile;
using Zeta.Game;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools.ProfileTags
{
    [XmlElement("ReloadProfile")]
    class ReloadProfileTag : ProfileBehavior
    {
        public ReloadProfileTag() { }
        private bool _done;
        public override bool IsDone
        {
            get { return _done; }
        }

        public Zeta.Game.Internals.Quest CurrentQuest { get { return ZetaDia.CurrentQuest; } }

        private static string _lastReloadLoopQuestStep = "";

        internal static string LastReloadLoopQuestStep
        {
            get { return _lastReloadLoopQuestStep; }
            set { _lastReloadLoopQuestStep = value; }
        }

        internal static int QuestStepReloadLoops { get; set; }

        string _currProfile = "";

        static ReloadProfileTag()
        {
            QuestStepReloadLoops = 0;
        }

        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                new Decorator(ret => ZetaDia.IsInGame && ZetaDia.Me.IsValid && QuestStepReloadLoops > 15,
                    new PrioritySelector(
                        new Decorator(ret => QuestToolsSettings.Instance.AllowProfileRestarts,
                            new Sequence(
                                new Action(ret => QuestStepReloadLoops = 0),
                                new Action(ret => ForceRestartAct())
                            )
                        ),
                        new Sequence(
                            new Action(ret => Logger.Log("*** Max Profile Reloads Threshold Breached *** ")),
                            new Action(ret => Logger.Log("*** Profile restarts DISABLED *** ")),
                            new Action(ret => Logger.Log("*** QuestTools STOPPING BOT *** ")),
                            new Action(ret => BotMain.Stop())
                        )
                    )
                ),
                new Decorator(ret => DateTime.UtcNow.Subtract(BotEvents.LastProfileReload).TotalSeconds < 2,
                    new Sequence(
                        new Action(ret => Logger.Log("Profile loading loop detected, counted {0} reloads", QuestStepReloadLoops)),
                        new Action(ret => _done = true)
                    )
                ),
                new Decorator(ret => ZetaDia.IsInGame && ZetaDia.Me.IsValid,
                    new Sequence(
                        new Action(ret => _currProfile = ProfileManager.CurrentProfile.Path),
                        new Action(ret => Logger.Log("Reloading profile {0} {1}", _currProfile, Status())),
                        new Action(ret => ReloadLoopTick()),
                        new Action(ret => BotEvents.LastProfileReload = DateTime.UtcNow),
                        new Action(ret => ProfileManager.Load(_currProfile)),
                        new Action(ret => Navigator.Clear())
                    )
                )
            );

        }

        private RunStatus ForceRestartAct()
        {
            Regex questingProfileName = new Regex(@"Act \d by rrrix");

            if (!questingProfileName.IsMatch(ProfileManager.CurrentProfile.Name))
                return RunStatus.Success;
            string act = "";

            switch (ZetaDia.CurrentAct)
            {
                case Act.A1: act = "Act1";
                    break;
                case Act.A2: act = "Act2";
                    break;
                case Act.A3: act = "Act3";
                    break;
                case Act.A4: act = "Act4";
                    break;
            }

            string restartActProfile = String.Format("{0}_StartNew.xml", act);
            Logger.Log("[QuestTools] Max Profile reloads reached, restarting Act! Loading Profile {0} - {1}", restartActProfile, Status());

            string profilePath = Path.Combine(Path.GetDirectoryName(ProfileManager.CurrentProfile.Path), restartActProfile);
            ProfileManager.Load(profilePath);

            return RunStatus.Success;
        }

        private void ReloadLoopTick()
        {
            // if this is the first time reloading this quest and step, set reload loops to zero
            string questId = QuestId + "_" + StepId;
            if (questId != LastReloadLoopQuestStep)
            {
                QuestStepReloadLoops = 0;
            }

            // increment ReloadLoops 
            QuestStepReloadLoops++;

            // record this quest Id and step Id
            LastReloadLoopQuestStep = questId;
        }

        private string Status()
        {
            return String.Format(
                "Act=\"{0}\" questId=\"{1}\" stepId=\"{2}\" levelAreaId=\"{3}\" worldId={4}",
                ZetaDia.CurrentAct,
                CurrentQuest.QuestSNO,
                CurrentQuest.StepId,
                ZetaDia.CurrentLevelAreaId,
                ZetaDia.CurrentWorldId
                );
        }
    }
}
