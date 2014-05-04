using Zeta.Bot.Profile;
using Zeta.XmlEngine;

namespace QuestTools.ProfileTags.Depreciated
{
    // TrinityInteract attempts a blind object-use of an SNO without movement
    [XmlElement("TrinityInteract")]
    public class TrinityInteract : ProfileBehavior
    {
        public TrinityInteract() { }

        public override void OnStart()
        {
            Logger.LogError("TrinityInteract is depreciated. Use MoveToActor instead.");
            _isDone = true;
            base.OnStart();
        }

        private bool _isDone;

        public override bool IsDone
        {
            get { return _isDone; }
        }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }
    }
}
