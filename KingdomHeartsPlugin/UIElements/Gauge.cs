using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using KingdomHeartsPlugin.Utilities;

namespace KingdomHeartsPlugin.UIElements
{
    // A gauge consisting of a series of vertex pairs corresponding to different fractions of a displayed quantity.
    public abstract class Gauge<T>(
        ImmutableArray<FractionVertexPair> vertexPairs,
        ImmutableArray<double> checkpoints,
        Vector2 boundingBoxMin,
        Vector2 boundingBoxMax)
        where T : Gauge<T>
    {
        # region Properties
        private ImmutableArray<FractionVertexPair> VertexPairs { get; } = vertexPairs;
        // The array of vertices forming the gauge.
        // The vertex pairs must always be in ascending order, with _vertexPairs[0].Fraction = 0.0 and
        // _vertexPairs[^1].Fraction = 1.0.
        // There must always be at least 2 vertex pairs; gauges with 1 or 0 pairs will be treated as empty.

        // The minimum coordinates in each dimension for the vertex pairs in this Gauge.
        public Vector2 BoundingBoxMin { get; } = boundingBoxMin;

        // The maximum coordinates in each dimension for the vertex pairs in this Gauge.
        public Vector2 BoundingBoxMax { get; } = boundingBoxMax;

        // Significant fractions of this Gauge.
        public ImmutableArray<double> Checkpoints { get; } = checkpoints;

        # endregion

        // Length of the largest segment that is too small to bother drawing
        protected const double TooSmallTolerance = 0.9d;

        // Adjusts the thickness of the Gauge by moving the inner side of it closer to the outer (negative delta)
        // or further away from it (positive delta). 
        public T AdjustThickness(float delta)
        {
            var boundingBoxMin = Vector2.NaN;
            var boundingBoxMax = Vector2.NaN;
            var vertexPairs = new List<FractionVertexPair>();
            
            for (var index = 0; index < VertexPairs.Length; index++)
            {
                if (delta <= -VertexPairs[index].Thickness)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(delta), delta,
                        $"AdjustThickness({delta}) would reduce pair[{index}].Thickness({
                            VertexPairs[index].Thickness}) to or below zero!");
                }
                boundingBoxMin = Vector2.MaxNumber(boundingBoxMin, VertexPairs[index].Min);
                boundingBoxMax = Vector2.MaxNumber(boundingBoxMax, VertexPairs[index].Max);
                // Use innerFraction 1.0f here because adjusting the outer radius can distort the shape of the gauge.
                vertexPairs.Add(VertexPairs[index].AdjustThickness(delta, 1.0f));
            }

            return Reconstruct([..vertexPairs], Checkpoints, boundingBoxMin, boundingBoxMax);
        }
        
        // Gets a subset of this Gauge consisting of the span between the two given fractions.
        // The resulting Gauge is rescaled so that 0.0 is its start and 1.0 is its end.
        // If the fraction of the gauge requested would be too small, Empty is returned.
        public T GetSubset(double fromFraction, double toFraction)
        {
            if (fromFraction < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(fromFraction), fromFraction,
                    "fromFraction must be at least 0.");
            }

            if (toFraction > 1d)
            {
                throw new ArgumentOutOfRangeException(nameof(toFraction), toFraction,
                    "toFraction must be at most 1.");
            }

            if (fromFraction > toFraction)
            {
                throw new ArgumentOutOfRangeException(nameof(toFraction), toFraction,
                    $"toFraction must be at least fromFraction ({fromFraction}).");
            }

            if (toFraction == 0 || VertexPairs.IsEmpty)
            {
                return Empty;
            }

            var vertexPairs = new List<FractionVertexPair>();

            // Skip until we find the pair a, b such that fromFraction is in [a, b)
            // index is the index of a
            // Linear search is fine here, but if there are performance problems, this is a candidate for optimization
            var index = 0;
            while (VertexPairs[index + 1].Fraction <= fromFraction)
            {
                index++;
                if (index >= VertexPairs.Length - 1)
                {
                    throw new IndexOutOfRangeException(
                        $"Went off the end of the vertex pairs list while searching for fromFraction {fromFraction}" +
                        $" (toFraction {toFraction}, _vertexPairs {VertexPairs})");
                }
            }

            // Found it! Synthesize a vertex pair that starts at our precise starting position
            var firstPair = FractionVertexPair.Lerp(VertexPairs[index], VertexPairs[index + 1], fromFraction, 0d);
            vertexPairs.Add(firstPair);
            var boundingBoxMin = firstPair.Min;
            var boundingBoxMax = firstPair.Max;

            // Now copy b until we find the pair a, b such that toFraction is in (a, b]
            // index is still the index of a, even though we're copying b
            while (VertexPairs[index + 1].Fraction < toFraction)
            {
                var nextPair = VertexPairs[index + 1];
                boundingBoxMin = Vector2.Min(boundingBoxMin, nextPair.Min);
                boundingBoxMax = Vector2.Max(boundingBoxMax, nextPair.Max);
                vertexPairs.Add(
                    nextPair with
                    {
                        Fraction = FractionVertexPair.RelativeFraction(fromFraction, toFraction, nextPair.Fraction)
                    });
                index++;
                if (index >= VertexPairs.Length - 1)
                {
                    throw new IndexOutOfRangeException(
                        $"Went off the end of the vertex pairs list while searching for toFraction {toFraction}" +
                        $" (fromFraction {fromFraction}, _vertexPairs {VertexPairs}, firstPair {firstPair})");
                }
            }

            // We now have the index of a for a pair a, b such that toFraction is in (a, b]
            // Synthesize a vertex pair that ends at our precise ending position
            var lastPair = FractionVertexPair.Lerp(VertexPairs[index], VertexPairs[index + 1], toFraction, 1d);
            vertexPairs.Add(lastPair);
            boundingBoxMin = Vector2.Min(boundingBoxMin, lastPair.Min);
            boundingBoxMax = Vector2.Max(boundingBoxMax, lastPair.Max);
            if (vertexPairs.Count == 2 &&
                Vector2.Max(
                    Vector2.Abs(firstPair.Inner - lastPair.Inner),
                    Vector2.Abs(firstPair.Outer - lastPair.Outer)).Length() < TooSmallTolerance)
            {
                return Empty;
            }

            return Reconstruct(
                [..vertexPairs],
                Checkpoints,
                boundingBoxMin,
                boundingBoxMax);
        }

        public T GetSubset(double toFraction)
        {
            return GetSubset(0, toFraction);
        }

        protected abstract T Reconstruct(
            ImmutableArray<FractionVertexPair> vertexPairs, ImmutableArray<double> checkpoints,
            Vector2 boundingBoxMin, Vector2 boundingBoxMax);

        protected abstract T Empty { get; }
        
        # region Fill and Stroke Helpers
        // Two vertices per vertex pair (it's in the name)
        private int Vertices => VertexPairs.Length * 2;

        // One segment for each adjacent pair of vertex pairs
        private int Segments => VertexPairs.Length - 1;

        // Two triangles per segment
        private int Triangles => Segments * 2;

        // Three indices per triangle
        private int Indices => Triangles * 3;
        # endregion

        public void FillFlat(ImDrawListPtr drawList, Vector2 offset, uint color, float? scale = null)
        {
            FillMulticolor(drawList: drawList, offset: offset,
                innerStartColor: color, innerEndColor: color,
                outerStartColor: color, outerEndColor: color,
                scale: scale);
        }
        
        public void FillInnerToOuter(ImDrawListPtr drawList, Vector2 offset, uint innerColor, uint outerColor, float? scale = null)
        {
            FillMulticolor(drawList: drawList, offset: offset,
                innerStartColor: innerColor, innerEndColor: innerColor,
                outerStartColor: outerColor, outerEndColor: outerColor,
                scale: scale);
        }
        
        public void FillStartToEnd(ImDrawListPtr drawList, Vector2 offset, uint startColor, uint endColor, float? scale = null)
        {
            FillMulticolor(drawList: drawList, offset: offset,
                innerStartColor: startColor, outerStartColor: startColor,
                innerEndColor: endColor, outerEndColor: endColor,
                scale: scale);
        }
        
        public void FillMulticolor(ImDrawListPtr drawList, Vector2 offset,
            uint innerStartColor, uint innerEndColor, uint outerStartColor, uint outerEndColor, 
            float? scale = null)
        {
            if (VertexPairs.Length < 2)
            {
                return;
            }

            var innerColor = ColorAddons.Interpolator(innerStartColor, innerEndColor);
            var outerColor = ColorAddons.Interpolator(outerStartColor, outerEndColor);
            
            var resolvedScale = scale ?? 1.0f;
            drawList.PushClipRect(
                BoundingBoxMin * resolvedScale + offset,
                BoundingBoxMax * resolvedScale + offset);

            drawList.PrimReserve(Indices, Vertices);
            for (var segmentIdx = 0; segmentIdx < Segments; segmentIdx += 1)
            {
                var pair = VertexPairs[segmentIdx];
                drawList.PrimWriteIdx((ushort)drawList.VtxCurrentIdx);
                drawList.PrimWriteIdx((ushort)(drawList.VtxCurrentIdx + 1));
                drawList.PrimWriteIdx((ushort)(drawList.VtxCurrentIdx + 2));
                drawList.PrimWriteIdx((ushort)(drawList.VtxCurrentIdx + 1));
                drawList.PrimWriteIdx((ushort)(drawList.VtxCurrentIdx + 2));
                drawList.PrimWriteIdx((ushort)(drawList.VtxCurrentIdx + 3));
                drawList.PrimWriteVtx(
                    pair.Inner * resolvedScale + offset,
                    drawList.Data.TexUvWhitePixel,
                    innerColor(pair.Fraction));
                drawList.PrimWriteVtx(
                    VertexPairs[segmentIdx].Outer * resolvedScale + offset,
                    drawList.Data.TexUvWhitePixel,
                    outerColor(pair.Fraction));
            }

            var lastPair = VertexPairs[^1];
            drawList.PrimWriteVtx(
                lastPair.Inner * resolvedScale + offset,
                drawList.Data.TexUvWhitePixel,
                innerColor(lastPair.Fraction));
            drawList.PrimWriteVtx(
                lastPair.Outer * resolvedScale + offset,
                drawList.Data.TexUvWhitePixel,
                outerColor(lastPair.Fraction));

            drawList.PopClipRect();
        }

        // Strokes the Gauge's perimeter with the given color and thickness.
        // Draws caps if drawCaps is true or if drawCaps is null/not given and the start and end of the gauge
        // do not coincide.
        // The default cap radius is half the line thickness. Since the Fill method does not round corners, any larger
        // would expose the square corners of the gauge where they poke through the rounded corners.
        public void Stroke(ImDrawListPtr drawList,
            Vector2 offset, uint strokeColor, ImDrawFlags strokeFlags, float thickness,
            float? scale = null, bool? drawCaps = null, float? capRadius = null)
        {
            if (VertexPairs.Length < 2)
            {
                return;
            }

            var resolvedDrawCaps = drawCaps ?? !VertexPairs[0].VerticesEqual(VertexPairs[^1]);
            var resolvedScale = scale ?? 1.0f;
            var thicknessSize = new Vector2((float) Math.Ceiling(thickness));
            // If a segment is thinner than the cap radius, using the full cap radius would make oversized arcs
            // that cross over each other, so don't go any bigger than half the start or end thickness.
            var resolvedCapRadius =
                resolvedDrawCaps ? Math.Min(
                    capRadius ?? thickness / 2,
                    Math.Min(VertexPairs[0].Thickness, VertexPairs[1].Thickness) * resolvedScale / 2) : 0.0f;
            var drawRoundedCaps = resolvedDrawCaps && resolvedCapRadius > 0.0f;
            // If we are drawing rounded caps, the first and last segments will be drawn as part of the arcs instead of
            // using lines.
            var roundedCapIndexOffset =  drawRoundedCaps ? 1 : 0;
            var capSize = resolvedDrawCaps ? new Vector2((float)Math.Ceiling(resolvedCapRadius)) : Vector2.Zero;
            drawList.PushClipRect(offset + BoundingBoxMin * resolvedScale - thicknessSize / 2 - capSize,
                offset + BoundingBoxMax * resolvedScale + thicknessSize / 2 + capSize);
            
            var firstCapDeltaAngle = drawRoundedCaps
                ? (float)-VertexPairs[0].PerpendicularAngleTowards(VertexPairs[1])
                : 0.0f;
            var firstCapCenterPair = drawRoundedCaps
                ? VertexPairs[0].Scale(resolvedScale).AdjustThickness(-resolvedCapRadius * 2)
                : VertexPairs[0];
            var firstCapOuterAngle = drawRoundedCaps
                ? (float)VertexPairs[0].InnerToOuterAngleRadians
                : 0.0f;
            var firstCapOffsetAngle = drawRoundedCaps
                ? firstCapOuterAngle + firstCapDeltaAngle
                : 0.0f;
            var firstCapInnerAngle = drawRoundedCaps
                ? firstCapOffsetAngle + firstCapDeltaAngle
                : 0.0f;
            if (drawRoundedCaps)
            {
                if (firstCapCenterPair.Thickness == 0)
                {
                    // Special case: We have exactly the width of the bar in cap diameter, so we need this to be just
                    // one arc, rather than two connected by a line.
                    drawList.PathArcTo(
                        offset + firstCapCenterPair.Center, resolvedCapRadius,
                        firstCapInnerAngle, firstCapOuterAngle);
                }
                else
                {
                    drawList.PathArcTo(
                        offset + firstCapCenterPair.Outer, resolvedCapRadius,
                        firstCapOffsetAngle, firstCapOuterAngle);
                }
            }
            
            for (var index = roundedCapIndexOffset; index < VertexPairs.Length - roundedCapIndexOffset; index++)
            {
                drawList.PathLineTo(offset + VertexPairs[index].Outer * resolvedScale);
            }

            if (!resolvedDrawCaps)
            {
                // Since we're not drawing the caps, the inner and outer paths are separate, so commit here.
                drawList.PathStroke(strokeColor, strokeFlags, thickness);
            }
            
            var lastCapDeltaAngle = drawRoundedCaps
                ? (float) VertexPairs[^1].PerpendicularAngleTowards(VertexPairs[^2])
                : 0.0f;
            var lastCapCenterPair = drawRoundedCaps
                ? VertexPairs[^1].Scale(resolvedScale).AdjustThickness(-resolvedCapRadius * 2)
                : VertexPairs[^1];
            var lastCapOuterAngle = drawRoundedCaps
                ? (float)VertexPairs[^1].InnerToOuterAngleRadians
                : 0.0f;
            var lastCapOffsetAngle = drawRoundedCaps
                ? lastCapOuterAngle + lastCapDeltaAngle
                : 0.0f;
            var lastCapInnerAngle = drawRoundedCaps
                ? lastCapOffsetAngle + lastCapDeltaAngle
                : 0.0f;
            if (drawRoundedCaps)
            {
                if (lastCapCenterPair.Thickness == 0)
                {   
                    // Special case: We have exactly the width of the bar in cap diameter, so we need this to be just
                    // one arc, rather than two connected by a line.
                    drawList.PathArcTo(
                        offset + lastCapCenterPair.Center, resolvedCapRadius,
                        lastCapInnerAngle, lastCapOuterAngle);
                }
                else
                {
                    drawList.PathArcTo(
                        offset + lastCapCenterPair.Outer, resolvedCapRadius, 
                        lastCapOuterAngle, lastCapOffsetAngle);
                    drawList.PathArcTo(
                        offset + lastCapCenterPair.Inner, resolvedCapRadius,
                        lastCapOffsetAngle, lastCapInnerAngle);
                }
            }
            
            for (var index = roundedCapIndexOffset; index < VertexPairs.Length - roundedCapIndexOffset; index++)
            {
                drawList.PathLineTo(offset + VertexPairs[^(index + 1)].Inner * resolvedScale);
            }

            if (drawRoundedCaps && firstCapCenterPair.Thickness > 0)
            {
                drawList.PathArcTo(
                    offset + firstCapCenterPair.Inner, resolvedCapRadius,
                    firstCapInnerAngle, firstCapOffsetAngle);
            }

            drawList.PathStroke(strokeColor,
                resolvedDrawCaps ? strokeFlags | ImDrawFlags.Closed : strokeFlags,
                thickness);

            drawList.PopClipRect();
        }
    }
}