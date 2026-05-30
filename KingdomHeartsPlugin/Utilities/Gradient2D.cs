using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Dalamud.Bindings.ImGui;

namespace KingdomHeartsPlugin.Utilities
{
    /// <summary>
    /// A 2-dimensional gradient supporting multiple color stops and intermediate color calculation.
    /// </summary>
    public sealed class Gradient2D : IEquatable<Gradient2D>, IFormattable
    {
        /// <summary>
        /// An empty gradient which is transparent black at all points.
        /// </summary>
        public static Gradient2D Transparent => SingleColor(0);
        
        /// <summary>
        /// Default single stop for an axis not involved in the gradient.
        /// </summary>
        private static readonly ImmutableArray<float> DefaultStops = [0.0f];
        
        /// <summary>
        /// Constructs the gradient and (in debug mode) verifies that it's a valid gradient.
        /// </summary>
        /// <param name="xStops">An array of points on the X-axis, sorted.</param>
        /// <param name="yStops">An array of points on the Y-axis, sorted.</param>
        /// <param name="colorStopsFloat4">An array of unpacked colors representing the same colors as
        /// <see cref="ColorStopsU32" />, used for calculating intermediate colors between points. The color of the stop
        /// at <c><see cref="XStops" />[x]</c>, <c><see cref="YStops" />[y]</c> is stored at
        /// <c><see cref="ColorStopsFloat4" />[y *
        /// <see cref="XStops" />.<see cref="ImmutableArray{float}.Length">Length</see> + x]</c>.</param>
        /// <param name="colorStopsU32">An array of packed colors representing the same colors as
        /// <see cref="ColorStopsFloat4" />, returned directly when a requested point is directly on a stop. The color
        /// of the stop at <c><see cref="XStops" />[x]</c>, <c><see cref="YStops" />[y]</c> is stored at
        /// <c><see cref="ColorStopsU32" />[y *
        /// <see cref="XStops" />.<see cref="ImmutableArray{float}.Length">Length</see> + x]</c>.</param>
        /// <exception cref="SafetyCheckException">Thrown in debug mode if the constructed gradient would be malformed.
        /// </exception>
        public Gradient2D(
            ImmutableArray<float> xStops, ImmutableArray<float> yStops,
            ImmutableArray<Vector4> colorStopsFloat4, ImmutableArray<uint> colorStopsU32)
        {
            XStops = xStops;
            YStops = yStops;
            ColorStopsFloat4 = colorStopsFloat4;
            ColorStopsU32 = colorStopsU32;
            CheckWellFormed();
        }
        
        /// <summary>
        /// An array of points on the X-axis, sorted.
        /// </summary>
        private ImmutableArray<float> XStops { get; }
        /// <summary>
        /// An array of points on the Y-axis, sorted.
        /// </summary>
        private ImmutableArray<float> YStops { get; }
        /// <summary>
        /// An array of unpacked colors representing the same colors as
        /// <see cref="ColorStopsU32" />, used for calculating intermediate colors between points. The color of the stop
        /// at <c><see cref="XStops" />[x]</c>, <c><see cref="YStops" />[y]</c> is stored at
        /// <c><see cref="ColorStopsFloat4" />[y *
        /// <see cref="XStops" />.<see cref="ImmutableArray{float}.Length">Length</see> + x]</c>.
        /// </summary>
        private ImmutableArray<Vector4> ColorStopsFloat4 { get; }
        /// <summary>
        /// An array of packed colors representing the same colors as
        /// <see cref="ColorStopsFloat4" />, returned directly when a requested point is directly on a stop. The color of the
        /// stop at <c><see cref="XStops" />[x]</c>, <c><see cref="YStops" />[y]</c> is stored at
        /// <c><see cref="ColorStopsU32" />[y *
        /// <see cref="XStops" />.<see cref="ImmutableArray{float}.Length">Length</see> + x]</c>.
        /// </summary>
        private ImmutableArray<uint> ColorStopsU32 { get; }

