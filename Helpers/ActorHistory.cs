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
            public Vector3 Position;
            public DateTime LastSeen;
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

        public static void UpdateActors()
        {
            if (DateTime.UtcNow.Subtract(_lastChangeCheckTime).TotalMilliseconds < 1000)
                return;

            _lastChangeCheckTime = DateTime.UtcNow;

            if (!ZetaDia.IsInGame || !ZetaDia.Me.IsValid || ZetaDia.IsLoadingWorld)
                return;

            (from o in ZetaDia.Actors.GetActorsOfType<DiaObject>(true)
             where (o is DiaGizmo || o is DiaUnit) && !(o is DiaPlayer)
             select o).ToList().ForEach(UpdateActor);   
        }

        public static void UpdateActor(DiaObject actor)
        {
            if (actor == null || !actor.IsValid)            
                return;            

            var updatedActor = new CachedActor
            {
                Position = actor.Position,
                WorldId = ZetaDia.CurrentWorldId,
                LastSeen = DateTime.UtcNow
            };

            if (Actors.ContainsKey(actor.ActorSNO))
            {
                //Logger.Log("Updating Existing Actor {0} ({0})", actor.Name, actor.ActorSNO);
                Actors[actor.ActorSNO] = updatedActor;
            }
            else
            {
                //Logger.Log("Recording New Actor {0} ({0})", actor.Name, actor.ActorSNO);
                Actors.Add(actor.ActorSNO, updatedActor);
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
