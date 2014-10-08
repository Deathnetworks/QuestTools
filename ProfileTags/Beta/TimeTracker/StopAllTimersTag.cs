using System;
using System.Linq;
using Zeta.Bot.Profile;
using Zeta.Game;
using Zeta.Game.Internals.SNO;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools.ProfileTags.Beta
{
    /// <summary>
    /// Stops all timers.
    /// </summary>
    [XmlElement("StopAllTimers")]
    public class StopAllTimersTag : ProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone { get { return _isDone; } }

        protected override Composite CreateBehavior()
        {
            return new Sequence(
                new Action(ret => TimeTracker.StopAll()),
                new Action(ret => _isDone = true)
            );
        }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }
    }
}
