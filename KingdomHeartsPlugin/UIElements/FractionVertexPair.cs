using System;
using System.Numerics;

namespace KingdomHeartsPlugin.UIElements
{

    public readonly record struct FractionVertexPair(double Fraction, Vector2 Inner, Vector2 Outer)
    {
        public Vector2 Min => Vector2.Min(Inner, Outer);
        public Vector2 Max => Vector2.Max(Inner, Outer);
        public Vector2 Center => (Inner + Outer) / 2;
        public Vector2 InnerToOuter => Outer - Inner;
        public float Thickness => InnerToOuter.Length();
        public double InnerToOuterAngleRadians => Math.Atan2(InnerToOuter.Y, InnerToOuter.X);

        // Checks whether the vertices are equal - the two vertex pairs describe the same segment.
        public bool VerticesEqual(FractionVertexPair other)
        {
            return other.Inner == Inner && other.Outer == Outer;
        }

        // Calculates the angle perpendicular to this segment's angle which is closest to being in the direction
        // of the other segment.
        public double PerpendicularAngleTowards(FractionVertexPair other)
        {
            var forwardVector = other.Center - Center;
            var upVector = InnerToOuter;
            var angleDifference =
                Math.Atan2(forwardVector.Y, forwardVector.X) - Math.Atan2(upVector.Y, upVector.X);
            if (Math.Abs(angleDifference) > Math.PI)
            {
                // We've wrapped around from -pi to +pi or vice versa. Un-wrap this by applying a 2pi difference. 
                angleDifference -= Math.Sign(angleDifference) * 2 * Math.PI;
            }

            return Math.CopySign(angleDifference, (float)Math.PI / 2);
        }

        // Creates a new FractionVertexPair with the Inner and Outer points moved closer (negative delta) or
        // further apart (positive delta). innerFraction controls which point moves; at 0.5 (the default) both
        // points move toward/away from each other equally, while at 1.0 only the inner point moves and at 0.0
        // only the outer point moves.
        public FractionVertexPair AdjustThickness(float delta, float innerFraction = 0.5f)
        {
            return new FractionVertexPair(Fraction,
                Inner - innerFraction * delta * InnerToOuter / InnerToOuter.Length(),
                Outer + (1 - innerFraction) * delta * InnerToOuter / InnerToOuter.Length());
        }

        // Synthesizes the vertex pair at sourceFraction, which must be a.Fraction <= sourceFraction <= b.Fraction.
        // Optionally also updates the fraction to targetFraction.
        public static FractionVertexPair Lerp(FractionVertexPair a, FractionVertexPair b, double sourceFraction,
            double? targetFraction = null)
        {
            var partial = (sourceFraction - a.Fraction) / (b.Fraction - a.Fraction);
            return new FractionVertexPair(
                targetFraction ?? sourceFraction,
                Vector2.Lerp(a.Inner, b.Inner, (float)partial),
                Vector2.Lerp(a.Outer, b.Outer, (float)partial));
        }

        // Shifts the vertices by the given offset, optionally also updating the fraction.
        public FractionVertexPair Offset(Vector2 offset, double? targetFraction = null)
        {
            return new FractionVertexPair(
                targetFraction ?? Fraction,
                Inner + offset,
                Outer + offset);
        }

        public FractionVertexPair Scale(float scale)
        {
            return new FractionVertexPair(Fraction, Inner * scale, Outer * scale);
        }

        // Gets the relative position of sourceFraction between fromFraction and toFraction.
        internal static double RelativeFraction(double fromFraction, double toFraction, double sourceFraction)
        {
            return (sourceFraction - fromFraction) / (toFraction - fromFraction);
        }
    }
}