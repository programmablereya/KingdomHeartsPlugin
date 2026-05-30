using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Bindings.ImGui;
using KingdomHeartsPlugin.Utilities;

namespace KingdomHeartsPlugin.UIElements
{
    /// <summary>
    /// Helper functions for building and drawing gauges of complicated shapes, using a series of vertex pairs
    /// corresponding to different fractions.
    ///
    /// <para>An <see cref="ImmutableArray" /> of <see cref="FractionVertexPair" />s is necessary for using the
    /// functions in this class. That array must conform to the restrictions set out in <see cref="CheckValidGauge" />.
    /// </para>
    /// </summary>
    public static class Gauge
    {
        /// <summary>
        /// A pair of vertices at a particular fraction of a gauge.
        /// </summary>
        /// <param name="Fraction">The fraction along the gauge at which this pair is located.</param>
        /// <param name="Inner">The inner point of the pair.</param>
        /// <param name="Outer">The outer point of the pair.</param>
        public readonly record struct FractionVertexPair(float Fraction, Vector2 Inner, Vector2 Outer) 
            : IComparable<FractionVertexPair>
        {
            /// <summary>
            /// Calculates the minimum in each dimension between <see cref="Inner" /> and <see cref="Outer" />.
            /// </summary>
            /// <returns>
            /// A vector containing <c>min(Inner.X, Outer.X)</c> in its X component and <c>min(Inner.Y, Outer.Y)</c> in
            /// its Y component.
            /// </returns>
            public Vector2 Min() => Vector2.Min(Inner, Outer);
            /// <summary>
            /// Calculates the minimum in each dimension between <see cref="Inner" /> and <see cref="Outer" />.
            /// </summary>
            /// <returns>
            /// A vector containing <c>max(Inner.X, Outer.X)</c> in its X component and <c>max(Inner.Y, Outer.Y)</c> in
            /// its Y component.
            /// </returns>
            public Vector2 Max() => Vector2.Max(Inner, Outer);
            /// <summary>
            /// Calculates the midpoint between <see cref="Inner" /> and <see cref="Outer" />.
            /// </summary>
            /// <returns>The midpoint of this vertex pair.</returns>
            public Vector2 Center() => Vector2.Lerp(Inner, Outer, 0.5f);
            /// <summary>
            /// Calculates the vector from <see cref="Inner" /> to <see cref="Outer" />.
            /// </summary>
            /// <returns>The vector representing the direction of this vertex pair.</returns>
            public Vector2 InnerToOuter() => Outer - Inner;
            /// <summary>
            /// Calculates the distance from <see cref="Inner" /> to <see cref="Outer" />.
            /// </summary>
            /// <returns>The distance between the two points of this vertex pair.</returns>
            public float Thickness() => Vector2.Distance(Inner, Outer);
            /// <summary>
            /// Gets the angle in radians in which the vector from inner to outer points.
            /// </summary>
            /// <returns>The angle in radians of the vector from inner to outer.</returns>
            public double InnerToOuterAngleRadians() => Circle.AngleRadiansAtPoint(Outer, Inner);
            
            /// <summary>
            /// Gets the point at the given fraction of this pair,
            /// ranging from <see cref="Inner" /> at and below <c>0.0f</c>
            /// to <see cref="Outer" /> at and above <c>1.0f</c>. 
            /// </summary>
            /// <param name="innerToOuterFraction">The fraction of this pair, ranging from <see cref="Inner" /> at and
            /// below <c>0.0f</c> to <see cref="Outer" /> at and above <c>1.0f</c>.</param>
            public Vector2 this[float innerToOuterFraction] =>
                Vector2.Lerp(Inner, Outer, Math.Clamp(innerToOuterFraction, 0.0f, 1.0f));

            /// <summary>
            /// Checks whether this vertex pair describes the same pair of vertices as the <see cref="other" />. 
            /// </summary>
            /// <param name="other">The other vertex pair to compare against.</param>
            /// <param name="tolerance">
            /// The tolerance within which the vertex pairs are considered equal.
            /// </param>
            /// <returns>True if the vertices in each pair are within the given tolerance of each other.</returns>
            public bool VerticesWithin(FractionVertexPair other, float tolerance)
            {
                return other.Inner == Inner && other.Outer == Outer || (
                    tolerance > 0.0f &&
                    tolerance * tolerance >=
                    Math.Max(Vector2.DistanceSquared(other.Outer, Outer), Vector2.DistanceSquared(other.Inner, Inner)));
            }

            // Calculates the angle perpendicular to this segment's angle which is closest to being in the direction
            // opposite the other segment.
            public double PerpendicularAngleAwayFrom(FractionVertexPair other)
            {
                var forwardVector = other.Center() - Center();
                var upVector = InnerToOuter();
                var forwardCross = Vector2.Cross(upVector, forwardVector);
                if (forwardCross == 0)
                {
                    // The vectors were parallel. Try again with the vector from inner to inner...
                    forwardVector = other.Inner - Inner;
                    forwardCross = Vector2.Cross(upVector, forwardVector);
                    if (forwardCross == 0)
                    {
                        // Still parallel, so try outer to outer...
                        forwardVector = other.Outer - Outer;
                        forwardCross = Vector2.Cross(upVector, forwardVector);
                    }
                }
                
                // Left-hand rule because the y-axis is reversed. If the cross product is positive, the next pair is
                // clockwise (positive) from the up angle, so we should go counterclockwise (negative);
                // if it's negative, the next pair is counterclockwise (negative) from the up angle, so we should go
                // clockwise (positive). If it's zero, the next pair is directly above or below us, so who cares
                // what direction we go.
                return InnerToOuterAngleRadians() + Math.CopySign(Math.PI / 2, -forwardCross);
            }

            // Creates a new FractionVertexPair with the Inner and Outer points moved closer (delta < 1) or
            // further apart (delta > 1). innerFraction controls which point moves; at 0.5 (the default) both
            // points move toward/away from each other equally, while at 1.0 only the inner point moves and at 0.0
            // only the outer point moves.
            public FractionVertexPair AdjustThickness(float fraction, float innerFraction = 0.5f)
            {
                var innerToOuter = InnerToOuter();
                return new FractionVertexPair(Fraction,
                    Inner - innerFraction * (fraction - 1) * innerToOuter,
                    Outer + (1 - innerFraction) * (fraction - 1) * innerToOuter);
            }

            // Synthesizes the vertex pair at sourceFraction, which must be a.Fraction <= sourceFraction <= b.Fraction.
            // Optionally also updates the fraction to targetFraction.
            public static FractionVertexPair SynthesizeInterpolated(FractionVertexPair a, FractionVertexPair b,
                float sourceFraction)
            {
                var partial = (sourceFraction - a.Fraction) / (b.Fraction - a.Fraction);
                return new FractionVertexPair(
                    sourceFraction,
                    Vector2.Lerp(a.Inner, b.Inner, partial),
                    Vector2.Lerp(a.Outer, b.Outer, partial));
            }

            // Shifts the vertices by the given offset, optionally also updating the fraction.
            public FractionVertexPair Offset(Vector2 offset, float? targetFraction = null)
            {
                return new FractionVertexPair(
                    targetFraction ?? Fraction,
                    Inner + offset,
                    Outer + offset);
            }

            public FractionVertexPair Transform(Matrix3x2 transform)
            {
                return new FractionVertexPair(
                    Fraction,
                    Vector2.Transform(Inner, transform),
                    Vector2.Transform(Outer, transform));
            }

            // Gets the relative position of sourceFraction between fromFraction and toFraction.
            public static float RelativeFraction(float fromFraction, float toFraction, float sourceFraction)
            {
                return (sourceFraction - fromFraction) / (toFraction - fromFraction);
            }

            public int CompareTo(FractionVertexPair other)
            {
                return Fraction.CompareTo(other.Fraction);
            }
        }
        
