using QuestTools.Helpers;
using QuestTools.ProfileTags.Complex;
using Zeta.Bot.Profile;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace QuestTools.ProfileTags
{
    [XmlElement("TrinitySetQuesting")]
    [XmlElement("SetQuesting")]
    public class SetQuestingTag : ProfileBehavior, IEnhancedProfileBehavior
    {
        public SetQuestingTag() { }
        private bool _isDone;

        public override bool IsDone
        {
            get { return _isDone; }
        }

        protected override Composite CreateBehavior()
        {
            return new Action(ret =>
            {
                Logger.Log("Setting Trinity Combat mode as QUESTING for the current profile.");
                if (!TrinityApi.SetProperty("Trinity.Combat.Abilities.CombatBase", "IsQuestingMode", true))
                {
                    Logger.Error("Unable to set IsQuestingMode Property!");
                }
                _isDone = true;
            });
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