        /// <summary>
        /// Checks that this is a valid Gradient2D, satisfying the following conditions:
        /// <list type="bullet">
        ///     <item><description>
        ///         Each stop in <see cref="XStops" /> and <see cref="YStops" /> is a finite number, and they are
        ///         strictly increasing within an array, each of which is nonempty.
        ///     </description></item>
        ///     <item><description>
        ///         The number of colors in <see cref="ColorStopsFloat4" /> and <see cref="ColorStopsU32" /> are equal
        ///         to each other and equal to the number of x-stops times the number of y-stops.
        ///     </description></item>
        ///     <item><description>
        ///         Each color in <see cref="ColorStopsFloat4" /> matches the equivalent entry in
        ///         <see cref="ColorStopsU32" /> when converted to a packed color.
        ///     </description></item>
        ///     <item><description>
        ///         Each color in <see cref="ColorStopsFloat4" /> has all components as finite numbers in [0.0f, 1.0f].
        ///     </description></item>
        /// </list>
        /// </summary>
        /// <exception cref="SafetyCheckException">Thrown if any of the conditions are failed.</exception>
        [Conditional("DEBUG")]
        private void CheckWellFormed()
        {
            if (ColorStopsFloat4.Length != ColorStopsU32.Length)
            {
                throw new SafetyCheckException(
                    $"{nameof(ColorStopsFloat4)}/{nameof(ColorStopsU32)}.Length mismatch",
                    $"{nameof(ColorStopsFloat4)} and {nameof(ColorStopsU32)} lengths do not match.");
            }

            for (var index = 0; index < ColorStopsFloat4.Length; index++)
            {
                if (!Vector4.AllWhereAllBitsSet(Vector4.IsFinite(ColorStopsFloat4[index])))
                {
                    throw new SafetyCheckException(
                        $"nonfinite {nameof(ColorStopsFloat4)}[{index}]",
                        $"{nameof(ColorStopsFloat4)}[{index}] = {ColorStopsFloat4[index]} which is not a vector of" +
                        $" finite numbers");
                }
                if (!(Vector4.GreaterThanOrEqualAll(ColorStopsFloat4[index], Vector4.Zero)
                      && Vector4.LessThanOrEqualAll(ColorStopsFloat4[index], Vector4.One)))
                {
                    throw new SafetyCheckException(
                        $"{nameof(ColorStopsFloat4)}[{index}] out of range",
                        $"{nameof(ColorStopsFloat4)}[{index}] = {ColorStopsFloat4[index]} which does not have all" +
                        $" components between 0.0f and 1.0f inclusive.");
                }
                if (ImGui.ColorConvertFloat4ToU32(ColorStopsFloat4[index]) != ColorStopsU32[index])
                {
                    throw new SafetyCheckException(
                        $"{nameof(ColorStopsFloat4)}/{nameof(ColorStopsU32)}[{index}] mismatch",
                        $"{nameof(ColorStopsFloat4)} and {nameof(ColorStopsU32)} do not match at {index}" +
                        $" (found {ColorStopsFloat4[index]}, {ColorStopsU32[index]}).");
                }
            }

            var expectedLength = XStops.Length * YStops.Length;
            if (ColorStopsFloat4.Length != expectedLength)
            {
                throw new SafetyCheckException(
                    $"bad {nameof(ColorStopsFloat4)}/{nameof(ColorStopsU32)}.Length",
                    $"{nameof(ColorStopsFloat4)} and {nameof(ColorStopsU32)} arrays should be {expectedLength} for" +
                    $" {XStops.Length} XStops * {YStops.Length} YStops, but were {ColorStopsFloat4.Length} instead");
            }
            
            CheckWellFormedStopsArray(XStops);
            CheckWellFormedStopsArray(YStops);
        }