        /// <summary>
        /// Default length of the largest segment that is close enough to be considered the same point.
        /// </summary>
        public const float DefaultTolerance = 0.9f;

        /// <summary>
        /// Checks that the array conforms to the following requirements assumed by functions in this class, and throws
        /// if it fails any of these checks:
        /// <list type="bullet">
        ///     <item><description>
        ///         There must be at least two elements in the array.
        ///     </description></item>
        ///     <item><description>
        ///         The <see cref="FractionVertexPair.Fraction">Fraction</see> of each element in the array must be unique,
        ///         a finite number (not <see cref="float.NaN">NaN</see> or positive or negative infinity), and sorted
        ///         in ascending order.
        ///     </description></item>
        ///     <item><description>
        ///         The <see cref="FractionVertexPair.Inner">Inner</see> and
        ///         <see cref="FractionVertexPair.Outer">Outer</see> vertices of each element in the array must contain
        ///         two finite elements (not <see cref="float.NaN">NaN</see> or positive or negative infinity) each.
        ///     </description></item>
        ///     <item><description>
        ///         The <see cref="FractionVertexPair.Inner">Inner</see> and
        ///         <see cref="FractionVertexPair.Outer">Outer</see> vertices of each element in the array must not be
        ///         within <see cref="tolerance" /> of each other.
        ///     </description></item>
        ///     <item><description>
        ///         The <see cref="FractionVertexPair.Center">Center</see> of each item in the array must not be
        ///         within <see cref="tolerance" /> of the adjacent elements' centers.
        ///     </description></item>
        /// </list>
        /// </summary>
        /// <param name="vertexPairs">The array of vertex pairs to check.</param>
        /// <param name="tolerance">The tolerance to use for testing for minimum distances.</param>
        /// <exception cref="SafetyCheckException">Thrown if any of the above rules are violated.</exception>
        [Conditional("DEBUG")]
        private static void CheckValidGauge(ImmutableArray<FractionVertexPair> vertexPairs, float tolerance)
        {
            if (vertexPairs.Length < 2)
            {
                throw new SafetyCheckException(
                    $"{nameof(vertexPairs)} too small",
                    $"at least 2 elements are necessary in {nameof(vertexPairs)}, but found {vertexPairs.Length}");
            }

            for (var index = 0; index < vertexPairs.Length; index++)
            {
                if (!float.IsFinite(vertexPairs[index].Fraction))
                {
                    throw new SafetyCheckException(
                        $"nonfinite {nameof(vertexPairs)}[{index}]",
                        $"Any {nameof(FractionVertexPair.Fraction)} in {nameof(vertexPairs)} must be finite numbers," +
                        $" but was {vertexPairs[index].Fraction} at index {index}");
                }

                if (index > 0 && !(vertexPairs[index].Fraction > vertexPairs[index - 1].Fraction))
                {
                    throw new SafetyCheckException(
                        $"{nameof(vertexPairs)}[{index}] out of order",
                        $"{nameof(FractionVertexPair.Fraction)} in {nameof(vertexPairs)} must be strictly ascending," +
                        $" but was {vertexPairs[index - 1].Fraction} at [{index - 1}]" +
                        $" and {vertexPairs[index].Fraction} at [{index}]");
                }

                if (!Vector2.AllWhereAllBitsSet(Vector2.IsFinite(vertexPairs[index].Inner)))
                {
                    throw new SafetyCheckException(
                        $"nonfinite {nameof(vertexPairs)}[{index}] inner",
                        $"Each {nameof(FractionVertexPair.Inner)} vertex in {nameof(vertexPairs)} must be finite," +
                        $" but was {vertexPairs[index].Inner} at index {index}");
                }

                if (!Vector2.AllWhereAllBitsSet(Vector2.IsFinite(vertexPairs[index].Outer)))
                {
                    throw new SafetyCheckException(
                        $"nonfinite {nameof(vertexPairs)}[{index}] outer",
                        $"Each {nameof(FractionVertexPair.Outer)} vertex in {nameof(vertexPairs)} must be finite," +
                        $" but was {vertexPairs[index].Outer} at index {index}");
                }

                var thickness = vertexPairs[index].Thickness();
                if (thickness < tolerance)
                {
                    throw new SafetyCheckException(
                        $"{nameof(vertexPairs)}[{index}] too thin",
                        $"Each pair of vertices in {nameof(vertexPairs)} must have a thickness of at least {tolerance}," +
                        $" but had a thickness of only {thickness} at index {index}");
                }

                var distanceFromLastSquared =
                    Vector2.DistanceSquared(vertexPairs[index].Center(), vertexPairs[index - 1].Center());
                if (index > 0 && distanceFromLastSquared < tolerance * tolerance)
                {
                    throw new SafetyCheckException(
                        $"{nameof(vertexPairs)}[{index}] too close",
                        $"Each pair of vertices in {nameof(vertexPairs)} must be separated by at least {tolerance}," +
                        $" but pairs at indices {index - 1} and {index} were only separated by" +
                        $" {Math.Sqrt(distanceFromLastSquared)}");
                }
            }
        }

