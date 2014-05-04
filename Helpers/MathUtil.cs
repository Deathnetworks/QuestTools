using System;
using Zeta.Common;
using Zeta.Game;

namespace QuestTools.Helpers
{
    class MathUtil
    {
        /* Stare at this for a while:
         * http://upload.wikimedia.org/wikipedia/commons/9/9a/Degree-Radian_Conversion.svg
         */

        internal static bool PositionIsInCircle(Vector3 position, Vector3 center, float radius)
        {
            if (center.Distance2DSqr(position) < (Math.Pow(radius, radius)))
                return true;
            return false;
        }

        internal static bool PositionIsInsideArc(Vector3 position, Vector3 center, float radius, float rotation, float arcDegrees)
        {
            if (PositionIsInCircle(position, center, radius))
            {
                return GetIsFacingPosition(position, center, rotation, arcDegrees);
            }
            return false;
        }

        internal static bool GetIsFacingPosition(Vector3 position, Vector3 center, float rotation, float arcDegrees)
        {
            var directionVector = GetDirectionVectorFromRotation(rotation);
            if (directionVector == Vector2.Zero)
                return false;
            Vector3 u = position - center;
            u.Z = 0f;
            Vector3 v = new Vector3(directionVector.X, directionVector.Y, 0f);
            bool result = ((MathEx.ToDegrees(Vector3.AngleBetween(u, v)) <= arcDegrees) ? 1 : 0) != 0;
            return result;
        }

        internal static Vector2 GetDirectionVectorFromRotation(double rotation)
        {
            return new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation));
        }


        #region Angle Finding
        /// <summary>
        /// Find the angle between two vectors. This will not only give the angle difference, but the direction.
        /// For example, it may give you -1 radian, or 1 radian, depending on the direction. Angle given will be the 
        /// angle from the FromVector to the DestVector, in radians.
        /// </summary>
        /// <param name="fromVector">Vector to start at.</param>
        /// <param name="destVector">Destination vector.</param>
        /// <param name="destVectorsRight">Right vector of the destination vector</param>
        /// <returns>Signed angle, in radians</returns>        
        /// <remarks>All three vectors must lie along the same plane.</remarks>
        public static double GetSignedAngleBetween2DVectors(Vector3 fromVector, Vector3 destVector, Vector3 destVectorsRight)
        {
            fromVector.Z = 0;
            destVector.Z = 0;
            destVectorsRight.Z = 0;

            fromVector.Normalize();
            destVector.Normalize();
            destVectorsRight.Normalize();

            float forwardDot = Vector3.Dot(fromVector, destVector);
            float rightDot = Vector3.Dot(fromVector, destVectorsRight);

            // Keep dot in range to prevent rounding errors
            forwardDot = MathEx.Clamp(forwardDot, -1.0f, 1.0f);

            double angleBetween = Math.Acos(forwardDot);

            if (rightDot < 0.0f)
                angleBetween *= -1.0f;

            return angleBetween;
        }
        public float UnsignedAngleBetweenTwoV3(Vector3 v1, Vector3 v2)
        {
            v1.Z = 0;
            v2.Z = 0;
            v1.Normalize();
            v2.Normalize();
            double angle = (float)Math.Acos(Vector3.Dot(v1, v2));
            return (float)angle;
        }
        /// <summary>
        /// Returns the Degree angle of a target location
        /// </summary>
        /// <param name="vStartLocation"></param>
        /// <param name="vTargetLocation"></param>
        /// <returns></returns>
        public static float FindDirectionDegree(Vector3 vStartLocation, Vector3 vTargetLocation)
        {
            return (float)RadianToDegree(NormalizeRadian((float)Math.Atan2(vTargetLocation.Y - vStartLocation.Y, vTargetLocation.X - vStartLocation.X)));
        }
        public static double FindDirectionRadian(Vector3 start, Vector3 end)
        {
            double radian = Math.Atan2(end.Y - start.Y, end.X - start.X);

            if (radian < 0)
            {
                double mod = -radian;
                mod %= Math.PI * 2d;
                mod = -mod + Math.PI * 2d;
                return mod;
            }
            return (radian % (Math.PI * 2d));
        }
        public Vector3 GetDirection(Vector3 origin, Vector3 destination)
        {
            Vector3 direction = destination - origin;
            direction.Normalize();
            return direction;
        }
        #endregion


        public static bool IntersectsPath(Vector3 obstacle, float radius, Vector3 start, Vector3 destination)
        {
            // fake-it to 2D
            obstacle.Z = 0;
            start.Z = 0;
            destination.Z = 0;

            return MathEx.IntersectsPath(obstacle, radius, start, destination);
        }

        #region Angular Measure Unit Conversion
        public static double Normalize180(double angleA, double angleB)
        {
            //Returns an angle in the range -180 to 180
            double diffangle = (angleA - angleB) + 180d;
            diffangle = (diffangle / 360.0);
            diffangle = ((diffangle - Math.Floor(diffangle)) * 360.0d) - 180d;
            return diffangle;
        }
        public static float NormalizeRadian(float radian)
        {
            if (radian < 0)
            {
                double mod = -radian;
                mod %= Math.PI * 2d;
                mod = -mod + Math.PI * 2d;
                return (float)mod;
            }
            return (float)(radian % (Math.PI * 2d));
        }
        public static double RadianToDegree(double angle)
        {
            return angle * (180.0 / Math.PI);
        }


        #endregion
        public static double GetRelativeAngularVariance(Vector3 origin, Vector3 destA, Vector3 destB)
        {
            float fDirectionToTarget = NormalizeRadian((float)Math.Atan2(destA.Y - origin.Y, destA.X - origin.X));
            float fDirectionToObstacle = NormalizeRadian((float)Math.Atan2(destB.Y - origin.Y, destB.X - origin.X));
            return AbsAngularDiffernce(RadianToDegree(fDirectionToTarget), RadianToDegree(fDirectionToObstacle));
        }
        public static double AbsAngularDiffernce(double angleA, double angleB)
        {
            return 180d - Math.Abs(180d - Math.Abs(angleA - angleB));
        }

        #region Human Readable Headings
        public static string GetHeadingToPoint(Vector3 targetPoint)
        {
            return GetHeading(FindDirectionDegree(ZetaDia.Me.Position, targetPoint));
        }
        public static string GetHeading(float heading)
        {
            var directions = new[] {
              //"n", "ne", "e", "se", "s", "sw", "w", "nw", "n"
                "s", "se", "e", "ne", "n", "nw", "w", "sw", "s"
            };

            var index = (((int)heading) + 23) / 45;
            return directions[index].ToUpper();
        }
        #endregion
    }
}