        /// <summary>
        /// Checks that <see cref="stops" /> is a valid stops array, where each element is a finite number,
        /// there is at least one element, and they are strictly increasing.
        /// </summary>
        /// <param name="stops">The array to check.</param>
        /// <param name="name">The name of <see cref="stops" /> for error messages.</param>
        /// <exception cref="SafetyCheckException">Thrown if any of the conditions are failed.</exception>
        [Conditional("DEBUG")]
        private static void CheckWellFormedStopsArray(
            ImmutableArray<float> stops, [CallerArgumentExpression(nameof(stops))]string name = "stops")
        {
            if (stops.IsEmpty)
            {
                throw new SafetyCheckException(
                    $"empty {name}",
                    $"stops arrays must have at least one element, but {name} was empty");
            }

            for (var i = 0; i < stops.Length; i++)
            {
                if (!float.IsFinite(stops[i]))
                {
                    throw new SafetyCheckException(
                        $"nonfinite {name}[{i}]",
                        $"all elements in {name} must be finite numbers, but found" +
                        $" {stops[i]} at index {i} in {name}");
                }
            }
            
            for (var i = 1; i < stops.Length; i++)
            {
                if (!(stops[i] > stops[i - 1]))
                {
                    throw new SafetyCheckException(
                        $"{name}[{i}] out of order",
                        $"all elements in {name} must be strictly greater than their predecessors," +
                        $" but {name} had {stops[i - 1]} at [{i - 1}] and {stops[i]} at [{i}]");
                }
            }
        }

        /// <summary>
        /// Creates a single-color gradient. Every point on this gradient will be the given color.
        /// </summary>
        /// <param name="color">The color to render.</param>
        /// <returns>A gradient with a single color.</returns>
        public static Gradient2D SingleColor(Vector4 color) =>
            new(DefaultStops, DefaultStops,
                [color], [ImGui.ColorConvertFloat4ToU32(color)]);
        /// <summary>
        /// Creates a single-color gradient. Every point on this gradient will be the given color.
        /// </summary>
        /// <param name="color">The color to render.</param>
        /// <returns>The created gradient.</returns>
        public static Gradient2D SingleColor(uint color) =>
            new(DefaultStops, DefaultStops,
                [ImGui.ColorConvertU32ToFloat4(color)], [color]);
        
        /// <summary>
        /// Creates a two-color gradient that varies along the X-axis.
        /// </summary>
        /// <param name="fromColor">The color of the stop at <c>X = 0.0f</c>.</param>
        /// <param name="toColor">The color of the stop at <c>X = 1.0f</c>.</param>
        /// <returns>The created gradient.</returns>
        public static Gradient2D TwoColorHorizontal(Vector4 fromColor, Vector4 toColor) =>
            new([0.0f, 1.0f], DefaultStops,
                [fromColor, toColor],
                [ImGui.ColorConvertFloat4ToU32(fromColor), ImGui.ColorConvertFloat4ToU32(toColor)]);
        /// <summary>
        /// Creates a two-color gradient that varies along the X-axis.
        /// </summary>
        /// <param name="fromColor">The color of the stop at <c>X = 0.0f</c>.</param>
        /// <param name="toColor">The color of the stop at <c>X = 1.0f</c>.</param>
        /// <returns>The created gradient.</returns>
        public static Gradient2D TwoColorHorizontal(uint fromColor, uint toColor) =>
            new([0.0f, 1.0f], DefaultStops,
                [ImGui.ColorConvertU32ToFloat4(fromColor), ImGui.ColorConvertU32ToFloat4(toColor)],
                [fromColor, toColor]);
        /// <summary>
        /// Creates a two-color gradient that varies along the Y-axis.
        /// </summary>
        /// <param name="fromColor">The color of the stop at <c>Y = 0.0f</c>.</param>
        /// <param name="toColor">The color of the stop at <c>Y = 1.0f</c>.</param>
        /// <returns>The created gradient.</returns>
        public static Gradient2D TwoColorVertical(Vector4 fromColor, Vector4 toColor) =>
            new(DefaultStops, [0.0f, 1.0f],
                [fromColor, toColor],
                [ImGui.ColorConvertFloat4ToU32(fromColor), ImGui.ColorConvertFloat4ToU32(toColor)]);
        /// <summary>
        /// Creates a two-color gradient that varies along the Y-axis.
        /// </summary>
        /// <param name="fromColor">The color of the stop at <c>Y = 0.0f</c>.</param>
        /// <param name="toColor">The color of the stop at <c>Y = 1.0f</c>.</param>
        /// <returns>The created gradient.</returns>
        public static Gradient2D TwoColorVertical(uint fromColor, uint toColor) =>
            new(DefaultStops, [0.0f, 1.0f],
                [ImGui.ColorConvertU32ToFloat4(fromColor), ImGui.ColorConvertU32ToFloat4(toColor)],
                [fromColor, toColor]);
        
