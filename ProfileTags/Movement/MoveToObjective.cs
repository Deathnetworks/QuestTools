using System;
using System.Collections.Generic;
using System.Linq;
using QuestTools.Navigation;
using QuestTools.ProfileTags.Complex;
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

namespace QuestTools.ProfileTags.Movement
{
    [XmlElement("MoveToObjective")]
    public class MoveToObjective : ProfileBehavior, IAsyncProfileBehavior
    {
        public MoveToObjective() { }
        private bool _isDone;
        /// <summary>
        /// Setting this to true will cause the Tree Walker to continue to the next profile tag
        /// </summary>
        public override bool IsDone { get { return !IsActiveQuestStep || _isDone; } }

        /// <summary>
        /// Profile Attribute to Will interact with Actor <see cref="InteractAttempts"/> times - optionally set to -1 for no interaction
        /// </summary>
        [XmlAttribute("interactAttempts")]
        public int InteractAttempts { get; set; }

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

        private Vector3 _position;
        /// <summary>
        /// This is the calculated position from X,Y,Z
        /// </summary>
        public Vector3 Position
        {
            get
            {
                if (_position == Vector3.Zero)
                    _position = new Vector3(X, Y, Z);
                return _position;
            }
        }

        private bool _clientNavFailed;

        private int _completedInteractAttempts;
        private int _startWorldId = -1;

        private MinimapMarker _miniMapMarker;
        private DiaObject _objectiveObject;

        private MoveResult _lastMoveResult = MoveResult.Moved;

        /// <summary>
        /// The last seen position of the minimap marker, as it can disappear if you stand on it
        /// </summary>
        private Vector3 _mapMarkerLastPosition;

        public override void OnStart()
        {
            // set defaults
            if (Math.Abs(PathPrecision) < 1f)
                PathPrecision = 20;
            if (PathPointLimit == 0)
                PathPointLimit = 250;
            if (Math.Abs(InteractRange) < 1f)
                InteractRange = 10;
            if (InteractAttempts == 0)
                InteractAttempts = 5;

            _lastMoveResult = MoveResult.Moved;
            _completedInteractAttempts = 0;
            _startWorldId = ZetaDia.CurrentWorldId;

            Navigator.Clear();
            Logger.Debug("Initialized {0}", Status());
        }

