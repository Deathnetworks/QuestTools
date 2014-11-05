using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using Zeta.Bot;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals.SNO;

namespace QuestTools.Helpers
{
    /// <summary>
    /// Archive of information about actors that we've seen recently.
    /// </summary>
    public class ActorHistory
    {
        private static DateTime _lastChangeCheckTime = DateTime.MinValue;

        public static readonly Dictionary<int, CachedActor> Actors = new Dictionary<int, CachedActor>();

        public class CachedActor
        {
            public int WorldId;
            public int LevelAreaId;
            public Vector3 Position;
            public Dictionary<SNOAnim, int> AnimationCount = new Dictionary<SNOAnim, int>();
            public DateTime LastSeen;

            public TimeSpan TimeSinceSeen
            {
                get
                {
                    return DateTime.UtcNow.Subtract(LastSeen);
                }
            }

            public bool IsRecentlyNearby
            {
                get
                {
                    return ZetaDia.CurrentLevelAreaId == LevelAreaId &&
                        TimeSinceSeen.TotalSeconds < 20 &&
                        Position.Distance(ZetaDia.Me.Position) < 120;
                }
            }

        }

        public static CachedActor GetActor(int actorId)
        {
            CachedActor cActor;
            return Actors.TryGetValue(actorId, out cActor) ? cActor : default(CachedActor);
        }

        public static bool HasBeenSeen(int actorId)
        {
            CachedActor cActor;
            return Actors.TryGetValue(actorId, out cActor);
        }

        public static Vector3 GetActorPosition(int actorId)
        {
            CachedActor cActor;
            return Actors.TryGetValue(actorId, out cActor) && cActor.WorldId == ZetaDia.CurrentWorldId ? cActor.Position : Vector3.Zero;
        }

        public static TimeSpan GetTimeSinceSeen(int actorId)
        {
            CachedActor cActor;
            return Actors.TryGetValue(actorId, out cActor) ? DateTime.UtcNow.Subtract(cActor.LastSeen) : TimeSpan.Zero;
        }

        private static int _currentLevelAreaId;
        private static int _currentWorldId;

        public static void UpdateActors()
        {
            if (DateTime.UtcNow.Subtract(_lastChangeCheckTime).TotalMilliseconds < 250)
                return;

            _lastChangeCheckTime = DateTime.UtcNow;

            if (!ZetaDia.IsInGame || ZetaDia.Me == null || !ZetaDia.Me.IsValid || ZetaDia.IsLoadingWorld || ZetaDia.WorldInfo.IsValid)
                return;

            try
            {
                _currentWorldId = ZetaDia.CurrentWorldId;
                _currentLevelAreaId = ZetaDia.CurrentLevelAreaId;

                (from o in ZetaDia.Actors.GetActorsOfType<DiaObject>(true)
                 where (o is DiaGizmo || o is DiaUnit) && !(o is DiaPlayer)
                 select o).ToList().ForEach(UpdateActor);   
            }
            catch (Exception ex)
            {
                Logger.Debug("Exception in ActorHistory: {0}", ex.ToString());
            }

        }

        public static void UpdateActor(DiaObject actor)
        {
            if (actor == null || !actor.IsValid)            
                return;

            CachedActor cachedActor;

            if (Actors.TryGetValue(actor.ActorSNO, out cachedActor))
            {
                cachedActor.Position = actor.Position;
                cachedActor.WorldId = _currentWorldId;
                cachedActor.LevelAreaId = _currentLevelAreaId;
                cachedActor.LastSeen = DateTime.UtcNow;
            }
            else
            {
                var newActor = new CachedActor
                {
                    Position = actor.Position,
                    WorldId = ZetaDia.CurrentWorldId,
                    LastSeen = DateTime.UtcNow
                };
                Actors.Add(actor.ActorSNO, newActor);
            }

            if (Actors.Count > 200)
                Actors.Remove(Actors.ElementAt(0).Key);   
        }

        public static void Clear()
        {
            _lastChangeCheckTime = DateTime.MinValue;
            Actors.Clear();
        }
    }
}
