﻿using QuestTools.Helpers;
using QuestTools.ProfileTags.Complex;
using Zeta.Bot.Profile;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace QuestTools.ProfileTags
{
    [XmlElement("TrinitySetQuesting")]
    [XmlElement("SetQuesting")]
    public class SetQuestingTag : ProfileBehavior, IAsyncProfileBehavior
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
                TrinityApi.SetProperty("CombatBase", "IsQuestingMode", true); // CombatBase.IsQuestingMode = true;
                Logger.Log("Setting Trinity Combat mode as QUESTING for the current profile.");
                _isDone = true;
            });
        }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }

        #region IAsyncProfileBehavior

        public void AsyncUpdateBehavior()
        {
            UpdateBehavior();
        }

        public void AsyncOnStart()
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