        /// <summary>
        /// Checks that a fraction is valid for use with a gauge, meaning that it is a finite number.
        /// </summary>
        /// <param name="fraction">The fraction to test for validity.</param>
        /// <param name="name">The name to use for the fraction in <see cref="SafetyCheckException" />.</param>
        /// <exception cref="SafetyCheckException">Thrown if the fraction is not valid.</exception> 
        [Conditional("DEBUG")]
        private static void CheckValidFraction(
            float fraction, [CallerArgumentExpression(nameof(fraction))] string name = "name")
        {
            if (!float.IsFinite(fraction))
            {
                throw new SafetyCheckException(
                    $"nonfinite {name}",
                    $"{name} must be a finite number, but was {fraction}");
            }
        }

        /// <summary>
        /// Checks that the given fractions are valid fractions individually and <see cref="fromFraction" /> is less
        /// than or equal to <see cref="toFraction" />.
        /// </summary>
        /// <param name="fromFraction">The start fraction to check.</param>
        /// <param name="toFraction">The end fraction to check.</param>
        /// <exception cref="SafetyCheckException">Thrown if the fraction pair violates these conditions.</exception> 
        private static void CheckValidFractionPair(float fromFraction, float toFraction)
        {
            CheckValidFraction(fromFraction);
            CheckValidFraction(toFraction);
            if (toFraction < fromFraction)
            {
                throw new SafetyCheckException(
                    "fraction pair out of order",
                    $"{nameof(toFraction)} ({toFraction}) must be greater than or equal to" +
                    $" {nameof(fromFraction)} ({fromFraction})");
            }
        }

        /// <summary>
        /// Checks that a <see cref="yStops" /> array is valid for use with <see cref="Fill" />, meaning that it is an ordered array of floats with
        /// all elements finite and strictly ascending.
        /// </summary>
        /// <param name="yStops">The yStops array to test for validity.</param>
        /// <exception cref="ArgumentException">Thrown if the array is not valid.</exception>
        [Conditional("DEBUG")]
        private static void CheckValidYStops(ImmutableArray<float> yStops)
        {
            if (yStops.Length < 2)
            {
                throw new SafetyCheckException(
                    $"{nameof(yStops)} too small",
                    $"{nameof(yStops)} must have at least two elements");
            }
            
            var lastValue = yStops[0];

            for (var index = 1; index < yStops.Length; index++)
            {
                if (!float.IsFinite(yStops[index]))
                {
                    throw new SafetyCheckException(
                        $"nonfinite {nameof(yStops)}[{index}]",
                        $"Any element in ${nameof(yStops)} must be a finite number," +
                        $" but was {yStops[index]} at index {index}");
                }

                if (!(yStops[index] > lastValue))
                {
                    throw new SafetyCheckException(
                        $"{nameof(yStops)}[{index}] out of order",
                        $"Each element in {nameof(yStops)} must be strictly ascending," +
                        $" but was {lastValue} at [{index - 1}] and {yStops[index]} at [{index}]");
                }

                lastValue = yStops[index];
            }
        }


        /// <summary>
        /// Verifies that the thickness factor is valid, meaning it is not 0.
        /// </summary>
        /// <param name="factor">The factor to verify.</param>
        /// <exception cref="SafetyCheckException">Thrown if the thickness fails validation.</exception>
        private static void CheckValidThicknessFactor(float factor)
        {
            if (!float.IsFinite(factor) || factor == 0.0f)
            {
                throw new SafetyCheckException(
                    "bad thickness",
                    $"{nameof(factor)} must be a finite, nonzero number, but was {factor}");
            }
        }

        /// <summary>
        /// Checks whether the given fringe or tolerance value is valid.
        /// </summary>
        /// <param name="fringe">The value of the fringe or tolerance to check.</param>
        /// <param name="name">The name to use for the given fringe or tolerance in the exception.</param>
        [Conditional("DEBUG")]
        private static void CheckValidFringeOrTolerance(float fringe,
            [CallerArgumentExpression(nameof(fringe))] string name = "fringe")
        {
            if (!float.IsFinite(fringe) || fringe < 0.0f)
            {
                throw new SafetyCheckException(
                    $"bad {name}",
                    $"{name} must be a finite, positive number or zero, but was {fringe}");
            }
        }

        /// <summary>
        /// Adjusts the thickness of the gauge by moving the sides of it closer to each other
        /// (<see cref="factor" /> less than one) or further apart (<see cref="factor" /> greater than one).
        /// <para>
        /// Depending on the gauge, this may distort the shape of the gauge depending on the value of innerFraction.
        /// </para> 
        /// </summary>
        /// <param name="gauge">A gauge, as described by <see cref="CheckValidGauge" />.</param>
        /// <param name="factor">The factor to multiply <see cref="FractionVertexPair.Thickness">Thickness</see> of the
        /// vertex pair by. Must be <c>!= 0.0f</c>.
        /// </param>
        /// <param name="boundingBoxMin">Returns the minimum coordinate in each dimension.</param>
        /// <param name="boundingBoxMax">Returns the maximum coordinate in each dimension.</param>
        /// <param name="innerFraction">The fraction of movement that will be done by the
        /// <see cref="FractionVertexPair.Inner">Inner</see> (vs. <see cref="FractionVertexPair.Outer">Outer</see>)
        /// vertex.</param>
        /// <param name="tolerance">The tolerance to use for testing for minimum distances.</param>
        /// <returns>The new gauge with the reduced or increased thickness.</returns>
        public static ImmutableArray<FractionVertexPair> AdjustThickness(
            ImmutableArray<FractionVertexPair> gauge, float factor, out Vector2 boundingBoxMin,
            out Vector2 boundingBoxMax, float innerFraction = 0.5f, float tolerance = DefaultTolerance)
        {
            CheckValidFringeOrTolerance(tolerance);
            CheckValidGauge(gauge, tolerance);
            CheckValidThicknessFactor(factor);
            var vertexPairs = ImmutableArray.CreateBuilder<FractionVertexPair>(gauge.Length);

            var firstPair = gauge[0].AdjustThickness(factor, innerFraction);
            boundingBoxMin = firstPair.Min();
            boundingBoxMax = firstPair.Max();
            vertexPairs.Add(firstPair);
            for (var index = 1; index < gauge.Length; index++)
            {
                var pair = gauge[index].AdjustThickness(factor, innerFraction);
                boundingBoxMin = Vector2.Min(boundingBoxMin, pair.Min());
                boundingBoxMax = Vector2.Min(boundingBoxMax, pair.Max());
                vertexPairs.Add(pair);
            }

            var output = vertexPairs.MoveToImmutable();
            CheckValidGauge(output, tolerance);
            return output;
        }

