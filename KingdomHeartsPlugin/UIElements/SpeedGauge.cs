using System;
using System.Collections.Immutable;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using KingdomHeartsPlugin.Utilities;

namespace KingdomHeartsPlugin.UIElements;


public record SpeedGauge
{
    private SpeedGauge(ImmutableArray<Gauge.FractionVertexPair> vertexPairs, Vector2 boundingBoxMin, Vector2 boundingBoxMax)
    {
        VertexPairs = vertexPairs;
        BoundingBoxMin = boundingBoxMin;
        BoundingBoxMax = boundingBoxMax;
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
    // top left corner.
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
        var origin = Vector2.Zero;
        var destination = new Vector2(
            baseWidth + growthWidth + growthWidthDelta + landingWidth,
            -(baseHeight + growthHeightDelta));

        // TODO: we should divide the growth width across the whole thing, not just the growth segment
        return new SpeedGauge([
            new Gauge.FractionVertexPair(0.0f, Vector2.Zero, new Vector2(0.0f, -baseHeight)),
            new Gauge.FractionVertexPair(
                baseWidth / totalWidth, 
                new Vector2(baseWidth, 0.0f), 
                new Vector2(baseWidth, -baseHeight)),
            new Gauge.FractionVertexPair(
                (baseWidth + growthWidth) / totalWidth,
                new Vector2(baseWidth + growthWidth, 0.0f),
                new Vector2(baseWidth + growthWidth + growthWidthDelta, -(baseHeight + growthHeightDelta))),
            new Gauge.FractionVertexPair(
                1.0f,
                new Vector2(baseWidth + growthWidth + landingWidth, 0.0f), 
                destination)
        ],
            boundingBoxMin: Vector2.Min(origin, destination),
            boundingBoxMax: Vector2.Max(origin, destination));
    }
    
    public SpeedGauge Slice(
        float startFraction = 0.0f, float endFraction = 1.0f, float tolerance = Gauge.DefaultTolerance)
    {
        if (VertexPairs.IsEmpty)
        {
            return Empty;
        }

        if ((startFraction, endFraction) == (0.0f, 1.0f))
        {
            return this;
        }
        var newGauge = Gauge.Slice(
            VertexPairs, out var boundingBoxMin, out var boundingBoxMax,
            startFraction, endFraction, tolerance);
        return newGauge.IsEmpty ? Empty : new SpeedGauge(newGauge, boundingBoxMin, boundingBoxMax);
    }

    public SpeedGauge AdjustThickness(float factor, float innerFraction = 1.0f)
    {
        if (VertexPairs.IsEmpty)
        {
            return this;
        }
        
        return new SpeedGauge(
            Gauge.AdjustThickness(
                VertexPairs, factor,
                out var boundingBoxMin, out var boundingBoxMax, innerFraction),
            boundingBoxMin,
            boundingBoxMax);
    }

    public SpeedGauge Transform(Matrix3x2 matrix)
    {
        if (matrix.IsIdentity)
        {
            return this;
        }
        if (VertexPairs.IsEmpty)
        {
            return Empty;
        }
        
        return new SpeedGauge(
            Gauge.Transform(VertexPairs, matrix, out var boundingBoxMin, out var boundingBoxMax),
            boundingBoxMin,
            boundingBoxMax);
    }
    
    public void Fill(ImDrawListPtr drawList, Gradient2D colors, ImmutableArray<float>? yStops = null, float antialiasingFringeAtEnds = 1.0f, float antialiasingFringeAtSides = 1.0f)
    {
        if (VertexPairs.IsEmpty)
        {
            return;
        }
        Gauge.Fill(VertexPairs, drawList, colors, yStops, antialiasingFringeAtEnds, antialiasingFringeAtSides);
    }

    public void Wireframe(ImDrawListPtr drawList, uint color, ImmutableArray<float>? yStops = null, ImDrawFlags drawFlags = ImDrawFlags.None, float thickness = 1.0f, float antialiasingFringeAtEnds = 1.0f, float antialiasingFringeAtSides = 1.0f)
    {
        if (VertexPairs.IsEmpty)
        {
            return;
        }
        Gauge.Wireframe(VertexPairs, drawList, color, yStops, drawFlags, thickness, antialiasingFringeAtEnds, antialiasingFringeAtSides);
    }

    private ImmutableArray<Gauge.FractionVertexPair> VertexPairs { get; }

    public Vector2 BoundingBoxMin { get; }
    public Vector2 BoundingBoxMax { get; }
    
    public static readonly SpeedGauge Empty = new SpeedGauge([], Vector2.NaN, Vector2.NaN);
}