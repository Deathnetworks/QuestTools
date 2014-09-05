﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Zeta.Bot;
using Zeta.Bot.Dungeons;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;

namespace QuestTools.Navigation
{
    public enum RouteMode
    {
        Default = 0, // TSP, default is default
        NearestUnvisited, // Simple sort on distance
        FurthestUnvisited, // Simple sort on distance
        NearestMinimapUnvisited, // Simple sort on distance with Minimap checking
        FurthestMinimapUnvisited, // Simple sort on distance with Minimap checking
        WeightedNearestUnvisited, // Rank by number of unvisited nodes connected to node
        WeightedNearestVisisted, // Rank by number of visisted nodes connected to node
        WeightedNearestMinimapUnvisited, // Rank by number of unvisited nodes connected to node, as shown on minimap
        WeightedNearestMinimapVisisted, // Rank by number of visisted nodes connected to node, as shown on minimap
        SceneTSP, // Scene exploration, traveling salesman problem
        SceneDirection, // Scene exploration, by direction
    }

    public enum Direction
    {
        Any = 0,
        North,
        South,
        East,
        West,
        NorthEast,
        NorthWest,
        SouthEast,
        SouthWest,
    }

    public class GridRoute
    {
        static GridRoute()
        {
            GameEvents.OnGameJoined += GameEvents_OnGameJoined;
            Pulsator.OnPulse += Pulsator_OnPulse;
        }

        private static void Pulsator_OnPulse(object sender, EventArgs e)
        {
            if (!ZetaDia.IsInGame || ZetaDia.IsLoadingWorld)
                return;

            if (CheckNewSceneLoaded())
            {
                Update();
            }
        }
        private static readonly HashSet<int> LoadedSceneIds = new HashSet<int>();

        private static bool CheckNewSceneLoaded()
        {
            bool newScenes = false;
            foreach (Scene scene in ZetaDia.Scenes.GetScenes())
            {
                if (!LoadedSceneIds.Contains(scene.Mesh.SceneId))
                {
                    newScenes = true;
                    LoadedSceneIds.Add(scene.Mesh.SceneId);
                }
            }

            // No new scenes have been loaded... so ignore it!
            if (!newScenes)
                return false;

            return true;
        }
        private static void GameEvents_OnGameJoined(object sender, EventArgs eventArgs)
        {
            Reset();
        }

        private const double MaxConnectedNodes = 8d;

        private static DungeonExplorer DungeonExplorer { get { return Zeta.Bot.Logic.BrainBehavior.DungeonExplorer; } }

        internal static ConcurrentBag<DungeonNode> GridNodes
        {
            get
            {
                switch (RouteMode)
                {
                    case RouteMode.SceneDirection:
                    case RouteMode.SceneTSP:
                        return SceneSegmentation.Nodes;
                    default:
                        return GridSegmentation.Nodes;
                }
            }
            set
            {
                switch (RouteMode)
                {
                    case RouteMode.SceneDirection:
                    case RouteMode.SceneTSP:
                        SceneSegmentation.Nodes = value;
                        break;
                    default:
                        GridSegmentation.Nodes = value;
                        break;
                }
            }
        }

        private static List<DungeonNode> AllNodes { get { return GridNodes.ToList(); } }
        private static List<DungeonNode> VisitedNodes { get { return GridNodes.Where(n => n.Visited).ToList(); } }
        private static List<DungeonNode> UnVisitedNodes { get { return GridNodes.Where(n => !n.Visited).ToList(); } }

        private static RouteMode _routeMode = RouteMode.Default;
        internal static RouteMode RouteMode
        {
            get
            {
                if (QuestToolsSettings.Instance.ForceRouteMode)
                    return QuestToolsSettings.Instance.RouteMode;
                return _routeMode;
            }
            set
            {
                _routeMode = value;
            }
        }

        internal static Direction Direction { get; set; }

        private static Queue<DungeonNode> _currentRoute;
        internal static Queue<DungeonNode> CurrentRoute
        {
            get
            {
                if (_currentRoute == null || _currentRoute.Count == 0)
                    Update();
                return _currentRoute;
            }
            private set
            {
                _currentRoute = value;
            }
        }

        internal static void SetCurrentNodeExplored()
        {
            var currentNode = CurrentRoute.Dequeue();
            currentNode.Visited = true;
        }

