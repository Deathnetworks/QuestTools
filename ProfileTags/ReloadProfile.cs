using System;
using System.IO;
using System.Text.RegularExpressions;
using Zeta.Bot;
using Zeta.Bot.Navigation;
using Zeta.Bot.Profile;
using Zeta.Game;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools
{
    [XmlElement("ReloadProfile")]
    class ReloadProfile : ProfileBehavior
    {
        private bool _done = false;
        public override bool IsDone
        {
            get { return _done; }
        }

        public Zeta.Game.Internals.Quest CurrentQuest { get { return ZetaDia.CurrentQuest; } }

        internal static string _lastReloadLoopQuestStep = "";
        internal static int _questStepReloadLoops = 0;

        string currProfile = "";
        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                new Decorator(ret => ZetaDia.IsInGame && ZetaDia.Me.IsValid && _questStepReloadLoops > 15,
                    new PrioritySelector(
                        new Decorator(ret => QuestToolsSettings.Instance.AllowProfileRestarts,
                            new Sequence(
                                new Action(ret => _questStepReloadLoops = 0),
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
                new Decorator(ret => DateTime.UtcNow.Subtract(QuestTools.lastProfileReload).TotalSeconds < 2,
                    new Sequence(
                        new Action(ret => Logger.Log("Profile loading loop detected, counted {0} reloads", _questStepReloadLoops)),
                        new Action(ret => _done = true)
                    )
                ),
                new Decorator(ret => ZetaDia.IsInGame && ZetaDia.Me.IsValid,
                    new Sequence(
                        new Action(ret => currProfile = ProfileManager.CurrentProfile.Path),
                        new Action(ret => Logger.Log("Reloading profile {0} {1}", currProfile, status())),
                        new Action(ret => ReloadLoopTick()),
                        new Action(ret => QuestTools.lastProfileReload = DateTime.UtcNow),
                        new Action(ret => ProfileManager.Load(currProfile)),
                        new Action(ret => Navigator.Clear())
                    )
                )
            );

        }

        private RunStatus ForceRestartAct()
        {
            Regex questingProfileName = new Regex(@"Act \d by rrrix");

            if (questingProfileName.IsMatch(ProfileManager.CurrentProfile.Name))
            {
                string act = "";
                string difficulty = "";

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
                    default:
                        break;
                }
                switch (ZetaDia.Service.Hero.CurrentDifficulty)
                {
                    case GameDifficulty.Normal: difficulty = "Normal";
                        break;
                    case GameDifficulty.Hard: difficulty = "Hard";
                        break;
                    case GameDifficulty.Expert: difficulty = "Expert";
                        break;
                    case GameDifficulty.Master: difficulty = "Master";
                        break;
                    case GameDifficulty.Torment1: difficulty = "Torment1";
                        break;
                    case GameDifficulty.Torment2: difficulty = "Torment2";
                        break;
                    case GameDifficulty.Torment3: difficulty = "Torment3";
                        break;
                    case GameDifficulty.Torment4: difficulty = "Torment4";
                        break;
                    case GameDifficulty.Torment5: difficulty = "Torment5";
                        break;
                    case GameDifficulty.Torment6: difficulty = "Torment6";
                        break;
                }

                string restartActProfile = String.Format("{0}_StartNew.xml", act);
                Logger.Log("[QuestTools] Max Profile reloads reached, restarting Act! Loading Profile {0} - {1}", restartActProfile, status());

                string profilePath = Path.Combine(Path.GetDirectoryName(ProfileManager.CurrentProfile.Path), restartActProfile);
                ProfileManager.Load(profilePath);
            }

            return RunStatus.Success;
        }

        private void ReloadLoopTick()
        {
            // if this is the first time reloading this quest and step, set reload loops to zero
            string thisQuestId = QuestId.ToString() + "_" + StepId.ToString();
            if (thisQuestId != _lastReloadLoopQuestStep)
            {
                _questStepReloadLoops = 0;
            }

            // increment ReloadLoops 
            _questStepReloadLoops++;

            // record this quest Id and step Id
            _lastReloadLoopQuestStep = thisQuestId;
        }

        private string status()
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

        private static string GetSecondsSinceProfileReload()
        {
            return DateTime.UtcNow.Subtract(QuestTools.lastProfileReload).TotalSeconds.ToString("0.0");
        }



    }
}