        /// <summary>
        /// Creates a four-color gradient that varies along the X- and Y-axes.
        /// </summary>
        /// <param name="fromXFromYColor">The color of the stop at <c>X = 0.0f, Y = 0.0f</c>.</param>
        /// <param name="toXFromYColor">The color of the stop at <c>X = 1.0f, Y = 0.0f</c>.</param>
        /// <param name="fromXToYColor">The color of the stop at <c>X = 0.0f, Y = 1.0f</c>.</param>
        /// <param name="toXToYColor">The color of the stop at <c>X = 1.0f, Y = 1.0f</c>.</param>
        /// <returns>The created gradient.</returns>
        public static Gradient2D FourColor(Vector4 fromXFromYColor, Vector4 toXFromYColor,
            Vector4 fromXToYColor, Vector4 toXToYColor) =>
            new([0.0f, 1.0f], [0.0f, 1.0f],
                [fromXFromYColor, toXFromYColor, fromXToYColor, toXToYColor],
                [
                    ImGui.ColorConvertFloat4ToU32(fromXFromYColor),
                    ImGui.ColorConvertFloat4ToU32(toXFromYColor),
                    ImGui.ColorConvertFloat4ToU32(fromXToYColor),
                    ImGui.ColorConvertFloat4ToU32(toXToYColor),
                ]);
        /// <summary>
        /// Creates a four-color gradient that varies along the X- and Y-axes.
        /// </summary>
        /// <param name="fromXFromYColor">The color of the stop at <c>X = 0.0f, Y = 0.0f</c>.</param>
        /// <param name="toXFromYColor">The color of the stop at <c>X = 1.0f, Y = 0.0f</c>.</param>
        /// <param name="fromXToYColor">The color of the stop at <c>X = 0.0f, Y = 1.0f</c>.</param>
        /// <param name="toXToYColor">The color of the stop at <c>X = 1.0f, Y = 1.0f</c>.</param>
        /// <returns>The created gradient.</returns>
        public static Gradient2D FourColor(uint fromXFromYColor, uint toXFromYColor,
            uint fromXToYColor, uint toXToYColor) =>
            new([0.0f, 1.0f], [0.0f, 1.0f],
                [
                    ImGui.ColorConvertU32ToFloat4(fromXFromYColor),
                    ImGui.ColorConvertU32ToFloat4(toXFromYColor),
                    ImGui.ColorConvertU32ToFloat4(fromXToYColor),
                    ImGui.ColorConvertU32ToFloat4(toXToYColor),
                ],
                [fromXFromYColor, toXFromYColor, fromXToYColor, toXToYColor]);