        /// <summary>
        /// Main MoveToObjective Behavior
        /// </summary>
        /// <returns></returns>
        protected override Composite CreateBehavior()
        {
            return
            new Sequence(
                new DecoratorContinue(ret => ZetaDia.Me.IsDead || ZetaDia.IsLoadingWorld,
                    new Sequence(
                      new Action(ret => Logger.Log("IsDead={0} IsLoadingWorld={1}", ZetaDia.Me.IsDead, ZetaDia.IsLoadingWorld)),
                      new Action(ret => RunStatus.Failure)
                   )
               ),
                new DecoratorContinue(ret => _lastMoveResult == MoveResult.ReachedDestination && _objectiveObject == null,
                    new Sequence(
                        new Action(ret => Logger.Log("ReachedDestination, no Objective found - finished!")),
                        new Action(ret => _isDone = true),
                        new Action(ret => RunStatus.Failure)
                    )
                ),
                new Action(ret => FindObjectiveMarker()),
                new DecoratorContinue(ret => _mapMarkerLastPosition != Vector3.Zero,
                    new Action(ret => RefreshActorInfo())
                ),
                new DecoratorContinue(ret => _objectiveObject == null && _miniMapMarker == null && Position == Vector3.Zero,
                    new Sequence(
                        new Action(ret => Logger.Log("Error: Could not find Objective Marker! {0}", Status())),
                        new Action(ret => _isDone = true)
                    )
                ),
                new Sequence(
                    new PrioritySelector(
                        new Decorator(ret => ZetaDia.CurrentWorldId != _startWorldId,
                            new Sequence(
                                new Action(ret => Logger.Log("World changed from {0} to {1}, finished {2}", _startWorldId, ZetaDia.CurrentWorldId, Status())),
                                new Action(ret => _isDone = true)
                            )
                        ),
                        new Decorator(ret => IsValidObjective() && _objectiveObject is DiaUnit && _objectiveObject.Position.Distance(ZetaDia.Me.Position) <= PathPrecision,
                            new Sequence(
                                new Action(ret => Logger.Log("We found the objective and its a monster, ending tag so we can kill it. {0}", Status())),
                                new Action(ret => _isDone = true)
                            )
                        ),
                        new Decorator(ret => _objectiveObject != null && _objectiveObject.IsValid,
                            new PrioritySelector(
                                new Decorator(ret => _lastMoveResult != MoveResult.ReachedDestination,
                                    new Sequence(
                                        new Action(ret => Logger.Debug("Moving to actor {0} {1}", _objectiveObject.ActorSNO, Status())),
                                        new Action(ret => _lastMoveResult = Navigator.MoveTo(_objectiveObject.Position))
                                   )
                                ),
                                new Decorator(ret => _objectiveObject is GizmoPortal,
                                    new PrioritySelector(
                                        new Decorator(ret => _lastMoveResult == MoveResult.ReachedDestination && _objectiveObject.Distance > InteractRange,
                                            new Sequence(
                                                new Action(ret => Logger.Log("ReachedDestination but not within InteractRange, finished")),
                                                new Action(ret => _isDone = true)
                                            )
                                        ),
                                        new Decorator(ret => GameUI.PartyLeaderBossAccept.IsVisible || GameUI.PartyFollowerBossAccept.IsVisible,
                                            new Action(ret=> Logger.Debug("Party Boss Button visible"))),
                                        new Decorator(ret => ZetaDia.Me.Movement.IsMoving,
                                            new Action(ret => CommonBehaviors.MoveStop())),
                                        new Sequence(
                                            new Action(ret => _objectiveObject.Interact()),
                                            new Action(ret => _completedInteractAttempts++),
                                            new Action(ret => Logger.Debug("Interacting with portal object {0}, result: {1}", _objectiveObject.ActorSNO, Status())),
                                            new Sleep(500),
                                            new Action(ret => GameEvents.FireWorldTransferStart())
                                        )
                                    )
                                )
                            )
                        ),
                        new Decorator(ret => _miniMapMarker != null && _objectiveObject == null,
                            new PrioritySelector(
                                new Decorator(ret => _miniMapMarker != null && _miniMapMarker.Position.Distance(ZetaDia.Me.Position) > PathPrecision,
                                    new Sequence(
                                        new Action(ret => Logger.Debug("Moving to Objective Marker {0}, {1}", _miniMapMarker.NameHash, Status())),
                                        new Action(ret => _lastMoveResult = Navigator.MoveTo(_miniMapMarker.Position))
                                    )
                                ),
                                new Decorator(ret => _miniMapMarker != null && _miniMapMarker.Position.Distance(ZetaDia.Me.Position) < PathPrecision,
                                    new Sequence(
                                        new Action(ret => Logger.Debug("Successfully Moved to Objective Marker {0}, {1}", _miniMapMarker.NameHash, Status())),
                                        new Action(ret => _isDone = true)
                                    )
                                )
                            )
                        ),
                        new Decorator(ret => _miniMapMarker == null && Position != Vector3.Zero && Position.Distance(ZetaDia.Me.Position) > PathPrecision,
                            new Sequence(
                                new Action(ret => _lastMoveResult = Navigator.MoveTo(Position)),
                                new DecoratorContinue(ret => _lastMoveResult == MoveResult.ReachedDestination,
                                    new Sequence(
                                        new Action(ret => Logger.Log("ReachedDestination of Position, minimap marker not found, finished.")),
                                        new Action(ret => _isDone = true)
                                    )
                                )
                            )
                        ),
                        new Action(ret =>
                            Logger.LogError("MoveToObjective Error: marker={0} actor={1}/{2} completedInteracts={3} isPortal={4} dist={5} interactRange={6}",
                            _miniMapMarker != null,
                            _objectiveObject != null,
                            (_objectiveObject != null && _objectiveObject.IsValid),
                            _completedInteractAttempts,
                            IsPortal,
                            (_objectiveObject != null && _objectiveObject.IsValid ? _objectiveObject.Position.Distance(ZetaDia.Me.Position) : 0f),
                            InteractRange)
                        )
                    )
                )
            );
        }