        /// <summary>
        /// Gets or synthesizes the vertex pair at <see cref="fraction" />, which must be a finite number.
        /// Synthesized vertex pairs are linearly interpolated between the two nearest <see cref="FractionVertexPair" />s.
        /// Also returns the <see cref="index" /> in the <see cref="gauge" /> where the greatest <see cref="FractionVertexPair.Fraction">Fraction</see> less than or equal to
        /// <see cref="fraction" /> was found, and whether the desired pair <see cref="exists" /> or had to be synthesized.
        /// </summary>
        /// <param name="gauge">The gauge, conforming to <see cref="CheckValidGauge" />, to get the vertex pair from.</param>
        /// <param name="fraction">
        ///     The desired fraction of the output <see cref="FractionVertexPair" />, in <c>[0.0f, 1.0f]</c>.
        /// </param>
        /// <param name="index">
        ///     The index of the last vertex pair with a <see cref="FractionVertexPair.Fraction">Fraction</see> less than or equal to the desired
        ///     <see cref="fraction" />. The return value is either an existing vertex pair at this <see cref="index" />, if
        ///     <see cref="exists" /> is true, or a synthesized vertex pair interpolated between the pairs at <see cref="index" /> and
        ///     <see cref="index" /> + 1, if <see cref="exists" /> is false.
        /// </param>
        /// <param name="exists">
        ///     True if a vertex pair near enough to the target <see cref="fraction" /> was already in the
        ///     <see cref="gauge" />, false if it was synthesized.
        /// </param>
        /// <param name="fromIndex">The index the search should start at. Defaults to the start of the array.</param>
        /// <param name="tolerance">The tolerance to use for testing for minimum distances.</param>
        /// <returns>A synthesized vertex pair with
        /// <see cref="FractionVertexPair.Fraction">Fraction</see> == <see cref="fraction" />, or the closest existing
        /// vertex pair, if one would be within <see cref="tolerance" /> or the result would be off the end of the
        /// existing gauge.</returns>
        private static FractionVertexPair GetVertexPairAt(
            ImmutableArray<FractionVertexPair> gauge, float fraction, out int index, out bool exists,
            int fromIndex = 0, float tolerance = DefaultTolerance)
        {
            CheckValidFringeOrTolerance(tolerance);
            if (fraction <= gauge[fromIndex].Fraction)
            {
                exists = true;
                index = fromIndex;
                return gauge[fromIndex];
            }

            if (fraction >= gauge[^1].Fraction)
            {
                exists = true;
                index = gauge.Length - 1;
                return gauge[index];
            }
            
            index = fromIndex;
            while (index < gauge.Length - 1 && gauge[index + 1].Fraction <= fraction)
            {
                index++;
            }

            var result = FractionVertexPair.SynthesizeInterpolated(gauge[index], gauge[index + 1], fraction);
            var resultCenter = result.Center();
            var fromPreviousSquared = Vector2.DistanceSquared(resultCenter, gauge[index].Center());
            var fromNextSquared = Vector2.DistanceSquared(resultCenter, gauge[index + 1].Center());
            var toleranceSquared = tolerance * tolerance;

            if (fromPreviousSquared <= toleranceSquared && fromPreviousSquared <= fromNextSquared)
            {
                exists = true;
                return gauge[index];
            }

            if (fromNextSquared <= toleranceSquared)
            {
                exists = true;
                return gauge[index + 1];
            }

            exists = false;
            return result;
        }

        /// <summary>
        /// Gets a slice of the gauge spanning from <see cref="fromFraction" /> to <see cref="toFraction" />, rescaled so that the new
        /// start and end points are <see cref="FractionVertexPair.Fraction">Fraction</see> = <c>0.0f</c> and <c>1.0f</c> respectively.
        /// </summary>
        /// <param name="gauge">The gauge, conforming to <see cref="CheckValidGauge" />, to get a subset of.</param>
        /// <param name="boundingBoxMin">Returns the minimum coordinate in each dimension.</param>
        /// <param name="boundingBoxMax">Returns the maximum coordinate in each dimension.</param>
        /// <param name="fromFraction">The fraction at which to start the gauge.</param>
        /// <param name="toFraction">The fraction at which to end the gauge.</param>
        /// <param name="tolerance">
        ///     The length of the largest segment which would be too small to bother with. Must be positive or zero.
        /// </param>
        /// <returns>
        /// The subset of the gauge between <see cref="fromFraction" /> and <see cref="toFraction" />, or an empty array if
        /// the gauge would be a single segment of size <see cref="tolerance" /> or smaller.
        /// </returns>
        public static ImmutableArray<FractionVertexPair> Slice(
            ImmutableArray<FractionVertexPair> gauge, out Vector2 boundingBoxMin, out Vector2 boundingBoxMax,
            float fromFraction = 0.0f, float toFraction = 1.0f, float tolerance = DefaultTolerance)
        {
            CheckValidFringeOrTolerance(tolerance);
            CheckValidGauge(gauge, tolerance);
            CheckValidFractionPair(fromFraction, toFraction);
            boundingBoxMin = Vector2.NaN;
            boundingBoxMax = Vector2.NaN;
            var fromPair = GetVertexPairAt(gauge, fromFraction, out var fromIndex, out var fromExists, tolerance: tolerance);
            var toPair = GetVertexPairAt(gauge, toFraction, out var toIndex, out var toExists, fromIndex, tolerance: tolerance);
            if (fromIndex == toIndex && fromExists && toExists)
            {
                // Same point, get out
                return [];
            }

            var copyStartIndex = fromIndex + 1;
            var copyEndIndex = toExists ? toIndex : toIndex + 1;

            if (copyStartIndex == copyEndIndex && fromPair.VerticesWithin(toPair, tolerance))
            {
                return [];
            }

            boundingBoxMin = Vector2.Min(fromPair.Min(), toPair.Min());
            boundingBoxMax = Vector2.Max(fromPair.Max(), toPair.Max());
            var builder =
                ImmutableArray.CreateBuilder<FractionVertexPair>(2 + copyEndIndex - copyStartIndex);
            builder.Add(fromPair with { Fraction = 0.0f });
            for (var index = copyStartIndex; index < copyEndIndex; index++)
            {
                var pair = gauge[index];
                boundingBoxMin = Vector2.Min(boundingBoxMin, pair.Min());
                boundingBoxMax = Vector2.Max(boundingBoxMax, pair.Max());
                builder.Add(pair with
                {
                    Fraction = FractionVertexPair.RelativeFraction(fromPair.Fraction, toPair.Fraction, pair.Fraction)
                });
            }

            builder.Add(toPair with { Fraction = 1.0f });
            var result = builder.MoveToImmutable();
            CheckValidGauge(result, tolerance);
            return result;
        }

