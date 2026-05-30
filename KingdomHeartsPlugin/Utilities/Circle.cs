using System;
using System.Numerics;

namespace KingdomHeartsPlugin.Utilities
{
    /// <summary>
    /// Utilities for calculating properties of circles and points thereon.
    /// </summary>
    public static class Circle
    {
        
        /// <summary>
        /// Calculates the point at <see cref="angleRadians" /> on the circle with the given <see cref="radius"/> and
        /// <see cref="center"/>.
        /// <para>Because Y-coordinates increase going down the screen, points are vertically mirrored compared to
        /// those in typical demonstrations where Y-coordinates increase going up the graph.</para>
        /// </summary>
        /// <param name="center">The center of the circle on which to get the point.</param>
        /// <param name="angleRadians">The angle at which to get the point on the circle, in radians.</param>
        /// <param name="radius">The radius of the circle on which to get the point.</param>
        /// <returns>The point at the given angle.</returns>
        public static Vector2 PointAtAngle(Vector2 center, double angleRadians, double radius)
        {
            return new Vector2(
                (float)(center.X + radius * Math.Cos(angleRadians)),
                (float)(center.Y + radius * Math.Sin(angleRadians)));
        }

        /// <summary>
        /// Gets the angle of the circle with the given center and point.
        /// </summary>
        /// <param name="point">The point to find the angle of.</param>
        /// <param name="center">The center point of the circle, defaulting to <c>0, 0</c>.</param>
        /// <returns>
        /// The angle in radians at the given <see cref="point"/> relative to the given <see cref="center" />.
        /// </returns>
        public static double AngleRadiansAtPoint(Vector2 point, Vector2? center)
        {
            var result = point - (center ?? Vector2.Zero);
            return Math.Atan2(result.Y, result.X);
        }

        /// <summary>
        /// Calculates the arc length of an arc with the given internal <see cref="angleRadians"/> of a circle with
        /// the given <see cref="radius" />.
        /// </summary>
        /// <param name="angleRadians">The internal angle of the arc to measure, in radians.</param>
        /// <param name="radius">The radius of the circle of which the arc is part.</param>
        /// <returns>The arc length of the described arc.</returns>
        public static float ArcLength(double angleRadians, float radius)
        {
            return (float) Math.Abs(angleRadians * radius);
        }
    }
}