﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Zeta.Bot.Dungeons;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.SNO;

namespace QuestTools.Navigation
{
    class SceneSegmentation
    {
        private static ConcurrentBag<DungeonNode> _nodes = new ConcurrentBag<DungeonNode>();
        public static ConcurrentBag<DungeonNode> Nodes
        {
            get
            {
                if (_nodes.IsEmpty)
                    Update();
                return _nodes;
            }
            set { _nodes = value; }
        }

        private static readonly Regex SceneConnectionDirectionsRegex = new Regex("_([NSEW]{2,})_", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static void Update()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var oldNodes = _nodes;

            var scenes = ZetaDia.Scenes.GetScenes().Where(s => s.Mesh.Zone != null).ToList();

            int minEdgeLength = (int)Math.Ceiling(scenes.Min(s => Math.Min(s.Mesh.Zone.ZoneMax.X - s.Mesh.Zone.ZoneMin.X, s.Mesh.Zone.ZoneMax.Y - s.Mesh.Zone.ZoneMin.Y)));

            int halfEdgeLength = minEdgeLength / 2;

            List<DungeonNode> nodes = new List<DungeonNode>();

            // Iterate through scenes, find connecting scene names and create a dungeon node to navigate to the scene center
            scenes.AsParallel().ForEach(scene =>
            {
                var zone = scene.Mesh.Zone;
                var zoneMin = zone.ZoneMin;
                var zoneMax = zone.ZoneMax;

                // The nodes are not actual GridSegmentation nodes, they're defined by the nav zone coordinates here
                var baseNode = new DungeonNode(zoneMin, zoneMax);
                if (nodes.All(node => node.WorldTopLeft != baseNode.WorldTopLeft))
                    nodes.Add(baseNode);

                // North
                var northNode = (new DungeonNode(new Vector2(zoneMin.X - halfEdgeLength, zoneMin.Y), new Vector2(zoneMax.X - halfEdgeLength, zoneMin.Y)));
                if (nodes.All(node => node.WorldTopLeft != northNode.WorldTopLeft))
                    nodes.Add(northNode);

                // South
                var southNode = (new DungeonNode(new Vector2(zoneMin.X + halfEdgeLength, zoneMin.Y), new Vector2(zoneMax.X + halfEdgeLength, zoneMin.Y)));
                if (nodes.All(node => node.WorldTopLeft != southNode.WorldTopLeft))
                    nodes.Add(southNode);

                // East
                var eastNode = (new DungeonNode(new Vector2(zoneMin.X, zoneMin.Y - halfEdgeLength), new Vector2(zoneMax.X, zoneMin.Y - halfEdgeLength)));
                if (nodes.All(node => node.WorldTopLeft != eastNode.WorldTopLeft))
                    nodes.Add(eastNode);

                // West
                var westNode = (new DungeonNode(new Vector2(zoneMin.X, zoneMin.Y + halfEdgeLength), new Vector2(zoneMax.X, zoneMin.Y + halfEdgeLength)));
                if (nodes.All(node => node.WorldTopLeft != westNode.WorldTopLeft))
                    nodes.Add(westNode);

            });

            if (oldNodes != null && oldNodes.Any())
            {
                nodes.AsParallel().ForEach(node =>
                {
                    var oldNode = oldNodes.FirstOrDefault(n => node.WorldTopLeft == n.WorldTopLeft);
                    if (oldNode != null && oldNode.Visited)
                        node.Visited = true;
                });

                oldNodes.AsParallel().ForEach(oldNode =>
                    {
                        if (nodes.All(newNode => newNode.Center != oldNode.WorldTopLeft))
                        {
                            nodes.Add(oldNode);
                        }
                    });
            }

            _nodes = new ConcurrentBag<DungeonNode>(nodes.Distinct());
            Logger.Debug("Updated SceneSegmentation with {0} nodes in {1:0}ms", _nodes.Count, stopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// Clears and updates the Node List
        /// </summary>
        public static void Reset()
        {
            _nodes = new ConcurrentBag<DungeonNode>();
            Update();
        }

        /// <summary>
        /// Gets the center of a given Navigation Zone
        /// </summary>
        /// <param name="zone"></param>
        /// <returns></returns>
        internal static Vector3 GetNavZoneCenter(NavZone zone)
        {
            float x = zone.ZoneMin.X + ((zone.ZoneMax.X - zone.ZoneMin.X) / 2);
            float y = zone.ZoneMin.Y + ((zone.ZoneMax.Y - zone.ZoneMin.Y) / 2);

            return new Vector3(x, y, 0);
        }

        /// <summary>
        /// Gets the center of a given Navigation Cell
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="zone"></param>
        /// <returns></returns>
        internal static Vector3 GetNavCellCenter(NavCell cell, NavZone zone)
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
        internal static Vector3 GetNavCellCenter(Vector3 min, Vector3 max, NavZone zone)
        {
            float x = zone.ZoneMin.X + min.X + ((max.X - min.X) / 2);
            float y = zone.ZoneMin.Y + min.Y + ((max.Y - min.Y) / 2);
            float z = min.Z + ((max.Z - min.Z) / 2);

            return new Vector3(x, y, z);
        }
    }
}