        internal static Vector3 GetCurrentDestination()
        {
            if (CurrentRoute != null && CurrentRoute.Peek() != null)
                return CurrentRoute.Peek().NavigableCenter;

            // Fallback
            return DungeonExplorer.CurrentRoute.Peek().NavigableCenter;
        }

        internal static void Update()
        {
            GridSegmentation.Update();

            switch (RouteMode)
            {
                case RouteMode.Default:
                    DungeonExplorer.Update();
                    break;
                case RouteMode.SceneTSP:
                case RouteMode.SceneDirection:
                    SceneSegmentation.Update();
                    break;
            }

            _currentRoute = GetRoute();
        }

        internal static Queue<DungeonNode> GetRoute()
        {
            Logger.Log("Generating new route with Route Mode {0}", RouteMode);
            Queue<DungeonNode> route = new Queue<DungeonNode>();

            switch (RouteMode)
            {
                case RouteMode.Default:
                    route = Zeta.Bot.Logic.BrainBehavior.DungeonExplorer.CurrentRoute;
                    break;
                case RouteMode.FurthestUnvisited:
                    route = GetFurthestUnvisitedRoute();
                    break;
                case RouteMode.NearestUnvisited:
                    route = GetNearestUnvisitedRoute();
                    break;
                case RouteMode.NearestMinimapUnvisited:
                    route = GetNearestMinimapUnvisitedRoute();
                    break;
                case RouteMode.FurthestMinimapUnvisited:
                    route = GetFurthestMinimapUnvisitedRoute();
                    break;
                case RouteMode.WeightedNearestUnvisited:
                    route = GetWeightedNearestUnvisitedRoute();
                    break;
                case RouteMode.WeightedNearestVisisted:
                    route = GetWeightedNearestVisitedRoute();
                    break;
                case RouteMode.WeightedNearestMinimapUnvisited:
                    route = GetWeightedNearestMinimapUnvisitedRoute();
                    break;
                case RouteMode.WeightedNearestMinimapVisisted:
                    route = GetWeightedNearestMinimapVisitedRoute();
                    break;
                case RouteMode.SceneTSP:
                    route = GetSceneNearestNeighborRoute();
                    break;
                case RouteMode.SceneDirection:
                    route = GetSceneDirectionRoute();
                    break;
            }

            if (SetNodesExploredAutomatically && route != null)
            {
                foreach (var node in route.Where(n => ZetaDia.Minimap.IsExplored(n.NavigableCenter, ZetaDia.Me.WorldDynamicId)))
                {
                    node.Visited = true;
                }
            }

            return route;
        }

        private static Queue<DungeonNode> GetNearestUnvisitedRoute()
        {
            Queue<DungeonNode> route = new Queue<DungeonNode>();
            Vector3 myPosition = ZetaDia.Me.Position;
            foreach (var node in GridNodes.Where(node => !node.Visited).OrderBy(node => node.NavigableCenter.Distance2DSqr(myPosition)).ToList())
            {
                route.Enqueue(node);
            }
            return route;
        }

        private static Queue<DungeonNode> GetFurthestUnvisitedRoute()
        {
            Queue<DungeonNode> route = new Queue<DungeonNode>();
            Vector3 myPosition = ZetaDia.Me.Position;
            foreach (var node in GridNodes.Where(node => !node.Visited).OrderByDescending(node => node.NavigableCenter.Distance2DSqr(myPosition)).ToList())
            {
                route.Enqueue(node);
            }
            return route;
        }

        private static Queue<DungeonNode> GetNearestMinimapUnvisitedRoute()
        {
            Queue<DungeonNode> route = new Queue<DungeonNode>();
            Vector3 myPosition = ZetaDia.Me.Position;
            foreach (var node in GridNodes.Where(node => !node.Visited)
                .OrderBy(node => ZetaDia.Minimap.IsExplored(node.NavigableCenter, ZetaDia.Me.WorldDynamicId))
                .ThenBy(node => node.NavigableCenter.Distance2DSqr(myPosition)).ToList())
            {
                route.Enqueue(node);
            }
            return route;
        }

