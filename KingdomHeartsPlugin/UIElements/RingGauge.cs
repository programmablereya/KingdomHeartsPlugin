using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;
using KingdomHeartsPlugin.Utilities;

namespace KingdomHeartsPlugin.UIElements;

public sealed record RingGauge
{
    private RingGauge(
        ImmutableArray<Gauge.FractionVertexPair> vertexPairs, float sectorFraction,
        Vector2 boundingBoxMin, Vector2 boundingBoxMax)
    {
        VertexPairs = vertexPairs;
        SectorFraction = sectorFraction;
        BoundingBoxMin = boundingBoxMin;
        BoundingBoxMax = boundingBoxMax;
    }

    // Constructs a RingGauge representing a (possibly empty) hollow sector of a circle centered at (0,0) and
    // number of defining points (>=3) and a (possibly empty) bar which stretches from the end of the sector.
    // If the gauge is too small to see, returns RingGauge.Empty.
    // Its one checkpoint is the fraction of the gauge which is part of the sector.
    public static RingGauge Construct(
        int circleResolution,
        float outerRadius,
        float innerRadius,
        double startAngleRadians,
        double endAngleRadians,
        float barLength,
        float tolerance = Gauge.DefaultTolerance)
    {
        if (barLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(barLength), barLength,
                "Bar length must be at least 0.");
        }

