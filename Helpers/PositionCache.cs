using System;
using System.Collections.Generic;
using System.Linq;
using Zeta.Common;
using Zeta.Game;

namespace QuestTools.Helpers
{
    public class PositionCache
    {
        public static HashSet<Vector3> Cache { get; set; }

        private static DateTime _lastRecordedPosition = DateTime.MinValue;

        const float MinRecordDistance = 10f;

        public static void RecordPosition()
        {
            if (Cache == null)
                Cache = new HashSet<Vector3>();

            if (DateTime.UtcNow.Subtract(_lastRecordedPosition).TotalMilliseconds < 1000)
                return;
            Vector3 myPos = ZetaDia.Me.Position;
            if (Cache.Any(p => p.Distance2DSqr(myPos) < MinRecordDistance * MinRecordDistance))
                return;

            _lastRecordedPosition = DateTime.UtcNow;
            Cache.Add(myPos);
        }
    }
}