        private static Queue<DungeonNode> GetFurthestMinimapUnvisitedRoute()
        {
            Queue<DungeonNode> route = new Queue<DungeonNode>();
            Vector3 myPosition = ZetaDia.Me.Position;
            foreach (var node in GridNodes.Where(node => !node.Visited)
                .OrderBy(node => ZetaDia.Minimap.IsExplored(node.NavigableCenter, ZetaDia.Me.WorldDynamicId))
                .ThenByDescending(node => node.NavigableCenter.Distance2DSqr(myPosition)).ToList())
            {
                route.Enqueue(node);
            }
            return route;
        }

        /// <summary>
        /// Gets the weighted nearest unvisited route.
        /// </summary>
        /// <returns></returns>
        internal static Queue<DungeonNode> GetWeightedNearestUnvisitedRoute()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            Vector3 myPosition = ZetaDia.Me.Position;

            Queue<DungeonNode> route = new Queue<DungeonNode>();
            List<WeightedDungeonNode> weightedNodes = new List<WeightedDungeonNode>();

            // We want to give a high weight to nodes which have unexplored nodes near it
            // A maximum weight will be given to a node with 4 directly connected unexplored (facing) nodes, and 4 corner-connected nodes
            // This is theoretically possible if we are standing IN this maximum-weighted unexplored node
            // Typically a node will have 1 or more directly connected nodes and 0 or more corner-connected nodes

            foreach (var unWeightedNode in UnVisitedNodes)
            {
                var weightedNode = new WeightedDungeonNode(unWeightedNode.WorldTopLeft, unWeightedNode.WorldBottomRight) { Weight = 0 };

                // Grab unvisited nodes only, this is what we'll use for our Weighting
                int numNodesConnected = UnVisitedNodes.Count(node => node.GridCenter.DistanceSqr(weightedNode.GridCenter) <= (MaxCornerDistance * MaxCornerDistance));
                weightedNode.Weight = numNodesConnected / MaxConnectedNodes;
                weightedNodes.Add(weightedNode);
            }

            foreach (var node in weightedNodes
                .OrderByDescending(n => n.Weight)
                .OrderBy(n => n.NavigableCenter.Distance2DSqr(myPosition)))
            {
                route.Enqueue(node);
            }

            Logger.Log("Generated new Weighted Nearest Unvisited Route with {0} nodes in {1}ms", route.Count, timer.ElapsedMilliseconds);
            return route;
        }

        internal static Queue<DungeonNode> GetWeightedNearestVisitedRoute()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            Vector3 myPosition = ZetaDia.Me.Position;

            Queue<DungeonNode> route = new Queue<DungeonNode>();
            List<WeightedDungeonNode> weightedNodes = new List<WeightedDungeonNode>();

            // We want to give a high weight to nodes which have Explored nodes near it
            // A maximum weight will be given to an unexplored node with 4 directly connected explored (facing) nodes, and 4 corner-connected nodes
            // This is theoretically possible if we are standing IN this maximum-weighted unexplored node
            // Typically a node will have 1 or more directly connected nodes and 0 or more corner-connected nodes

            foreach (var unWeightedNode in UnVisitedNodes)
            {
                var weightedNode = new WeightedDungeonNode(unWeightedNode.WorldTopLeft, unWeightedNode.WorldBottomRight) { Weight = 0 };

                // Number of visited nodes connected to this unvisited node will give higher weight
                int numNodesConnected = VisitedNodes.Count(node => node.GridCenter.DistanceSqr(weightedNode.GridCenter) <= (MaxCornerDistance * MaxCornerDistance));
                weightedNode.Weight = numNodesConnected / MaxConnectedNodes;
                weightedNodes.Add(weightedNode);
            }

