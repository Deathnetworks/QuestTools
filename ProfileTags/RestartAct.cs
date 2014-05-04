using System;
using System.IO;
using Zeta.Bot;
using Zeta.Bot.Profile;
using Zeta.Game;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools.ProfileTags
{
    [XmlElement("RestartAct")]
    public class RestartActTag : ProfileBehavior
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

        private RunStatus ForceRestartAct()
        {
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
                case Act.A5: act = "Act5";
                    break;
            }

            string restartActProfile = String.Format("{0}_StartNew.xml", act);
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
    }
}