        private bool IsObjectiveInRange()
        {
            if (_miniMapMarker != null && _miniMapMarker.Position.Distance2D(ZetaDia.Me.Position) < PathPrecision)
            {
                return true;
            }
            return false;
        }

        private bool IsValidObjective()
        {
            try
            {
                return (_objectiveObject != null &&  _objectiveObject.IsValid &&
                    _objectiveObject.CommonData.GetAttribute<int>(ActorAttributeType.BountyObjective) > 0) || _objectiveObject is GizmoPortal;
            }
            catch (Exception ex)
            {
                Logger.Log("Exception in IsValidObjective(), {0}", ex);
            }
            return false;
        }

        private void FindObjectiveMarker()
        {
            // Get Special objective Marker
            _miniMapMarker = ZetaDia.Minimap.Markers.CurrentWorldMarkers
                .Where(m => m.IsPointOfInterest && m.Id < 1000)
                .OrderBy(m => m.Position.Distance2DSqr(ZetaDia.Me.Position)).FirstOrDefault();

            if (_miniMapMarker == null)
            {
                // Get point of interest marker
                _miniMapMarker = ZetaDia.Minimap.Markers.CurrentWorldMarkers
                .Where(m => m.IsPointOfInterest)
                .OrderBy(m => m.Position.Distance2DSqr(ZetaDia.Me.Position)).FirstOrDefault();
            }

            //if (_miniMapMarker != null)
            //{
            //    Logger.Log("Using Objective Style Minimap Marker: {0} dist: {1:0} isExit: {2} isEntrance {3}",
            //        _miniMapMarker.NameHash,
            //        _miniMapMarker.Position.Distance2D(ZetaDia.Me.Position),
            //        _miniMapMarker.IsPortalExit,
            //        _miniMapMarker.IsPortalEntrance);
            //}

            if (_miniMapMarker != null && _miniMapMarker.Position != Vector3.Zero)
            {
                _mapMarkerLastPosition = _miniMapMarker.Position;
            }

        }

        private void RefreshActorInfo()
        {
            Vector3 myPos = ZetaDia.Me.Position;

            if (_objectiveObject != null && _objectiveObject.IsValid)
            {
                if (QuestTools.EnableDebugLogging)
                {
                    Logger.Log("Found actor {0} {1} {2} of distance {3} from point {4}",
                        _objectiveObject.ActorSNO, _objectiveObject.Name, _objectiveObject.ActorType, 
                        _objectiveObject.Position.Distance(_mapMarkerLastPosition), _mapMarkerLastPosition);
                }
            }
            else if (_mapMarkerLastPosition.Distance2D(myPos) <= 200)
            {
                try
                {
                    // Monsters are the actual objective marker
                    _objectiveObject = ZetaDia.Actors.GetActorsOfType<DiaUnit>(true)
                            .Where(a => a.CommonData != null && a.IsValid && a.Position.Distance2D(_mapMarkerLastPosition) <= PathPrecision
                                    && (a.CommonData.GetAttribute<int>(ActorAttributeType.BountyObjective) != 0))
                            .OrderBy(a => a.Position.Distance2D(_mapMarkerLastPosition))
                            .FirstOrDefault();

                    if (_objectiveObject == null)
                    {
                        // Portals are not the actual objective but at the marker location
                        _objectiveObject = ZetaDia.Actors.GetActorsOfType<DiaObject>(true)
                            .Where(o => o != null && o.IsValid && o.IsValid && o is GizmoPortal
                                    && o.Position.Distance2DSqr(_mapMarkerLastPosition) <= 9f)
                           .OrderBy(o => o.Distance)
                           .FirstOrDefault();
                    }

                }
                catch (Exception ex)
                {
                    Logger.Log("Failed trying to find actor {0}", ex);
                }
            }
            else if (_mapMarkerLastPosition != Vector3.Zero && _mapMarkerLastPosition.Distance2D(myPos) <= 90)
            {
                _objectiveObject = ZetaDia.Actors.GetActorsOfType<DiaObject>(true)
                    .OrderBy(a => a.Position.Distance2D(_mapMarkerLastPosition)).FirstOrDefault();

                if (_objectiveObject != null && _objectiveObject.IsValid)
                {
                    InteractRange = _objectiveObject.CollisionSphere.Radius;
                    Logger.Log("Found Actor from Objective Marker! mapMarkerPos={0} actor={1} {2} {3} {4}",
                        _mapMarkerLastPosition, _objectiveObject.ActorSNO, _objectiveObject.Name, _objectiveObject.ActorType, _objectiveObject.Position);
                }
            }

            if (_objectiveObject != null && _objectiveObject.IsValid)
            {
                if (IsValidObjective())
                {
                    // need to lock on to a specific actor or we'll just keep finding other things near the marker.
                    Logger.Log("Found our Objective Actor! mapMarkerPos={0} actor={1} {2} {3} {4}",
                        _mapMarkerLastPosition, _objectiveObject.ActorSNO, _objectiveObject.Name, _objectiveObject.ActorType, _objectiveObject.Position);
                }
            }

            if (_objectiveObject is GizmoPortal && !IsPortal)
            {
                IsPortal = true;
            }
        }

