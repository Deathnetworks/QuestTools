﻿using System;
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
    /// XML tag for a profile to START a timer
    /// </summary>
    [XmlElement("StartTimer")]
    public class StartTimerTag : ProfileBehavior
    {
        public StartTimerTag() { }
        private bool _isDone;
        public override bool IsDone { get { return _isDone; } }

        /// <summary>
        /// The unique Identifier for this timer, used to identify what the timer is in the reports
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// The group that this timer belongs to, useful for stopping multiple timers at once.
        /// </summary>
        [XmlAttribute("group")]
        public string Group { get; set; }

        protected override Composite CreateBehavior()
        {
            var quest = ZetaDia.ActInfo.AllQuests.FirstOrDefault(q => q.QuestSNO == QuestId);

            return new Sequence(
                new Action(ret => TimeTracker.Start(new Timing
                {
                    Name = Name,
                    StartTime = DateTime.UtcNow,
                    Group = Group,
                    QuestIsBounty = (quest != null) && quest.QuestType == QuestType.Bounty,
                    QuestName = (quest != null) ? quest.Quest.ToString() : string.Empty,
                    QuestId = (quest != null) ? quest.QuestSNO : -1,
                })),
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
