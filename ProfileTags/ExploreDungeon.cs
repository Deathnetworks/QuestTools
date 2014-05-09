using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using QuestTools.Helpers;
using Zeta.Bot.Dungeons;
using Zeta.Bot.Logic;
using Zeta.Bot.Navigation;
using Zeta.Bot.Profile;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using Zeta.Game.Internals.SNO;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools.ProfileTags
{
    /// <summary>
    /// ExploreDungeon is fuly backwards compatible with the built-in Demonbuddy ExploreArea tag. It provides additional features such as:
    /// Moving to investigate MiniMapMarker pings and the current ExitNameHash if provided and visible (mini map marker 0 and the current exitNameHash)
    /// Moving to investigate Priority Scenes if provided (PrioritizeScenes subtags)
    /// Ignoring DungeonExplorer nodes in certain scenes if provided (IgnoreScenes subtags)
    /// Reduced backtracking (via pathPrecision attribute and combat skip ahead cache)
    /// Multiple ActorId's for the ObjectFound end type (AlternateActors sub-tags)
    /// </summary>
    [XmlElement("TrinityExploreDungeon")]
    [XmlElement("ExploreDungeon")]
    public class ExploreDungeonTag : ProfileBehavior
    {
        public ExploreDungeonTag() { }

        #region XML Attributes
        /// <summary>
        /// The SNOId of the Actor that we're looking for, used with until="ObjectFound"
        /// </summary>
        [XmlAttribute("actorId")]
        public int ActorId { get; set; }

        /// <summary>
        /// Sets a custom grid segmentation Box Size (default 15)
        /// </summary>
        [XmlAttribute("boxSize")]
        public int BoxSize { get; set; }

        /// <summary>
        /// Sets a custom grid segmentation Box Tolerance (default 0.55)
        /// </summary>
        [XmlAttribute("boxTolerance")]
        public float BoxTolerance { get; set; }

        /// <summary>
        /// The nameHash of the exit the bot will move to and finish the tag when found
        /// </summary>
        [XmlAttribute("exitNameHash")]
        public int ExitNameHash { get; set; }

        [XmlAttribute("ignoreGridReset")]
        public bool IgnoreGridReset { get; set; }

        /// <summary>
        /// Not currently implimented
        /// </summary>
        [XmlAttribute("leaveWhenFinished")]
        public bool LeaveWhenExplored { get; set; }

        /// <summary>
        /// The distance the bot must be from an actor before marking the tag as complete, when used with until="ObjectFound"
        /// </summary>
        [XmlAttribute("objectDistance")]
        public float ObjectDistance { get; set; }

        /// <summary>
        /// The until="" atribute must match one of these
        /// </summary>
        public enum TrinityExploreEndType
        {
            FullyExplored = 0,
            ObjectFound,
            ExitFound,
            SceneFound,
            SceneLeftOrActorFound,
            BountyComplete,
            RiftComplete,
            PortalExitFound,
        }

        [XmlAttribute("endType")]
        [XmlAttribute("until")]
        public TrinityExploreEndType EndType { get; set; }

        /// <summary>
        /// The Scene SNOId, used with ExploreUntil="SceneFound"
        /// </summary>
        [XmlAttribute("sceneId")]
        public int SceneId { get; set; }

        /// <summary>
        /// The Scene Name, used with ExploreUntil="SceneFound", a sub-string match will work
        /// </summary>
        [XmlAttribute("sceneName")]
        public string SceneName { get; set; }

        /// <summary>
        /// The distance the bot will mark dungeon nodes as "visited" (default is 1/2 of box size, minimum 10)
        /// </summary>
        [XmlAttribute("pathPrecision")]
        public float PathPrecision { get; set; }

        /// <summary>
        /// The distance before reaching a MiniMapMarker before marking it as visited
        /// </summary>
        [XmlAttribute("markerDistance")]
        public float MarkerDistance { get; set; }

        /// <summary>
        /// Disable Mini Map Marker Scouting
        /// </summary>
        [XmlAttribute("ignoreMarkers")]
        public bool IgnoreMarkers { get; set; }

        public enum TimeoutType
        {
            Timer,
            GoldInactivity,
            None,
        }

        /// <summary>
        /// The TimeoutType to use (default None, no timeout)
        /// </summary>
        [XmlAttribute("timeoutType")]
        public TimeoutType ExploreTimeoutType { get; set; }

        /// <summary>
        /// Value in Seconds. 
        /// The timeout value to use, when used with Timer will force-end the tag after a certain time. When used with GoldInactivity will end the tag after coinages doesn't change for the given period
        /// </summary>
        [XmlAttribute("timeoutValue")]
        public int TimeoutValue { get; set; }

        /// <summary>
        /// If we want to use a townportal before ending the tag when a timeout happens
        /// </summary>
        [XmlAttribute("townPortalOnTimeout")]
        public bool TownPortalOnTimeout { get; set; }

        /// <summary>
        /// Ignore last N nodes of dungeon explorer, when using endType=FullyExplored
        /// </summary>
        [XmlAttribute("ignoreLastNodes")]
        public int IgnoreLastNodes { get; set; }

        /// <summary>
        /// Used with IgnoreLastNodes, minimum visited node count before tag can end. 
        /// The minVisistedNodes is purely, and only for use with ignoreLastNodes - it does not serve any other function like you expect. 
        /// The reason this attribute exists, is to prevent prematurely exiting the dungeon exploration when used with ignoreLastNodes. 
        /// For example, when the bot first starts exploring an area, it needs to navigate a few dungeon nodes first before other dungeon nodes even appear - otherwise with ignoreLastNodes > 2, 
        /// the bot would immediately exit from navigation without exploring anything at all.
        /// </summary>
        [XmlAttribute("minVisitedNodes")]
        public int MinVisistedNodes { get; set; }

        [XmlAttribute("SetNodesExploredAutomatically")]
        [XmlAttribute("setNodesExploredAutomatically")]
        [DefaultValue(true)]
        public bool SetNodesExploredAutomatically { get; set; }

        [XmlAttribute("minObjectOccurances")]
        public int MinOccurances { get; set; }

        [XmlAttribute("interactWithObject")]
        public bool InteractWithObject { get; set; }

        [XmlAttribute("interactRange")]
        public float ObjectInteractRange { get; set; }
        #endregion

        #region XML Elements
        /// <summary>
        /// The list of Scene SNOId's or Scene Names that the bot will ignore dungeon nodes in
        /// </summary>
        [XmlElement("IgnoreScenes")]
        public List<IgnoreScene> IgnoreScenes { get; set; }

        /// <summary>
        /// The list of Scene SNOId's or Scene Names that the bot will prioritize (only works when the scene is "loaded")
        /// </summary>
        [XmlElement("PriorityScenes")]
        [XmlElement("PrioritizeScenes")]
        public List<PrioritizeScene> PriorityScenes { get; set; }

        /// <summary>
        /// The list of Scene SNOId's or Scene Names that the bot will use for endtype SceneLeftOrActorFound
        /// </summary>
        [XmlElement("AlternateScenes")]
        public List<AlternateScene> AlternateScenes { get; set; }

        /// <summary>
        /// The Ignore Scene class, used as IgnoreScenes child elements
        /// </summary>
        [XmlElement("IgnoreScene")]
        public class IgnoreScene : IEquatable<Scene>
        {
            [XmlAttribute("sceneName")]
            public string SceneName { get; set; }
            [XmlAttribute("sceneId")]
            public int SceneId { get; set; }

            public IgnoreScene()
            {
                SceneId = -1;
                SceneName = String.Empty;
            }

            public IgnoreScene(string name)
            {
                SceneName = name;
            }
            public IgnoreScene(int id)
            {
                SceneId = id;
            }

            public bool Equals(Scene other)
            {
                return (!string.IsNullOrWhiteSpace(SceneName) && other.Name.ToLowerInvariant().Contains(SceneName.ToLowerInvariant())) || other.SceneInfo.SNOId == SceneId;
            }
        }

        private CachedValue<List<Area>> _ignoredAreas;
        private List<Area> IgnoredAreas
        {
            get
            {
                if (_ignoredAreas == null)
                    _ignoredAreas = new CachedValue<List<Area>>(() => { return GetIgnoredAreas(); }, TimeSpan.FromSeconds(1));
                return _ignoredAreas.Value;
            }
        }

        private List<Area> GetIgnoredAreas()
        {
            var ignoredScenes = ZetaDia.Scenes.GetScenes()
                .Where(scn => scn.IsValid && IgnoreScenes.Any(igns => igns.Equals(scn)) && !PriorityScenes.Any(psc => psc.Equals(scn)))
                .Select(scn =>
                    scn.Mesh.Zone == null
                    ? new Area(new Vector2(float.MinValue, float.MinValue), new Vector2(float.MaxValue, float.MaxValue))
                    : new Area(scn.Mesh.Zone.ZoneMin, scn.Mesh.Zone.ZoneMax))
                    .ToList();
            return ignoredScenes;
        }

        private class Area
        {
            public Area() { }
            public Vector2 Min { get; set; }
            public Vector2 Max { get; set; }

            /// <summary>
            /// Initializes a new instance of the Area class.
            /// </summary>
            public Area(Vector2 min, Vector2 max)
            {
                Min = min;
                Max = max;
            }

            public bool IsPositionInside(Vector2 position)
            {
                return position.X >= Min.X && position.X <= Max.X && position.Y >= Min.Y && position.Y <= Max.Y;
            }

            public bool IsPositionInside(Vector3 position)
            {
                return IsPositionInside(position.ToVector2());
            }
        }

        /// <summary>
        /// The Priority Scene class, used as PrioritizeScenes child elements
        /// </summary>
        [XmlElement("PriorityScene")]
        [XmlElement("PrioritizeScene")]
        public class PrioritizeScene : IEquatable<Scene>
        {
            [XmlAttribute("sceneName")]
            public string SceneName { get; set; }
            [XmlAttribute("sceneId")]
            public int SceneId { get; set; }
            [XmlAttribute("pathPrecision")]
            public float PathPrecision { get; set; }

            public PrioritizeScene()
            {
                PathPrecision = 15f;
                SceneName = String.Empty;
                SceneId = -1;
            }

            public PrioritizeScene(string name)
            {
                SceneName = name;
            }
            public PrioritizeScene(int id)
            {
                SceneId = id;
            }
            public bool Equals(Scene other)
            {
                return (SceneName != String.Empty && other.Name.ToLowerInvariant().Contains(SceneName.ToLowerInvariant())) || other.SceneInfo.SNOId == SceneId;
            }
        }

        /// <summary>
        /// The Alternate Scene class, used as AlternateScenes child elements
        /// </summary>
        [XmlElement("AlternateScene")]
        public class AlternateScene : IEquatable<Scene>
        {
            [XmlAttribute("sceneName")]
            public string SceneName { get; set; }
            [XmlAttribute("sceneId")]
            public int SceneId { get; set; }
            [XmlAttribute("pathPrecision")]
            public float PathPrecision { get; set; }

            public AlternateScene()
            {
                PathPrecision = 15f;
                SceneName = String.Empty;
                SceneId = -1;
            }

            public AlternateScene(string name)
            {
                SceneName = name;
            }
            public AlternateScene(int id)
            {
                SceneId = id;
            }
            public bool Equals(Scene other)
            {
                return (SceneName != String.Empty && other.Name.ToLowerInvariant().Contains(SceneName.ToLowerInvariant())) || other.SceneInfo.SNOId == SceneId;
            }
        }

        [XmlElement("AlternateActors")]
        public List<AlternateActor> AlternateActors { get; set; }

        [XmlElement("AlternateActor")]
        public class AlternateActor
        {
            [XmlAttribute("actorId")]
            public int ActorId { get; set; }

            [XmlAttribute("objectDistance")]
            public float ObjectDistance { get; set; }

            [XmlAttribute("interactRange")]
            public float InteractRange { get; set; }

            public AlternateActor()
            {
                ActorId = -1;
                ObjectDistance = 60f;
            }
        }

        [XmlElement("AlternateMarkers")]
        public List<AlternateMarker> AlternateMarkers { get; set; }

        [XmlElement("AlternateMarker")]
        public class AlternateMarker
        {
            [XmlAttribute("markerNameHash")]
            public int MarkerNameHash { get; set; }

            [XmlAttribute("markerDistance")]
            public float MarkerDistance { get; set; }

            public AlternateMarker()
            {
                MarkerNameHash = 0;
                MarkerDistance = 45f;
            }
        }

        [XmlElement("Objectives")]
        public List<Objective> Objectives { get; set; }

        [XmlElement("Objective")]
        public class Objective
        {
            [XmlAttribute("actorId")]
            public int ActorId { get; set; }

            [XmlAttribute("markerNameHash")]
            public int MarkerNameHash { get; set; }

            [XmlAttribute("count")]
            public int Count { get; set; }

            [XmlAttribute("endAnimation")]
            public SNOAnim EndAnimation { get; set; }

            [XmlAttribute("interact")]
            public bool Interact { get; set; }

            public Objective()
            {

            }
        }
        #endregion

        readonly HashSet<Tuple<int, Vector3>> _foundObjects = new HashSet<Tuple<int, Vector3>>();

        /// <summary>
        /// The Position of the CurrentNode NavigableCenter
        /// </summary>
        private Vector3 CurrentNavTarget
        {
            get
            {
                if (_prioritySceneTarget != Vector3.Zero)
                {
                    return _prioritySceneTarget;
                }

                if (GetIsInPandemoniumFortress())
                {

                    if (_lastPandFortressTarget != Vector3.Zero && GridSegmentation.Nodes.Any(n => !n.Visited && n.NavigableCenter == _lastPandFortressTarget))
                        return _lastPandFortressTarget;

                    GetPandFortressNavTarget();

                    if (_lastPandFortressTarget != Vector3.Zero)
                        return _lastPandFortressTarget;
                }

                if (GetRouteUnvisitedNodeCount() > 0)
                {
                    return BrainBehavior.DungeonExplorer.CurrentNode.NavigableCenter;
                }
                return Vector3.Zero;
            }
        }

        // Adding these for SimpleFollow compatability
        public float X { get { return CurrentNavTarget.X; } }
        public float Y { get { return CurrentNavTarget.Y; } }
        public float Z { get { return CurrentNavTarget.Z; } }

        private bool _initDone;

        /// <summary>
        /// The current player position
        /// </summary>
        private static Vector3 MyPosition { get { return ZetaDia.Me.Position; } }

        /// <summary>
        /// The last position we updated the SearchGridProvider at
        /// </summary>
        private Vector3 _gridProviderUpdatePosition = Vector3.Zero;

        /// <summary>
        /// Called when the profile behavior starts
        /// </summary>
        public override void OnStart()
        {
            Logger.Log("TrinityExploreDungeon Started");

            if (SetNodesExploredAutomatically)
            {
                Logger.Debug("Minimap Explored Nodes Enabled");
                BrainBehavior.DungeonExplorer.SetNodesExploredAutomatically = true;
            }
            else
            {
                Logger.Debug("Minimap Explored Nodes Disabled");
                BrainBehavior.DungeonExplorer.SetNodesExploredAutomatically = false;
            }

            if (!IgnoreGridReset && !ZetaDia.Me.IsDead && DateTime.UtcNow.Subtract(Death.LastDeathTime).TotalSeconds > 3)
            {
                UpdateSearchGridProvider();

                CheckResetDungeonExplorer();

                GridSegmentation.Reset();
                BrainBehavior.DungeonExplorer.Reset();
                MiniMapMarker.KnownMarkers.Clear();
            }

            if (!_initDone)
            {
                Init();
            }
            _tagTimer.Reset();
            _timesForcedReset = 0;

            if (Objectives == null)
                Objectives = new List<Objective>();

            if (ObjectDistance < 1f)
                ObjectDistance = 25f;

            NavigationProvider.EnableDebugLogging = true;

            PrintNodeCounts("PostInit");
        }


        /// <summary>
        /// The main profile behavior
        /// </summary>
        /// <returns></returns>
        protected override Composite CreateBehavior()
        {
            return
            new Sequence(
                new DecoratorContinue(ret => !IgnoreMarkers,
                    new Sequence(
                        MiniMapMarker.DetectMiniMapMarkers(),
                        MiniMapMarker.DetectMiniMapMarkers(ExitNameHash),
                        MiniMapMarker.DetectMiniMapMarkers(Objectives),
                        MiniMapMarker.DetectMiniMapMarkers(AlternateMarkers)
                    )
                ),
                // I dunno if this will work...
                new DecoratorContinue(ret => Navigator.StuckHandler.IsStuck,
                    new Action(ret => _stuckCount++)),
                new DecoratorContinue(ret => !Navigator.StuckHandler.IsStuck,
                    new Action(ret => _stuckCount = 0)),
                UpdateSearchGridProvider(),
                new Action(ret => CheckResetDungeonExplorer()),
                new PrioritySelector(
                    CheckIsObjectiveFinished(),
                    PrioritySceneCheck(),
                    new Decorator(ret => !IgnoreMarkers,
                        MiniMapMarker.VisitMiniMapMarkers(MyPosition, MarkerDistance)
                    ),
                    new Decorator(ret => ShouldInvestigateActor(),
                        new PrioritySelector(
                            new Decorator(ret => CurrentActor != null && CurrentActor.IsValid &&
                                Objectives.Any(o => o.ActorId == CurrentActor.ActorSNO && o.Interact) &&
                                CurrentActor.Position.Distance(ZetaDia.Me.Position) <= CurrentActor.CollisionSphere.Radius,
                                new Sequence(
                                    new Action(ret => CurrentActor.Interact())
                                )
                            ),
                            InvestigateActor()
                        )
                    ),
                    new Sequence(
                        new DecoratorContinue(ret => DungeonRouteIsEmpty(),
                            new Action(ret => UpdateRoute())
                        ),
                        CheckIsExplorerFinished()
                    ),
                    new DecoratorContinue(ret => DungeonRouteIsValid(),
                        new PrioritySelector(
                            CheckNodeFinished(),
                            new Sequence(
                                new Action(ret => PrintNodeCounts("MainBehavior")),
                                new PrioritySelector(
                                    MoveToAndUseDeathGate(),
                                    new Action(ret => MoveToNextNode())
                                )
                            )
                        )
                    ),
                    new Action(ret => Logger.Debug("Error 1: Unknown error occured!"))
                )
            );
        }

        private Vector3 _lastPathCheckTarget = Vector3.Zero;
        private bool _lastPathCheckResult;
        private bool CanFullPathToCurrentNavTarget()
        {
            var navTarget = CurrentNavTarget;

            // too far away to actually know...
            if (navTarget.Distance2D(ZetaDia.Me.Position) > 500)
                return true;

            if (navTarget == Vector3.Zero)
                return false;

            if (_lastPathCheckTarget == navTarget)
                return _lastPathCheckResult;

            _lastPathCheckTarget = navTarget;
            _lastPathCheckResult = NavigationProvider.CanPathWithinDistance(navTarget, PathPrecision);
            if (!_lastPathCheckResult)
                Logger.Debug("Unable to fully path to {0} with precision {1}, distance {2:0}", navTarget, PathPrecision, navTarget.Distance2D(ZetaDia.Me.Position));

            return _lastPathCheckResult;
        }

        private static MainGridProvider MainGridProvider
        {
            get
            {
                return (MainGridProvider)Navigator.SearchGridProvider;
            }
        }
        /// <summary>
        /// Re-sets the DungeonExplorer, BoxSize, BoxTolerance, and Updates the current route
        /// </summary>
        private void CheckResetDungeonExplorer()
        {
            if (!ZetaDia.IsInGame || ZetaDia.IsLoadingWorld || !ZetaDia.WorldInfo.IsValid || !ZetaDia.Scenes.IsValid || !ZetaDia.Service.IsValid)
                return;

            // I added this because GridSegmentation may (rarely) reset itself without us doing it to 15/.55.
            if ((BoxSize != 0 && BoxTolerance != 0) && (GridSegmentation.BoxSize != BoxSize || GridSegmentation.BoxTolerance != BoxTolerance) || (GetGridSegmentationNodeCount() == 0))
            {
                Logger.Debug("Box Size or Tolerance has been changed! {0}/{1}", GridSegmentation.BoxSize, GridSegmentation.BoxTolerance);

                BrainBehavior.DungeonExplorer.Reset();
                PrintNodeCounts("BrainBehavior.DungeonExplorer.Reset");

                GridSegmentation.BoxSize = BoxSize;
                GridSegmentation.BoxTolerance = BoxTolerance;
                PrintNodeCounts("SetBoxSize+Tolerance");

                BrainBehavior.DungeonExplorer.Update();
                PrintNodeCounts("BrainBehavior.DungeonExplorer.Update");
            }
        }

        private static bool DungeonRouteIsValid()
        {
            return BrainBehavior.DungeonExplorer != null && BrainBehavior.DungeonExplorer.CurrentRoute != null && BrainBehavior.DungeonExplorer.CurrentRoute.Any();
        }

        private static bool DungeonRouteIsEmpty()
        {
            return BrainBehavior.DungeonExplorer != null && BrainBehavior.DungeonExplorer.CurrentRoute != null && !BrainBehavior.DungeonExplorer.CurrentRoute.Any();
        }

        private bool CurrentActorIsFinished
        {
            get
            {
                return Objectives.Any(o => o.ActorId == CurrentActor.ActorSNO && o.EndAnimation == CurrentActor.CommonData.CurrentAnimation);
            }
        }

        private DiaObject CurrentActor
        {
            get
            {
                var actor =
                ZetaDia.Actors.GetActorsOfType<DiaObject>(true)
                .Where(diaObj => diaObj.IsValid && (diaObj.ActorSNO == ActorId ||
                    Objectives.Any(o => o.ActorId != 0 && o.ActorId == diaObj.ActorSNO)) &&
                    PositionCache.Cache.Any(pos => pos.Distance2DSqr(diaObj.Position) >= ObjectDistance * ObjectDistance) &&
                    _foundObjects.All(fo => fo.Equals(new Tuple<int, Vector3>(diaObj.ActorSNO, diaObj.Position))))
                .OrderBy(o => o.Distance)
                .FirstOrDefault();

                if (actor != null && actor.IsValid)
                    return actor;

                return default(DiaObject);
            }
        }

        private Composite InvestigateActor()
        {
            return new Action(ret =>
            {
                RecordPosition();

                var actor = ZetaDia.Actors.GetActorsOfType<DiaObject>(true).FirstOrDefault(a => a.IsValid && a.ActorSNO == ActorId);

                if (actor != null && actor.IsValid && actor.Position.Distance2DSqr(MyPosition) >= ObjectDistance * ObjectDistance)
                    Navigator.MoveTo(actor.Position, string.Format("InvestigateActor {0} {1} {2}", actor.ActorSNO, actor.Name, actor.ActorType));
            });
        }

        private bool ShouldInvestigateActor()
        {
            if (ActorId == 0 || Objectives.All(o => o.ActorId == 0))
                return false;

            var actors = ZetaDia.Actors.GetActorsOfType<DiaObject>(true)
                .Where(diaObj => diaObj.IsValid && (diaObj.ActorSNO == ActorId ||
                    AlternateActors.Any(alternateActor => alternateActor.ActorId != 0 && alternateActor.ActorId == diaObj.ActorSNO) ||
                    Objectives.Any(objective => objective.ActorId != 0 && objective.ActorId == diaObj.ActorSNO) ||
                    diaObj.CommonData.GetAttribute<int>(ActorAttributeType.BountyObjective) > 0) &&
                    PositionCache.Cache.Any(pos => pos.Distance2DSqr(diaObj.Position) >= ObjectDistance * ObjectDistance) &&
                    _foundObjects.All(fo => fo.Equals(new Tuple<int, Vector3>(diaObj.ActorSNO, diaObj.Position)))).ToList();

            if (!actors.Any())
                return false;

            var actor = actors.OrderBy(a => a.Distance).FirstOrDefault();

            if (actor != null && actor.Distance <= ObjectDistance)
                return false;

            return true;
        }

        /// <summary>
        /// The last scene SNOId we entered
        /// </summary>
        private int _lastSceneId = -1;
        /// <summary>
        /// Updates the search grid provider as needed
        /// </summary>
        /// <returns></returns>
        private Composite UpdateSearchGridProvider()
        {
            return
            new DecoratorContinue(ret => _lastSceneId != ZetaDia.Me.SceneId || Vector3.Distance(MyPosition, _gridProviderUpdatePosition) > 150,
                new Sequence(
                    new Action(ret => _lastSceneId = ZetaDia.Me.SceneId),
                    new Action(ret => Navigator.SearchGridProvider.Update()),
                    new Action(ret => _gridProviderUpdatePosition = MyPosition),
                    new Action(ret => MiniMapMarker.UpdateFailedMarkers())
                )
            );
        }

        /// <summary>
        /// Checks if we are using a timeout and will end the tag if the timer has breached the given value
        /// </summary>
        /// <returns></returns>
        private Composite TimeoutCheck()
        {
            return
            new PrioritySelector(
                new Decorator(ret => _timeoutBreached,
                    new Sequence(
                        new DecoratorContinue(ret => TownPortalOnTimeout && !ZetaDia.IsInTown,
                            new Sequence(
                                new Action(ret => Logger.Log(
                                    "TrinityExploreDungeon timer tripped ({0}), tag finished, Using Town Portal!", TimeoutValue)),
                                Zeta.Bot.CommonBehaviors.CreateUseTownPortal(),
                                new Action(ret => _isDone = true)
                            )
                        ),
                        new DecoratorContinue(ret => !TownPortalOnTimeout,
                            new Sequence(
                                new Action(ret => Logger.Log(
                                    "TrinityExploreDungeon timer tripped ({0}), tag finished!", TimeoutValue)),
                                new Action(ret => _isDone = true)
                            )
                        )
                    )
                ),
                new Decorator(ret => ExploreTimeoutType == TimeoutType.Timer,
                    new Action(ret => CheckSetTimer())
                ),
                new Decorator(ret => ExploreTimeoutType == TimeoutType.GoldInactivity,
                    new Action(ret => CheckSetGoldInactive())
                )
            );
        }

        bool _timeoutBreached;
        readonly Stopwatch _tagTimer = new Stopwatch();

        /// <summary>
        /// Will start the timer if needed, and end the tag if the timer has exceeded the TimeoutValue
        /// </summary>
        /// <returns></returns>
        private RunStatus CheckSetTimer()
        {
            if (!_tagTimer.IsRunning)
            {
                _tagTimer.Start();
                return RunStatus.Failure;
            }
            if (ExploreTimeoutType == TimeoutType.Timer && _tagTimer.Elapsed.TotalSeconds > TimeoutValue)
            {
                Logger.Log("TrinityExploreDungeon timer ended ({0}), tag finished!", TimeoutValue);
                _timeoutBreached = true;
                return RunStatus.Success;
            }
            return RunStatus.Failure;
        }

        private int _lastCoinage = -1;

        /// <summary>
        /// Will check if the bot has not picked up any gold within the allocated TimeoutValue
        /// </summary>
        /// <returns></returns>
        private RunStatus CheckSetGoldInactive()
        {
            CheckSetTimer();
            if (_lastCoinage == -1)
            {
                _lastCoinage = Player.Coinage;
                return RunStatus.Failure;
            }
            if (_lastCoinage != Player.Coinage)
            {
                _tagTimer.Restart();
                return RunStatus.Failure;
            }
            if (_lastCoinage == Player.Coinage && _tagTimer.Elapsed.TotalSeconds > TimeoutValue)
            {
                Logger.Log("TrinityExploreDungeon gold inactivity timer tripped ({0}), tag finished!", TimeoutValue);
                _timeoutBreached = true;
                return RunStatus.Success;
            }

            return RunStatus.Failure;
        }

        private int _timesForcedReset;
        private const int TimesForceResetMax = 5;

        /// <summary>
        /// Checks to see if the tag is finished as needed
        /// </summary>
        /// <returns></returns>
        private Composite CheckIsExplorerFinished()
        {
            return
            new PrioritySelector(
                CheckIsObjectiveFinished(),
                new Decorator(ret => GetRouteUnvisitedNodeCount() == 0 && _timesForcedReset > TimesForceResetMax,
                    new Sequence(
                        new Action(ret => Logger.Log(
                            "Visited all nodes but objective not complete, forced reset more than {0} times, finished!", TimesForceResetMax)),
                        new Action(ret => _isDone = true)
                    )
                ),
                new Decorator(ret => GetRouteUnvisitedNodeCount() == 0,
                    new Sequence(
                        new Action(ret => Logger.Log("Visited all nodes but objective not complete, forcing grid reset!")),
                        new DecoratorContinue(ret => BrainBehavior.DungeonExplorer.SetNodesExploredAutomatically,
                            new Sequence(
                                new Action(ret => Logger.Log("Disabling SetNodesExploredAutomatically")),
                                new Action(ret => BrainBehavior.DungeonExplorer.SetNodesExploredAutomatically = false)
                            )
                        ),
                        new DecoratorContinue(ret => _timesForcedReset > 2 && GetCurrentRouteNodeCount() == 1,
                            new Sequence(
                                new Action(ret => Logger.Log("Only 1 node found and 3 grid resets, falling back to failsafe!")),
                                new Action(ret => BoxSize = 25),
                                new Action(ret => BoxTolerance = 0.01f),
                                new Action(ret => IgnoreScenes.Clear())
                            )
                        ),
                        new Action(ret => _timesForcedReset++),
                        new Action(ret => PositionCache.Cache.Clear()),
                        new Action(ret => MiniMapMarker.KnownMarkers.Clear()),
                        new Action(ret => ForceUpdateScenes()),
                        new Action(ret => GridSegmentation.Reset()),
                        new Action(ret => GridSegmentation.Update()),
                        new Action(ret => BrainBehavior.DungeonExplorer.Reset()),
                        new Action(ret => _priorityScenesInvestigated.Clear()),
                        new Action(ret => UpdateRoute())
                    )
                )
           );
        }

        private static void ForceUpdateScenes()
        {
            foreach (Scene scene in ZetaDia.Scenes.GetScenes().ToList())
            {
                scene.UpdatePointer(scene.BaseAddress);
            }
        }

        /// <summary>
        /// Checks to see if the tag is finished as needed
        /// </summary>
        /// <returns></returns>
        private Composite CheckIsObjectiveFinished()
        {
            return
            new PrioritySelector(
                TimeoutCheck(),
                new Decorator(ret => EndType == TrinityExploreEndType.RiftComplete && GetIsRiftDone(),
                    new Sequence(
                        new Action(ret => Logger.Log("Rift is done. Tag Finished.")),
                        new Action(ret => _isDone = true)
                    )
                ),
                new Decorator(ret => EndType == TrinityExploreEndType.PortalExitFound &&
                    PortalExitMarker() != null && PortalExitMarker().Position.Distance2D(MyPosition) <= MarkerDistance,
                    new Sequence(
                        new Action(ret => Logger.Log("Found portal exit! Tag Finished.")),
                        new Action(ret => _isDone = true)
                    )
                ),
                new Decorator(ret => EndType == TrinityExploreEndType.BountyComplete && GetIsBountyDone(),
                    new Sequence(
                        new Action(ret => Logger.Log("Bounty is done. Tag Finished.")),
                        new Action(ret => _isDone = true)
                    )
                ),
                new Decorator(ret => EndType == TrinityExploreEndType.FullyExplored && IgnoreLastNodes > 0 && GetRouteUnvisitedNodeCount() <= IgnoreLastNodes && GetGridSegmentationVisistedNodeCount() >= MinVisistedNodes,
                    new Sequence(
                        new Action(ret => Logger.Log("Fully explored area! Ignoring {0} nodes. Tag Finished.", IgnoreLastNodes)),
                        new Action(ret => _isDone = true)
                    )
                ),
                new Decorator(ret => EndType == TrinityExploreEndType.FullyExplored && GetRouteUnvisitedNodeCount() == 0,
                    new Sequence(
                        new Action(ret => Logger.Log("Fully explored area! Tag Finished.", 0)),
                        new Action(ret => _isDone = true)
                    )
                ),
                new Decorator(ret => EndType == TrinityExploreEndType.ExitFound && ExitNameHash != 0 && IsExitNameHashVisible(),
                    new Sequence(
                        new Action(ret => Logger.Log("Found exitNameHash {0}!", ExitNameHash)),
                        new Action(ret => _isDone = true)
                    )
                ),
                new Decorator(ret => (EndType == TrinityExploreEndType.ObjectFound || EndType == TrinityExploreEndType.SceneLeftOrActorFound) && ActorId != 0 && ZetaDia.Actors.GetActorsOfType<DiaObject>(true)
                    .Any(a => a.ActorSNO == ActorId && a.Distance <= ObjectDistance),
                    new Sequence(
                        new Action(ret => Logger.Log("Found Object {0}!", ActorId)),
                        new Action(ret => _isDone = true)
                    )
                ),
                new Decorator(ret => (EndType == TrinityExploreEndType.ObjectFound || EndType == TrinityExploreEndType.SceneLeftOrActorFound) && AlternateActorsFound(),
                    new Sequence(
                        new Action(ret => Logger.Log("Found Alternate Object {0}!", GetAlternateActor().ActorSNO)),
                        new Action(ret => _isDone = true)
                    )
                ),
                new Decorator(ret => EndType == TrinityExploreEndType.SceneFound && ZetaDia.Me.SceneId == SceneId,
                    new Sequence(
                        new Action(ret => Logger.Log("Found SceneId {0}!", SceneId)),
                        new Action(ret => _isDone = true)
                    )
                ),
                new Decorator(ret => EndType == TrinityExploreEndType.SceneFound && !string.IsNullOrWhiteSpace(SceneName) && ZetaDia.Me.CurrentScene.Name.ToLower().Contains(SceneName.ToLower()),
                    new Sequence(
                        new Action(ret => Logger.Log("Found SceneName {0}!", SceneName)),
                        new Action(ret => _isDone = true)
                    )
                ),
                new Decorator(ret => EndType == TrinityExploreEndType.SceneLeftOrActorFound && SceneId != 0 && SceneIdLeft(),
                    new Sequence(
                        new Action(ret => Logger.Log("Left SceneId {0}!", SceneId)),
                        new Action(ret => _isDone = true)
                    )
                ),
                new Decorator(ret => (EndType == TrinityExploreEndType.SceneFound || EndType == TrinityExploreEndType.SceneLeftOrActorFound) && !string.IsNullOrWhiteSpace(SceneName) && SceneNameLeft(),
                    new Sequence(
                        new Action(ret => Logger.Log("Left SceneName {0}!", SceneName)),
                        new Action(ret => _isDone = true)
                    )
                ),
                new Decorator(ret => ZetaDia.IsInTown,
                    new Sequence(
                        new Action(ret => Logger.Log("Cannot use TrinityExploreDungeon in town - tag finished!", SceneName)),
                        new Action(ret => _isDone = true)
                    )
                )
            );
        }

        private static MinimapMarker PortalExitMarker()
        {
            return ZetaDia.Minimap.Markers.CurrentWorldMarkers.FirstOrDefault(m => m.IsPortalExit);
        }

        private bool AlternateActorsFound()
        {
            return AlternateActors.Any() && ZetaDia.Actors.GetActorsOfType<DiaObject>(true)
                    .Where(o => AlternateActors.Any(a => a.ActorId == o.ActorSNO && o.Distance <= a.ObjectDistance)).Any();
        }

        private bool SceneIdLeft()
        {
            return ZetaDia.Me.SceneId != SceneId;
        }

        private bool SceneNameLeft()
        {
            return !ZetaDia.Me.CurrentScene.Name.ToLower().Contains(SceneName.ToLower()) && AlternateScenes != null && AlternateScenes.Any() && AlternateScenes.All(o => !ZetaDia.Me.CurrentScene.Name.ToLower().Contains(o.SceneName.ToLower()));
        }

        private DiaObject GetAlternateActor()
        {
            return ZetaDia.Actors.GetActorsOfType<DiaObject>(true)
                    .Where(o => AlternateActors.Any(a => a.ActorId == o.ActorSNO && o.Distance <= a.ObjectDistance)).OrderBy(o => o.Distance).FirstOrDefault();
        }

        /// <summary>
        /// Determine if the tag ExitNameHash is visible in the list of Current World Markers
        /// </summary>
        /// <returns></returns>
        private bool IsExitNameHashVisible()
        {
            return ZetaDia.Minimap.Markers.CurrentWorldMarkers.Any(m => m.NameHash == ExitNameHash && m.Position.Distance2D(MyPosition) <= MarkerDistance + 10f);
        }

        #region PriorityScenes
        private Vector3 _prioritySceneTarget = Vector3.Zero;
        private int _prioritySceneSNOId = -1;
        private Scene _currentPriorityScene;
        private float _priorityScenePathPrecision = -1f;
        /// <summary>
        /// A list of Scene SNOId's that have already been investigated
        /// </summary>
        private readonly List<int> _priorityScenesInvestigated = new List<int>();

        private DateTime _lastCheckedScenes = DateTime.MinValue;

        private int _stuckCount;

        /// <summary>
        /// Will find and move to Prioritized Scene's based on Scene SNOId or Name
        /// </summary>
        /// <returns></returns>
        private Composite PrioritySceneCheck()
        {
            return
            new Decorator(ret => PriorityScenes != null && PriorityScenes.Any(),
                new Sequence(
                    new DecoratorContinue(ret => DateTime.UtcNow.Subtract(_lastCheckedScenes).TotalMilliseconds > 1000,
                        new Sequence(
                            new Action(ret => _lastCheckedScenes = DateTime.UtcNow),
                            new Action(ret => FindPrioritySceneTarget())
                        )
                    ),
                    new Decorator(ret => _prioritySceneTarget != Vector3.Zero,
                        new PrioritySelector(
                            new Decorator(ret => _stuckCount > 3,
                                new Sequence(
                                     new Action(ret => Logger.Log("Too many stuck attempts, canceling Priority Scene {0} {1} center {2} Distance {3:0}",
                                        _currentPriorityScene.Name, _currentPriorityScene.SceneInfo.SNOId, _prioritySceneTarget, _prioritySceneTarget.Distance2D(MyPosition))),
                                   new Action(ret => PrioritySceneMoveToFinished())
                                )
                            ),
                            new Decorator(ret => _prioritySceneTarget.Distance2D(MyPosition) <= _priorityScenePathPrecision,
                                new Sequence(
                                    new Action(ret => Logger.Log("Successfully navigated to priority scene {0} {1} center {2} Distance {3:0}",
                                        _currentPriorityScene.Name, _currentPriorityScene.SceneInfo.SNOId, _prioritySceneTarget, _prioritySceneTarget.Distance2D(MyPosition))),
                                    new Action(ret => PrioritySceneMoveToFinished())
                                )
                            ),
                            new PrioritySelector(
                                MoveToAndUseDeathGate(),
                                new Action(ret => MoveToPriorityScene())
                            )
                        )
                    )
                )
            );
        }

        /// <summary>
        /// Handles actual movement to the Priority Scene
        /// </summary>
        private void MoveToPriorityScene()
        {
            string info = string.Format("Moving to Priority Scene {0} - {1} Center {2}",
                _currentPriorityScene.Name, _currentPriorityScene.SceneInfo.SNOId, _prioritySceneTarget);
            Logger.Debug(info);

            MoveResult moveResult = Navigator.MoveTo(_prioritySceneTarget, info);

            if (moveResult == MoveResult.PathGenerationFailed || moveResult == MoveResult.ReachedDestination)
            {
                Logger.Debug("Unable to navigate to Scene {0} - {1} Center {2} Distance {3:0}, cancelling!",
                    _currentPriorityScene.Name, _currentPriorityScene.SceneInfo.SNOId, _prioritySceneTarget, _prioritySceneTarget.Distance2D(MyPosition));
                PrioritySceneMoveToFinished();
            }
        }

        /// <summary>
        /// Sets a priority scene as finished
        /// </summary>
        private void PrioritySceneMoveToFinished()
        {
            _priorityScenesInvestigated.Add(_prioritySceneSNOId);
            _prioritySceneSNOId = -1;
            _prioritySceneTarget = Vector3.Zero;
            UpdateRoute();
        }

        /// <summary>
        /// Finds a navigable point in a priority scene
        /// </summary>
        private void FindPrioritySceneTarget()
        {
            if (!PriorityScenes.Any())
                return;

            if (_prioritySceneTarget != Vector3.Zero)
                return;

            bool foundPriorityScene = false;

            // find any matching priority scenes in scene manager - match by name or SNOId

            List<Scene> priorityScenes = ZetaDia.Scenes.GetScenes()
                .Where(s => PriorityScenes.Any(ps => ps.SceneId != -1 && s.SceneInfo.SNOId == ps.SceneId)).ToList();

            priorityScenes.AddRange(ZetaDia.Scenes.GetScenes()
                 .Where(s => PriorityScenes.Any(ps => ps.SceneName.Trim() != String.Empty && ps.SceneId == -1 && s.Name.ToLower().Contains(ps.SceneName.ToLower()))).ToList());

            List<Scene> foundPriorityScenes = new List<Scene>();
            Dictionary<int, Vector3> foundPrioritySceneIndex = new Dictionary<int, Vector3>();

            foreach (Scene scene in priorityScenes)
            {
                if (!scene.IsValid)
                    continue;
                if (!scene.SceneInfo.IsValid)
                    continue;
                if (!scene.Mesh.Zone.IsValid)
                    continue;
                if (!scene.Mesh.Zone.NavZoneDef.IsValid)
                    continue;

                if (_priorityScenesInvestigated.Contains(scene.SceneInfo.SNOId))
                    continue;

                foundPriorityScene = true;

                NavZone navZone = scene.Mesh.Zone;
                NavZoneDef zoneDef = navZone.NavZoneDef;

                Vector3 zoneCenter = GetNavZoneCenter(navZone);

                List<NavCell> navCells = zoneDef.NavCells.Where(c => c.IsValid && c.Flags.HasFlag(NavCellFlags.AllowWalk)).ToList();

                if (!navCells.Any())
                    continue;

                NavCell bestCell = navCells.OrderBy(c => GetNavCellCenter(c.Min, c.Max, navZone).Distance2D(zoneCenter)).FirstOrDefault();

                if (bestCell != null && !foundPrioritySceneIndex.ContainsKey(scene.SceneInfo.SNOId))
                {
                    foundPrioritySceneIndex.Add(scene.SceneInfo.SNOId, GetNavCellCenter(bestCell, navZone));
                    foundPriorityScenes.Add(scene);
                }
                else
                {
                    Logger.Debug("Found Priority Scene but could not find a navigable point!", true);
                }
            }

            if (foundPrioritySceneIndex.Any())
            {
                KeyValuePair<int, Vector3> nearestPriorityScene = foundPrioritySceneIndex.OrderBy(s => s.Value.Distance2D(MyPosition)).FirstOrDefault();

                _prioritySceneSNOId = nearestPriorityScene.Key;
                _prioritySceneTarget = nearestPriorityScene.Value;
                _currentPriorityScene = foundPriorityScenes.FirstOrDefault(s => s.SceneInfo.SNOId == _prioritySceneSNOId);
                _priorityScenePathPrecision = GetPriorityScenePathPrecision(priorityScenes.FirstOrDefault(s => s.SceneInfo.SNOId == nearestPriorityScene.Key));

                Logger.Debug("Found Priority Scene {0} - {1} Center {2} Distance {3:0}",
                    _currentPriorityScene.Name, _currentPriorityScene.SceneInfo.SNOId, _prioritySceneTarget, _prioritySceneTarget.Distance2D(MyPosition));
            }

            if (!foundPriorityScene)
            {
                _prioritySceneTarget = Vector3.Zero;
            }
        }

        private float GetPriorityScenePathPrecision(Scene scene)
        {
            return PriorityScenes.FirstOrDefault(ps => ps.SceneId != 0 && ps.SceneId == scene.SceneInfo.SNOId || scene.Name.ToLower().Contains(ps.SceneName.ToLower())).PathPrecision;
        }

        /// <summary>
        /// Gets the center of a given Navigation Zone
        /// </summary>
        /// <param name="zone"></param>
        /// <returns></returns>
        private Vector3 GetNavZoneCenter(NavZone zone)
        {
            float X = zone.ZoneMin.X + ((zone.ZoneMax.X - zone.ZoneMin.X) / 2);
            float Y = zone.ZoneMin.Y + ((zone.ZoneMax.Y - zone.ZoneMin.Y) / 2);

            return new Vector3(X, Y, 0);
        }

        /// <summary>
        /// Gets the center of a given Navigation Cell
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="zone"></param>
        /// <returns></returns>
        private Vector3 GetNavCellCenter(NavCell cell, NavZone zone)
        {
            return GetNavCellCenter(cell.Min, cell.Max, zone);
        }

        /// <summary>
        /// Gets the center of a given box with min/max, adjusted for the Navigation Zone
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="zone"></param>
        /// <returns></returns>
        private Vector3 GetNavCellCenter(Vector3 min, Vector3 max, NavZone zone)
        {
            float X = zone.ZoneMin.X + min.X + ((max.X - min.X) / 2);
            float Y = zone.ZoneMin.Y + min.Y + ((max.Y - min.Y) / 2);
            float Z = min.Z + ((max.Z - min.Z) / 2);

            return new Vector3(X, Y, Z);
        }

        /// <summary>
        /// Checks to see if the current DungeonExplorer node is in an Ignored scene, and marks the node immediately visited if so
        /// </summary>
        /// <returns></returns>
        private Composite CheckIgnoredScenes()
        {
            return
            new Decorator(ret => _timesForcedReset == 0 && IgnoreScenes != null && IgnoreScenes.Any(),
                new PrioritySelector(
                    new Decorator(ret => IsPositionInsideIgnoredScene(CurrentNavTarget),
                        new Sequence(
                            new Action(ret => SetNodeVisited("Node is in Ignored Scene"))
                        )
                    )
                )
            );
        }

        private bool IsPositionInsideIgnoredScene(Vector3 position)
        {
            return IgnoredAreas.Any(a => a.IsPositionInside(position));
        }

        /// <summary>
        /// Determines if a given Vector3 is in a provided IgnoreScene (if the scene is loaded)
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private bool PositionInsideIgnoredScene(Vector3 position)
        {
            List<Scene> ignoredScenes = ZetaDia.Scenes.GetScenes()
                .Where(scn => scn.IsValid && (IgnoreScenes.Any(igscn => !string.IsNullOrWhiteSpace(igscn.SceneName) && scn.Name.ToLower().Contains(igscn.SceneName.ToLower())) ||
                    IgnoreScenes.Any(igscn => scn.SceneInfo.SNOId == igscn.SceneId) &&
                    !PriorityScenes.Any(psc => !string.IsNullOrWhiteSpace(psc.SceneName) && scn.Name.ToLower().Contains(psc.SceneName)) &&
                    !PriorityScenes.Any(psc => psc.SceneId != -1 && scn.SceneInfo.SNOId != psc.SceneId))).ToList();

            foreach (Scene scene in ignoredScenes)
            {
                if (scene.Mesh.Zone == null)
                    return true;

                Vector2 pos = position.ToVector2();
                Vector2 min = scene.Mesh.Zone.ZoneMin;
                Vector2 max = scene.Mesh.Zone.ZoneMax;

                if (pos.X >= min.X && pos.X <= max.X && pos.Y >= min.Y && pos.Y <= max.Y)
                    return true;
            }
            return false;
        }

        #region Grid and Routes
        /// <summary>
        /// Determines if the current node can be marked as Visited, and does so if needed
        /// </summary>
        /// <returns></returns>
        private Composite CheckNodeFinished()
        {
            return
            new PrioritySelector(
                new Decorator(ret => _lastMoveResult == MoveResult.ReachedDestination,
                    new Sequence(
                        new Action(ret => SetNodeVisited("Reached Destination")),
                        new Action(ret => _lastMoveResult = MoveResult.Moved),
                        new Action(ret => UpdateRoute())
                    )
                ),
                new Decorator(ret => BrainBehavior.DungeonExplorer.CurrentNode.Visited,
                    new Sequence(
                        new Action(ret => Logger.Debug("Current node was already marked as visited!")),
                        new Action(ret => BrainBehavior.DungeonExplorer.CurrentRoute.Dequeue()),
                        new Action(ret => UpdateRoute())
                    )
                ),
                new Decorator(ret => GetRouteUnvisitedNodeCount() == 0 || !BrainBehavior.DungeonExplorer.CurrentRoute.Any(),
                    new Sequence(
                        new Action(ret => Logger.Debug("Error - CheckIsNodeFinished() called while Route is empty!")),
                        new Action(ret => UpdateRoute())
                    )
                ),
                new Decorator(ret => CurrentNavTarget.Distance2D(MyPosition) <= PathPrecision,
                    new Sequence(
                        new Action(ret => SetNodeVisited(String.Format("Node {0} is within PathPrecision ({1:0}/{2:0})",
                            CurrentNavTarget, CurrentNavTarget.Distance2D(MyPosition), PathPrecision))),
                        new Action(ret => UpdateRoute())
                    )
                ),
                new Decorator(ret => CurrentNavTarget.Distance2D(MyPosition) <= 90f && !MainGridProvider.CanStandAt(MainGridProvider.WorldToGrid(CurrentNavTarget.ToVector2())),
                    new Sequence(
                        new Action(ret => SetNodeVisited("Center Not Navigable")),
                        new Action(ret => UpdateRoute())
                    )
                ),
                new Decorator(ret => PositionCache.Cache.Any(p => p.Distance2DSqr(CurrentNavTarget) <= PathPrecision * PathPrecision),
                    new Sequence(
                        new Action(ret => SetNodeVisited("Found node to be in position cache, marking done")),
                        new Action(ret => UpdateRoute())
                    )
                ),
                CheckIgnoredScenes()
            );
        }

        /// <summary>
        /// Updates the DungeonExplorer Route
        /// </summary>
        private void UpdateRoute()
        {
            CheckResetDungeonExplorer();

            BrainBehavior.DungeonExplorer.Update();
            PrintNodeCounts("BrainBehavior.DungeonExplorer.Update");

            // Throw an exception if this shiz don't work
            ValidateCurrentRoute();
        }

        /// <summary>
        /// Marks the current dungeon Explorer as Visited and dequeues it from the route
        /// </summary>
        /// <param name="reason"></param>
        private void SetNodeVisited(string reason = "")
        {
            if (GetIsInPandemoniumFortress() && CurrentNavTarget != BrainBehavior.DungeonExplorer.CurrentNode.NavigableCenter)
            {
                var dungeonNode = GridSegmentation.Nodes.FirstOrDefault(n => n.NavigableCenter == CurrentNavTarget);
                if (dungeonNode != null)
                    dungeonNode.Visited = true;
            }
            else
            {
                Logger.Debug("Dequeueing current node {0} - {1}", CurrentNavTarget, reason);
                BrainBehavior.DungeonExplorer.CurrentNode.Visited = true;
                BrainBehavior.DungeonExplorer.CurrentRoute.Dequeue();
            }

            MarkNearbyNodesVisited();
            ClearDeathGateCheck();

            PrintNodeCounts("SetNodeVisited");
        }

        public void MarkNearbyNodesVisited()
        {
            bool anyMarked = false;
            foreach (DungeonNode node in GridSegmentation.Nodes.Where(n => !n.Visited))
            {
                var navCenter = node.NavigableCenter;
                float distance = navCenter.Distance2D(MyPosition);
                if (distance > PathPrecision)
                    continue;

                anyMarked = true;
                node.Visited = true;
                string reason2 = String.Format("Node {0} is within path precision {1:0}/{2:0}", navCenter, distance, PathPrecision);
                Logger.Debug("Marking unvisited nearby node as visited - {0}", reason2);
            }
            if (anyMarked)
                BrainBehavior.DungeonExplorer.Update();
        }

        /// <summary>
        /// Makes sure the current route is not null! Bad stuff happens if it's null...
        /// </summary>
        private static void ValidateCurrentRoute()
        {
            if (BrainBehavior.DungeonExplorer.CurrentRoute == null)
            {
                throw new ApplicationException("DungeonExplorer CurrentRoute is null");
            }
        }

        /// <summary>
        /// Prints a plethora of useful information about the Dungeon Exploration process
        /// </summary>
        /// <param name="step"></param>
        private void PrintNodeCounts(string step = "")
        {
            if (!QuestToolsSettings.Instance.DebugEnabled)
                return;

            var log = String.Format("Nodes [Unvisited: Route:{0} Grid:{2} | Grid-Visited: {1}] Box:{3}/{4} Step:{5} PP:{6:0} Dir: {7} Current: {8}",
                GetRouteUnvisitedNodeCount(),                                // 0
                GetGridSegmentationVisistedNodeCount(),                      // 1
                GetGridSegmentationUnvisitedNodeCount(),                     // 2
                GridSegmentation.BoxSize,                                    // 3
                GridSegmentation.BoxTolerance,                               // 4
                step,                                                        // 5
                PathPrecision,                                               // 6
                MathUtil.GetHeadingToPoint(CurrentNavTarget),                // 7
                StringUtils.GetSimplePosition(CurrentNavTarget)              // 8
                );

            Logger.Debug(log);
        }

        /*
         * Dungeon Explorer Nodes
         */
        /// <summary>
        /// Gets the number of unvisited nodes in the DungeonExplorer Route
        /// </summary>
        /// <returns></returns>
        private static int GetRouteUnvisitedNodeCount()
        {
            if (GetCurrentRouteNodeCount() > 0)
                return BrainBehavior.DungeonExplorer.CurrentRoute.Count(n => !n.Visited);
            return 0;
        }

        /// <summary>
        /// Gets the number of visisted nodes in the DungeonExplorer Route
        /// </summary>
        /// <returns></returns>
        private int GetRouteVisistedNodeCount()
        {
            if (GetCurrentRouteNodeCount() > 0)
                return BrainBehavior.DungeonExplorer.CurrentRoute.Count(n => n.Visited);
            return 0;
        }

        /// <summary>
        /// Gets the number of nodes in the DungeonExplorer Route
        /// </summary>
        /// <returns></returns>
        private static int GetCurrentRouteNodeCount()
        {
            if (BrainBehavior.DungeonExplorer.CurrentRoute != null)
                return BrainBehavior.DungeonExplorer.CurrentRoute.Count();
            return 0;
        }

        /*
         *  Grid Segmentation Nodes
         */
        /// <summary>
        /// Gets the number of Unvisited nodes as reported by the Grid Segmentation provider
        /// </summary>
        /// <returns></returns>
        private static int GetGridSegmentationUnvisitedNodeCount()
        {
            if (GetGridSegmentationNodeCount() > 0)
                return GridSegmentation.Nodes.Count(n => !n.Visited);
            return 0;
        }

        /// <summary>
        /// Gets the number of Visited nodes as reported by the Grid Segmentation provider
        /// </summary>
        /// <returns></returns>
        private int GetGridSegmentationVisistedNodeCount()
        {
            if (GetCurrentRouteNodeCount() > 0)
                return GridSegmentation.Nodes.Count(n => n.Visited);
            return 0;
        }

        /// <summary>
        /// Gets the total number of nodes with the current BoxSize/Tolerance as reported by the Grid Segmentation Provider
        /// </summary>
        /// <returns></returns>
        private static int GetGridSegmentationNodeCount()
        {
            if (GridSegmentation.Nodes != null)
                return GridSegmentation.Nodes.Count();
            return 0;
        }
        #endregion

        private MoveResult _lastMoveResult = MoveResult.Moved;
        private DateTime _lastGeneratedPath = DateTime.MinValue;
        #endregion

        private Vector3 _lastDestination;

        /// <summary>
        /// Moves the bot to the next DungeonExplorer node
        /// </summary>
        private void MoveToNextNode()
        {
            RecordPosition();

            if (_lastDestination != CurrentNavTarget)
            {
                Navigator.Clear();
                UpdateSearchGridProvider();
                ClearDeathGateCheck();

                Logger.Log("New Nav Target: {0}", CurrentNavTarget);
                _lastDestination = CurrentNavTarget;
            }

            //if (QuestToolsSettings.Instance.DebugEnabled)
            //{
            //    //NavigationProvider.EnableDebugLogging = true;

            //    nodeName = String.Format("DungeonNode {0} Distance: {1:0} Direction: {2}",
            //     StringUtils.GetProfileCoordinates(CurrentNavTarget),
            //     CurrentNavTarget.Distance(ZetaDia.Me.Position),
            //     MathUtil.GetHeadingToPoint(CurrentNavTarget));
            //}

            //Logger.Log("MoveToNextNode {0}", CurrentNavTarget);

            _lastMoveResult = Navigator.MoveTo(CurrentNavTarget);
        }

        /// <summary>
        /// Records current position to Position Cache
        /// </summary>
        private void RecordPosition()
        {
            PositionCache.RecordPosition();

            MarkNearbyNodesVisited();
        }

        /// <summary>
        /// Initizializes the profile tag and sets defaults as needed
        /// </summary>
        private void Init(bool forced = false)
        {
            if (BoxSize == 0)
                BoxSize = 25;

            if (BoxTolerance < 0.01f)
                BoxTolerance = 0.01f;

            if (PathPrecision < 1f)
                PathPrecision = BoxSize / 2f;

            const float minPathPrecision = 5f;

            if (PathPrecision < minPathPrecision)
                PathPrecision = minPathPrecision;

            if (ObjectDistance < 1f)
                ObjectDistance = 40f;

            if (MarkerDistance < 1f)
                MarkerDistance = 40f;

            if (TimeoutValue == 0 && ExploreTimeoutType != TimeoutType.None)
                TimeoutValue = 1800;

            PositionCache.Cache = new HashSet<Vector3>();
            _priorityScenesInvestigated.Clear();
            MiniMapMarker.KnownMarkers.Clear();
            if (PriorityScenes == null)
                PriorityScenes = new List<PrioritizeScene>();

            if (IgnoreScenes == null)
                IgnoreScenes = new List<IgnoreScene>();

            if (AlternateActors == null)
                AlternateActors = new List<AlternateActor>();

            if (!forced)
            {
                Logger.Debug(
                    "Initialized TrinityExploreDungeon: boxSize={0} boxTolerance={1:0.00} endType={2} timeoutType={3} timeoutValue={4} pathPrecision={5:0} sceneId={6} actorId={7} objectDistance={8} markerDistance={9} exitNameHash={10}",
                    GridSegmentation.BoxSize, GridSegmentation.BoxTolerance, EndType, ExploreTimeoutType, TimeoutValue, PathPrecision, SceneId, ActorId, ObjectDistance, MarkerDistance, ExitNameHash);
            }
            _initDone = true;
        }

        #region ProfileBehavior
        private bool _isDone;
        /// <summary>
        /// When true, the next profile tag is used
        /// </summary>
        public override bool IsDone
        {
            get { return !IsActiveQuestStep || _isDone; }
        }

        /// <summary>
        /// Resets this profile tag to defaults
        /// </summary>
        public override void ResetCachedDone()
        {
            Logger.Debug("TrinityExploreDungeon ResetCachedDone()");
            _isDone = false;
            _initDone = false;
            _timeoutBreached = false;
            _tagTimer.Reset();
            base.ResetCachedDone();
        }
        #endregion

        #region Adventure Mode
        public bool IsInAdventureMode()
        {
            // Only valid for Adventure mode
            if (ZetaDia.CurrentAct == Act.OpenWorld)
                return true;

            return false;
        }

        private DateTime _lastCheckRiftDone = DateTime.MinValue;

        public bool GetIsRiftDone()
        {
            if (DateTime.UtcNow.Subtract(_lastCheckRiftDone).TotalSeconds < 1)
                return false;

            _lastCheckRiftDone = DateTime.UtcNow;

            if (ZetaDia.Me.IsInBossEncounter)
                return false;

            // X1_LR_DungeonFinder
            if (ZetaDia.CurrentAct == Act.OpenWorld && DataDictionary.RiftWorldIds.Contains(ZetaDia.CurrentWorldId) &&
                ZetaDia.ActInfo.AllQuests.Any(q => q.QuestSNO == 337492 && q.QuestStep == 10))
            {
                Logger.Log("Rift Quest Complete!");
                return true;
            }

            int riftWorldIndex = DataDictionary.RiftWorldIds.IndexOf(ZetaDia.CurrentWorldId);
            if (riftWorldIndex != -1 &&
                ZetaDia.Minimap.Markers.CurrentWorldMarkers
                .Any(m => m.NameHash == DataDictionary.RiftPortalHashes[riftWorldIndex] &&
                    m.Position.Distance2D(MyPosition) <= MarkerDistance + 10f &&
                    (Math.Abs(m.Position.Z - ZetaDia.Me.Position.Z) <= 14f || Navigator.Raycast(MyPosition, m.Position)) &&
                    !MiniMapMarker.TownHubMarkers.Contains(m.NameHash)))
            {
                int marker = DataDictionary.RiftPortalHashes[riftWorldIndex];
                Logger.Log("Rift exit marker {0} within range!", marker);
                return true;
            }

            return false;
        }

        private DateTime _lastCheckBountyDone = DateTime.MinValue;
        public bool GetIsBountyDone()
        {
            try
            {
                if (DateTime.UtcNow.Subtract(_lastCheckBountyDone).TotalSeconds < 1)
                    return false;

                _lastCheckBountyDone = DateTime.UtcNow;

                // Only valid for Adventure mode
                if (ZetaDia.CurrentAct != Act.OpenWorld)
                    return false;

                // We're in a rift, not a bounty!
                if (ZetaDia.CurrentAct == Act.OpenWorld && DataDictionary.RiftWorldIds.Contains(ZetaDia.CurrentWorldId))
                    return false;

                if (ZetaDia.IsInTown)
                {
                    Logger.Log("In Town, Assuming done.");
                    return true;
                }

                if (ZetaDia.Me.IsInBossEncounter)
                    return false;

                // Bounty Turn-in
                if (ZetaDia.ActInfo.AllQuests.Any(q => DataDictionary.BountyTurnInQuests.Contains(q.QuestSNO) && q.State == QuestState.InProgress))
                {
                    Logger.Log("Bounty Turn-In available, Assuming done.");
                    return true;
                }

                var b = ZetaDia.ActInfo.ActiveBounty;
                if (b == null)
                {
                    Logger.Log("Active bounty returned null, Assuming done.");
                    return true;
                }
                if (ZetaDia.ActInfo.ActiveQuests.Any(q => q.Quest.ToString().ToLower().StartsWith("x1_AdventureMode_BountyTurnin") && q.State == QuestState.InProgress))
                {
                    Logger.Log("Bounty Turn-in quest is In-Progress, Assuming done.");
                    return true;
                }
                //If completed or on next step, we are good.
                if (b.Info.State == QuestState.Completed)
                {
                    Logger.Log("Seems completed!");
                    return true;
                }


            }
            catch (Exception ex)
            {
                Logger.Log("Exception reading ActiveBounty " + ex.Message);
            }

            return false;
        }
        #endregion

        #region Death Gate Handling


        private List<Vector3> _pandemoniumFortressCanPathCache = new List<Vector3>();
        private Vector3 _lastPandFortressTarget = Vector3.Zero;

        private void GetPandFortressNavTarget()
        {
            if (_lastPandFortressTarget != Vector3.Zero && GridSegmentation.Nodes.Any(n => n.Visited && n.NavigableCenter == _lastPandFortressTarget))
            {
                _lastPandFortressTarget = Vector3.Zero;
            }

            List<Tuple<Vector3, Vector2>> nodeList = GridSegmentation.Nodes.Where(n => !n.Visited).OrderBy(n => n.Center.DistanceSqr(MyPosition.ToVector2())).Select(n => new Tuple<Vector3, Vector2>(n.NavigableCenter, n.Center)).ToList();
            if (!nodeList.Any())
                return;

            _pandemoniumFortressCanPathCache.Clear();
            foreach (var nodeCenter in nodeList.Select(n => n.Item1))
            {
                bool canPath = NavigationProvider.CanPathWithinDistance(nodeCenter);
                // Find the first node we can path to (ordered by distance above)
                if (!canPath)
                    continue;
                _pandemoniumFortressCanPathCache.Add(nodeCenter);
                break;
            }

            _lastPandFortressTarget = nodeList
                .OrderBy(n => _pandemoniumFortressCanPathCache.Contains(n.Item1))
                .ThenBy(n => n.Item2.DistanceSqr(MyPosition.ToVector2()))
                .Select(n => n.Item1)
                .FirstOrDefault();
        }

        private static bool GetIsInPandemoniumFortress()
        {
            return DataDictionary.PandemoniumFortressWorlds.Contains(ZetaDia.CurrentWorldId) ||
                DataDictionary.PandemoniumFortressLevelAreaIds.Contains(ZetaDia.CurrentLevelAreaId);
        }


        private Vector3 _deathGateInteractStartPosition = Vector3.Zero;
        private MoveResult _navTargetMoveResult;
        private Decorator MoveToAndUseDeathGate()
        {
            return
            new Decorator(ret => GetIsInPandemoniumFortress() && AnyDeathGates && !CanFullPathToCurrentNavTarget(),
                new PrioritySelector(
                    new Decorator(ret => _deathGateInteractStartPosition != Vector3.Zero && MyPosition.Distance2D(_deathGateInteractStartPosition) > 15f,
                        new Sequence(
                            new Action(ret => ClearDeathGateCheck()),
                            new DecoratorContinue(ret => !CanFullPathToCurrentNavTarget(),
                                new Action(ret => SetNodeVisited("Unable to Path"))
                            )
                        )
                    ),
                    new Decorator(ret => _navTargetMoveResult != MoveResult.ReachedDestination,
                        new Sequence(
                            new Action(ret => Logger.Debug("Moving to Current Nav Target {0}", CurrentNavTarget)),
                            new Action(ret => _navTargetMoveResult = Navigator.MoveTo(CurrentNavTarget))
                        )
                    ),
                    new Decorator(ret => NearestDeathGate.Position.Distance2DSqr(ZetaDia.Me.Position) > 10f * 10f,
                        new Sequence(
                            new Action(ret => Logger.Debug("Moving to Death Gate at {0}", NearestDeathGate.Position)),
                            new Action(ret => _lastMoveResult = Navigator.MoveTo(NearestDeathGate.Position))
                        )
                    ),
                    new Sequence(
                        new Action(ret => Logger.Debug("Interacting With Death Gate")),
                        new Action(ret => _deathGateInteractStartPosition = MyPosition),
                        new Action(ret => NearestDeathGate.Interact()),
                        new Sleep(1500)
                    )
                )
            );
        }

        /// <summary>
        /// Resets all variables used with checking, moving to, and interacting with Death Gates
        /// </summary>
        private void ClearDeathGateCheck()
        {
            Logger.Debug("Clearing Death Gate Check");
            _deathGateInteractStartPosition = Vector3.Zero;
            _navTargetMoveResult = default(MoveResult);
            _canPathCache = new Dictionary<int, bool>();
            _lastPathCheckTarget = Vector3.Zero;
            _nearestDeathGate = null;
            _lastPandFortressTarget = Vector3.Zero;
            _pandemoniumFortressCanPathCache.Clear();
            NavigationProvider.Clear();
            UpdateRoute();
        }

        private Dictionary<int, bool> _canPathCache = new Dictionary<int, bool>();
        private DiaObject _nearestDeathGate;
        public DiaObject NearestDeathGate
        {
            get
            {
                if (!GetIsInPandemoniumFortress())
                    return null;

                if (_nearestDeathGate != null && _nearestDeathGate.IsValid)
                    return _nearestDeathGate;

                var deathgates =
                    (from o in ZetaDia.Actors.GetActorsOfType<DiaObject>(true)
                     where o.IsValid && DataDictionary.DeathGates.Contains(o.ActorSNO) && o.Distance < 500 &&
                     !_canPathCache.ContainsKey(o.RActorGuid)
                     orderby o.Position.Distance2D(CurrentNavTarget)
                     select o).ToList();

                if (!deathgates.Any())
                    return _nearestDeathGate;

                foreach (var portal in deathgates)
                {
                    _canPathCache.Add(portal.RActorGuid, NavigationProvider.CanPathWithinDistance(portal.Position, 10f));
                }

                if (QuestToolsSettings.Instance.DebugEnabled)
                {
                    string debugNoise = deathgates.Aggregate("Found DeathGates: ",
                        (current, p) => current + string.Format("\n{0} distance: {1:0}, distance to node: {2:0} canPath: {3}",
                            StringUtils.GetProfileCoordinates(p.Position), p.Distance, p.Position.Distance2D(CurrentNavTarget),
                            _canPathCache[p.RActorGuid]));
                    Logger.Debug(debugNoise);
                }

                _nearestDeathGate = deathgates.FirstOrDefault(p => _canPathCache[p.RActorGuid]);
                return _nearestDeathGate;
            }
        }

        public bool AnyDeathGates
        {
            get
            {
                return NearestDeathGate != null && NearestDeathGate.IsValid;

            }
        }
        #endregion

        public DefaultNavigationProvider NavigationProvider
        {
            get
            {
                return Navigator.GetNavigationProviderAs<DefaultNavigationProvider>();
            }
        }
    }
}

/*
 * Never need to call GridSegmentation.Update()
 * GridSegmentation.Reset() is automatically called on world change
 * DungeonExplorer.Reset() will reset the current route and revisit nodes
 * DungeonExplorer.Update() will update the current route to include new scenes
 */