        /// <summary>
        /// Gets the index of the closest element of <see cref="stops" /> less than or equal to
        /// <see cref="coordinate" />.
        /// </summary>
        /// <param name="coordinate">The value to search for in <see cref="stops" />.</param>
        /// <param name="stops">The array of stops to search within.</param>
        /// <param name="isOnStop"></param>
        /// <returns>
        ///     The index into <see cref="stops" /> to use for <see cref="coordinate" />, if <see cref="isOnStop" />,
        ///     else the index <c>i</c> of the first stop in a pair of stops
        ///     <c><see cref="stops" />[i]</c>, <c><see cref="stops" />[i + 1]</c> that should be
        ///     interpolated for <see cref="coordinate" />.
        /// </returns>
        private static int GetIndexInStopsArray(float coordinate, ImmutableArray<float> stops, out bool isOnStop)
        {
            if (stops.Length < 2 || coordinate < stops[0])
            {
                isOnStop = true;
                return 0;
            }

            for (var index = 0; index < stops.Length - 1; index++)
            {
                var first = stops[index];
                var next = stops[index + 1];
                // A difference less than half of 1/255th of the difference between first and next is negligible; it
                // will result in the same packed color as the stop regardless of what the stops are.
                var epsilon = 0.5f * (next - first) / 255.0f;
                if (coordinate < first + epsilon)
                {
                    isOnStop = true;
                    return index;
                }
                if (coordinate <= next - epsilon)
                {
                    isOnStop = false;
                    return index;
                }
            }

            isOnStop = true;
            return stops.Length - 1;
        }
        
        /// <summary>
        /// Calculates the index into <see cref="ColorStopsFloat4" /> or <see cref="ColorStopsU32" /> to use for the
        /// given indices into <see cref="XStops" /> and <see cref="YStops" />.
        /// </summary>
        /// <param name="x">The index into <see cref="XStops" /> to find a stop for.</param>
        /// <param name="y">The index into <see cref="YStops" /> to find a stop for.</param>
        /// <returns>The color stop index at the <see cref="x"/>, <see cref="y"/> position.</returns>
        private int GetColorStopIndexAt(int x, int y)
        {
            return Math.Max(XStops.Length, 1) * y + x;
        }

        /// <summary>
        /// The packed color in the gradient at <see cref="point"/>.
        /// </summary>
        /// <param name="point">
        /// The point on the gradient, between <c>(0.0f, 0.0f)</c> and <c>(1.0f, 1.0f)</c> inclusive.</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if the <see cref="Gradient2D"/> is malformed.</exception>
        public uint this[Vector2 point]
        {
            get
            {
                var xStopIndex = GetIndexInStopsArray(point.X, XStops, out var xOnStop);
                var yStopIndex = GetIndexInStopsArray(point.Y, YStops, out var yOnStop);

                if (xOnStop)
                {
                    if (yOnStop)
                    {
                        var idx = GetColorStopIndexAt(xStopIndex, yStopIndex);
                        return ColorStopsU32[idx];
                    }
                    else
                    {
                        var before = YStops[yStopIndex];
                        var after = YStops[yStopIndex + 1];
                        var unpacked = Vector4.Lerp(
                            ColorStopsFloat4[GetColorStopIndexAt(xStopIndex, yStopIndex)],
                            ColorStopsFloat4[GetColorStopIndexAt(xStopIndex, yStopIndex + 1)],
                            (point.Y - before) / (after - before));
                        return ImGui.ColorConvertFloat4ToU32(unpacked);
                    }
                }
                else
                {
                    if (yOnStop)
                    {
                        var before = XStops[xStopIndex];
                        var after = XStops[xStopIndex + 1];
                        var unpacked = Vector4.Lerp(
                            ColorStopsFloat4[GetColorStopIndexAt(xStopIndex, yStopIndex)],
                            ColorStopsFloat4[GetColorStopIndexAt(xStopIndex + 1, yStopIndex)],
                            (point.X - before) / (after - before));
                        return ImGui.ColorConvertFloat4ToU32(unpacked);
                    }
                    else
                    {
                        var beforeX = XStops[xStopIndex];
                        var afterX = XStops[xStopIndex + 1];
                        var xFraction = (point.X - beforeX) / (afterX - beforeX); 
                        var beforeY = YStops[yStopIndex];
                        var afterY = YStops[yStopIndex + 1];
                        var yFraction = (point.Y - beforeY) / (afterY - beforeY);
                        var unpacked = Vector4.Lerp(
                            Vector4.Lerp(
                                ColorStopsFloat4[GetColorStopIndexAt(xStopIndex, yStopIndex)],
                                ColorStopsFloat4[GetColorStopIndexAt(xStopIndex + 1, yStopIndex)],
                                xFraction),
                            Vector4.Lerp(
                                ColorStopsFloat4[GetColorStopIndexAt(xStopIndex, yStopIndex + 1)],
                                ColorStopsFloat4[GetColorStopIndexAt(xStopIndex + 1, yStopIndex + 1)],
                                xFraction),
                            yFraction);
                        return ImGui.ColorConvertFloat4ToU32(unpacked);
                    }
                }
            }
        }

