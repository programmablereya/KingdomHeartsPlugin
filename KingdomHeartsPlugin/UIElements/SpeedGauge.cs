using System;
using System.Collections.Immutable;
using System.Numerics;

namespace KingdomHeartsPlugin.UIElements;


public class SpeedGauge : Gauge<SpeedGauge>
{
    private SpeedGauge(ImmutableArray<FractionVertexPair> vertexPairs, ImmutableArray<double> checkpoints, Vector2 boundingBoxMin, Vector2 boundingBoxMax) : base(vertexPairs, checkpoints, boundingBoxMin, boundingBoxMax)
    {
    }
        
    // A SpeedGauge is a gauge consisting of three segments:
    // * a horizontal segment, the base, in which the gauge acts like a normal bar gauge
    // * a diagonal segment, the growth, in which the gauge starts to grow larger and skew forward
    // * another horizontal segment, the landing, in which the gauge acts like a normal skewed bar gauge
    // This gives it the impression of building up speed and power, so it's a SpeedGauge.
    // Constructs a SpeedGauge with:
    // a rectangular bar baseWidth x baseHeight,
    // a quadrilateral bar that starts at baseHeight and ends at baseHeight + growthHeightDelta,
    //     with its inner side being growthWidth and the outer side's width being growthWidth + growthWidthDelta
    // a parallelogram (skewed) bar with the length of its inner and outer sides landingWidth and
    //     its height baseHeight + growthHeightDelta and its outer side offset horizontally from its inner side
    //     by growthWidthDelta
    // Its origin is the inner corner at which baseWidth/Height begin - if baseWidth/Height are positive, it is the
    // bottom left corner.
    public static SpeedGauge Construct(
        float baseWidth,
        float baseHeight,
        float growthWidth,
        float growthWidthDelta,
        float growthHeightDelta,
        float landingWidth)
    {
        if (baseHeight == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseHeight), baseHeight,
                "Base height must be nonzero.");
        }
        if (Math.Sign(baseHeight + growthHeightDelta) != Math.Sign(baseHeight))
        {
            throw new ArgumentOutOfRangeException(nameof(growthHeightDelta), growthHeightDelta,
                "growthHeightDelta must agree in sign with baseHeight or have a lower magnitude than it.");
        }
        if (baseWidth != 0 && growthWidth != 0 && Math.Sign(growthWidth) != Math.Sign(baseWidth))
        {
            throw new ArgumentOutOfRangeException(nameof(growthWidth), growthWidth,
                $"growthWidth must agree with baseWidth sign ({Math.Sign(baseWidth)}), if both are nonzero");
        }
        if (baseWidth != 0 && landingWidth != 0 && Math.Sign(landingWidth) != Math.Sign(baseWidth))
        {
            throw new ArgumentOutOfRangeException(nameof(landingWidth), landingWidth,
                $"landingWidth must agree with baseWidth sign ({Math.Sign(baseWidth)}), if both are nonzero");
        }
        if (growthWidth != 0 && landingWidth != 0 && Math.Sign(landingWidth) != Math.Sign(growthWidth))
        {
            throw new ArgumentOutOfRangeException(nameof(landingWidth), landingWidth,
                $"landingWidth must agree with growthWidth sign ({Math.Sign(growthWidth)}), if both are nonzero");
        }
        // Now we know all three of these agree in sign, so...
        var totalWidth = baseWidth + growthWidth + landingWidth;
        if (totalWidth == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(growthWidth), growthWidth,
                "At least one of baseWidth, growthWidth, or landingWidth must be nonzero.");
        }
        if (Math.Sign(growthWidth + growthWidthDelta) != Math.Sign(totalWidth))
        {
            throw new ArgumentOutOfRangeException(nameof(growthWidthDelta), growthWidthDelta,
                $"growthWidthDelta must agree in sign with other widths ({Math.Sign(totalWidth)})" +
                $" or have a lower magnitude than growthWidth ({growthWidth}).");
        }

        var destination = new Vector2(
            baseWidth + growthWidth + growthWidthDelta + landingWidth,
            -(baseHeight + growthHeightDelta));
        
        return new SpeedGauge([
            new FractionVertexPair(0.0, Vector2.Zero, new Vector2(0.0f, -baseHeight)),
            new FractionVertexPair(
                (double) baseWidth / totalWidth, 
                new Vector2(baseWidth, 0.0f), 
                new Vector2(baseWidth, -baseHeight)),
            new FractionVertexPair(
                (double) (baseWidth + growthWidth) / totalWidth,
                new Vector2(baseWidth + growthWidth, 0.0f),
                new Vector2(baseWidth + growthWidth + growthWidthDelta, -(baseHeight + growthHeightDelta))),
            new FractionVertexPair(
                1.0,
                new Vector2(baseWidth + growthWidth + landingWidth, 0.0f), 
                destination)
        ], [], Vector2.Min(Vector2.Zero, destination), Vector2.Max(Vector2.Zero, destination));
    }


    protected override SpeedGauge Reconstruct(ImmutableArray<FractionVertexPair> vertexPairs, ImmutableArray<double> checkpoints, Vector2 boundingBoxMin,
        Vector2 boundingBoxMax)
    {
        return new SpeedGauge(vertexPairs, checkpoints, boundingBoxMin, boundingBoxMax);
    }

    protected override SpeedGauge Empty => EmptySpeedGauge;
    public static readonly SpeedGauge EmptySpeedGauge = new SpeedGauge([], [], Vector2.NaN, Vector2.NaN);
}