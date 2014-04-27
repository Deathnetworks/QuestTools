using System;
using Zeta.Bot.Navigation;
using Zeta.Bot.Profile;
using Zeta.Common;
using Zeta.Game;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools
{
    /// <summary>
    /// The default Demonbuddy MoveToTag will often never complete due to a Navigator MoveResult of PathGenerating or PathGenerationFailed.
    /// This custom behavior will fail-safe if it cannot generate a path, and will also use a point of "LocalNavDistance" away between the player and destination 
    /// as its temporary destination. Usually a distance between 150-250 is ideal for pathing, and works in situations like random dungeons and between New Tristram and anywhere in that world.
    /// </summary>
    [XmlElement("SafeMoveTo")]
    class SafeMoveTo : ProfileBehavior
    {
        private bool isDone = false;
        public override bool IsDone
        {
            get { return !IsActiveQuestStep || isDone; }
        }

        [XmlAttribute("pathPrecision")]
        public int PathPrecision { get; set; }

        [XmlAttribute("straightLinePathing")]
        public bool StraightLinePathing { get; set; }

        [XmlAttribute("useNavigator")]
        public bool UseNavigator { get; set; }

        [XmlAttribute("x")]
        public float X { get; set; }

        [XmlAttribute("y")]
        public float Y { get; set; }

        [XmlAttribute("z")]
        public float Z { get; set; }

        public Vector3 Position { get { return new Vector3(X, Y, Z); } }

        /// <summary>
        /// This is used for very distance Position coordinates; where Demonbuddy cannot make a client-side pathing request 
        /// and has to contact the server. A value too large (over 300) will sometimes cause pathing requests to fail (PathGenerationFailed).
        /// </summary>
        [XmlAttribute("localNavDistance")]
        [XmlAttribute("pathPointLimit")]
        [XmlAttribute("raycastDistance")]
        public int PathPointLimit { get; set; }

        /// <summary>
        /// This will set a time in seconds that this tag is allowed to run for
        /// </summary>
        [XmlAttribute("timeout")]
        public int Timeout { get; set; }

        [XmlAttribute("allowLongDistance")]
        public bool AllowLongDistance { get; set; }

        private Vector3 _navTarget;
        private MoveResult _LastMoveResult = default(MoveResult);
        private DateTime _TagStartTime;
        private readonly QTNavigator _QtNavigator = new QTNavigator();
        private DateTime _LastGeneratedNavPoint = DateTime.MinValue;
        private double maxNavPointAgeMs = 15000;

        public SafeMoveTo()
        {
        }

        /// <summary>
        /// Main SafeMoveTo behavior
        /// </summary>
        /// <returns></returns>
        protected override Composite CreateBehavior()
        {
            return new Sequence(
                new DecoratorContinue(ret => ZetaDia.Me.IsDead,
                    new Action(ret => RunStatus.Failure)
                ),
                new Action(ret => GameUI.SafeClickUIButtons()),
                new PrioritySelector(
                    new Decorator(ctx => _TagStartTime != DateTime.UtcNow && DateTime.UtcNow.Subtract(_TagStartTime).TotalSeconds > Timeout,
                        new Sequence(
                            new Action(ret => Logger.Log("Timeout of {0} seconds exceeded for Profile Behavior (start: {1} now: {2}) {3}", Timeout, _TagStartTime.ToLocalTime(), DateTime.Now, status())),
                            new Action(ret => isDone = true)
                        )
                    ),
                    new Decorator(ctx => !AllowLongDistance && Position.Distance2D(ZetaDia.Me.Position) > 1500,
                        new Sequence(
                            new Action(ret => Logger.Log("Error! Destination distance is {0}", Position.Distance2D(ZetaDia.Me.Position))),
                            new Action(ret => isDone = true)
                        )
                    ),
                    new Switch<MoveResult>(ret => Move(),
                        new SwitchArgument<MoveResult>(MoveResult.ReachedDestination,
                            new Sequence(
                                new Action(ret => Logger.Log("ReachedDestination! {0}", status())),
                                new Action(ret => isDone = true)
                            )
                        ),
                        new SwitchArgument<MoveResult>(MoveResult.PathGenerationFailed,
                            new Sequence(
                                new Action(ret => Logger.Log("Move Failed: {0}! {1}", ret, status())),
                                new Action(ret => isDone = true)
                            )                                
                        )
                    )                    
                )
            );
        }

        private MoveResult Move()
        {
            MoveResult moveResult = default(MoveResult);

            if (Position.Distance2D(ZetaDia.Me.Position) > PathPrecision)
            {
                _navTarget = Position;

                double timeSinceLastGenerated = DateTime.UtcNow.Subtract(_LastGeneratedNavPoint).TotalMilliseconds;
                if (Position.Distance2D(ZetaDia.Me.Position) > PathPointLimit && timeSinceLastGenerated > maxNavPointAgeMs)
                {
                    // generate a local client pathing point
                    _navTarget = MathEx.CalculatePointFrom(ZetaDia.Me.Position, Position, Position.Distance2D(ZetaDia.Me.Position) - PathPointLimit);
                }
                if (StraightLinePathing)
                {
                    // just "Click" 
                    Navigator.PlayerMover.MoveTowards(Position);
                    moveResult = MoveResult.Moved;
                }
                else
                {
                    // Use the Navigator or PathFinder
                    moveResult = _QtNavigator.MoveTo(_navTarget, status(), true);
                }
                LogStatus();

                return moveResult;
            }

            return MoveResult.ReachedDestination;
        }

        public override void OnStart()
        {
            if (PathPrecision == 0)
                PathPrecision = 15;
            if (PathPointLimit == 0)
                PathPointLimit = 250;
            if (Timeout == 0)
                Timeout = 180;

            _LastGeneratedNavPoint = DateTime.MinValue;
            _LastMoveResult = MoveResult.Moved;
            _TagStartTime = DateTime.UtcNow;

            QuestTools.PositionCache.Clear();

            Navigator.Clear();
            Logger.Log("Initialized {0}", status());
        }

        private void LogStatus()
        {
            if (QuestTools.EnableDebugLogging)
            {
                double distance = Math.Round(Position.Distance2D(ZetaDia.Me.Position) / 10.0, 0) * 10;

                Logger.Debug("Distance to target: {0:0} {1}", distance, status());
            }
        }

        /// <summary>
        /// Returns a friendly string of variables for logging purposes
        /// </summary>
        /// <returns></returns>
        private String status()
        {
            if (QuestToolsSettings.Instance.DebugEnabled)
                return String.Format("questId=\"{0}\" stepId=\"{1}\" x=\"{2}\" y=\"{3}\" z=\"{4}\" pathPrecision={5} MoveResult={6} statusText={7}",
                    ZetaDia.CurrentQuest.QuestSNO, ZetaDia.CurrentQuest.StepId, X, Y, Z, PathPrecision, _LastMoveResult, StatusText);

            return string.Empty;
        }

        public override void ResetCachedDone()
        {
            isDone = false;
            _LastGeneratedNavPoint = DateTime.MinValue;
            _LastMoveResult = MoveResult.Moved;
            _TagStartTime = DateTime.MinValue;
        }
    }
}