        /// <summary>
        /// Transforms the <see cref="gauge" /> using the <see cref="transform" /> matrix.
        /// </summary>
        /// <param name="gauge">The gauge, conforming to <see cref="CheckValidGauge" />, to be transformed.</param>
        /// <param name="transform">The transform matrix to apply to each vertex in the gauge.</param>
        /// <param name="boundingBoxMin">The smallest coordinates in each dimension in the transformed gauge.</param>
        /// <param name="boundingBoxMax">The largest coordinates in each dimension in the transformed gauge.</param>
        /// <param name="tolerance">The tolerance to use for testing for minimum distances.</param>
        /// <returns>The transformed gauge.</returns>
        public static ImmutableArray<FractionVertexPair> Transform(
            ImmutableArray<FractionVertexPair> gauge, Matrix3x2 transform,
            out Vector2 boundingBoxMin, out Vector2 boundingBoxMax, float tolerance = DefaultTolerance)
        {
            CheckValidFringeOrTolerance(tolerance);
            CheckValidGauge(gauge, tolerance);
            var builder = ImmutableArray.CreateBuilder<FractionVertexPair>(gauge.Length);
            var firstPair = gauge[0].Transform(transform);
            boundingBoxMin = firstPair.Min();
            boundingBoxMax = firstPair.Max();
            builder.Add(firstPair);
            for (var index = 1; index < gauge.Length; index++)
            {
                var pair = gauge[index].Transform(transform);
                boundingBoxMin = Vector2.Min(boundingBoxMin, pair.Min());
                boundingBoxMax = Vector2.Max(boundingBoxMax, pair.Max());
                builder.Add(pair);
            }

            var result = builder.MoveToImmutable();
            CheckValidGauge(result, tolerance);
            return result;
        }

        /// <summary>
        /// Returns a gauge with additional interpolated stops at the given locations, reusing preexisting stops if
        /// they exist.
        /// </summary>
        /// <param name="gauge">The gauge, conforming to <see cref="CheckValidGauge" />, to add additional stops to.</param>
        /// <param name="extraFractions">The additional fractions to add explicit stops to.</param>
        /// <param name="tolerance">The tolerance to use for testing for minimum distances.</param>
        /// <returns>The gauge enhanced with additional stops.</returns>
        public static ImmutableArray<FractionVertexPair> EnsureStopsAt(
            ImmutableArray<FractionVertexPair> gauge, ImmutableArray<float> extraFractions, float tolerance = DefaultTolerance)
        {
            CheckValidFringeOrTolerance(tolerance);
            CheckValidGauge(gauge, tolerance);
            var xStopsBuilder =
                ImmutableArray.CreateBuilder<FractionVertexPair>(extraFractions.Length + gauge.Length);
            var nextExtraStopIndex = 0;
            var skippedStops = 0;
            // We don't know what stops outside the start and end of the gauge we've been given would look like.
            for (;
                 nextExtraStopIndex < extraFractions.Length &&
                 extraFractions[nextExtraStopIndex] < gauge[0].Fraction;
                 nextExtraStopIndex++)
            {
                skippedStops++;
            }
            // As a result, the first element in the array is always going to be the first element in the gauge. 
            xStopsBuilder.Add(gauge[0]);
            var nextPairIndex = 1;
            // Interleave the fraction pairs with the extra stops, transforming along the way.
            while (nextPairIndex < gauge.Length && nextExtraStopIndex < extraFractions.Length)
            {
                var nextPair = gauge[nextPairIndex];
                var nextExtraFraction = extraFractions[nextExtraStopIndex];
                
                if (nextExtraFraction <= nextPair.Fraction)
                {
                    var resolvedNext =
                        GetVertexPairAt(gauge, nextExtraFraction,
                            out _, out var exists, nextPairIndex - 1);
                    if (exists)
                    {
                        skippedStops++;
                    }
                    else
                    {
                        xStopsBuilder.Add(resolvedNext);
                    }
                    nextExtraStopIndex++;
                }
                else
                {
                    nextPairIndex++;
                    xStopsBuilder.Add(nextPair);
                }
            }

            // We only care about any gauge pairs outside the extras, not the other way around, because we don't know
            // what the gauge looks like beyond what we've been given.
            skippedStops += extraFractions.Length - nextExtraStopIndex;
            for (; nextPairIndex < gauge.Length; nextPairIndex++)
            {
                xStopsBuilder.Add(gauge[nextPairIndex]);
            }

            if (skippedStops > 0)
            {
                xStopsBuilder.Capacity -= skippedStops;
            }

            var result = xStopsBuilder.MoveToImmutable();
            CheckValidGauge(result, tolerance);
            return result;
        }

        /// <summary>
        /// Default Y stops for gauge filling, when Y stops aren't specified.
        /// </summary>
        private static readonly ImmutableArray<float> DefaultYStops = [0.0f, 1.0f];

