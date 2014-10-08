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
            public Vector3 Position;
            public Dictionary<SNOAnim, int> AnimationCount = new Dictionary<SNOAnim, int>();
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

        public static HashSet<int> UnitsWithAnimationTracking = new HashSet<int>();

        public static int GetActorAnimationCount(int actorId, string animationName)
        {
            if (!UnitsWithAnimationTracking.Contains(actorId))
                UnitsWithAnimationTracking.Add(actorId);

            CachedActor cActor;           
            if (Actors.TryGetValue(actorId, out cActor))
            {
                var anim = animationName.ChangeType<SNOAnim>();
                int animCount;
                if (anim != SNOAnim.Invalid && cActor.AnimationCount.TryGetValue(anim, out animCount))
                {
                    return animCount;
                }
            }
            return 0;
        }

        public static TimeSpan GetTimeSinceSeen(int actorId)
        {
            CachedActor cActor;
            return Actors.TryGetValue(actorId, out cActor) ? DateTime.UtcNow.Subtract(cActor.LastSeen) : TimeSpan.Zero;
        }

        public static void UpdateActors()
        {
            if (DateTime.UtcNow.Subtract(_lastChangeCheckTime).TotalMilliseconds < 500)
                return;

            _lastChangeCheckTime = DateTime.UtcNow;

            if (!ZetaDia.IsInGame || !ZetaDia.Me.IsValid || ZetaDia.IsLoadingWorld)
                return;

            try
            {
                (from o in ZetaDia.Actors.GetActorsOfType<DiaObject>(true)
                 where (o.ActorType == ActorType.Gizmo || o is DiaUnit) && !(o is DiaPlayer)
                 select o).ToList().ForEach(UpdateActor);   
            }
            catch (Exception)
            {
            }

        }

        public static void UpdateActor(DiaObject actor)
        {
            if (actor == null || !actor.IsValid)            
                return;

            var shouldTrackAnimations = actor.CommonData != null && actor.CommonData.IsValid && actor is DiaUnit && (actor as DiaUnit).IsHostile && actor.CommonData.CurrentAnimation != SNOAnim.Invalid;
            //var shouldTrackAnimations = actor.CommonData != null && actor.CommonData.IsValid && actor is DiaUnit && actor.CommonData.CurrentAnimation != SNOAnim.Invalid;

            CachedActor cachedActor;

            if (Actors.TryGetValue(actor.ActorSNO, out cachedActor))
            {
                //Logger.Log("Updating Existing Actor {0} ({0})", actor.Name, actor.ActorSNO);
                cachedActor.Position = actor.Position;
                cachedActor.WorldId = ZetaDia.CurrentWorldId;
                cachedActor.LastSeen = DateTime.UtcNow;

                if (UnitsWithAnimationTracking.Contains(actor.ActorSNO) && shouldTrackAnimations)
                {
                    int seenAnimCount;
                    if (cachedActor.AnimationCount.TryGetValue(actor.CommonData.CurrentAnimation, out seenAnimCount))
                    {
                        //Logger.Log("Actor={0} {1} Animation Count={2}", actor.Name, actor.CommonData.CurrentAnimation, seenAnimCount + 1);
                        cachedActor.AnimationCount[actor.CommonData.CurrentAnimation] = seenAnimCount + 1;
                    }
                    else
                    {
                        cachedActor.AnimationCount.Add(actor.CommonData.CurrentAnimation, 1);
                    }                        
                }

            }
            else
            {
                var newActor = new CachedActor
                {
                    Position = actor.Position,
                    WorldId = ZetaDia.CurrentWorldId,
                    LastSeen = DateTime.UtcNow
                };

                if (UnitsWithAnimationTracking.Contains(actor.ActorSNO) && shouldTrackAnimations)
                    newActor.AnimationCount.Add(actor.CommonData.CurrentAnimation,1);

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
