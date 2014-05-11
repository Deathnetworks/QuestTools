using System;
using System.Collections.Generic;
using System.Linq;
using QuestTools.ProfileTags;
using Zeta.Bot.Navigation;
using Zeta.Common;
using Zeta.Game;
using Zeta.TreeSharp;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools.Helpers
{
    /// <summary>
    /// Class to help track MiniMapMarkers during Dungeon Exploration
    /// </summary>
    public class MiniMapMarker : IEquatable<MiniMapMarker>
    {
        //private const int WAYPOINT_MARKER = -1751517829;

        private const int RiftGuardianHash = 1603556356;
        private const int WaypointHash = -1751517829;

        private const int PointOfInterestTexture = 81058;

        internal static HashSet<int> TownHubMarkers = new HashSet<int>
        {
            1877684886, // A5 Hub
            1683860485, // A5 Hub
        };

        public int MarkerNameHash { get; set; }
        public Vector3 Position { get; set; }
        public bool Visited { get; set; }
        public bool Failed { get; set; }
        public bool IsPointOfInterest { get; set; }
        public bool IsExit { get; set; }

        internal static List<MiniMapMarker> KnownMarkers = new List<MiniMapMarker>();

        internal static MoveResult LastMoveResult = MoveResult.Moved;

        internal static bool AnyUnvisitedMarkers()
        {
            return KnownMarkers.Any(m => !m.Visited && !m.Failed);
        }

        internal static void SetNearbyMarkersVisited(Vector3 near, float pathPrecision)
        {
            MiniMapMarker nearestMarker = GetNearestUnvisitedMarker(near);
            if (nearestMarker == null)
                return;

            foreach (MiniMapMarker marker in KnownMarkers
                .Where(m => m.Equals(nearestMarker) &&
                near.Distance2D(m.Position) <= pathPrecision &&
                DataDictionary.RiftPortalHashes.Contains(m.MarkerNameHash)))
            {
                Logger.Log("Setting MiniMapMarker {0} as Visited, within PathPrecision {1:0}", marker.MarkerNameHash, pathPrecision);
                marker.Visited = true;
                LastMoveResult = MoveResult.Moved;
            }

            // Navigator will return "ReacheDestination" when it can't fully move to the specified position
            if (LastMoveResult == MoveResult.ReachedDestination)
            {
                foreach (MiniMapMarker marker in KnownMarkers.Where(m => m.Equals(nearestMarker)))
                {
                    Logger.Log("Setting MiniMapMarker {0} as Visited, MoveResult=ReachedDestination", marker.MarkerNameHash);
                    marker.Visited = true;
                    LastMoveResult = MoveResult.Moved;
                }
            }

            if (LastMoveResult != MoveResult.PathGenerationFailed)
                return;
            foreach (MiniMapMarker marker in KnownMarkers.Where(m => m.Equals(nearestMarker)))
            {
                Logger.Log("Unable to navigate to marker, setting MiniMapMarker {0} at {1} as failed", marker.MarkerNameHash, marker.Position);
                marker.Failed = true;
                LastMoveResult = MoveResult.Moved;
            }
        }

        internal static MiniMapMarker GetNearestUnvisitedMarker(Vector3 near)
        {
            return KnownMarkers
                .OrderBy(m => !m.IsPointOfInterest)
                .ThenBy(m => m.Position.Distance2DSqr(near))
                .FirstOrDefault(m => !m.Visited && !m.Failed);
        }

        private static DefaultNavigationProvider _navProvider;

        internal static void UpdateFailedMarkers()
        {
            if (_navProvider == null)
                _navProvider = new DefaultNavigationProvider();

            foreach (MiniMapMarker marker in KnownMarkers.Where(m => m.Failed).Where(marker => _navProvider.CanPathWithinDistance(marker.Position, 10f)))
            {
                Logger.Log("Was able to generate full path to failed MiniMapMarker {0} at {1}, marking as good", marker.MarkerNameHash, marker.Position);
                marker.Failed = false;
                LastMoveResult = MoveResult.PathGenerated;
            }
        }

        private static Composite CreateAddRiftMarkers()
        {
            return
            new DecoratorContinue(ret => ZetaDia.CurrentAct == Act.OpenWorld && DataDictionary.RiftWorldIds.Contains(ZetaDia.CurrentWorldId),
                new Action(ret =>
                    {
                        foreach (var nameHash in DataDictionary.RiftPortalHashes)
                        {
                            AddMarkersToList(nameHash);
                        }

                        foreach (var marker in ZetaDia.Minimap.Markers.CurrentWorldMarkers.Where(m => (m.IsPortalExit || m.IsPointOfInterest) && !TownHubMarkers.Contains(m.NameHash)))
                        {
                            AddMarkersToList(marker.NameHash);
                        }
                    })
            );
        }

        private static IEnumerable<Zeta.Game.Internals.MinimapMarker> GetMarkerList(int includeMarker)
        {
            return ZetaDia.Minimap.Markers.CurrentWorldMarkers
                .Where(m => (
                    m.NameHash == includeMarker ||
                    m.NameHash == 0 ||
                    m.NameHash == RiftGuardianHash ||
                    m.IsPointOfInterest || // Already matches POI hash
                    m.IsPortalExit ||
                    m.Position.ToVector2().DistanceSqr(ZetaDia.Me.Position.ToVector2()) <= 500 * 500) && // within 500 yards
                    !KnownMarkers.Any(ml => ml.Position == m.Position && ml.MarkerNameHash == m.NameHash))
                    .OrderBy(m => m.IsPointOfInterest)
                    .ThenBy(m => m.Position.ToVector2().DistanceSqr(ZetaDia.Me.Position.ToVector2()));
        }

        internal static void AddMarkersToList(int includeMarker = 0)
        {
            foreach (Zeta.Game.Internals.MinimapMarker marker in GetMarkerList(includeMarker))
            {
                MiniMapMarker mmm = new MiniMapMarker
                {
                    MarkerNameHash = marker.NameHash,
                    Position = marker.Position,
                    Visited = false,
                    IsExit = marker.IsPortalExit,
                    IsPointOfInterest = marker.IsPointOfInterest,
                };

                float distance = mmm.Position.Distance2D(ZetaDia.Me.Position);

                Logger.Log("Adding MiniMapMarker {0} at {1}, IsPOI: {2}, IsExit: {3}, Distance: {4:0} to KnownMarkers",
                    mmm.MarkerNameHash, mmm.Position, mmm.IsPointOfInterest, mmm.IsExit, distance);

                KnownMarkers.Add(mmm);
            }
        }

        internal static void AddMarkersToList(List<ExploreDungeonTag.Objective> objectives)
        {
            if (objectives == null)
                return;

            foreach (var objective in objectives.Where(o => o.MarkerNameHash != 0)
                .Where(objective => ZetaDia.Minimap.Markers.CurrentWorldMarkers.Any(m => m.NameHash == objective.MarkerNameHash)))
            {
                AddMarkersToList(objective.MarkerNameHash);
            }
        }

        internal static void AddMarkersToList(List<ExploreDungeonTag.AlternateMarker> markers)
        {
            if (markers == null)
                return;

            foreach (var marker in markers.Where(o => o.MarkerNameHash != 0)
                .Where(marker => ZetaDia.Minimap.Markers.CurrentWorldMarkers.Any(m => m.NameHash == marker.MarkerNameHash)))
            {
                AddMarkersToList(marker.MarkerNameHash);
            }
        }

        internal static Composite DetectMiniMapMarkers(int includeMarker = 0)
        {
            return
            new Sequence(
                CreateAddRiftMarkers(),
                new DecoratorContinue(ret => ZetaDia.Minimap.Markers.CurrentWorldMarkers
                    .Any(m => (m.NameHash == 0 || m.NameHash == includeMarker) && !KnownMarkers.Any(m2 => m2.Position != m.Position && m2.MarkerNameHash == m.NameHash)),
                    new Sequence(
                    new Action(ret => AddMarkersToList(includeMarker))
                    )
                )
            );
        }

        internal static Composite DetectMiniMapMarkers(List<ExploreDungeonTag.Objective> objectives)
        {
            return
            new Sequence(
                new Action(ret => AddMarkersToList(objectives))
            );
        }

        internal static Composite DetectMiniMapMarkers(List<ExploreDungeonTag.AlternateMarker> markers)
        {
            return
            new Sequence(
                new Action(ret => AddMarkersToList(markers))
            );
        }

        internal static Composite VisitMiniMapMarkers(Vector3 near, float markerDistance)
        {
            return
            new Decorator(ret => AnyUnvisitedMarkers(),
                new Sequence(
                    new DecoratorContinue(ret => LastMoveResult == MoveResult.ReachedDestination,
                        new Action(ret => SetNearbyMarkersVisited(ZetaDia.Me.Position, markerDistance))
                    ),
                    new Decorator(ret => GetNearestUnvisitedMarker(ZetaDia.Me.Position) != null,
                        new Sequence(ctx => GetNearestUnvisitedMarker(near),
                            new Action(ret => LastMoveResult = Navigator.MoveTo((ret as MiniMapMarker).Position)),
                            new Action(ret => Logger.Log("Moved to inspect nameHash {0} at {1}, IsPOI: {2} IsExit: {3} MoveResult: {4}",
                                (ret as MiniMapMarker).MarkerNameHash, 
                                (ret as MiniMapMarker).Position,
                                (ret as MiniMapMarker).IsPointOfInterest,
                                (ret as MiniMapMarker).IsExit,
                                LastMoveResult))
                        )
                    )
                )
            );
        }

        public bool Equals(MiniMapMarker other)
        {
            return other.Position == Position && other.MarkerNameHash == MarkerNameHash;
        }
    }
}
