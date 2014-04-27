using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Zeta.Bot;
using Zeta.Bot.Navigation;
using Zeta.Bot.Profile;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using Zeta.Game.Internals.Actors.Gizmos;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools
{
    [XmlElement("MoveToMapMarker")]
    public class MoveToMapMarker : ProfileBehavior
    {
        private bool isDone = false;
        /// <summary>
        /// Setting this to true will cause the Tree Walker to continue to the next profile tag
        /// </summary>
        public override bool IsDone
        {
            get
            {
                return !IsActiveQuestStep || isDone;
            }
        }

        /// <summary>
        /// Profile Attribute to Will interact with Actor <see cref="InteractAttempts"/> times - optionally set to -1 for no interaction
        /// </summary>
        [XmlAttribute("interactAttempts")]
        public int InteractAttempts { get; set; }

        /// <summary>
        /// Profile Attribute to The exitNameHash or hash code of the map marker you wish to find, move to, and interact with
        /// </summary>
        [XmlAttribute("exitNameHash")]
        [XmlAttribute("mapMarkerNameHash")]
        [XmlAttribute("markerNameHash")]
        [XmlAttribute("portalNameHash")]
        public int MapMarkerNameHash { get; set; }

        /// <summary>
        /// Profile Attribute to set a minimum search range for your map marker or Actor near a MiniMapMarker (if it exists) or if MaxSearchDistance is not set
        /// </summary>
        [XmlAttribute("pathPrecision")]
        public float PathPrecision { get; set; }

        [XmlAttribute("straightLinePathing")]
        public bool StraightLinePathing { get; set; }

        /// <summary>
        /// Profile Attribute to set a minimum interact range for your map marker or Actor
        /// </summary>
        [XmlAttribute("interactRange")]
        public float InteractRange { get; set; }

        /// <summary>
        /// Profile Attribute to Optionally set this to true if you're using a portal. Requires use of destinationWorldId. <seealso cref="MoveToMapMarker.DestinationWorldId"/>
        /// </summary>
        [XmlAttribute("isPortal")]
        public bool IsPortal { get; set; }

        /// <summary>
        /// Profile Attribute to Optionally set this to identify an Actor for this behavior to find, moveto, and interact with
        /// </summary>
        [XmlAttribute("actorId")]
        public int ActorId { get; set; }

        /// <summary>
        /// Set this to the destination world ID you're moving to to end this behavior
        /// </summary>
        [XmlAttribute("destinationWorldId")]
        public int DestinationWorldId { get; set; }
        /// <summary>
        /// Profile Attribute that is used for very distance Position coordinates; where Demonbuddy cannot make a client-side pathing request 
        /// and has to contact the server. A value too large (usually over 300 or so) can cause pathing requests to fail or never return in un-meshed locations.
        /// </summary>
        [XmlAttribute("pathPointLimit")]
        public int PathPointLimit { get; set; }

        [XmlAttribute("x")]
        public float X { get; set; }

        [XmlAttribute("y")]
        public float Y { get; set; }

        [XmlAttribute("z")]
        public float Z { get; set; }

        [XmlAttribute("maxSearchDistance")]
        public float MaxSearchDistance { get; set; }

        /// <summary>
        /// This is the longest time this behavior can run for. Default is 600 seconds (10 minutes).
        /// </summary>
        [XmlAttribute("timeoutSeconds")]
        public int TimeoutSeconds { get; set; }

        private Vector3 position;
        /// <summary>
        /// This is the calculated position from X,Y,Z
        /// </summary>
        public Vector3 Position
        {
            get
            {
                if (position == Vector3.Zero)
                    position = new Vector3(X, Y, Z);
                return position;
            }
        }

        private bool clientNavFailed = false;
        private bool initialized = false;

        private int timeoutSecondsDefault = 600;
        private int completedInteractAttempts = 0;
        private int currentStuckCount = 0;
        private int maxStuckCountSeconds = 30;
        private int maxStuckRange = 15;
        private int startWorldId = -1;

        private Vector3 lastPosition = Vector3.Zero;
        private DateTime behaviorStartTime = DateTime.MinValue;
        private DateTime stuckStart = DateTime.MinValue;
        private DateTime lastCheckedStuck = DateTime.MinValue;

        private MinimapMarker miniMapMarker;
        private DiaObject actor;

        private MoveResult lastMoveResult = MoveResult.Moved;

        /// <summary>
        /// The last seen position of the minimap marker, as it can disappear if you stand on it
        /// </summary>
        private Vector3 mapMarkerLastPosition = new Vector3();
        private Point my2DPoint = Point.Empty;
        private Point destination2DPoint = Point.Empty;

        public override void OnStart()
        {
            // set defaults
            if (PathPrecision == 0)
                PathPrecision = 20;
            if (PathPointLimit == 0)
                PathPointLimit = 250;
            if (InteractRange == 0)
                InteractRange = 10;
            if (InteractAttempts == 0)
                InteractAttempts = 5;
            if (TimeoutSeconds == 0)
                TimeoutSeconds = timeoutSecondsDefault;
            if (behaviorStartTime == DateTime.MinValue)
                behaviorStartTime = DateTime.UtcNow;
            if (MaxSearchDistance <= 0)
                MaxSearchDistance = 10;

            lastPosition = Vector3.Zero;
            lastInteractTime = DateTime.MinValue;
            stuckStart = DateTime.UtcNow;
            lastCheckedStuck = DateTime.UtcNow;
            lastMoveResult = MoveResult.Moved;
            completedInteractAttempts = 0;
            startWorldId = ZetaDia.CurrentWorldId;

            initialized = true;

            Navigator.Clear();
            Logger.Debug("Initialized {0}", Status());
        }

        /// <summary>
        /// Main MoveToMapMarker Behavior
        /// </summary>
        /// <returns></returns>
        protected override Composite CreateBehavior()
        {
            return
            new Sequence(
                new DecoratorContinue(ret => ZetaDia.Me.IsDead || ZetaDia.IsLoadingWorld,
                    new Sequence(
                      new Action(ret => Logger.Log("IsDead={0} IsLoadingWorld={1}", ZetaDia.Me.IsDead, ZetaDia.IsLoadingWorld)),
                      new Action(ret => { return RunStatus.Failure; })
                   )
               ),
                new DecoratorContinue(ret => lastMoveResult == MoveResult.ReachedDestination && actor == null,
                    new Sequence(
                        new Action(ret => Logger.Log("{0}, finished!", lastMoveResult)),
                        new Action(ret => isDone = true),
                        new Action(ret => { return RunStatus.Failure; })
                    )
                ),
                CheckTimeout(),
                new Action(ret => FindMiniMapMarker()),
                new DecoratorContinue(ret => mapMarkerLastPosition != Vector3.Zero,
                    new Action(ret => RefreshActorInfo())
                ),
                new DecoratorContinue(ret => actor == null && miniMapMarker == null && Position == Vector3.Zero,
                    new Sequence(
                        new Action(ret => miniMapMarker = GetRiftExitMarker())
                    )
                ),
                new DecoratorContinue(ret => actor == null && miniMapMarker == null && Position == Vector3.Zero,
                    new Sequence(
                        new Action(ret => Logger.Debug("Error: Could not find MiniMapMarker nor PortalObject nor Position {0}", Status())),
                        new Action(ret => isDone = true)
                    )
                ),
                new Sequence(
                    new Action(ret => GameUI.SafeClickUIButtons()),
                    new PrioritySelector(
                        new Decorator(ret => GameUI.IsPartyDialogVisible,
                            new Action(ret => Logger.Log("Party Dialog is visible"))
                        ),
                        new Decorator(ret => GameUI.IsElementVisible(GameUI.GenericOK),
                            new Action(ret => Logger.Log("Generic OK is visible"))
                        ),
                        new Decorator(ret => DestinationWorldId == -1 && ZetaDia.CurrentWorldId != startWorldId,
                            new Sequence(
                                new Action(ret => Logger.Log("World changed ({0} to {1}), destinationWorlId={2}, finished {3}",
                                    startWorldId, ZetaDia.CurrentWorldId, DestinationWorldId, Status())),
                                new Action(ret => isDone = true)
                            )
                        ),
                        new Decorator(ret => DestinationWorldId != 0 && ZetaDia.CurrentWorldId == DestinationWorldId,
                            new Sequence(
                                new Action(ret => Logger.Log("DestinationWorlId matched, finished {0}", Status())),
                                new Action(ret => isDone = true)
                            )
                        ),
                        new Decorator(ret => completedInteractAttempts > 1 && lastPosition.Distance(ZetaDia.Me.Position) > 4f,
                            new Sequence(
                                new Action(ret => isDone = true),
                                new Action(ret => Logger.Log("Moved {0:0} yards after interaction, finished {1}", lastPosition.Distance(ZetaDia.Me.Position), Status()))
                            )
                        ),
                        new Decorator(ret => actor != null && actor.IsValid,
                            new PrioritySelector(
                                MoveToActorOutsideRange(),
                                UseActorIfInRange()
                            )
                        ),
                        new Decorator(ret => miniMapMarker != null && actor == null,
                            new PrioritySelector(
                                MoveToMapMarkerOnly(),
                                MoveToMapMarkerSuccess()
                            )
                        ),
                        MoveToPosition()
                    )
                )
            );
        }

        private static MinimapMarker GetRiftExitMarker()
        {
            int index = DataDictionary.RiftWorldIds.IndexOf(ZetaDia.CurrentWorldId);
            if (index == -1)
                return null;

            return ZetaDia.Minimap.Markers.CurrentWorldMarkers
                .OrderBy(m => m.Position.Distance2D(ZetaDia.Me.Position))
                .FirstOrDefault(m => m.NameHash == DataDictionary.RiftPortalHashes[index]);
        }

        private void FindMiniMapMarker()
        {
            // Special condition for Rift portals
            if (MapMarkerNameHash == 0 && Position == Vector3.Zero && ActorId == 0 && IsPortal && DestinationWorldId == -1)
            {
                miniMapMarker = GetRiftExitMarker();
                if (miniMapMarker != null)
                {
                    MapMarkerNameHash = miniMapMarker.NameHash;
                    Logger.Log("Using Rift Style Minimap Marker: {0} dist: {1:0} isExit: {2}",
                        miniMapMarker.NameHash,
                        miniMapMarker.Position.Distance2D(ZetaDia.Me.Position),
                        miniMapMarker.IsPortalExit);
                }
            }

            // find our map marker
            if (miniMapMarker == null)
            {
                if (Position != Vector3.Zero)
                {
                    miniMapMarker = ZetaDia.Minimap.Markers.CurrentWorldMarkers
                        .Where(marker => marker != null && marker.NameHash == MapMarkerNameHash &&
                            Position.Distance(marker.Position) < MaxSearchDistance)
                        .OrderBy(o => o.Position.Distance(ZetaDia.Me.Position)).FirstOrDefault();
                }
                else
                {
                    miniMapMarker = ZetaDia.Minimap.Markers.CurrentWorldMarkers
                        .Where(marker => marker != null && marker.NameHash == MapMarkerNameHash)
                        .OrderBy(o => o.Position.Distance(ZetaDia.Me.Position)).FirstOrDefault();
                }
            }
            if (miniMapMarker != null && miniMapMarker.Position != Vector3.Zero)
            {
                mapMarkerLastPosition = miniMapMarker.Position;
            }

        }
        private PrioritySelector CheckStuck()
        {
            return new PrioritySelector(
                new Decorator(ret => currentStuckCount > 0 && DateTime.UtcNow.Subtract(stuckStart).TotalSeconds > maxStuckCountSeconds,
                    new Action(delegate
                    {
                        Logger.Debug("Looks like we're stuck since it's been {0} seconds stuck... finishing", DateTime.UtcNow.Subtract(stuckStart).TotalSeconds);
                        isDone = true;
                        return RunStatus.Success;
                    })
                ),
                new Decorator(ret => DateTime.UtcNow.Subtract(lastCheckedStuck).TotalMilliseconds < 500,
                    new Action(delegate
                        {
                            return RunStatus.Success;
                        }
                    )
                ),
                new Decorator(ret => ZetaDia.Me.Position.Distance(lastPosition) < maxStuckRange,
                    new Action(delegate
                    {
                        currentStuckCount++;
                        lastCheckedStuck = DateTime.UtcNow;
                        lastPosition = ZetaDia.Me.Position;
                        if (currentStuckCount > DateTime.UtcNow.Subtract(stuckStart).TotalSeconds * .5)
                            clientNavFailed = true;

                        if (QuestTools.EnableDebugLogging)
                        {
                            Logger.Debug("Stuck count: {0}", currentStuckCount);
                        }
                        return RunStatus.Success;
                    })
                ),
                new Decorator(ret => ZetaDia.Me.Position.Distance(lastPosition) > maxStuckRange,
                    new Action(delegate
                    {
                        currentStuckCount = 0;
                        lastCheckedStuck = DateTime.UtcNow;
                        lastPosition = ZetaDia.Me.Position;

                        return RunStatus.Success;
                    })
                ),
                new Action(delegate
                    {
                        lastPosition = ZetaDia.Me.Position;
                        return RunStatus.Success;
                    }
                )
            );
        }
        private DecoratorContinue CheckTimeout()
        {
            return
            new DecoratorContinue(ret => Math.Abs(DateTime.UtcNow.Subtract(behaviorStartTime).TotalSeconds) > TimeoutSeconds,
                new Sequence(
                    new Action(ret => isDone = true),
                    new Action(ret => Logger.Log("Timeout of {0} seconds exceeded in current behavior", TimeoutSeconds)),
                    new Action(ret => { return RunStatus.Failure; })
                )
            );
        }

        private Decorator MoveToMapMarkerSuccess()
        {
            return // moved to map marker successfully
            new Decorator(ret => miniMapMarker != null && miniMapMarker.Position.Distance(ZetaDia.Me.Position) < PathPrecision,
                new Action(delegate
                    {
                        Logger.Debug("Successfully Moved to Map Marker {0}, distance: {1} {2}", miniMapMarker.NameHash, miniMapMarker.Position.Distance(ZetaDia.Me.Position), Status());
                        isDone = true;
                        return RunStatus.Success;
                    }
                )
            );
        }
        private void RefreshActorInfo()
        {
            Vector3 myPos = ZetaDia.Me.Position;

            if ((actor == null || (actor != null && !actor.IsValid)) && ActorId != 0)
            {
                actor = ZetaDia.Actors.GetActorsOfType<DiaObject>(true, false)
                    .Where(o => o.IsValid && o.ActorSNO == ActorId && ActorWithinRangeOfMarker(o))
                    .OrderBy(o => DistanceToMapMarker(o))
                    .FirstOrDefault();
            }
            if (actor != null && actor.IsValid)
            {
                if (QuestTools.EnableDebugLogging)
                {
                    Logger.Debug("Found actor {0} {1} {2} of distance {3} from point {4}",
                                        actor.ActorSNO, actor.Name, actor.ActorType, actor.Position.Distance(mapMarkerLastPosition), mapMarkerLastPosition);
                }
            }
            else if (ActorId != 0 && Position != Vector3.Zero && position.Distance(ZetaDia.Me.Position) <= PathPrecision)
            {
                actor = ZetaDia.Actors.GetActorsOfType<DiaObject>(true, false).Where(o => o != null && o.IsValid && o.ActorSNO == ActorId
                   && o.Position.Distance2D(Position) <= PathPrecision).OrderBy(o => o.Distance).FirstOrDefault();
            }
            else if (ActorId != 0 && mapMarkerLastPosition != Vector3.Zero && mapMarkerLastPosition.Distance(myPos) <= PathPrecision)
            {
                // No ActorID defined, using Marker position to find actor
                actor = ZetaDia.Actors.GetActorsOfType<DiaObject>(true, false).Where(o => o != null && o.IsValid && o.ActorSNO == ActorId
                   && o.Position.Distance2D(mapMarkerLastPosition) <= PathPrecision).OrderBy(o => o.Distance).FirstOrDefault();
            }
            else if (ActorId == 0 && mapMarkerLastPosition != Vector3.Zero && mapMarkerLastPosition.Distance2D(myPos) <= 90)
            {
                actor = ZetaDia.Actors.GetActorsOfType<DiaObject>(true, false)
                    .Where(a => a.Position.Distance2D(mapMarkerLastPosition) <= MaxSearchDistance)
                    .OrderBy(a => a.Position.Distance2D(mapMarkerLastPosition)).FirstOrDefault();

                if (actor != null)
                {
                    InteractRange = actor.CollisionSphere.Radius;
                    Logger.Debug("Found Actor from Map Marker! mapMarkerPos={0} actor={1} {2} {3} {4}",
                        mapMarkerLastPosition, actor.ActorSNO, actor.Name, actor.ActorType, actor.Position);
                }
            }
            else if (mapMarkerLastPosition.Distance(ZetaDia.Me.Position) < PathPrecision)
            {
                if (QuestTools.EnableDebugLogging)
                {
                    Logger.Debug("Could not find an actor {0} within range {1} from point {2}",
                                       ActorId, PathPrecision, mapMarkerLastPosition);
                }
            }

            if (actor != null && actor is GizmoPortal && !IsPortal)
            {
                IsPortal = true;
            }
        }

        private float DistanceToMapMarker(DiaObject o)
        {
            return o.Position.Distance(miniMapMarker.Position);
        }

        private bool ActorWithinRangeOfMarker(DiaObject o)
        {
            bool test = false;

            if (o != null && o.Position != null && miniMapMarker != null && miniMapMarker.Position != null)
            {
                test = o.Position.Distance(miniMapMarker.Position) <= PathPrecision;
            }
            return test;
        }

        private Decorator MoveToActorOutsideRange()
        {
            return // move to the actor if defined and outside of InteractRange
            new Decorator(ret => actor.Position.Distance2D(ZetaDia.Me.Position) > InteractRange,
                new Action(
                    delegate
                    {
                        Logger.Debug("Moving to actor {0}, distance: {1} {2}", actor.ActorSNO, actor.Position.Distance(ZetaDia.Me.Position), Status());
                        if (!Move(actor.Position))
                        {
                            Logger.Debug("Move result failed, we're done {0}", Status());
                            isDone = true;
                            return RunStatus.Failure;
                        }

                        return RunStatus.Success;
                    }
                )
            );
        }

        private DateTime lastInteractTime = DateTime.MinValue;
        private Decorator UseActorIfInRange()
        {
            return // use the actor if defined and within range
            new Wait(2, ret => actor.Position.Distance(ZetaDia.Me.Position) <= InteractRange && InteractAttempts > -1 && (completedInteractAttempts < InteractAttempts || IsPortal),
                new Sequence(
                    new Action(ret => lastPosition = ZetaDia.Me.Position),
                    new Action(ret => actor.Interact()),
                    new Action(ret => completedInteractAttempts++),
                    new DecoratorContinue(ret => QuestTools.EnableDebugLogging,
                        new Action(ret => Logger.Debug("Interacting with portal object {0}, result: {1}", actor.ActorSNO, Status()))
                    ),
                    new Sleep(500),
                    new Action(ret => GameEvents.FireWorldTransferStart())
                )
            );
        }

        private Decorator MoveToMapMarkerOnly()
        {
            return // just move to the map marker
            new Decorator(ret => miniMapMarker != null && miniMapMarker.Position.Distance(ZetaDia.Me.Position) > PathPrecision,
                new Action(delegate
                    {
                        bool success = false;
                        success = Move(miniMapMarker.Position, String.Format("Minimap Marker {0}", miniMapMarker.NameHash));

                        if (!success)
                        {
                            Navigator.Clear();
                        }
                        else
                        {
                            if (QuestTools.EnableDebugLogging)
                            {
                                Logger.Debug("Moving to Map Marker {0}, distance: {1:0} {2}", miniMapMarker.NameHash, miniMapMarker.Position.Distance(ZetaDia.Me.Position), Status());
                            }
                        }

                        return RunStatus.Success;
                    }
                )
            );
        }

        private Decorator MoveToPosition()
        {
            return //Position defined only - can't find map marker nor actor
            new Decorator(ret => miniMapMarker == null && Position != Vector3.Zero,
                new Action(delegate
                    {
                        bool moveStatus = false;

                        if (Position.Distance(ZetaDia.Me.Position) > PathPrecision)
                        {
                            moveStatus = Move(Position, "Position only");
                        }
                        else
                        {
                            Logger.Debug("Position Defined only - Within {0} of destination {1}", PathPrecision, Position);
                            isDone = true;
                        }
                        if (!moveStatus)
                        {
                            Logger.Debug("Movement failed to position {0}", Position);
                            //isDone = true;
                        }
                        return RunStatus.Success;
                    }
                )
            );
        }

        /// <summary>
        /// Move without a destination name, see <seealso cref="MoveToMapMarker.Move"/>
        /// </summary>
        /// <param name="newpos"></param>
        /// <returns></returns>
        private bool Move(Vector3 newpos)
        {
            return Move(newpos, null);
        }

        List<Vector3> allPoints = new List<Vector3>();
        List<Vector3> validPoints = new List<Vector3>();
        private QTNavigator QTNavigator = new QTNavigator();

        /// <summary>
        /// Safely Moves the player to the requested destination <seealso cref="MoveToMapMarker.PathPointLimit"/>
        /// </summary>
        /// <param name="newpos">Vector3 of the new position</param>
        /// <param name="destinationName">For logging purposes</param>
        /// <returns></returns>
        private bool Move(Vector3 newpos, string destinationName = "")
        {
            bool result = false;

            if (StraightLinePathing)
            {
                Navigator.PlayerMover.MoveTowards(newpos);
                lastMoveResult = MoveResult.Moved;
                result = true;
            }

            if (!ZetaDia.WorldInfo.IsGenerated)
            {
                if (clientNavFailed && PathPointLimit > 20)
                {
                    PathPointLimit = PathPointLimit - 10;
                }
                else if (clientNavFailed && PathPointLimit <= 20)
                {
                    PathPointLimit = 250;
                }

                if (newpos.Distance(ZetaDia.Me.Position) > PathPointLimit)
                {
                    newpos = MathEx.CalculatePointFrom(ZetaDia.Me.Position, newpos, newpos.Distance(ZetaDia.Me.Position) - PathPointLimit);
                }
            }
            float destinationDistance = newpos.Distance(ZetaDia.Me.Position);

            lastMoveResult = QTNavigator.MoveTo(newpos, destinationName + String.Format(" distance={0:0}", destinationDistance), true);

            switch (lastMoveResult)
            {
                case MoveResult.Moved:
                case MoveResult.ReachedDestination:
                case MoveResult.UnstuckAttempt:
                    clientNavFailed = false;
                    result = true;
                    break;
                case MoveResult.PathGenerated:
                case MoveResult.PathGenerating:
                case MoveResult.PathGenerationFailed:
                case MoveResult.Failed:
                    Navigator.PlayerMover.MoveTowards(Position);
                    result = false;
                    clientNavFailed = true;
                    break;
            }

            if (QuestTools.EnableDebugLogging)
            {
                Logger.Debug("MoveResult: {0}, newpos={1} Distance={2}, destinationName={3}",
                    lastMoveResult.ToString(), newpos, newpos.Distance(ZetaDia.Me.Position), destinationName);
            }
            return result;
        }

        public bool isValid()
        {
            try
            {
                if (!ZetaDia.IsInGame || ZetaDia.IsLoadingWorld)
                    return false;

                // check if everything we need here is safe to use
                if (ZetaDia.Me != null && ZetaDia.Me.IsValid &&
                    ZetaDia.Me.CommonData != null && ZetaDia.Me.CommonData.IsValid)
                    return true;
            }
            catch
            {
            }
            return false;
        }

        public String Status()
        {
            string extraInfo = "";
            if (DataDictionary.RiftWorldIds.Contains(ZetaDia.CurrentWorldId))
                extraInfo += "IsRift ";

            if (QuestToolsSettings.Instance.DebugEnabled)
                return String.Format("questId={0} stepId={1} actorId={2} exitNameHash={3} isPortal={4} destinationWorldId={5} " +
                    "pathPointLimit={6} interactAttempts={7} interactRange={8} pathPrecision={9} x=\"{10}\" y=\"{11}\" z=\"{12}\" " + extraInfo,
                    this.QuestId, this.StepId, this.ActorId, this.MapMarkerNameHash, this.IsPortal, this.DestinationWorldId,
                    this.PathPointLimit, this.InteractAttempts, this.InteractRange, this.PathPrecision, this.X, this.Y, this.Z
                    );

            return string.Empty;
        }

        public override void ResetCachedDone()
        {
            isDone = false;
            initialized = false;
            base.ResetCachedDone();
        }

    }
}
