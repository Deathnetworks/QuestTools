using System;
using System.Linq;
using Zeta.Bot;
using Zeta.Bot.Profile;
using Zeta.Game;
using Zeta.Game.Internals.Actors;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools
{
    [XmlElement("ResumeUseTownPortal")]
    public class ResumeUseTownPortalTag : ProfileBehavior
    {
        private bool _isDone = false;
        public override bool IsDone { get { return _isDone; } }

        [XmlAttribute("forceUsePortal")]
        public bool ForceUsePortal { get; set; }

        [XmlAttribute("timeLimit")]
        public int TimeLimit { get; set; }

        public override void OnStart()
        {
            if (TimeLimit == 0)
                TimeLimit = 30;

            Logger.Log("ResumeUseTownPortal initialized");
        }

        protected override Composite CreateBehavior()
        {

            return
            new PrioritySelector(
                new Decorator(ret => !ZetaDia.IsInTown,
                    new Action(ret => _isDone = true)
                ),
                new Decorator(ret => DateTime.UtcNow.Subtract(QuestTools.LastJoinedGame).TotalSeconds > TimeLimit && !ForceUsePortal,
                    new Action(ret => ResumeWindowBreached())
                ),
                new Decorator(ret => DateTime.UtcNow.Subtract(QuestTools.LastJoinedGame).TotalSeconds <= TimeLimit || ForceUsePortal,
                    new Decorator(ret => IsTownPortalNearby,
                        new Sequence(
                            new Action(ret => Logger.Log("Taking town portal back")),
                            Zeta.Bot.CommonBehaviors.TakeTownPortalBack(true),
                            new Sleep(500),
                            new Action(ret => GameEvents.FireWorldTransferStart())
                        )
                    )
                ),
                new Action(ret => _isDone = true)
            );
        }

        private bool IsTownPortalNearby
        {
            get { return ZetaDia.Actors.GetActorsOfType<DiaObject>(true, false).Where(o => o.ActorSNO == 191492).Any(); }
        }

        private void ResumeWindowBreached()
        {
            Logger.Log("ResumeUseTownPortal resume window breached, tag finished (no action taken)");
            _isDone = true;
        }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }
    }
}
