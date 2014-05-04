using System.Linq;
using Zeta.Common;

namespace QuestTools.Helpers
{
    public class StringUtils
    {
        public static string GetProfilePosition(Vector3 pos)
        {
            return string.Format("x=\"{0:0}\" y=\"{1:0}\" z=\"{2:0}\" ", pos.X, pos.Y, pos.Z);
        }
        public static string GetSimplePosition(Vector3 pos)
        {
            return string.Format("{0:0}, {1:0}, {2:0}", pos.X, pos.Y, pos.Z);
        }
        public static string SpacedConcat(params object[] args)
        {
            return args.Aggregate("", (current, o) => current + (o + ", "));
        }
        public static string GetProfileCoordinates(Vector3 position)
        {
            return string.Format("x=\"{0:0}\" y=\"{1:0}\" z=\"{2:0}\"", position.X, position.Y, position.Z);
        }

    }
}