        if (innerRadius < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(innerRadius), innerRadius,
                "Inner radius must be at least 0.");
        }

        if (outerRadius <= innerRadius)
        {
            throw new ArgumentOutOfRangeException(nameof(outerRadius), outerRadius,
                $"Outer radius must be greater than inner radius ({innerRadius}).");
        }

        if (circleResolution < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(circleResolution), circleResolution,
                "Number of points/segments used to construct a circle must be at least 3.");
        }
        
        var signedSectorAngleRadians = endAngleRadians - startAngleRadians;
        var sectorAngleRadians = Math.Abs(signedSectorAngleRadians);
        
        // Uses the arc length, since shield uses a smaller inner ring but a larger outer ring 
        var arcLength = Circle.ArcLength(sectorAngleRadians, outerRadius);
        
        var totalLength = arcLength + barLength;
        if (totalLength <= tolerance)
        {
            return Empty;
        }
        var sectorFraction = arcLength / totalLength;
        
        var fragmentsNeeded = (int) Math.Ceiling(circleResolution * sectorAngleRadians / (2 * Math.PI));
        var sectorBarBoundary = new Gauge.FractionVertexPair(
            sectorFraction,
            Circle.PointAtAngle(Vector2.Zero, endAngleRadians, innerRadius),
            Circle.PointAtAngle(Vector2.Zero, endAngleRadians, outerRadius));
        var boundingBoxMin = sectorBarBoundary.Min();
        var boundingBoxMax = sectorBarBoundary.Max();
        var vertexPairs = new List<Gauge.FractionVertexPair>();
        
        if (fragmentsNeeded > 0)
        {
            for (var fragmentIndex = 0; fragmentIndex < fragmentsNeeded; fragmentIndex++)
            {
                var fragmentAngleRadians =
                    startAngleRadians + fragmentIndex * signedSectorAngleRadians / fragmentsNeeded;
                var fragmentFraction = sectorFraction * fragmentIndex / fragmentsNeeded;
                var fragmentVertexPair = new Gauge.FractionVertexPair(
                    fragmentFraction, 
                    Circle.PointAtAngle(Vector2.Zero, fragmentAngleRadians, innerRadius),
                    Circle.PointAtAngle(Vector2.Zero, fragmentAngleRadians, outerRadius));
                boundingBoxMin = Vector2.Min(boundingBoxMin, fragmentVertexPair.Min());
                boundingBoxMax = Vector2.Max(boundingBoxMax, fragmentVertexPair.Max());
                vertexPairs.Add(fragmentVertexPair);
            }
        }
        
        vertexPairs.Add(sectorBarBoundary);

        if (barLength > 0)
        {
            /*
             * The bar is functionally just the parallelogram formed by the innerRadius and outerRadius from the
             * endAngle offset by barLength in the perpendicular direction, i.e., 90 degrees - or half pi - from the
             * end of the sector, in the same direction we were going for the rest of the sector.
             */
            var barAngleRadians = endAngleRadians + Math.CopySign(Math.PI / 2d, signedSectorAngleRadians);
            var barEndOffset = Circle.PointAtAngle(Vector2.Zero, barAngleRadians, barLength);
            var barEndPair = sectorBarBoundary.Offset(barEndOffset, 1.0f);
            boundingBoxMin = Vector2.Min(boundingBoxMin, barEndPair.Min());
            boundingBoxMax = Vector2.Max(boundingBoxMax, barEndPair.Max());
            vertexPairs.Add(barEndPair);
        }
        
        return new RingGauge([..vertexPairs], sectorFraction, boundingBoxMin, boundingBoxMax);
    }

    public RingGauge Slice(
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
        var sectorFraction =
            (Math.Clamp(SectorFraction, startFraction, endFraction) - startFraction) / (endFraction - startFraction);
        var newGauge = Gauge.Slice(
            VertexPairs, out var boundingBoxMin, out var boundingBoxMax,
            startFraction, endFraction, tolerance);
        return newGauge.IsEmpty ? Empty : new RingGauge(newGauge, sectorFraction, boundingBoxMin, boundingBoxMax);
    }

    public RingGauge AdjustThickness(float factor, float innerFraction = 1.0f)
    {
        if (VertexPairs.IsEmpty)
        {
            return this;
        }
        
        return new RingGauge(
            Gauge.AdjustThickness(
                VertexPairs, factor,
                out var boundingBoxMin, out var boundingBoxMax, innerFraction),
            SectorFraction,
            boundingBoxMin,
            boundingBoxMax);
    }

    public RingGauge Transform(Matrix3x2 matrix)
    {
        if (matrix.IsIdentity)
        {
            return this;
        }
        if (VertexPairs.IsEmpty)
        {
            return Empty;
        }
        
        return new RingGauge(
            Gauge.Transform(VertexPairs, matrix, out var boundingBoxMin, out var boundingBoxMax),
            SectorFraction,
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

    public void Stroke(ImDrawListPtr drawList, uint color, ImDrawFlags strokeFlags, float thickness,
        bool? drawCaps = null, float? capRadius = null)
    {
        if (VertexPairs.IsEmpty)
        {
            return;
        }
        Gauge.Stroke(VertexPairs, drawList, color, strokeFlags, thickness, drawCaps, capRadius);
    }

    // The fraction of this RingGauge made up by the sector, spanning Fraction = 0.0 to SectorFraction.
    public float SectorFraction { get; }

    // The fraction of this RingGauge made up by the bar, spanning Fraction = SectorFraction to 1.0.
    public float BarFraction => 1 - SectorFraction;
    
    public Vector2 BoundingBoxMin { get; }
    public Vector2 BoundingBoxMax { get; }
    
    private ImmutableArray<Gauge.FractionVertexPair> VertexPairs { get; }

    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append(nameof(BoundingBoxMin));
        builder.Append(" = ");
        builder.Append(BoundingBoxMin);
        builder.Append(", ");
        builder.Append(nameof(BoundingBoxMax));
        builder.Append(" = ");
        builder.Append(BoundingBoxMax);
        builder.Append(", ");
        builder.Append(nameof(SectorFraction));
        builder.Append(" = ");
        builder.Append(SectorFraction);
        builder.Append(", ");
        builder.Append(nameof(VertexPairs));
        if (!VertexPairs.IsEmpty)
        {
            builder.Append(" = { ");
            builder.Append(VertexPairs[0]);
            for (var index = 1; index < VertexPairs.Length; index++)
            {
                builder.Append(", ");
                builder.Append(VertexPairs[index]);
            }
            builder.Append(" }");
        }
        else
        {
            builder.Append(" = { }");    
        }

        return true;
    }

    public static readonly RingGauge Empty =
        new([], float.NaN, Vector2.NaN, Vector2.NaN);
}