        /// <summary>
        /// Renders the <see cref="gauge" />'s fill with the given <see cref="colors" /> onto <see cref="drawList" />.
        /// </summary>
        /// <param name="gauge">
        /// The gauge, conforming to <see cref="CheckValidGauge" />, to render the fill of.
        /// </param>
        /// <param name="drawList">The draw list to render the gauge to.</param>
        /// <param name="colors">The 2-dimensional gradient to color the gauge with. <c>X</c> in the gradient tracks the
        /// <see cref="FractionVertexPair.Fraction">Fraction</see> along the gauge from start to end. <c>Y</c> in the
        /// gradient moves from <c>0.0f</c> at the <see cref="FractionVertexPair.Inner">Inner</see> vertex of each pair
        /// to <c>1.0f</c> at the <see cref="FractionVertexPair.Outer">Outer</see> vertex of each pair.
        /// 
        /// <para>Note that additional vertices for additional color stops must be explicitly specified, so use
        /// EnsureStopsAt and pass <see cref="yStops" /> if the gradient has stops beyond the four corners.</para></param>
        /// <param name="yStops">
        /// The fractions of the span from <see cref="FractionVertexPair.Inner">Inner</see> <c>= 0.0f</c> to
        /// <see cref="FractionVertexPair.Outer">Outer</see> <c>= 1.0f</c> at which to draw vertices, sorted
        /// ascending.
        /// If the gradient has stops other than <c>0.0f</c> and <c>1.0f</c> in the Y-axis, this is important to
        /// actually display those extra stops! Defaults to <c>[0.0f, 1.0f]</c>.
        /// </param>
        /// <param name="antialiasingFringeAtEnds">The size of the fringe to add to the ends of the gauge to avoid
        /// aliasing. Set to 0 to disable.</param>
        /// <param name="antialiasingFringeAtSides">The size of the fringe to add to the sides of the gauge to avoid
        /// aliasing. Set to 0 to disable.</param>
        /// <param name="tolerance">The tolerance to use for testing for minimum distances.</param>
        public static void Fill(
            ImmutableArray<FractionVertexPair> gauge, ImDrawListPtr drawList, Gradient2D colors,
            ImmutableArray<float>? yStops = null, float antialiasingFringeAtEnds = 1.0f,
            float antialiasingFringeAtSides = 1.0f, float tolerance = DefaultTolerance)
        {
            CheckValidFringeOrTolerance(tolerance);
            CheckValidGauge(gauge, tolerance);
            var resolvedYStops = DefaultYStops;
            if (yStops.HasValue)
            {
                CheckValidYStops(yStops.Value);
                resolvedYStops = yStops.Value;
            }

            CheckValidFringeOrTolerance(antialiasingFringeAtEnds);
            CheckValidFringeOrTolerance(antialiasingFringeAtSides);

            var (indices, vertices, segments, quadsPerSegment) =
                CalculateStatisticsForGauge(
                    gauge.Length,
                    resolvedYStops.Length,
                    antialiasingFringeAtEnds,
                    antialiasingFringeAtSides);

            drawList.PrimReserve(indices, vertices);
            foreach (var index in CalculateIndicesForGauge(segments, quadsPerSegment))
            {
                drawList.PrimWriteIdx((ushort) (drawList.VtxCurrentIdx + index));
            }

            foreach (var (position, color) in
                     CalculateVerticesForGauge(gauge, colors, resolvedYStops,
                         antialiasingFringeAtEnds, antialiasingFringeAtSides))
            {
                drawList.PrimWriteVtx(position, drawList.Data.TexUvWhitePixel, color);
            }
        }

        private static (int indices, int vertices, int segments, int quadsPerSegment) CalculateStatisticsForGauge(
            int xStops, int yStops, float antialiasingFringeAtEnds, float antialiasingFringeAtSides)
        {
            // The yStops (+ antialiasing fringe if enabled) are the vertices we'll be using at each step
            var verticesPerStep = yStops + (antialiasingFringeAtSides > 0 ? 2 : 0);
            // The gauge fractions (+ antialiasing fringe if enabled) are the steps themselves
            var steps = xStops + (antialiasingFringeAtEnds > 0 ? 2 : 0);
            var vertices = steps * verticesPerStep;
            // One segment for each adjacent pair of steps
            var segments = steps - 1;
            // One quad for each adjacent pair of vertices in a step
            var quadsPerSegment = verticesPerStep - 1;
            // Two triangles per quad
            var trianglesPerSegment = quadsPerSegment * 2;
            var triangles = segments * trianglesPerSegment;
            // Three indices per triangle
            var indices = triangles * 3;
            return (indices, vertices, segments, quadsPerSegment);
        }

        /// <summary>
        /// Helper function for <see cref="Fill" /> and <see cref="Wireframe"/> that generates the indices connecting
        /// vertices generated by <see cref="CalculateVerticesForGauge" />.
        /// </summary>
        /// <param name="segments">The number of segments to draw.</param>
        /// <param name="quadsPerSegment">The number of quads in each segment.</param>
        /// <returns>A coroutine generating indices into the output of <see cref="CalculateVerticesForGauge" />.
        /// </returns>
        private static IEnumerable<ushort> CalculateIndicesForGauge(int segments, int quadsPerSegment)
        {
            var verticesPerStep = quadsPerSegment + 1;
            for (var segmentIdx = 0; segmentIdx < segments; segmentIdx++)
            {
                for (var quadIdx = 0; quadIdx < quadsPerSegment; quadIdx++)
                {
                    yield return (ushort)(segmentIdx * verticesPerStep + quadIdx);
                    yield return (ushort)(segmentIdx * verticesPerStep + quadIdx + 1);
                    yield return (ushort)((segmentIdx + 1) * verticesPerStep + quadIdx);
                    yield return (ushort)(segmentIdx * verticesPerStep + quadIdx + 1);
                    yield return (ushort)((segmentIdx + 1) * verticesPerStep + quadIdx);
                    yield return (ushort)((segmentIdx + 1) * verticesPerStep + quadIdx + 1);
                }
            }
        }

