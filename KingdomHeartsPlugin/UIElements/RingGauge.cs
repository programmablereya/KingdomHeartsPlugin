using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;

namespace KingdomHeartsPlugin.UIElements;

public class RingGauge : Gauge<RingGauge>
{
    private RingGauge(
        ImmutableArray<FractionVertexPair> vertexPairs, ImmutableArray<double> checkpoints,
        Vector2 boundingBoxMin, Vector2 boundingBoxMax)
        : base(vertexPairs, checkpoints, boundingBoxMin, boundingBoxMax)
    {
    }

    // Constructs a RingGauge representing a (possibly empty) hollow sector of a circle centered at (0,0) and
    // number of defining points (>=3) and a (possibly empty) bar which stretches from the end of the sector.
    // If the gauge is too small to see, returns RingGauge.Empty.
    // Its one checkpoint is the fraction of the gauge which is part of the sector.
    public static RingGauge Construct(
        int circleResolution,
        double outerRadius,
        double innerRadius,
        double startAngleRadians,
        double endAngleRadians,
        double barLength)
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
        var arcLength = sectorAngleRadians * outerRadius;
        
        var totalLength = arcLength + barLength;
        if (totalLength <= TooSmallTolerance)
        {
            return EmptyRingGauge;
        }
        var sectorFraction = arcLength / totalLength;
        
        var fragmentsNeeded = (int) Math.Ceiling(circleResolution * sectorAngleRadians / (2 * Math.PI));
        var sectorBarBoundary = new FractionVertexPair(
            sectorFraction,
            Circle.getPointOnCircle(Vector2.Zero, endAngleRadians, innerRadius),
            Circle.getPointOnCircle(Vector2.Zero, endAngleRadians, outerRadius));
        var vertexPairs = new List<FractionVertexPair>();
        var boundingBoxMin = sectorBarBoundary.Min;
        var boundingBoxMax = sectorBarBoundary.Max;
        
        if (fragmentsNeeded > 0)
        {
            for (var fragmentIndex = 0; fragmentIndex < fragmentsNeeded; fragmentIndex++)
            {
                var fragmentAngleRadians =
                    startAngleRadians + fragmentIndex * signedSectorAngleRadians / fragmentsNeeded;
                var fragmentFraction = sectorFraction * fragmentIndex / fragmentsNeeded;
                var fragmentVertexPair = new FractionVertexPair(
                    fragmentFraction,
                    Circle.getPointOnCircle(Vector2.Zero, fragmentAngleRadians, innerRadius),
                    Circle.getPointOnCircle(Vector2.Zero, fragmentAngleRadians, outerRadius));
                boundingBoxMin = Vector2.Min(fragmentVertexPair.Min, boundingBoxMin);
                boundingBoxMax = Vector2.Max(fragmentVertexPair.Max, boundingBoxMax);
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
            var barEndOffset = new Vector2(
                (float)(Math.Cos(barAngleRadians) * barLength),
                (float)(Math.Sin(barAngleRadians) * barLength));
            var barEndPair = sectorBarBoundary.Offset(barEndOffset, 1.0d);
            boundingBoxMin = Vector2.Min(barEndPair.Min, boundingBoxMin);
            boundingBoxMax = Vector2.Max(barEndPair.Max, boundingBoxMax);
            vertexPairs.Add(barEndPair);
        }
        
        return new RingGauge([..vertexPairs], [sectorFraction], boundingBoxMin, boundingBoxMax);
    }

    protected override RingGauge Reconstruct(ImmutableArray<FractionVertexPair> vertexPairs, ImmutableArray<double> checkpoints, Vector2 boundingBoxMin,
        Vector2 boundingBoxMax)
    {
        return new RingGauge(vertexPairs, checkpoints, boundingBoxMin, boundingBoxMax);
    }

    // The fraction of this RingGauge made up by the sector, spanning Fraction = 0.0 to SectorFraction.
    public double SectorFraction => Checkpoints[0];
    // The fraction of this RingGauge made up by the bar, spanning Fraction = SectorFraction to 1.0.
    public double BarFraction => 1 - Checkpoints[0];
    
    protected override RingGauge Empty => EmptyRingGauge;
    public static readonly RingGauge EmptyRingGauge = new([], [0.0], Vector2.NaN, Vector2.NaN);
}