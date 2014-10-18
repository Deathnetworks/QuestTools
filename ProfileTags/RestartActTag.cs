using System;
using System.IO;
using QuestTools.ProfileTags.Complex;
using Zeta.Bot;
using Zeta.Bot.Profile;
using Zeta.Game;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools.ProfileTags
{
    [XmlElement("RestartAct")]
    public class RestartActTag : ProfileBehavior, IEnhancedProfileBehavior
    {
        public RestartActTag() { }
        private bool _isDone;
        public override bool IsDone { get { return _isDone; } }

        public override void OnStart()
        {
            Logger.Log("RestartAct initialized");
        }

        protected override Composite CreateBehavior()
        {
            return
            new Action(ret => ForceRestartAct());
        }

        private static RunStatus ForceRestartAct()
        {
            string restartActProfile = ZetaDia.CurrentAct + "_StartNew.xml";
            Logger.Log("[QuestTools] Restarting Act - loading {0}", restartActProfile);

            string profilePath = Path.Combine(Path.GetDirectoryName(ProfileManager.CurrentProfile.Path), restartActProfile);
            ProfileManager.Load(profilePath);

            return RunStatus.Success;
        }
        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        #region IEnhancedProfileBehavior

        public void Update()
        {
            UpdateBehavior();
        }

        public void Start()
        {
            OnStart();
        }

        public void Done()
        {
            _isDone = true;
        }

        #endregion
    }
}