        /// <summary>
        /// Helper function for <see cref="Fill"/> and <see cref="Wireframe"/> that generates the vertices to be drawn
        /// for a whole gauge.
        /// </summary>
        /// <param name="gauge">The array of vertex pairs to draw.</param>
        /// <param name="colors">The gradient to use to color the vertices.</param>
        /// <param name="yStops">The array of y-stops to draw for each pair.</param>
        /// <param name="antialiasingFringeAtEnds">
        ///     How much antialiasing fringe to draw at the ends of the gauge.</param>
        /// <param name="antialiasingFringeAtSides">
        ///     How much antialiasing fringe to draw at the sides of the gauge.</param>
        /// <returns>A coroutine generating vertices to fill or wireframe.</returns>
        private static IEnumerable<(Vector2 position, uint color)> CalculateVerticesForGauge(
            ImmutableArray<FractionVertexPair> gauge, Gradient2D colors, ImmutableArray<float> yStops,
            float antialiasingFringeAtEnds, float antialiasingFringeAtSides)
        {
            if (antialiasingFringeAtEnds > 0)
            {
                var pair = gauge[0].Offset(
                    Circle.PointAtAngle(
                        Vector2.Zero,
                        gauge[0].PerpendicularAngleAwayFrom(gauge[1]),
                        antialiasingFringeAtEnds));
                foreach (var (position, color) in 
                         CalculateVerticesForPair(pair, colors, yStops, antialiasingFringeAtSides))
                {
                    yield return (position, color & ~ImGuiAdditions.AlphaMask);
                }
            }

            foreach (var pair in gauge)
            {
                foreach (var tuple in CalculateVerticesForPair(pair, colors, yStops, antialiasingFringeAtSides))
                {
                    yield return tuple;
                }
            }
            
            if (antialiasingFringeAtEnds > 0)
            {
                var pair = gauge[^1].Offset(
                    Circle.PointAtAngle(
                        Vector2.Zero,
                        gauge[^1].PerpendicularAngleAwayFrom(gauge[^2]),
                        antialiasingFringeAtEnds));
                foreach (var (position, color) in
                         CalculateVerticesForPair(pair, colors, yStops, antialiasingFringeAtSides))
                {
                    yield return (position, color & ~ImGuiAdditions.AlphaMask);
                }
            }
        }

        /// <summary>
        /// Helper function for <see cref="Fill"/> and <see cref="Wireframe"/> that generates the vertices to be drawn
        /// for a single vertex pair.
        /// </summary>
        /// <param name="pair">The vertex pair to calculate vertices for.</param>
        /// <param name="colors">The gradient to use to color the vertices.</param>
        /// <param name="yStops">The array of y-stops to draw for each pair.</param>
        /// <param name="antialiasingFringeAtSides">
        ///     How much antialiasing fringe to draw at the sides of the gauge.</param>
        /// <returns>A coroutine generating vertices to fill or wireframe.</returns>
        private static IEnumerable<(Vector2 position, uint color)> CalculateVerticesForPair(
            FractionVertexPair pair, Gradient2D colors, ImmutableArray<float> yStops,
            float antialiasingFringeAtSides)
        {
            var fringeDistance = Vector2.Zero; 
            if (antialiasingFringeAtSides > 0)
            {
                var innerToOuter = Vector2.Normalize(pair.InnerToOuter());
                fringeDistance = antialiasingFringeAtSides * innerToOuter;
                yield return (
                    pair[yStops[0]] - fringeDistance,
                    colors[new Vector2(pair.Fraction, yStops[0])] &  ~ImGuiAdditions.AlphaMask);
            }

            foreach (var yFraction in yStops)
            {
                yield return (pair[yFraction], colors[new Vector2(pair.Fraction, yFraction)]);
            }

            if (antialiasingFringeAtSides > 0)
            {
                yield return (
                    pair[yStops[^1]] - fringeDistance,
                    colors[new Vector2(pair.Fraction, yStops[^1])] &  ~ImGuiAdditions.AlphaMask);
            }
        }

        /// <summary>
        /// Draws the area rendered by the gauge in wireframe, for debugging gauge shapes.
        /// </summary>
        /// <param name="gauge">
        /// The gauge, conforming to <see cref="CheckValidGauge" />, to render the wireframe of.
        /// </param>
        /// <param name="drawList">The draw list to render the gauge to.</param>
        /// <param name="color">The packed color to use for the wireframe.</param>
        /// <param name="yStops">
        /// The fractions of the span from <see cref="FractionVertexPair.Inner">Inner</see> <c>= 0.0f</c> to
        /// <see cref="FractionVertexPair.Outer">Outer</see> <c>= 1.0f</c> at which to draw vertices, sorted
        /// ascending. Defaults to <c>[0.0f, 1.0f]</c>.
        /// </param>
        /// <param name="flags">The flags to use to draw the wireframe.</param>
        /// <param name="thickness">The stroke width with which to draw the wireframe.</param>
        /// <param name="antialiasingFringeAtEnds">
        ///     How much antialiasing fringe to draw at the ends of the gauge. This changes what triangles are drawn in
        ///     the wireframe, not the antialiasing of the lines of those triangles; for that, use
        ///     <see cref="ImDrawListPtr.FringeScale" />.</param>
        /// <param name="antialiasingFringeAtSides">
        ///     How much antialiasing fringe to draw at the sides of the gauge. This changes what triangles are drawn in
        ///     the wireframe, not the antialiasing of the lines of those triangles; for that, use
        ///     <see cref="ImDrawListPtr.FringeScale" />.</param>
        /// <param name="tolerance">The tolerance to use for testing for minimum distances.</param>
        public static void Wireframe(
            ImmutableArray<FractionVertexPair> gauge, ImDrawListPtr drawList, uint color,
            ImmutableArray<float>? yStops = null, ImDrawFlags flags = ImDrawFlags.None, float thickness = 1.0f,
            float antialiasingFringeAtEnds = 1.0f, float antialiasingFringeAtSides = 1.0f, float tolerance = DefaultTolerance)
        {
            CheckValidFringeOrTolerance(tolerance);
            CheckValidGauge(gauge, tolerance);
            var resolvedYStops = DefaultYStops;
            if (yStops.HasValue)
            {
                CheckValidYStops(yStops.Value);
                resolvedYStops = yStops.Value;
            }

            CheckValidFringeOrTolerance(antialiasingFringeAtSides);
            CheckValidFringeOrTolerance(antialiasingFringeAtEnds);
            
            var (_, vertices, segments, quadsPerSegment) =
                CalculateStatisticsForGauge(
                    gauge.Length,
                    resolvedYStops.Length,
                    antialiasingFringeAtEnds,
                    antialiasingFringeAtSides);
            
            var verticesRecorder = ImmutableArray.CreateBuilder<Vector2>(vertices);
            foreach (var (position, _) in
                     CalculateVerticesForGauge(gauge, Gradient2D.Transparent, resolvedYStops,
                         antialiasingFringeAtEnds, antialiasingFringeAtSides))
            {
                verticesRecorder.Add(position);
            }
            
            var verticesRecorded = verticesRecorder.MoveToImmutable();
            
            var indexEnumerator = CalculateIndicesForGauge(segments, quadsPerSegment).GetEnumerator();
            try
            {
                while (indexEnumerator.MoveNext())
                {
                    drawList.PathLineTo(verticesRecorded[indexEnumerator.Current]);
                    indexEnumerator.MoveNext();
                    drawList.PathLineTo(verticesRecorded[indexEnumerator.Current]);
                    indexEnumerator.MoveNext();
                    drawList.PathLineTo(verticesRecorded[indexEnumerator.Current]);
                    drawList.PathStroke(color, flags | ImDrawFlags.Closed, thickness);
                }
            }
            finally
            {
                indexEnumerator.Dispose();
            }
        }

