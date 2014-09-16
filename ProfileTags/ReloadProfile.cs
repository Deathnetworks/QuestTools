using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
    /// <summary>
    /// Reloads the current profile, and optionally restarts the act quest if the profile has been reloaded too many times.
    /// </summary>
    [XmlElement("ReloadProfile")]
    class ReloadProfileTag : ProfileBehavior
    {
        public ReloadProfileTag() { }
        private bool _done;
        public override bool IsDone
        {
            get { return _done; }
        }

        [XmlAttribute("force")]
        public bool Force { get; set; }

        public Zeta.Game.Internals.Quest CurrentQuest { get { return ZetaDia.CurrentQuest; } }

        private static string _lastReloadLoopQuestStep = "";

        /// <summary>
        /// Gets or sets the last reload loop quest step.
        /// </summary>
        /// <value>
        /// The last reload loop quest step.
        /// </value>
        internal static string LastReloadLoopQuestStep
        {
            get { return _lastReloadLoopQuestStep; }
            set { _lastReloadLoopQuestStep = value; }
        }

        internal static int QuestStepReloadLoops { get; set; }

        string _currProfile = "";

        /// <summary>
        /// Initializes the <see cref="ReloadProfileTag"/> class.
        /// </summary>
        static ReloadProfileTag()
        {
            QuestStepReloadLoops = 0;
        }

        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(ret => MainCoroutine());
        }

        /// <summary>
        /// The main Coroutine
        /// </summary>
        /// <returns></returns>
        private async Task<bool> MainCoroutine()
        {
            if (!QuestToolsSettings.Instance.AllowProfileReloading && !Force)
            {
                Logger.Log("Profile reloading disabled, skipping tag. questId=\"{0}\" stepId=\"{1}\"", ZetaDia.CurrentQuest.QuestSNO, ZetaDia.CurrentQuest.StepId);
                _done = true;
                return false;
            }

            if (ZetaDia.IsInGame && ZetaDia.Me.IsValid && QuestStepReloadLoops > 15)
            {
                if (QuestToolsSettings.Instance.AllowProfileRestarts)
                {
                    QuestStepReloadLoops = 0;
                    ForceRestartAct();
                    return true;
                }

                Logger.Log("*** Max Profile Reloads Threshold Breached *** ");
                Logger.Log("*** Profile restarts DISABLED *** ");
                Logger.Log("*** QuestTools STOPPING BOT *** ");
                BotMain.Stop();
                return true;
            }
            if (DateTime.UtcNow.Subtract(BotEvents.LastProfileReload).TotalSeconds < 2)
            {
                Logger.Log("Profile loading loop detected, counted {0} reloads", QuestStepReloadLoops);
                _done = true;
                return true;
            }

            if (ZetaDia.IsInGame && ZetaDia.Me.IsValid)
            {
                _currProfile = ProfileManager.CurrentProfile.Path;
                Logger.Log("Reloading profile {0} {1}", _currProfile, QuestInfo());
                CountReloads();
                BotEvents.LastProfileReload = DateTime.UtcNow;
                ProfileManager.Load(_currProfile);
                Navigator.Clear();

                return true;
            }

            return true;
        }

        /// <summary>
        /// Reloads the ActX_Start.xml profile
        /// </summary>
        /// <returns></returns>
        private void ForceRestartAct()
        {
            Regex questingProfileName = new Regex(@"Act \d by rrrix");

            if (!questingProfileName.IsMatch(ProfileManager.CurrentProfile.Name))
                return;

            string restartActProfile = String.Format("{0}_StartNew.xml", ZetaDia.CurrentAct);
            Logger.Log("[QuestTools] Max Profile reloads reached, restarting Act! Loading Profile {0} - {1}", restartActProfile, QuestInfo());

            string profilePath = Path.Combine(Path.GetDirectoryName(ProfileManager.CurrentProfile.Path), restartActProfile);
            ProfileManager.Load(profilePath);
        }

        /// <summary>
        /// Counts the reloads.
        /// </summary>
        private void CountReloads()
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

        /// <summary>
        /// Quests the information.
        /// </summary>
        /// <returns></returns>
        private string QuestInfo()
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
