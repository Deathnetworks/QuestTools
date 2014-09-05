using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public static ConcurrentBag<DungeonNode> Nodes { get; set; }

        public static void Update()
        {
            var oldNodes = Nodes;

            var scenes = ZetaDia.Scenes.GetScenes().ToList();

            int minEdge = (int)Math.Ceiling(scenes.Min(s => Math.Min(s.Mesh.Zone.ZoneMax.X - s.Mesh.Zone.ZoneMin.X, s.Mesh.Zone.ZoneMax.Y - s.Mesh.Zone.ZoneMin.Y)));

            //GridRoute.BoxSize = minEdge;
            //GridRoute.BoxTolerance = 0.01f; // Override to make sure we explore all necessary scenes, we can't simulate this through the map viewer

            // The nodes are not actual GridSegmentation nodes, they're defined by the nav zone coordinates here
            var nodes = scenes.Select(scene => new DungeonNode(scene.Mesh.Zone.ZoneMin, scene.Mesh.Zone.ZoneMax)).ToList();

            if (oldNodes != null && oldNodes.Any())
            {
                foreach (var node in nodes)
                {
                    var oldNode = oldNodes.FirstOrDefault(n => n.Equals(node));
                    if (oldNode != null && oldNode.Visited)
                        node.Visited = true;
                }
            }

            Nodes = new ConcurrentBag<DungeonNode>(nodes);
        }

        /// <summary>
        /// Clears and updates the Node List
        /// </summary>
        public static void Reset()
        {
            Nodes = new ConcurrentBag<DungeonNode>();
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
