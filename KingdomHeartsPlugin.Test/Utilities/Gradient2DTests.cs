using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using KingdomHeartsPlugin.Test.Testing.SafetyCheckVerification;
using KingdomHeartsPlugin.Utilities;

namespace KingdomHeartsPlugin.Test.Utilities;

public class Gradient2DTests
{
    [Test]
    [SafetyCheckTest]
    [MethodDataSource(nameof(ValidGradientParameters))]
    public async Task ValidConstructionCompletes(
        string name, ImmutableArray<float> xStops, ImmutableArray<float> yStops,
        ImmutableArray<Vector4> unpacked, ImmutableArray<uint> packed)
    {
        await Assert.That(() => new Gradient2D(xStops, yStops, unpacked, packed)).PassesSafetyChecks();
    }
    
    public static IEnumerable<(string name,
        ImmutableArray<float> xStops, ImmutableArray<float> yStops,
        ImmutableArray<Vector4> unpacked, ImmutableArray<uint> packed)> ValidGradientParameters =>
    [
        new("trivial form", [0.0f], [0.0f], [Vector4.Zero], [0x0]),
        new("horizontal", [0.0f, 1.0f], [0.0f], [Vector4.Zero, Vector4.One], [0x0, 0xFFFFFFFF]),
        new("vertical", [0.0f], [0.0f, 1.0f], [Vector4.Zero, Vector4.One], [0x0, 0xFFFFFFFF]),
        new("multistop horizontal", [0.0f, 0.5f, 1.0f], [0.0f],
            [Vector4.Zero, Vector4.One, Vector4.Zero], [0x0, 0xFFFFFFFF, 0x0]),
        new("multistop vertical", [0.0f], [0.0f, 0.5f, 1.0f],
            [Vector4.Zero, Vector4.One, Vector4.Zero], [0x0, 0xFFFFFFFF, 0x0]),
        new("square", [0.0f, 1.0f], [0.0f, 1.0f],
            [Vector4.UnitX, Vector4.UnitY, Vector4.UnitZ, Vector4.UnitW],
            [0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000]),
        new("small square", [0.3f, 0.7f], [0.3f, 0.7f],
            [Vector4.UnitX, Vector4.UnitY, Vector4.UnitZ, Vector4.UnitW],
            [0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000]),
        new("multistop square",
            [0.0f, 0.5f, 1.0f], [0.0f, 0.5f, 1.0f],
            [Vector4.Zero, Vector4.One, Vector4.Zero,
                Vector4.One, Vector4.Zero, Vector4.One,
                Vector4.Zero, Vector4.One, Vector4.Zero],
            [0x0, 0xFFFFFFFF, 0x0,
                0xFFFFFFFF, 0x0, 0xFFFFFFFF,
                0x0, 0xFFFFFFFF, 0x0]),
        
        new("X-stop above 1.0f", [2.0f], [0.0f], [Vector4.Zero], [0x0]),
        new("X-stop below 0.0f", [-1.0f], [0.0f], [Vector4.Zero], [0x0]),
        new("Y-stop above 1.0f", [0.0f], [2.0f], [Vector4.Zero], [0x0]),
        new("Y-stop below 0.0f", [0.0f], [-1.0f], [Vector4.Zero], [0x0]),
    ];
    
    [Test]
    [SafetyCheckTest]
    [MethodDataSource(nameof(InvalidGradientParameters))]
    [DisplayName("InvalidConstructionThrows: $name fails with code \"$failureReason\"")]
    public async Task InvalidConstructionThrows(
        string name, string failureReason,
        ImmutableArray<float> xStops, ImmutableArray<float> yStops,
        ImmutableArray<Vector4> unpacked, ImmutableArray<uint> packed)
    {
        await Assert.That(() => new Gradient2D(xStops, yStops, unpacked, packed))
            .FailsSafetyCheckWithCode(failureReason);
    }
    
