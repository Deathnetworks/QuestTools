using Zeta.Bot.Profile;
using Zeta.XmlEngine;

namespace QuestTools.ProfileTags.Depreciated
{
    [XmlElement("TrinityMoveTo")]
    public class TrinityMoveTo : ProfileBehavior
    {
        public TrinityMoveTo() { }

        private bool _isDone;
        public override bool IsDone { get { return _isDone; }
        }
        public override void OnStart()
        {
            Logger.LogError("TrinityMoveTo is depreciated. Use MoveTo or SafeMoveTo instead.");
            base.OnStart();
        }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }
    }
}