        public bool IsPortal { get; set; }

        private float DistanceToMapMarker(DiaObject o)
        {
            return o.Position.Distance(_miniMapMarker.Position);
        }

        private bool ActorWithinRangeOfMarker(DiaObject o)
        {
            bool test = false;

            if (o != null && _miniMapMarker != null)
            {
                test = o.Position.Distance(_miniMapMarker.Position) <= PathPrecision;
            }
            return test;
        }
        

        /// <summary>
        /// Move without a destination name
        /// </summary>
        /// <param name="newpos"></param>
        /// <returns></returns>
        private bool Move(Vector3 newpos)
        {
            return Move(newpos, null);
        }

        List<Vector3> _allPoints = new List<Vector3>();
        List<Vector3> _validPoints = new List<Vector3>();
        private readonly QTNavigator _qtNavigator = new QTNavigator();

        /// <summary>
        /// Safely Moves the player to the requested destination <seealso cref="MoveToObjective.PathPointLimit"/>
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
                _lastMoveResult = MoveResult.Moved;
                result = true;
            }

            if (!ZetaDia.WorldInfo.IsGenerated)
            {
                if (_clientNavFailed && PathPointLimit > 20)
                {
                    PathPointLimit = PathPointLimit - 10;
                }
                else if (_clientNavFailed && PathPointLimit <= 20)
                {
                    PathPointLimit = 250;
                }

                if (newpos.Distance(ZetaDia.Me.Position) > PathPointLimit)
                {
                    newpos = MathEx.CalculatePointFrom(ZetaDia.Me.Position, newpos, newpos.Distance(ZetaDia.Me.Position) - PathPointLimit);
                }
            }
            float destinationDistance = newpos.Distance(ZetaDia.Me.Position);

            _lastMoveResult = _qtNavigator.MoveTo(newpos, destinationName + String.Format(" distance={0:0}", destinationDistance));

            switch (_lastMoveResult)
            {
                case MoveResult.Moved:
                case MoveResult.ReachedDestination:
                case MoveResult.UnstuckAttempt:
                    _clientNavFailed = false;
                    result = true;
                    break;
                case MoveResult.PathGenerated:
                case MoveResult.PathGenerating:
                case MoveResult.PathGenerationFailed:
                case MoveResult.Failed:
                    Navigator.PlayerMover.MoveTowards(Position);
                    result = false;
                    _clientNavFailed = true;
                    break;
            }

            if (QuestTools.EnableDebugLogging)
            {
                Logger.Debug("MoveResult: {0}, newpos={1} Distance={2}, destinationName={3}",
                    _lastMoveResult.ToString(), newpos, newpos.Distance(ZetaDia.Me.Position), destinationName);
            }
            return result;
        }

        public bool IsValid()
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
                return String.Format("questId={0} stepId={1} " +
                    "pathPointLimit={2} interactAttempts={3} interactRange={4} pathPrecision={5} x=\"{6}\" y=\"{7}\" z=\"{8}\" " + extraInfo,
                    QuestId, StepId, PathPointLimit, InteractAttempts, InteractRange, PathPrecision, X, Y, Z
                    );

            return string.Empty;
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
