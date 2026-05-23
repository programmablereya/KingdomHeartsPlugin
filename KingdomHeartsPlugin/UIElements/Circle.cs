using System;
using System.Numerics;

namespace KingdomHeartsPlugin.UIElements
{
    /*
     * Circle-drawing utilities.
     * The Kingdom Hearts UI loves its circles. We use this instead of the built-in Ellipse functions from ImGui because
     * ImGui does not let us customize the individual points of the ellipse, meaning that we can only make solid-color
     * circles. Since we want to make gradients, that's right out.
     */
    internal class Circle
    {
        internal static Vector2 getPointOnCircle(Vector2 circleCenter, double angleRadians, double radius)
        {
            return new Vector2(
                (float)(circleCenter.X + radius * Math.Cos(angleRadians)),
                // the Y-axis on the display is inverted from the one used in math demonstrations
                (float)(circleCenter.Y + radius * -Math.Sin(angleRadians)));
        }
    }
}