    public static IEnumerable<(string name, string failureReason,
        ImmutableArray<float> xStops, ImmutableArray<float> yStops,
        ImmutableArray<Vector4> unpacked, ImmutableArray<uint> packed)> InvalidGradientParameters =>
    [
        new("all empty", "empty XStops", [], [], [], []),
        new("X empty", "empty XStops", [], [0.0f], [], []),
        new("Y empty", "empty YStops", [0.0f], [], [], []),
        new("no colors in single stop", "bad ColorStopsFloat4/ColorStopsU32.Length",
            [0.0f], [0.0f], [], []),
        new("multiple colors in single stop", "bad ColorStopsFloat4/ColorStopsU32.Length",
            [0.0f], [0.0f], [Vector4.Zero, Vector4.One], [0x0, 0xFFFFFFFF]),
        new("wrong length for unpacked colors", "ColorStopsFloat4/ColorStopsU32.Length mismatch",
            [0.0f], [0.0f], [Vector4.Zero, Vector4.One], [0x0]),
        new("wrong length for packed colors", "ColorStopsFloat4/ColorStopsU32.Length mismatch",
            [0.0f], [0.0f], [Vector4.Zero], [0x0, 0xFFFFFFFF]),
        new("x-stops out of order", "XStops[1] out of order",
            [1.0f, 0.0f], [0.0f], [Vector4.Zero, Vector4.One], [0x0, 0xFFFFFFFF]),
        new("y-stops out of order", "YStops[1] out of order",
            [0.0f], [1.0f, 0.0f], [Vector4.Zero, Vector4.One], [0x0, 0xFFFFFFFF]),
        new("positive infinity X-stop", "nonfinite XStops[0]",
            [float.PositiveInfinity], [0.0f], [Vector4.Zero], [0x0]),
        new("negative infinity X-stop", "nonfinite XStops[0]",
            [float.NegativeInfinity], [0.0f], [Vector4.Zero], [0x0]),
        new("NaN X-stop", "nonfinite XStops[0]",
            [float.NaN], [0.0f], [Vector4.Zero], [0x0]),
        new("positive infinity Y-stop", "nonfinite YStops[0]",
            [0.0f], [float.PositiveInfinity], [Vector4.Zero], [0x0]),
        new("negative infinity Y-stop", "nonfinite YStops[0]",
            [0.0f], [float.NegativeInfinity], [Vector4.Zero], [0x0]),
        new("NaN Y-stop", "nonfinite YStops[0]",
            [0.0f], [float.NaN], [Vector4.Zero], [0x0]),
        new("not enough colors in square", "bad ColorStopsFloat4/ColorStopsU32.Length",
            [0.0f, 0.5f, 1.0f], [0.0f, 0.5f, 1.0f],
            [Vector4.Zero, Vector4.UnitW, Vector4.One, Vector4.One, Vector4.UnitW, Vector4.Zero],
            [0x0, 0xFF000000, 0xFFFFFFFF, 0xFFFFFFFF, 0xFF000000, 0x00000000]),
        new("too many colors in square", "bad ColorStopsFloat4/ColorStopsU32.Length",
            [0.0f, 0.5f, 1.0f], [0.0f, 0.5f, 1.0f],
            [Vector4.Zero, Vector4.UnitW, Vector4.One,
                Vector4.One, Vector4.UnitW, Vector4.Zero,
                Vector4.UnitW, Vector4.Zero, Vector4.One,
                Vector4.UnitW, Vector4.One, Vector4.Zero],
            [0x0, 0xFF000000, 0xFFFFFFFF,
                0xFFFFFFFF, 0xFF000000, 0x00000000,
                0xFF000000, 0x00000000, 0xFFFFFFFF,
                0xFF000000, 0xFFFFFFFF, 0x00000000]),
        new("non-matching colors", "ColorStopsFloat4/ColorStopsU32[0] mismatch",
            [0.0f], [0.0f], [Vector4.Zero], [0xFFFFFFFF]),
        new("positive infinity in color", "nonfinite ColorStopsFloat4[0]",
            [0.0f], [0.0f], [Vector4.One with { W = float.PositiveInfinity}], [0xFFFFFFFF]),
        new("negative infinity in color", "nonfinite ColorStopsFloat4[0]",
            [0.0f], [0.0f], [Vector4.Zero with { Y = float.NegativeInfinity}], [0x00000000]),
        new("NaN in color", "nonfinite ColorStopsFloat4[0]",
            [0.0f], [0.0f], [Vector4.Zero with { Z = float.NaN}], [0x00000000]),
        new("color value too high", "ColorStopsFloat4[0] out of range",
            [0.0f], [0.0f], [Vector4.One with { X = 2.0f }], [0xFFFFFFFF]),
        new("color value too low", "ColorStopsFloat4[0] out of range",
            [0.0f], [0.0f], [Vector4.One with { X = -1.0f }], [0xFFFFFFFF]),
    ];
    
    [Test]
    [ReleaseBehaviorTest]
    public async Task EmptyGradient2DCrashesOnIndexer()
    {
        var empty =
            await Assert.That(() => new Gradient2D([], [], [], []))
                .ThrowsNothing().And.IsNotNull();
        await Assert.That(() => empty[Vector2.Zero])
            .Throws<IndexOutOfRangeException>();
    }

    [Test]
    public async Task NullInequality()
    {
        var gradient = Gradient2D.Transparent;
        await Assert.That(gradient.Equals(null)).IsFalse();
        await Assert.That(gradient!.Equals((object?) null)).IsFalse();
    }
    
