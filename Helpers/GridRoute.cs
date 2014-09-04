using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Zeta.Bot.Dungeons;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;

namespace QuestTools.Helpers
{
    public enum RouteMode
    {
        Default = 0, // TSP, default is default
        NearestUnvisited, // Simple sort on distance
        FurthestUnvisited, // Simple sort on distance
        WeightedNearestUnvisited, // Rank by number of unvisited nodes connected to node
        WeightedNearestVisisted, // Rank by number of visisted nodes connected to node
        WeightedNearestMinimapUnvisited, // Rank by number of unvisited nodes connected to node, as shown on minimap
        WeightedNearestMinimapVisisted, // Rank by number of visisted nodes connected to node, as shown on minimap
    }

    public class GridRoute
    {
        private const double MaxConnectedNodes = 8d;

        private static DungeonExplorer DungeonExplorer { get { return Zeta.Bot.Logic.BrainBehavior.DungeonExplorer; } }

        private static ConcurrentBag<DungeonNode> GridNodes
        {
            get { return GridSegmentation.Nodes; }
            set { GridSegmentation.Nodes = value; }
        }

        private static List<DungeonNode> AllNodes { get { return GridNodes.ToList(); } }
        private static List<DungeonNode> VisitedNodes { get { return GridNodes.Where(n => n.Visited).ToList(); } }
        private static List<DungeonNode> UnVisitedNodes { get { return GridNodes.Where(n => !n.Visited).ToList(); } } 

        internal static RouteMode RouteMode { get; set; }

        private static Queue<DungeonNode> _currentRoute;
        internal static Queue<DungeonNode> CurrentRoute
        {
            get
            {
                if (_currentRoute == null || _currentRoute.Count == 0)
                    _currentRoute = GetRoute(RouteMode);
                return _currentRoute;
            }
            private set
            {
                _currentRoute = value;
            }
        }

        public static void SetCurrentNodeExplored()
        {
            var currentNode = CurrentRoute.Dequeue();
            currentNode.Visited = true;
        }

        public static Vector3 GetCurrentDestination()
        {
            if (CurrentRoute != null && CurrentRoute.Peek() != null)
                return CurrentRoute.Peek().NavigableCenter;

            // Fallback
            return DungeonExplorer.CurrentRoute.Peek().NavigableCenter;
        }


        public static Queue<DungeonNode> GetRoute(RouteMode routeMode = RouteMode.Default)
        {
            Queue<DungeonNode> route = new Queue<DungeonNode>();

            switch (routeMode)
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
                case RouteMode.WeightedNearestUnvisited:
                    route = GetWeightedNearestUnvisitedRoute();
                    break;
                case RouteMode.WeightedNearestVisisted:
                    route = GetWeightedNearestVisitedRoute();
                    break;
                case RouteMode.WeightedNearestMinimapUnvisited:
                    break;
                case RouteMode.WeightedNearestMinimapVisisted:
                    break;
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

        /// <summary>
        /// Gets the weighted nearest unvisited route.
        /// </summary>
        /// <returns></returns>
        public static Queue<DungeonNode> GetWeightedNearestUnvisitedRoute()
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

            Logger.Log("Generated new Weighted Nearest Unvisited Route in {0}ms", timer.ElapsedMilliseconds);
            return route;
        }

        public static Queue<DungeonNode> GetWeightedNearestVisitedRoute()
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
            Logger.Log("Generated new Weighted Nearest Visited Route in {0}ms", timer.ElapsedMilliseconds);
            return route;
        }

        public static Queue<DungeonNode> GetWeightedNearestMinimapUnvisitedRoute()
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
                .OrderByDescending(n => ZetaDia.Minimap.IsExplored(n.NavigableCenter, ZetaDia.Me.WorldDynamicId))
                .OrderByDescending(n => n.Weight)
                .OrderBy(n => n.NavigableCenter.Distance2DSqr(myPosition)))
            {
                route.Enqueue(node);
            }

            Logger.Log("Generated new Weighted Nearest Minimap Unvisited Route in {0}ms", timer.ElapsedMilliseconds);
            return route;
        }

        public static Queue<DungeonNode> GetWeightedNearestMinimapVisitedRoute()
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

            Logger.Log("Generated new Weighted Nearest Minimap Unvisited Route in {0}ms", timer.ElapsedMilliseconds);
            return route;
        }

        public static DungeonNode CurrentNode
        {
            get { return CurrentRoute.Peek(); }
        }

        public static int BoxSize
        {
            get { return GridSegmentation.BoxSize; }
            set { GridSegmentation.BoxSize = value; }
        }

        private static double BoxSquared { get { return BoxSize * BoxSize; } }
        private static double MaxCornerDistance { get { return Math.Sqrt(BoxSquared + BoxSquared); } }


        public static float BoxTolerance
        {
            get { return GridSegmentation.BoxTolerance; }
            set { GridSegmentation.BoxTolerance = value; }
        }

        public static void Reset(int boxSize = 30, float boxTolerance = 0.05f)
        {
            GridSegmentation.Reset(boxSize, boxTolerance);
        }
    }

}
