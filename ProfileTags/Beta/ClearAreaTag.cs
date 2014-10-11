using System;
using System.Collections.Generic;
using System.Linq;
using Zeta.Bot;
using Zeta.Bot.Navigation;
using Zeta.Bot.Profile;
using Zeta.Common;
using Zeta.Game;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using Action = Zeta.TreeSharp.Action;

namespace QuestTools.ProfileTags.Complex
{
    /// <summary>
    /// Run a circle around the current location then return to starting point
    /// </summary>
    [XmlElement("ClearArea")]
    public class ClearAreaTag : ProfileBehavior
    {
        private bool _isDone;

        private List<Vector3> _points;
        private DefaultNavigationProvider _navigator;

        [XmlAttribute("radius")]
        public int Radius { get; set; }

        [XmlAttribute("points")]
        public int Points { get; set; }

        [XmlAttribute("pathPrecision")]
        public float PathPrecision { get; set; }

        public override bool IsDone
        {
            get { return _isDone; }
        }

        public override void OnStart()
        {
            Radius = Radius < 10 ? 10 : Radius;
            Points = Points < 4 || Points > 30 ? 10 : Points;
            PathPrecision = PathPrecision < 2f ? 5f : PathPrecision;

            _points = GetCirclePoints(Points, Radius, ZetaDia.Me.Position);
            _points.Add(ZetaDia.Me.Position);
            _navigator = Navigator.GetNavigationProviderAs<DefaultNavigationProvider>();

            base.OnStart();
        }

        protected override Composite CreateBehavior()
        {
            return new Decorator(ret => !_isDone, new Sequence(

                new Decorator(ret => _points.Any(), CommonBehaviors.MoveTo(ret => _points.First())),

                new Decorator(ret => !IsReachable, new Action(ret => _points.RemoveAt(0))),

                new Decorator(ret => !_points.Any(), new Action(ret => _isDone = true))

               )
            );
        }

        private bool IsReachable
        {
            get
            {
                if (!_points.Any() || _points.First().Distance2D(ZetaDia.Me.Position) < 10f)
                    return false;

                if (!_navigator.CanPathWithinDistance(_points.First(), PathPrecision) || Navigator.StuckHandler.IsStuck)
                    return false;

                return true;
            }
        }

        private List<Vector3> GetCirclePoints(int points, double radius, Vector3 center)
        {
            var result = new List<Vector3>();
            double slice = 2*Math.PI/points;
            for (int i = 0; i < points; i++)
            {
                double angle = slice*i;
                var newX = (int) (center.X + radius*Math.Cos(angle));
                var newY = (int) (center.Y + radius*Math.Sin(angle));

                var newpoint = new Vector3(newX, newY, center.Z);
                result.Add(newpoint);

                Logger.Debug("Calculated point {0}: {1}", i, newpoint.ToString());
            }
            return result;
        }

        public override void ResetCachedDone()
        {
            _isDone = false;
            base.ResetCachedDone();
        }
    }
}