        /// <summary>
        /// Verifies that the given alpha value is valid for multiplying by.
        /// </summary>
        /// <param name="alpha">The multiplier to multiply the alpha by.</param>
        /// <param name="name">Name of the parameter for the exception code.</param>
        /// <exception cref="SafetyCheckException">If the alpha fails validation.</exception>
        [Conditional("DEBUG")]
        private static void CheckValidAlpha(float alpha, [CallerArgumentExpression(nameof(alpha))] string name = "alpha")
        {
            if (!float.IsFinite(alpha))
            {
                throw new SafetyCheckException(
                    $"{name} not finite", $"{name} must be a finite number, but was {alpha}");
            }
            if (alpha is > 1.0f or < 0.0f)
            {
                throw new SafetyCheckException(
                    $"{name} out of range", $"{name} needs to be between 0.0f and 1.0f inclusive, but was {alpha}");
            }
        } 
        
        /// <summary>
        /// Creates a new gradient which is equal to <see cref="gradient" /> multiplied by the given
        /// <see cref="alpha" /> value.
        /// </summary>
        /// <param name="gradient">The gradient to multiply with the given alpha.</param>
        /// <param name="alpha">The alpha to multiply by.</param>
        /// <returns>
        /// A new gradient with the same stops as <see cref="gradient" />, all with their alpha values multiplied by the
        /// given <see cref="alpha" />.</returns>
        public static Gradient2D operator *(Gradient2D gradient, float alpha)
        {
            CheckValidAlpha(alpha);
            var floatBuilder = ImmutableArray.CreateBuilder<Vector4>(gradient.ColorStopsFloat4.Length);
            var uintBuilder = ImmutableArray.CreateBuilder<uint>(gradient.ColorStopsU32.Length);

            for (var index = 0; index < gradient.ColorStopsFloat4.Length; index++)
            {
                var applied = gradient.ColorStopsFloat4[index]
                    with { W = alpha * gradient.ColorStopsFloat4[index].W };
                var packed = ImGui.ColorConvertFloat4ToU32(applied);
                floatBuilder.Add(applied);
                uintBuilder.Add(packed);
            }
            
            return new Gradient2D(gradient.XStops, gradient.YStops,
                floatBuilder.ToImmutable(), uintBuilder.ToImmutable());
        }

        /// <summary>
        /// Creates a new gradient which is equal to <see cref="gradient" /> multiplied by the given
        /// <see cref="alpha" /> value.
        /// </summary>
        /// <param name="alpha">The alpha to multiply by.</param>
        /// <param name="gradient">The gradient to multiply with the given alpha.</param>
        /// <returns>
        /// A new gradient with the same stops as <see cref="gradient" />, all with their alpha values multiplied by the
        /// given <see cref="alpha" />.</returns>
        public static Gradient2D operator *(float alpha, Gradient2D gradient)
        {
            return gradient * alpha;
        }
        
