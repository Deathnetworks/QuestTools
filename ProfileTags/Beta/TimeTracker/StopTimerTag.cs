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
    /// XML tag for a profile to STOP a timer
    /// </summary>
    [XmlElement("StopTimer")]
    public class StopTimerTag : ProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone { get { return _isDone; } }

        /// <summary>
        /// Specifying a value for name="" will the timer with that name to be stopped.
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// Specifying a value for group="" will cause all timers with that group name to be stopped.
        /// </summary>
        [XmlAttribute("group")]
        public string Group { get; set; }

        protected override Composite CreateBehavior()
        {
            return new Sequence(
                new PrioritySelector(

                    new Decorator(ret => Name != null,
                        new Action(ret => TimeTracker.StopTimer(Name))),

                    new Decorator(ret => Group != null,
                        new Action(ret => TimeTracker.StopGroup(Group)))

                ),
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
