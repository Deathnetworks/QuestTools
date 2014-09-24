using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Zeta.Bot;
using Zeta.Bot.Coroutines;
using Zeta.Bot.Profile;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors.Gizmos;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace QuestTools.ProfileTags.Movement
{
    [XmlElement("Waypoint")]
    public class Waypoint : ProfileBehavior
    {
        private bool _isDone;
        public override bool IsDone
        {
            get { return _isDone; }
        }

        [XmlAttribute("waypointNumber")]
        public int WaypointNumber { get; set; }

        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(ret => WaypointTask());
        }

        private int _startLevelAreaId;

        public override void OnStart()
        {
            _startLevelAreaId = ZetaDia.CurrentLevelAreaId;
            base.OnStart();
        }

        private async Task<bool> WaypointTask()
        {
            if (ZetaDia.IsLoadingWorld)
                return true;

            if (_startLevelAreaId != 0 && _startLevelAreaId != ZetaDia.CurrentLevelAreaId)
            {
                Logger.Log("Used waypoint {0} to LevelAreaId {1}", WaypointNumber, ZetaDia.CurrentLevelAreaId);
                _isDone = true;
                return true;
            }

            if (WaypointNumber == 0)
            {
                _isDone = true;
                Logger.LogError("WaypointNumber is 0!");
                return false;
            }

            if (!Waypoints.Any())
            {
                _isDone = true;
                Logger.LogError("No waypoints available!");
                return false;
            }

            var waypoint = Waypoints.OrderBy(wp => wp.Distance).FirstOrDefault();

            if (waypoint == null)
            {
                _isDone = true;
                Logger.LogError("Unknown error, waypoint is null!");
                return false;
            }

            if (waypoint.Position.Distance2D(ZetaDia.Me.Position) > 5f)
            {
                Logger.Debug("Moving to waypoint");
                await CommonCoroutines.MoveTo(waypoint.Position, "Waypoint");
                return true;
            }

            if (waypoint.Position.Distance2D(ZetaDia.Me.Position) <= 5f && !UIElements.WaypointMap.IsVisible)
            {
                Logger.Debug("Interacting with Waypoint");
                waypoint.Interact();
                await Coroutine.Sleep(250);
                return true;
            }

            if (UIElements.WaypointMap.IsVisible)
            {
                Logger.Log("Using waypoint {0}", WaypointNumber);
                ZetaDia.Me.UseWaypoint(WaypointNumber);
                if (ZetaDia.IsInTown)
                    await Coroutine.Sleep(1000);
                else
                    await Coroutine.Sleep(3000);
            }
            return true;

        }

        private List<GizmoWaypoint> Waypoints
        {
            get
            {
                return ZetaDia.Actors.GetActorsOfType<GizmoWaypoint>(true).Where(o => o.IsValid && o.CommonData.IsValid).ToList();
            }
        }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }
    }
}