        /// <summary>
        /// Returns whether the other gradient is another gradient which is equal, i.e., it describes the same colors at
        /// the same stops.
        /// </summary>
        /// <param name="other">The other gradient to compare against.</param>
        /// <returns><see langword="true"/> if <see cref="other" /> describes another gradient with the same properties,
        /// <see langword="false"/> else.
        /// </returns>
        public bool Equals(Gradient2D? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return (XStops.Length == 1 && other.XStops.Length == 1 || Enumerable.SequenceEqual(XStops, other.XStops))
                   && (YStops.Length == 1 && other.YStops.Length == 1 || Enumerable.SequenceEqual(YStops, other.YStops))
                   && (ColorStopsFloat4.Length == 1 && other.ColorStopsFloat4.Length == 1 ||
                       Enumerable.SequenceEqual(ColorStopsFloat4, other.ColorStopsFloat4))
                   && Enumerable.SequenceEqual(ColorStopsU32, other.ColorStopsU32);
        }
        
        /// <summary>
        /// Returns whether the other object is another gradient which is equal, i.e., it describes the same colors at
        /// the same stops.
        /// </summary>
        /// <param name="obj">The other gradient to compare against.</param>
        /// <returns><see langword="true"/> if <see cref="obj" /> describes another gradient with the same properties,
        /// <see langword="false"/> else.
        /// </returns>
        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Gradient2D)obj);
        }
        
        /// <summary>
        /// Hashes this instance using the equality rules to only hash relevant fields.
        /// </summary>
        /// <returns>A hash using the relevant fields of the gradients.</returns>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            if (XStops.Length > 1) { 
                foreach (var x in XStops)
                {
                    hash.Add(x);                
                }
            }
            
            if (YStops.Length > 1) {
                foreach (var y in YStops)
                {
                    hash.Add(y);
                }
            }

            if (ColorStopsFloat4.Length > 1)
            {
                foreach (var colorStop in ColorStopsFloat4)
                {
                    hash.Add(colorStop);
                }
            }

            foreach (var colorStop in ColorStopsU32)
            {
                hash.Add(colorStop);
            }

            return hash.ToHashCode();
        }
        
        /// <summary>
        /// Returns whether two gradients are equal, i.e., they describe the same colors at the same stops or both are
        /// null.
        /// </summary>
        /// <param name="left">The first gradient to compare.</param>
        /// <param name="right">The second gradient to compare.</param>
        /// <returns><see langword="true"/> if <see cref="left" /> and <see cref="right" /> describe gradients with the
        /// same properties, <see langword="false"/> else.
        /// </returns>
        public static bool operator ==(Gradient2D? left, Gradient2D? right)
        {
            return Equals(left, right);
        }
        
        /// <summary>
        /// Returns whether two gradients are unequal, i.e., they don't describe the same colors at the same stops or
        /// one is null.
        /// </summary>
        /// <param name="left">The first gradient to compare.</param>
        /// <param name="right">The second gradient to compare.</param>
        /// <returns><see langword="false"/> if <see cref="left" /> and <see cref="right" /> describe gradients with the
        /// same properties, <see langword="true"/> else.
        /// </returns>
        public static bool operator !=(Gradient2D? left, Gradient2D? right)
        {
            return !Equals(left, right);
        }
        
        /// <summary>
        /// Prints a human-readable representation of the instance with the given format used for floating-point numbers
        /// (gradient stops and color components). 
        /// </summary>
        /// <param name="format">The format specifier to use for floating-point numbers, default P2.</param>
        /// <param name="formatProvider">The format provider to use for formatting numbers, default
        /// <see cref="CultureInfo.CurrentCulture" />.</param>
        /// <returns>A human-readable string representation of this instance.</returns>
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            var resolvedFormat = format ?? "P2";
            var resolvedFormatProvider = formatProvider ?? CultureInfo.CurrentCulture;
            var builder = new StringBuilder();
            builder.Append(nameof(Gradient2D));
            builder.Append(" { ");
            // Exclude the stops arrays if they're exactly 1 element long, since that means that axis doesn't
            // participate in the gradient.
            if (XStops.Length > 1)
            {
                builder.Append(nameof(XStops));
                builder.Append(": [");
                builder.Append(XStops[0].ToString(resolvedFormat, resolvedFormatProvider));
                for (var idx = 1; idx < XStops.Length; idx++)
                {
                    builder.Append(", ");
                    builder.Append(XStops[idx].ToString(resolvedFormat, resolvedFormatProvider));
                }
                builder.Append("]; ");
            }
            if (YStops.Length > 1)
            {
                builder.Append(nameof(YStops));
                builder.Append(": [");
                builder.Append(YStops[0].ToString(resolvedFormat, resolvedFormatProvider));
                for (var idx = 1; idx < YStops.Length; idx++)
                {
                    builder.Append(", ");
                    builder.Append(YStops[idx].ToString(resolvedFormat, resolvedFormatProvider));
                }
                builder.Append("]; ");
            }

            // Exclude the unpacked colors if they're exactly 1 element long, since that means they'll never be checked
            // and only the packed color will be used.
            if (ColorStopsFloat4.Length > 1)
            {
                builder.Append(nameof(ColorStopsFloat4));
                builder.Append(": [");
                builder.Append(Vector4ToColorString(ColorStopsFloat4[0], resolvedFormat, resolvedFormatProvider));
                for (var idx = 1; idx < ColorStopsFloat4.Length; idx++)
                {
                    builder.Append(", ");
                    builder.Append(Vector4ToColorString(ColorStopsFloat4[idx], resolvedFormat, resolvedFormatProvider));
                }

                builder.Append("]; ");
            }

            if (ColorStopsU32.Length > 1)
            {
                builder.Append(nameof(ColorStopsU32));
                builder.Append(": [");
            }

            if (ColorStopsU32.Length > 0)
            {
                builder.Append(U32ToColorString(ColorStopsU32[0], resolvedFormatProvider));
            }

            if (ColorStopsU32.Length > 1)
            {
                for (var idx = 1; idx < ColorStopsU32.Length; idx++)
                {
                    builder.Append(", ");
                    builder.Append(U32ToColorString(ColorStopsU32[idx], resolvedFormatProvider));
                }

                builder.Append(']');
            }

            builder.Append(" }");
            return builder.ToString();
        }

        /// <summary>
        /// Helper for <see cref="ToString(string?, IFormatProvider?)" /> that prints the unpacked color as four floats
        /// with the given format.
        /// </summary>
        /// <param name="color">The vector containing the color information to format.</param>
        /// <param name="format">The format to use for each floating-point number.</param>
        /// <param name="formatProvider">The format provider to format the number with.</param>
        /// <returns>The formatted color string.</returns>
        private static string Vector4ToColorString(Vector4 color, string format, IFormatProvider formatProvider)
        {
            var builder = new StringBuilder();
            builder.Append("R ");
            builder.Append(color.X.ToString(format, formatProvider));
            builder.Append("/G ");
            builder.Append(color.Y.ToString(format, formatProvider));
            builder.Append("/B ");
            builder.Append(color.Z.ToString(format, formatProvider));
            builder.Append("/A ");
            builder.Append(color.W.ToString(format, formatProvider));
            return builder.ToString();
        }

        /// <summary>
        /// Helper for <see cref="ToString(string?, IFormatProvider?)" /> that prints the packed color as a single
        /// 8-digit hexadecimal number prefixed with 0x. 
        /// </summary>
        /// <param name="color">The vector containing the color information to format.</param>
        /// <param name="formatProvider">The format provider to format the number with.</param>
        /// <returns>The formatted color string.</returns>
        private static string U32ToColorString(uint color, IFormatProvider formatProvider)
        {
            return "0x" + color.ToString("X8", formatProvider);
        }

        /// <summary>
        /// Prints a human-readable representation of the instance with the format P2 used for floating-point numbers
        /// (gradient stops and color components) and <see cref="CultureInfo.CurrentCulture" /> used for formatting. 
        /// </summary>
        /// <returns>A human-readable string representation of this instance.</returns>
        public override string ToString()
        {
            return ToString(null, null);
        }
    }
}