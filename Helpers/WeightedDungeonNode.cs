using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zeta.Bot.Dungeons;
using Zeta.Common;

namespace QuestTools.Helpers
{
    class WeightedDungeonNode : DungeonNode
    {
        public WeightedDungeonNode(Vector2 worldTopLeft, Vector2 worldBottomRight) : base(worldTopLeft, worldBottomRight)
        {
        }

        public double Weight { get; set; }
    }
}