            foreach (var node in weightedNodes
                .OrderByDescending(n => n.Weight)
                .OrderBy(n => n.NavigableCenter.Distance2DSqr(myPosition)))
            {
                route.Enqueue(node);
            }
            Logger.Log("Generated new Weighted Nearest Visited Route with {0} nodes in {1}ms", route.Count, timer.ElapsedMilliseconds);
            return route;
        }

        internal static Queue<DungeonNode> GetWeightedNearestMinimapUnvisitedRoute()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            Vector3 myPosition = ZetaDia.Me.Position;

            Queue<DungeonNode> route = new Queue<DungeonNode>();
            List<WeightedDungeonNode> weightedNodes = new List<WeightedDungeonNode>();

            // We want to give a high weight to nodes which have unexplored nodes near it
            // A maximum weight will be given to a node with 4 directly connected unexplored (facing) nodes, and 4 corner-connected nodes
            // This is theoretically possible if we are standing IN this maximum-weighted unexplored node
            // Typically a node will have 1 or more directly connected nodes and 0 or more corner-connected nodes

            foreach (var unWeightedNode in UnVisitedNodes)
            {
                var weightedNode = new WeightedDungeonNode(unWeightedNode.WorldTopLeft, unWeightedNode.WorldBottomRight) { Weight = 0 };

                // Grab unvisited nodes only, this is what we'll use for our Weighting
                int numNodesConnected = UnVisitedNodes.Count(node => node.GridCenter.DistanceSqr(weightedNode.GridCenter) <= (MaxCornerDistance * MaxCornerDistance));
                weightedNode.Weight = numNodesConnected / MaxConnectedNodes;
                weightedNodes.Add(weightedNode);
            }

            foreach (var node in weightedNodes
                .OrderBy(n => ZetaDia.Minimap.IsExplored(n.NavigableCenter, ZetaDia.Me.WorldDynamicId))
                .OrderByDescending(n => n.Weight)
                .OrderBy(n => n.NavigableCenter.Distance2DSqr(myPosition)))
            {
                route.Enqueue(node);
            }

            Logger.Log("Generated new Weighted Nearest Minimap Unvisited Route with {0} nodes in {1}ms", route.Count, timer.ElapsedMilliseconds);
            return route;
        }

        internal static Queue<DungeonNode> GetWeightedNearestMinimapVisitedRoute()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            Vector3 myPosition = ZetaDia.Me.Position;

            Queue<DungeonNode> route = new Queue<DungeonNode>();
            List<WeightedDungeonNode> weightedNodes = new List<WeightedDungeonNode>();

            // We want to give a high weight to nodes which have Explored nodes near it
            // A maximum weight will be given to an unexplored node with 4 directly connected explored (facing) nodes, and 4 corner-connected nodes
            // This is theoretically possible if we are standing IN this maximum-weighted unexplored node
            // Typically a node will have 1 or more directly connected nodes and 0 or more corner-connected nodes

            foreach (var unWeightedNode in UnVisitedNodes)
            {
                var weightedNode = new WeightedDungeonNode(unWeightedNode.WorldTopLeft, unWeightedNode.WorldBottomRight) { Weight = 0 };

                // Number of visited nodes connected to this unvisited node will give higher weight
                int numNodesConnected = VisitedNodes.Count(node => node.GridCenter.DistanceSqr(weightedNode.GridCenter) <= (MaxCornerDistance * MaxCornerDistance));
                weightedNode.Weight = numNodesConnected / MaxConnectedNodes;
                weightedNodes.Add(weightedNode);
            }

            foreach (var node in weightedNodes
                .OrderBy(n => ZetaDia.Minimap.IsExplored(n.NavigableCenter, ZetaDia.Me.WorldDynamicId))
                .OrderByDescending(n => n.Weight)
                .OrderBy(n => n.NavigableCenter.Distance2DSqr(myPosition)))
            {
                route.Enqueue(node);
            }

            Logger.Log("Generated new Weighted Nearest Minimap Visited Route with {0} nodes in {1}ms", route.Count, timer.ElapsedMilliseconds);
            return route;
        }

        /// <summary>
        /// Uses Nearest Neighbor Simple TSP
        /// </summary>
        /// <returns></returns>
        internal static Queue<DungeonNode> GetSceneNearestNeighborRoute()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            Vector3 myPosition = ZetaDia.Me.Position;

            Queue<DungeonNode> route = new Queue<DungeonNode>();
            SceneSegmentation.Update();

            if (!UnVisitedNodes.Any())
                return default(Queue<DungeonNode>);

            List<DungeonNode> unsortedNodes = UnVisitedNodes.ToList();
            List<DungeonNode> sortedNodes = new List<DungeonNode>();

            var nearestNode = unsortedNodes.OrderBy(n => n.NavigableCenter.Distance2DSqr(myPosition)).First();
            sortedNodes.Add(nearestNode);
            unsortedNodes.Remove(nearestNode);

            // Enqueue closest node
            while (unsortedNodes.Any())
            {
                var nextNode = unsortedNodes
                    // Minimap Unvisited first
                    .OrderBy(n => ZetaDia.Minimap.IsExplored(n.NavigableCenter, ZetaDia.Me.WorldDynamicId))
                    .ThenBy(n => n.NavigableCenter.Distance2DSqr(sortedNodes.Last().NavigableCenter))
                    .First();
                sortedNodes.Add(nextNode);
                unsortedNodes.Remove(nextNode);
            }

            route = new Queue<DungeonNode>(sortedNodes);

            Logger.Log("Generated new Scene Route with {0} nodes in {1}ms", route.Count, timer.ElapsedMilliseconds);
            return route;
        }

        internal static Queue<DungeonNode> GetSceneDirectionRoute()
        {
            if (Direction == Direction.Any)
            {
                Logger.Log("No Direction selected for Scene Route - using Scene TSP");
                return GetSceneNearestNeighborRoute();
            }

            Stopwatch timer = new Stopwatch();
            timer.Start();
            Vector3 myPosition = ZetaDia.Me.Position;

            Queue<DungeonNode> route = new Queue<DungeonNode>();

            /* 
             * North = -X
             * South = +X
             * East = +Y
             * West = -Y
             * NorthEast = -X+Y
             * NorthWest = -X-Y
             * SouthEast = +X+Y
             * SouthWest = +X-Y
             */
            switch (Direction)
            {
                case Direction.North:
                    route = new Queue<DungeonNode>(UnVisitedNodes.OrderBy(node => node.GridCenter.X));
                    break;
                case Direction.South:
                    route = new Queue<DungeonNode>(UnVisitedNodes.OrderByDescending(node => node.GridCenter.X));
                    break;
                case Direction.East:
                    route = new Queue<DungeonNode>(UnVisitedNodes.OrderByDescending(node => node.GridCenter.Y));
                    break;
                case Direction.West:
                    route = new Queue<DungeonNode>(UnVisitedNodes.OrderBy(node => node.GridCenter.Y));
                    break;
                case Direction.NorthEast:
                    route = new Queue<DungeonNode>(UnVisitedNodes.OrderBy(node => (node.GridCenter.X - node.GridCenter.Y)));
                    break;
                case Direction.NorthWest:
                    route = new Queue<DungeonNode>(UnVisitedNodes.OrderBy(node => (node.GridCenter.X + node.GridCenter.Y)));
                    break;
                case Direction.SouthEast:
                    route = new Queue<DungeonNode>(UnVisitedNodes.OrderByDescending(node => (node.GridCenter.X + node.GridCenter.Y)));
                    break;
                case Direction.SouthWest:
                    route = new Queue<DungeonNode>(UnVisitedNodes.OrderByDescending(node => (node.GridCenter.X - node.GridCenter.Y)));
                    break;
            }

            Logger.Log("Generated new Scene Direction Route with {0} nodes in {1}ms", route.Count, timer.ElapsedMilliseconds);
            return route;

        }


        internal static DungeonNode CurrentNode
        {
            get { return CurrentRoute.Peek(); }
        }

        internal static bool SetNodesExploredAutomatically
        {
            get { return DungeonExplorer.SetNodesExploredAutomatically; }
            set { DungeonExplorer.SetNodesExploredAutomatically = value; }
        }

        internal static int BoxSize
        {
            get { return GridSegmentation.BoxSize; }
            set { GridSegmentation.BoxSize = value; }
        }

        private static double BoxSquared { get { return BoxSize * BoxSize; } }
        private static double MaxCornerDistance { get { return Math.Sqrt(BoxSquared + BoxSquared); } }


        internal static float BoxTolerance
        {
            get { return GridSegmentation.BoxTolerance; }
            set { GridSegmentation.BoxTolerance = value; }
        }

        internal static void Reset(int boxSize = 30, float boxTolerance = 0.05f)
        {
            GridSegmentation.Reset(boxSize, boxTolerance);
            GridSegmentation.Update();

            switch (RouteMode)
            {
                case RouteMode.SceneDirection:
                case RouteMode.SceneTSP:
                    SceneSegmentation.Reset();
                    break;
            }

            Update();
        }
    }

}