    [Test]
    public async Task ReflexiveEquality()
    {
        var gradient = Gradient2D.Transparent;
        await Assert.That(gradient.Equals(gradient)).IsTrue();
        await Assert.That(gradient.Equals((object?) gradient)).IsTrue();
    }

    [Test]
    public async Task OffTypeInequality()
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        await Assert.That(Gradient2D.Transparent.Equals("not a gradient")).IsFalse();
    }
    
    [Test]
    [MethodDataSource(nameof(EqualityTestCases))]
    public async Task Equality(string name, Gradient2D gradientA, Gradient2D gradientB)
    {
        await Assert.That(gradientA).IsEqualTo(gradientB);
        await Assert.That(gradientB).IsEqualTo(gradientA);
    }

    [Test]
    [MethodDataSource(nameof(EqualityTestCases))]
    public async Task EqualityOperators(string name, Gradient2D gradientA, Gradient2D gradientB)
    {
        await Assert.That(gradientA == gradientB).IsTrue();
        await Assert.That(gradientB == gradientA).IsTrue();
        await Assert.That(gradientA != gradientB).IsFalse();
        await Assert.That(gradientB != gradientA).IsFalse();
    }

    [Test]
    [MethodDataSource(nameof(EqualityTestCases))]
    public async Task HashCodeEquality(string name, Gradient2D gradientA, Gradient2D gradientB)
    {
        await Assert.That(gradientA.GetHashCode()).IsEqualTo(gradientB.GetHashCode());
    }

    [Test]
    [MethodDataSource(nameof(EqualityTestCases))]
    public async Task ToStringEquality(string name, Gradient2D gradientA, Gradient2D gradientB)
    {
        await Assert.That(gradientA.ToString()).IsEqualTo(gradientB.ToString());
    }

    public static IEnumerable<(string name, Gradient2D gradientA, Gradient2D gradientB)> EqualityTestCases =>
    [
        new("self-equality for transparent", Gradient2D.Transparent, Gradient2D.Transparent),
        new("self-equality for packed single color",
            Gradient2D.SingleColor(0x0), Gradient2D.SingleColor(0x0)),
        new("self-equality for unpacked single color",
            Gradient2D.SingleColor(Vector4.Zero), Gradient2D.SingleColor(Vector4.Zero)),
        new("equality for packed and unpacked single color",
            Gradient2D.SingleColor(0x0), Gradient2D.SingleColor(Vector4.Zero)),
        new("equality for packed and handmade single color",
            Gradient2D.SingleColor(0x0),
            new Gradient2D([0.0f], [0.0f], [Vector4.Zero], [0x0])),
        new("self-equality for handmade single color",
            new Gradient2D([0.0f], [0.0f], [Vector4.Zero], [0x0]),
            new Gradient2D([0.0f], [0.0f], [Vector4.Zero], [0x0])),
        new("equality for single color with different single stops",
            new Gradient2D([1.0f], [1.0f], [Vector4.Zero], [0x0]),
            new Gradient2D([0.0f], [0.0f], [Vector4.Zero], [0x0])),
        new("equality for single color with different unpacked color that rounds to same packed color",
            new Gradient2D([0.0f], [0.0f], [Vector4.Zero], [0x0]),
            new Gradient2D([0.0f], [0.0f], [new Vector4(1.0f/512.0f)], [0x0])),
        
        new("self-equality for packed horizontal",
            Gradient2D.TwoColorHorizontal(0x0, 0xFFFFFFFF),
            Gradient2D.TwoColorHorizontal(0x0, 0xFFFFFFFF)),
        new("self-equality for unpacked horizontal",
            Gradient2D.TwoColorHorizontal(Vector4.Zero, Vector4.One),
            Gradient2D.TwoColorHorizontal(Vector4.Zero, Vector4.One)),
        new("equality for packed and unpacked horizontal",
            Gradient2D.TwoColorHorizontal(0x0, 0xFFFFFFFF),
            Gradient2D.TwoColorHorizontal(Vector4.Zero, Vector4.One)),
        
        new("self-equality for packed vertical",
            Gradient2D.TwoColorVertical(0x0, 0xFFFFFFFF),
            Gradient2D.TwoColorVertical(0x0, 0xFFFFFFFF)),
        new("self-equality for unpacked vertical",
            Gradient2D.TwoColorVertical(Vector4.Zero, Vector4.One),
            Gradient2D.TwoColorVertical(Vector4.Zero, Vector4.One)),
        new("equality for packed and unpacked vertical",
            Gradient2D.TwoColorVertical(0x0, 0xFFFFFFFF),
            Gradient2D.TwoColorVertical(Vector4.Zero, Vector4.One)),
    ];
    
    [Test]
    [MethodDataSource(nameof(InequalityTestCases))]
    public async Task Inequality(string name, Gradient2D gradientA, Gradient2D gradientB)
    {
        await Assert.That(gradientA).IsNotEqualTo(gradientB);
        await Assert.That(gradientB).IsNotEqualTo(gradientA);
    }
    
    [Test]
    [MethodDataSource(nameof(InequalityTestCases))]
    public async Task InequalityOperators(string name, Gradient2D gradientA, Gradient2D gradientB)
    {
        await Assert.That(gradientA == gradientB).IsFalse();
        await Assert.That(gradientB == gradientA).IsFalse();
        await Assert.That(gradientA != gradientB).IsTrue();
        await Assert.That(gradientB != gradientA).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(InequalityTestCases))]
    public async Task ToStringInequality(string name, Gradient2D gradientA, Gradient2D gradientB)
    {
        await Assert.That(gradientA.ToString()).IsNotEqualTo(gradientB.ToString());
    }
    
    public static IEnumerable<(string name, Gradient2D gradientA, Gradient2D gradientB)> InequalityTestCases =>
    [
        new("different single colors",
            Gradient2D.SingleColor(Vector4.Zero), Gradient2D.SingleColor(Vector4.One)),
        
        new("horizontal vs single color",
            Gradient2D.SingleColor(Vector4.Zero),
            Gradient2D.TwoColorHorizontal(Vector4.Zero, Vector4.One)),
        new("different horizontal colors",
            Gradient2D.TwoColorHorizontal(Vector4.Zero, Vector4.One),
            Gradient2D.TwoColorHorizontal(Vector4.One, Vector4.Zero)),
        
        new("vertical vs single color",
            Gradient2D.SingleColor(Vector4.Zero),
            Gradient2D.TwoColorVertical(Vector4.Zero, Vector4.One)),
        new("vertical vs horizontal",
            Gradient2D.TwoColorHorizontal(Vector4.Zero, Vector4.One),
            Gradient2D.TwoColorVertical(Vector4.Zero, Vector4.One)),
        new("different vertical colors",
            Gradient2D.TwoColorVertical(Vector4.Zero, Vector4.One),
            Gradient2D.TwoColorVertical(Vector4.One, Vector4.Zero)),
        
        new("four color vs single color",
            Gradient2D.FourColor(Vector4.Zero, Vector4.One, Vector4.UnitW, Vector4.UnitX),
            Gradient2D.SingleColor(Vector4.Zero)),
        new("four color vs horizontal",
            Gradient2D.FourColor(Vector4.Zero, Vector4.One, Vector4.UnitW, Vector4.UnitX),
            Gradient2D.TwoColorHorizontal(Vector4.Zero, Vector4.One)),
        new("four color vs vertical",
            Gradient2D.FourColor(Vector4.Zero, Vector4.One, Vector4.UnitW, Vector4.UnitX),
            Gradient2D.TwoColorVertical(Vector4.Zero, Vector4.One)),
        new("different four colors",
            Gradient2D.FourColor(Vector4.Zero, Vector4.One, Vector4.UnitW, Vector4.UnitX),
            Gradient2D.FourColor(Vector4.One, Vector4.Zero, Vector4.UnitW, Vector4.UnitX)),
        
        new("different Y stops",
            new Gradient2D([0.0f], [0.0f, 1.0f], [Vector4.Zero, Vector4.One], [0x0, 0xFFFFFFFF]),
            new Gradient2D([0.0f], [0.2f, 0.8f], [Vector4.Zero, Vector4.One], [0x0, 0xFFFFFFFF])),
        new("different X stops",
            new Gradient2D([0.0f, 1.0f], [0.0f], [Vector4.Zero, Vector4.One], [0x0, 0xFFFFFFFF]),
            new Gradient2D([0.2f, 0.8f], [0.0f], [Vector4.Zero, Vector4.One], [0x0, 0xFFFFFFFF])),
        
        new("different unpacked colors that round to the same packed colors",
            new Gradient2D([0.0f, 1.0f], [0.0f], [Vector4.Zero, Vector4.One], [0x0, 0xFFFFFFFF]),
            new Gradient2D([0.0f, 1.0f], [0.0f], [new Vector4(1.0f/512.0f), new Vector4(511.0f/512.0f)], [0x0, 0xFFFFFFFF])),
    ];
    
    [Test]
    [MethodDataSource(nameof(ToStringTestCases))]
    public async Task ToString(string name, Gradient2D gradient, string? format, string expected)
    {
        await Assert.That(gradient.ToString(format, CultureInfo.InvariantCulture)).IsEqualTo(expected);
    }

    public static IEnumerable<(string name, Gradient2D gradient, string? format, string expected)> ToStringTestCases =>
    [
        new("single color", Gradient2D.SingleColor(0), null, "Gradient2D { 0x00000000 }"),
        new("horizontal", Gradient2D.TwoColorHorizontal(Vector4.Zero, Vector4.One), null,
            "Gradient2D { XStops: [0.00 %, 100.00 %];" +
            " ColorStopsFloat4: [R 0.00 %/G 0.00 %/B 0.00 %/A 0.00 %," +
            " R 100.00 %/G 100.00 %/B 100.00 %/A 100.00 %];" +
            " ColorStopsU32: [0x00000000, 0xFFFFFFFF] }"),
        new("horizontal fixed", Gradient2D.TwoColorHorizontal(Vector4.Zero, Vector4.One), "F0",
            "Gradient2D { XStops: [0, 1];" +
            " ColorStopsFloat4: [R 0/G 0/B 0/A 0," +
            " R 1/G 1/B 1/A 1];" +
            " ColorStopsU32: [0x00000000, 0xFFFFFFFF] }"),
        new("vertical", Gradient2D.TwoColorVertical(Vector4.Zero, Vector4.One), null,
            "Gradient2D { YStops: [0.00 %, 100.00 %];" +
            " ColorStopsFloat4: [R 0.00 %/G 0.00 %/B 0.00 %/A 0.00 %," +
            " R 100.00 %/G 100.00 %/B 100.00 %/A 100.00 %];" +
            " ColorStopsU32: [0x00000000, 0xFFFFFFFF] }"),        
        new("vertical fixed", Gradient2D.TwoColorVertical(Vector4.Zero, Vector4.One), "F0",
            "Gradient2D { YStops: [0, 1];" +
            " ColorStopsFloat4: [R 0/G 0/B 0/A 0," +
            " R 1/G 1/B 1/A 1];" +
            " ColorStopsU32: [0x00000000, 0xFFFFFFFF] }"),
        new("four color", Gradient2D.FourColor(Vector4.Zero, Vector4.One, Vector4.UnitW, Vector4.UnitX), null,
            "Gradient2D { XStops: [0.00 %, 100.00 %]; YStops: [0.00 %, 100.00 %];" +
            " ColorStopsFloat4: [R 0.00 %/G 0.00 %/B 0.00 %/A 0.00 %," +
            " R 100.00 %/G 100.00 %/B 100.00 %/A 100.00 %," +
            " R 0.00 %/G 0.00 %/B 0.00 %/A 100.00 %," +
            " R 100.00 %/G 0.00 %/B 0.00 %/A 0.00 %];" +
            " ColorStopsU32: [0x00000000, 0xFFFFFFFF, 0xFF000000, 0x000000FF] }"),
    ];
    
    [Test]
    [MethodDataSource(nameof(SingleColorTestCases))]
    public async Task SingleColorPacked(string name, Vector2 point)
    {
        await Assert.That(() => Gradient2D.SingleColor(0xFFBF8040)[point])
            .IsEqualTo(0xFFBF8040);
    }

    [Test]
    [MethodDataSource(nameof(SingleColorTestCases))]
    public async Task SingleColorUnpacked(string name, Vector2 point)
    {
        await Assert.That(() => Gradient2D.SingleColor(new Vector4(0.25f, 0.5f, 0.75f, 1.0f))[point])
            .IsEqualTo(0xFFBF8040);
    }

    [Test]
    [MethodDataSource(nameof(SingleColorTestCases))]
    public async Task SingleColorAdjustedStops(string name, Vector2 point)
    {
        await Assert.That(() => new Gradient2D(
                [0.5f], [0.5f],
                [new Vector4(0.25f, 0.5f, 0.75f, 1.0f)],
                [0xFFBF8040])[point])
            .IsEqualTo(0xFFBF8040);
    }
    
    public static IEnumerable<(string name, Vector2 point)> SingleColorTestCases =>
    [
        new("top left", Vector2.Zero),
        new("top right", Vector2.UnitX),
        new("bottom right", Vector2.One),
        new("bottom left", Vector2.UnitY),
        new("center", new Vector2(0.5f)),
        new("top center", new Vector2(0.5f, 0.0f)),
        new("center left", new Vector2(0.0f, 0.5f)),
        new("bottom center", new Vector2(0.5f, 1.0f)),
        new("center right", new Vector2(1.0f, 0.5f)),
        new("infinity", Vector2.PositiveInfinity),
        new("negative infinity", Vector2.NegativeInfinity),
        new("NaN", Vector2.NaN),
        new("epsilon", Vector2.Epsilon),
    ];
    
    [Test]
    [MethodDataSource(nameof(VerticalTestCases))]
    public async Task VerticalGradientPacked(string name, Vector2 point, uint expected)
    {
        await Assert.That(() => Gradient2D.TwoColorVertical(0x00000000, 0xFFFFFFFF)[point]).IsEqualTo(expected);
    }
    
    [Test]
    [MethodDataSource(nameof(VerticalTestCases))]
    public async Task VerticalGradientUnpacked(string name, Vector2 point, uint expected)
    {
        await Assert.That(() => Gradient2D.TwoColorVertical(Vector4.Zero, Vector4.One)[point]).IsEqualTo(expected);
    }
    
    public static IEnumerable<(string name, Vector2 point, uint output)> VerticalTestCases =>
    [
        new("top left", Vector2.Zero, 0x00000000),
        new("top right", Vector2.UnitX, 0x00000000),
        new("bottom right", Vector2.One, 0xFFFFFFFF),
        new("bottom left", Vector2.UnitY, 0xFFFFFFFF),
        new("center", new Vector2(0.5f), 0x80808080),
        new("top center", new Vector2(0.5f, 0.0f), 0x00000000),
        new("center left", new Vector2(0.0f, 0.5f), 0x80808080),
        new("bottom center", new Vector2(0.5f, 1.0f), 0xFFFFFFFF),
        new("center right", new Vector2(1.0f, 0.5f), 0x80808080),
        new("infinity", Vector2.PositiveInfinity, 0xFFFFFFFF),
        new("negative infinity", Vector2.NegativeInfinity, 0x00000000),
        new("NaN", Vector2.NaN, 0xFFFFFFFF),
        new("epsilon", Vector2.Epsilon, 0x00000000),
    ];
    
    [Test]
    [MethodDataSource(nameof(HorizontalTestCases))]
    public async Task HorizontalGradientPacked(string name, Vector2 point, uint expected)
    {
        await Assert.That(() => Gradient2D.TwoColorHorizontal(0x00000000, 0xFFFFFFFF)[point]).IsEqualTo(expected);
    }
    
    [Test]
    [MethodDataSource(nameof(HorizontalTestCases))]
    public async Task HorizontalGradientUnpacked(string name, Vector2 point, uint expected)
    {
        await Assert.That(() => Gradient2D.TwoColorHorizontal(Vector4.Zero, Vector4.One)[point]).IsEqualTo(expected);
    }
    
    public static IEnumerable<(string name, Vector2 point, uint output)> HorizontalTestCases =>
    [
        new("top left", Vector2.Zero, 0x00000000),
        new("top right", Vector2.UnitX, 0xFFFFFFFF),
        new("bottom right", Vector2.One, 0xFFFFFFFF),
        new("bottom left", Vector2.UnitY, 0x00000000),
        new("center", new Vector2(0.5f), 0x80808080),
        new("top center", new Vector2(0.5f, 0.0f), 0x80808080),
        new("center left", new Vector2(0.0f, 0.5f), 0x00000000),
        new("bottom center", new Vector2(0.5f, 1.0f), 0x80808080),
        new("center right", new Vector2(1.0f, 0.5f), 0xFFFFFFFF),
        new("infinity", Vector2.PositiveInfinity, 0xFFFFFFFF),
        new("negative infinity", Vector2.NegativeInfinity, 0x00000000),
        new("NaN", Vector2.NaN, 0xFFFFFFFF),
        new("epsilon", Vector2.Epsilon, 0x00000000),
    ];
    
    [Test]
    [MethodDataSource(nameof(FourColorTestCases))]
    public async Task FourColorGradientPacked(string name, Vector2 point, uint expected)
    {
        await Assert.That(() => Gradient2D.FourColor(
            fromXFromYColor: 0xFF000000,
            toXFromYColor: 0x000000FF,
            fromXToYColor: 0x0000FF00,
            toXToYColor: 0x00FF0000)[point]).IsEqualTo(expected);
    }
    
    [Test]
    [MethodDataSource(nameof(FourColorTestCases))]
    public async Task FourColorGradientUnpacked(string name, Vector2 point, uint expected)
    {
        await Assert.That(() => Gradient2D.FourColor(
            fromXFromYColor: Vector4.UnitW,
            toXFromYColor: Vector4.UnitX,
            fromXToYColor: Vector4.UnitY,
            toXToYColor: Vector4.UnitZ)[point]).IsEqualTo(expected);
    }
    
    public static IEnumerable<(string name, Vector2 point, uint output)> FourColorTestCases =>
    [
        new("top left", Vector2.Zero, 0xFF000000),
        new("top right", Vector2.UnitX, 0x000000FF),
        new("bottom right", Vector2.One, 0x00FF0000),
        new("bottom left", Vector2.UnitY, 0x0000FF00),
        new("center", new Vector2(0.5f), 0x40404040),
        new("top center", new Vector2(0.5f, 0.0f), 0x80000080),
        new("center left", new Vector2(0.0f, 0.5f), 0x80008000),
        new("bottom center", new Vector2(0.5f, 1.0f), 0x00808000),
        new("center right", new Vector2(1.0f, 0.5f), 0x00800080),
        new("infinity", Vector2.PositiveInfinity, 0x00FF0000),
        new("negative infinity", Vector2.NegativeInfinity, 0xFF000000),
        new("NaN", Vector2.NaN, 0x00FF0000),
        new("epsilon", Vector2.Epsilon, 0xFF000000),
    ];
    
    [Test]
    [MethodDataSource(nameof(NineColorTestCases))]
    public async Task NineColorGradient(string name, Vector2 point, uint expected)
    {
        await Assert.That(() => new Gradient2D(
            [0.25f, 0.5f, 0.75f], [0.1f, 0.4f, 0.6f],
            [Vector4.UnitW, Vector4.One, Vector4.UnitZ,
                Vector4.UnitY, Vector4.UnitX, Vector4.Zero,
                new Vector4(0.5f), new Vector4(0.5f, 0.5f, 0.5f, 1.0f), new Vector4(1.0f, 1.0f, 1.0f, 0.5f)],
                [0xFF000000, 0xFFFFFFFF, 0x00FF0000,
                0x0000FF00, 0x000000FF, 0x00000000,
                0x80808080, 0xFF808080, 0x80FFFFFF]
            )[point]).IsEqualTo(expected);
    }
    public static IEnumerable<(string name, Vector2 point, uint output)> NineColorTestCases =>
    [
        new("top left", Vector2.Zero, 0xFF000000),
        new("top left stop", new Vector2(0.25f, 0.1f), 0xFF000000),
        new("top at left stop", new Vector2(0.25f, 0.0f), 0xFF000000),
        new("left at top stop", new Vector2(0.0f, 0.1f), 0xFF000000),
        new("negative infinity", Vector2.NegativeInfinity, 0xFF000000),
        new("epsilon", Vector2.Epsilon, 0xFF000000),

        new("top left-center", new Vector2(0.375f, 0.1f), 0xFF808080),
        new("top at left-center", new Vector2(0.375f, 0.0f), 0xFF808080),
        
        new("top center stop", new Vector2(0.5f, 0.1f), 0xFFFFFFFF),
        new("top at center stop", new Vector2(0.5f, 0.0f), 0xFFFFFFFF),
        
        new("top right-center stop", new Vector2(0.625f, 0.1f), 0x80FF8080),
        new("top at right-center", new Vector2(0.625f, 0.0f), 0x80FF8080),
        
        new("top right", Vector2.UnitX, 0x00FF0000),
        new("top right stop", new Vector2(0.75f, 0.1f), 0x00FF0000),
        new("top at right stop", new Vector2(0.75f, 0.0f), 0x00FF0000),
        new("right at top stop", new Vector2(1.0f, 0.1f), 0x00FF0000),
        
        new("top-center left", new Vector2(0.25f, 0.25f), 0x80008000),
        new("left at top-center", new Vector2(0.0f, 0.25f), 0x80008000),
        
        new("top-center left-center", new Vector2(0.375f, 0.25f), 0x80408080),
        
        new("top-center center", new Vector2(0.5f, 0.25f), 0x808080FF),
        
        new("top-center right-center", new Vector2(0.625f, 0.25f), 0x40804080),
        
        new("top-center right", new Vector2(0.75f, 0.25f), 0x00800000),
        new("right at top-center", new Vector2(1.0f, 0.25f), 0x00800000),
        
        new("center left stop", new Vector2(0.25f, 0.4f), 0x0000FF00),
        new("left at center stop", new Vector2(0.0f, 0.4f), 0x0000FF00),
        
        new("center left-center", new Vector2(0.375f, 0.4f), 0x00008080),
        
        new("center stop", new Vector2(0.5f, 0.4f), 0x000000FF),
        
        new("center right-center", new Vector2(0.625f, 0.4f), 0x00000080),
        
        new("center right", new Vector2(0.75f, 0.4f), 0x00000000),
        new("right at center stop", new Vector2(1.0f, 0.4f), 0x00000000),
        
        new("bottom-center left", new Vector2(0.25f, 0.5f), 0x4040BF40),
        new("left at bottom-center", new Vector2(0.0f, 0.5f), 0x4040BF40),
        
        new("bottom-center left-center", new Vector2(0.375f, 0.5f), 0x60408080),
        
        new("bottom-center center", new Vector2(0.5f, 0.5f), 0x7F4040BF),
        
        new("bottom-center right-center", new Vector2(0.625f, 0.5f), 0x6060609F),
        
        new("bottom-center right", new Vector2(0.75f, 0.5f), 0x407F7F7F),
        new("right at bottom-center", new Vector2(1.0f, 0.5f), 0x407F7F7F),
        
        new("bottom left", Vector2.UnitY, 0x80808080),
        new("bottom left stop", new Vector2(0.25f, 0.6f), 0x80808080),
        new("bottom at left stop", new Vector2(0.25f, 1.0f), 0x80808080),
        new("left at bottom stop", new Vector2(0.0f, 0.6f), 0x80808080),
        
        new("bottom left-center", new Vector2(0.375f, 0.6f), 0xBF808080),
        new("bottom at left-center", new Vector2(0.375f, 1.0f), 0xBF808080),
        
        new("bottom center stop", new Vector2(0.5f, 0.6f), 0xFF808080),
        new("bottom at center stop", new Vector2(0.5f, 1.0f), 0xFF808080),
        
        new("bottom right-center", new Vector2(0.625f, 0.6f), 0xBFBFBFBF),
        new("bottom at right-center", new Vector2(0.625f, 0.6f), 0xBFBFBFBF),
        
        new("bottom right", Vector2.One, 0x80FFFFFF),
        new("bottom right stop", new Vector2(0.75f, 0.6f), 0x80FFFFFF),
        new("bottom at right stop", new Vector2(0.75f, 1.0f), 0x80FFFFFF),
        new("right at bottom stop", new Vector2(1.0f, 0.6f), 0x80FFFFFF),
        new("infinity", Vector2.PositiveInfinity, 0x80FFFFFF),
        new("NaN", Vector2.NaN, 0x80FFFFFF),
    ];

    [Test]
    [MethodDataSource(nameof(AlphaMultiplierTests))]
    public async Task AlphaMultiplier(string name, Gradient2D source, float multiplier, Gradient2D expected)
    {
        await Assert.That(source * multiplier).IsEqualTo(expected);
        await Assert.That(multiplier * source).IsEqualTo(expected);
    }

    public static IEnumerable<(string name, Gradient2D source, float multiplier, Gradient2D expected)>
        AlphaMultiplierTests =>
    [
        new("single color", Gradient2D.SingleColor(Vector4.One), 0.5f, Gradient2D.SingleColor(new Vector4(Vector3.One, 0.5f))),
        new("identity", Gradient2D.SingleColor(Vector4.One), 1.0f, Gradient2D.SingleColor(Vector4.One)),
        new("zero", Gradient2D.SingleColor(Vector4.One), 0.0f, Gradient2D.SingleColor(new Vector4(Vector3.One, 0.0f))),
        new("horizontal", Gradient2D.TwoColorHorizontal(Vector4.Zero, Vector4.One), 0.5f,
            Gradient2D.TwoColorHorizontal(Vector4.Zero, new Vector4(Vector3.One, 0.5f))),
        new("vertical", Gradient2D.TwoColorVertical(new Vector4(0.5f), Vector4.One), 0.5f,
            Gradient2D.TwoColorVertical(new Vector4(new Vector3(0.5f), 0.25f), new Vector4(Vector3.One, 0.5f))),
    ];

    [Test]
    [SafetyCheckTest]
    [MethodDataSource(nameof(AlphaMultiplierSafetyCheckTests))]
    public async Task AlphaMultiplierSafetyCheck(string name, Gradient2D source, float multiplier, string expectedCode)
    {
        await Assert.That(() => source * multiplier).FailsSafetyCheckWithCode(expectedCode);
    }

    public static IEnumerable<(string name, Gradient2D source, float multiplier, string expectedCode)>
        AlphaMultiplierSafetyCheckTests =>
    [
        new("large multiplier", Gradient2D.SingleColor(Vector4.One), 2.0f, "alpha out of range"),
        new("negative multiplier", Gradient2D.SingleColor(Vector4.One), -1.0f, "alpha out of range"),
        new("NaN", Gradient2D.SingleColor(Vector4.One), float.NaN, "alpha not finite"),
        new("infinite multiplier", Gradient2D.SingleColor(Vector4.One), float.PositiveInfinity, "alpha not finite"),
        new("negative infinite multiplier", Gradient2D.SingleColor(Vector4.One), float.NegativeInfinity, "alpha not finite"),
    ];
}