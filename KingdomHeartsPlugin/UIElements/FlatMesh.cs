using System;
using System.Collections.Immutable;
using System.Drawing;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using KingdomHeartsPlugin.Utilities;

namespace KingdomHeartsPlugin.UIElements
{

    public readonly record struct FlatMesh(
        ImmutableArray<Vector2> Vertices,
        ImmutableArray<ushort> Indices,
        ImmutableArray<int> PathVertexCounts,
        // Transforms points in Vertices into points in color space - spanning the original bounding box of the mesh
        Matrix3x2 ColorTransform,
        Vector2 BoundingBoxMin,
        Vector2 BoundingBoxMax)
    {
        // How large the mesh is.
        // Adding this to Offset (below) and the origin gets the max corner of the bounding box.
        public Vector2 Size => BoundingBoxMax - BoundingBoxMin;

        // Where the mesh begins relative to the origin specified.
        // Adding this to the origin gets the min corner of the bounding box.
        public Vector2 Offset => BoundingBoxMin;

        public static FlatMesh Construct(
            ImmutableArray<Vector2> vertices, ImmutableArray<ushort> indices,
            ImmutableArray<int>? pathVertexCounts = null)
        {
            if (indices.Length % 3 != 0)
            {
                throw new ArgumentException(
                    $"Got {indices} indices, but need a multiple of 3 for triangles", nameof(indices));
            }

            if (pathVertexCounts.HasValue)
            {
                var resolvedCounts = (ImmutableArray<int>)pathVertexCounts; 
                var totalVertices = resolvedCounts[0];
                for (var index = 1; index < resolvedCounts.Length; index++)
                {
                    totalVertices += resolvedCounts[index];
                }

                if (totalVertices > vertices.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(pathVertexCounts), pathVertexCounts,
                        $"Too many vertices specified in paths - only have {vertices.Length} vertices," +
                        $" but have {totalVertices} in paths.");
                }
            }

            if (indices.Length > 0)
            {
                var biggestVertexIndex = indices[0];
                for (var index = 1; index < indices.Length; index++)
                {
                    biggestVertexIndex = Math.Max(indices[index], biggestVertexIndex);
                }

                if (biggestVertexIndex >= vertices.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(indices), indices,
                        $"Triangles use nonexistent vertex(ices) with index(ices) up to {biggestVertexIndex}," +
                        $" but only {vertices.Length} vertices were defined.");
                }
            }

            var min = vertices[0];
            var max = vertices[0];
            for (var index = 1; index < vertices.Length; index++)
            {
                min = Vector2.Min(min, vertices[index]);
                max = Vector2.Max(max, vertices[index]);
            }
            
            var size = max - min;
            var colorTransform =
                Matrix3x2.CreateScale(1/size.X, 1/size.Y) * Matrix3x2.CreateTranslation(-min);
            return new FlatMesh(vertices, indices, pathVertexCounts ?? [vertices.Length], colorTransform, min, max);
        }

        public FlatMesh Transform(Matrix3x2 transform)
        {
            var verticesBuilder = ImmutableArray.CreateBuilder<Vector2>(Vertices.Length); 
            var min = Vector2.Transform(Vertices[0], transform);
            var max = min;
            verticesBuilder.Add(min);
            for (var index = 1; index < Vertices.Length; index++)
            {
                var vertex = Vector2.Transform(Vertices[index], transform);
                min = Vector2.Min(min, vertex);
                max = Vector2.Max(max, vertex);
                verticesBuilder.Add(vertex);
            }

            var invertTransformSuccess = Matrix3x2.Invert(transform, out var inverseTransform);
            return new FlatMesh(
                verticesBuilder.MoveToImmutable(),
                Indices,
                PathVertexCounts,
                invertTransformSuccess ? ColorTransform * inverseTransform : Matrix3x2.Identity,
                min,
                max);
        }
        
        public void Fill(ImDrawListPtr drawList, Gradient2D colors)
        {
            drawList.PushClipRect(BoundingBoxMin, BoundingBoxMax);

            drawList.PrimReserve(Indices.Length, Vertices.Length);
            foreach (var vertexIndex in Indices)
            {
                drawList.PrimWriteIdx((ushort)(drawList.VtxCurrentIdx + vertexIndex));
            }

            for (var index = 0; index < Vertices.Length; index++)
            {
                drawList.PrimWriteVtx(
                    Vertices[index],
                    drawList.Data.TexUvWhitePixel,
                    colors[Vector2.Transform(Vertices[index], ColorTransform)]);
            }

            drawList.PopClipRect();
        }

        // mainly for debugging
        public void Wireframe(ImDrawListPtr drawList, uint color, float thickness, ImDrawFlags flags = ImDrawFlags.None)
        {
            var thicknessSize = new Vector2(thickness);
            drawList.PushClipRect(
                BoundingBoxMin - thicknessSize / 2,
                BoundingBoxMax + thicknessSize / 2);

            for (var index = 0; index < Indices.Length; index += 3)
            {
                drawList.PathLineTo(Vertices[Indices[index]]);
                drawList.PathLineTo(Vertices[Indices[index + 1]]);
                drawList.PathLineTo(Vertices[Indices[index + 1]]);
                drawList.PathStroke(color, flags | ImDrawFlags.Closed, thickness);
            }

            drawList.PopClipRect();
        }

        public void Stroke(ImDrawListPtr drawList, uint color, float thickness, ImDrawFlags flags = ImDrawFlags.None)
        {
            var thicknessSize = new Vector2(thickness);
            drawList.PushClipRect(
                BoundingBoxMin - thicknessSize / 2,
                BoundingBoxMax + thicknessSize / 2);

            var pathIndexStart = 0;
            foreach (var pathVertexCount in PathVertexCounts)
            {
                for (var index = 0; index < pathVertexCount; index++)
                {
                    drawList.PathLineTo(Vertices[pathIndexStart + index]);
                }

                drawList.PathStroke(color, flags | ImDrawFlags.Closed, thickness);
                pathIndexStart += pathVertexCount;
            }

            drawList.PopClipRect();
        }
    }
}