        // Strokes the Gauge's perimeter with the given color and thickness.
        // Draws caps if drawCaps is true or if drawCaps is null/not given and the start and end of the gauge
        // do not coincide.
        // The default cap radius is half the line thickness. Since the Fill method does not round corners, any larger
        // would expose the square corners of the gauge where they poke through the rounded corners.
        public static void Stroke(ImmutableArray<FractionVertexPair> gauge, ImDrawListPtr drawList,
            uint strokeColor, ImDrawFlags strokeFlags, float thickness,
            bool? drawCaps = null, float? capRadius = null, float tolerance = DefaultTolerance)
        {
            CheckValidGauge(gauge, tolerance);

            var resolvedDrawCaps = drawCaps ?? !gauge[0].VerticesWithin(gauge[^1], tolerance);
            // If a segment is thinner than the cap radius, using the full cap radius would make oversized arcs
            // that cross over each other, so don't go any bigger than half the start or end thickness.
            var resolvedCapRadius =
                resolvedDrawCaps
                    ? Math.Min(
                        capRadius ?? thickness / 2,
                        Math.Min(gauge[0].Thickness(), gauge[^1].Thickness()) / 2)
                    : 0.0f;
            var drawRoundedCaps = resolvedCapRadius > 0.0f;
            // If we are drawing rounded caps, the first and last segments will be drawn as part of the arcs instead of
            // using lines.
            var roundedCapIndexOffset = drawRoundedCaps ? 1 : 0;
            
            var firstCapOffsetAngle = drawRoundedCaps
                ? (float)gauge[0].PerpendicularAngleAwayFrom(gauge[1])
                : 0.0f;
            var firstCapCenterPair = drawRoundedCaps
                ? gauge[0].AdjustThickness(1 - resolvedCapRadius * 2 / gauge[0].Thickness())
                : gauge[0];
            var firstCapOuterAngle = drawRoundedCaps
                ? (float)gauge[0].InnerToOuterAngleRadians()
                : 0.0f;
            var firstCapDeltaAngle = firstCapOffsetAngle - firstCapOuterAngle;
            var firstCapInnerAngle = firstCapOffsetAngle + firstCapDeltaAngle;
            if (drawRoundedCaps)
            {
                drawList.PathArcTo(
                    firstCapCenterPair.Outer, resolvedCapRadius,
                    // Special case: If we have exactly the width of the bar in cap diameter, we need this to be just
                    // one arc, rather than two connected by a line.
                    firstCapCenterPair.Thickness() == 0 ? firstCapInnerAngle : firstCapOffsetAngle,
                    firstCapOuterAngle);
            }

            for (var index = roundedCapIndexOffset; index < gauge.Length - roundedCapIndexOffset; index++)
            {
                drawList.PathLineTo(gauge[index].Outer);
            }

            if (!resolvedDrawCaps)
            {
                // Since we're not drawing the caps, the inner and outer paths are separate, so commit here.
                drawList.PathStroke(strokeColor, strokeFlags, thickness);
            }

            var lastCapOffsetAngle = drawRoundedCaps
                ? (float)gauge[^1].PerpendicularAngleAwayFrom(gauge[^2])
                : 0.0f;
            var lastCapCenterPair = drawRoundedCaps
                ? gauge[^1].AdjustThickness(1.0f - resolvedCapRadius * 2.0f / gauge[^1].Thickness())
                : gauge[^1];
            var lastCapOuterAngle = drawRoundedCaps
                ? (float)gauge[^1].InnerToOuterAngleRadians()
                : 0.0f;
            var lastCapDeltaAngle = lastCapOffsetAngle - lastCapOuterAngle;
            var lastCapInnerAngle = lastCapOffsetAngle + lastCapDeltaAngle;
            if (drawRoundedCaps)
            {
                if (lastCapCenterPair.Thickness() == 0)
                {
                    // Special case: We have exactly the width of the bar in cap diameter, so we need this to be just
                    // one arc, rather than two connected by a line.
                    drawList.PathArcTo(
                        lastCapCenterPair.Outer, resolvedCapRadius,
                        lastCapOuterAngle, lastCapInnerAngle);
                }
                else
                {
                    drawList.PathArcTo(
                        lastCapCenterPair.Outer, resolvedCapRadius,
                        lastCapOuterAngle, lastCapOffsetAngle);
                    drawList.PathArcTo(
                        lastCapCenterPair.Inner, resolvedCapRadius,
                        lastCapOffsetAngle, lastCapInnerAngle);
                }
            }

            for (var index = roundedCapIndexOffset; index < gauge.Length - roundedCapIndexOffset; index++)
            {
                drawList.PathLineTo(gauge[^(index + 1)].Inner);
            }

            if (drawRoundedCaps && firstCapCenterPair.Thickness() > 0)
            {
                drawList.PathArcTo(
                    firstCapCenterPair.Inner, resolvedCapRadius,
                    firstCapInnerAngle, firstCapOffsetAngle);
            }

            drawList.PathStroke(strokeColor,
                resolvedDrawCaps ? strokeFlags | ImDrawFlags.Closed : strokeFlags,
                thickness);
        }
    }
}