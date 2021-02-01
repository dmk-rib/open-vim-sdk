﻿using System;
using System.Collections.Generic;
using System.Linq;
using Vim.LinqArray;
using Vim.Math3d;

namespace Vim.G3d
{
    public static class GeometryAttributesExtensions
    {
        public static int ExpectedElementCount(this IGeometryAttributes self, AttributeDescriptor desc)
        {
            switch (desc.Association)
            {
                case Association.assoc_all:
                    return 1;
                case Association.assoc_none:
                    return 0;
                case Association.assoc_vertex:
                    return self.NumVertices;
                case Association.assoc_face:
                    return self.NumFaces;
                case Association.assoc_corner:
                    return self.NumCorners;
                case Association.assoc_edge:
                    return self.NumCorners;
                case Association.assoc_subgeometry:
                    return self.NumSubgeometries;
                case Association.assoc_instance:
                    return self.NumInstances;
            }
            return -1;
        }

        public static IArray<string> AttributeNames(this IGeometryAttributes g)
            => g.Attributes.Select(attr => attr.Name);

        public static GeometryAttribute<T> GetAttribute<T>(this IGeometryAttributes g, string attributeName) where T : unmanaged
            => g.GetAttribute(attributeName)?.AsType<T>();

        public static GeometryAttribute DefaultAttribute(this IGeometryAttributes self, string name)
            => self.DefaultAttribute(AttributeDescriptor.Parse(name));

        public static GeometryAttribute DefaultAttribute(this IGeometryAttributes self, AttributeDescriptor desc)
            => desc.ToDefaultAttribute(self.ExpectedElementCount(desc));

        public static GeometryAttribute GetOrDefaultAttribute(this IGeometryAttributes self, AttributeDescriptor desc)
            => self.GetAttribute(desc.ToString()) ?? desc.ToDefaultAttribute(self.ExpectedElementCount(desc));

        public static IEnumerable<GeometryAttribute> NoneAttributes(this IGeometryAttributes g)
            => g.Attributes.Where(a => a.Descriptor.Association == Association.assoc_none);

        public static IEnumerable<GeometryAttribute> CornerAttributes(this IGeometryAttributes g)
            => g.Attributes.Where(a => a.Descriptor.Association == Association.assoc_corner);

        public static IEnumerable<GeometryAttribute> EdgeAttributes(this IGeometryAttributes g)
            => g.Attributes.Where(a => a.Descriptor.Association == Association.assoc_edge);

        public static IEnumerable<GeometryAttribute> FaceAttributes(this IGeometryAttributes g)
            => g.Attributes.Where(a => a.Descriptor.Association == Association.assoc_face);

        public static IEnumerable<GeometryAttribute> VertexAttributes(this IGeometryAttributes g)
            => g.Attributes.Where(a => a.Descriptor.Association == Association.assoc_vertex);

        public static IEnumerable<GeometryAttribute> InstanceAttributes(this IGeometryAttributes g)
            => g.Attributes.Where(a => a.Descriptor.Association == Association.assoc_instance);

        public static IEnumerable<GeometryAttribute> SubGeometryAttributes(this IGeometryAttributes g)
            => g.Attributes.Where(a => a.Descriptor.Association == Association.assoc_subgeometry);

        public static IEnumerable<GeometryAttribute> WholeGeometryAttributes(this IGeometryAttributes g)
            => g.Attributes.Where(a => a.Descriptor.Association == Association.assoc_all);

        public static bool HasSameAttributes(this IGeometryAttributes g1, IGeometryAttributes g2)
            => g1.Attributes.Count == g2.Attributes.Count && g1.Attributes.Indices().All(i => g1.Attributes[i].Name == g2.Attributes[i].Name);

        public static int FaceToCorner(this IGeometryAttributes g, int f)
            => f * g.NumCornersPerFace;

        /// <summary>
        /// Given a set of face indices, creates an array of corner indices 
        /// </summary>
        public static IArray<int> FaceIndicesToCornerIndices(this IGeometryAttributes g, IArray<int> faceIndices)
            => (faceIndices.Count * g.NumCornersPerFace)
                .Select(i => g.FaceToCorner(faceIndices[i / g.NumCornersPerFace]) + i % g.NumCornersPerFace);
       
        /// <summary>
        /// Given a set of face indices, creates an array of indices of the first corner in each face 
        /// </summary>
        public static IArray<int> FaceIndicesToFirstCornerIndices(this IGeometryAttributes g, IArray<int> faceIndices)
            => faceIndices.Select(f => f * g.NumCornersPerFace);

        public static int CornerToFace(this IGeometryAttributes g, int c)
            => c / g.NumCornersPerFace;

        public static IArray<int> CornersToFaces(this IGeometryAttributes g)
            => g.NumCorners.Select(g.CornerToFace);

        public static int CornerNumber(this IGeometryAttributes g, int c)
            => c % g.NumCornersPerFace;

        public static IGeometryAttributes ToGeometryAttributes(this IEnumerable<GeometryAttribute> attributes)
            => new GeometryAttributes(attributes);

        public static IGeometryAttributes ToGeometryAttributes(this IArray<GeometryAttribute> attributes)
            => attributes.ToEnumerable().ToGeometryAttributes();

        public static IGeometryAttributes AddAttributes(this IGeometryAttributes attributes, params GeometryAttribute[] newAttributes)
            => attributes.Attributes.ToEnumerable().Concat(newAttributes).ToGeometryAttributes();

        public static GeometryAttribute GetAttributeOrDefault(this IGeometryAttributes g, string name)
            => g.GetAttribute(name) ?? g.DefaultAttribute(name);

        public static IGeometryAttributes Merge(this IArray<G3D> gs)
            => gs.Select(x => (IGeometryAttributes)x).Merge();

        public static IGeometryAttributes Merge(this IGeometryAttributes self, params IGeometryAttributes[] gs)
            => gs.ToIArray().Prepend(self).Merge();

        public static IGeometryAttributes Merge(this IArray<IGeometryAttributes> gs)
        {
            if (gs.Count == 0)
                return GeometryAttributes.Empty;

            var first = gs[0];

            if (gs.Count == 1)
                return first;

            if (!gs.All(g => g.NumCornersPerFace == first.NumCornersPerFace))
                throw new Exception("Cannot merge geometries with different numbers of corners per faces");

            // Merge all of the attributes of the different geometries
            // Except: indices, group indexes, subgeo, and instance attributes 
            var attributes = first.VertexAttributes()
                .Concat(first.CornerAttributes())
                .Concat(first.EdgeAttributes())
                .Concat(first.NoneAttributes())
                .Concat(first.FaceAttributes())
                // Skip the index semantic because things get re-ordered 
                .Where(attr => attr.Descriptor.Semantic != Semantic.Index)
                .ToArray();

            // Merge the attributes
            var others = gs.Skip(1).ToEnumerable();
            var attributeList = attributes.Select(
                attr => attr.Merge(others.Select(g => g.GetAttributeOrDefault(attr.Name)))).ToList();

            // Compute the vertex offsets for each sub-geometry
            var vertexOffsets = gs.Select(m => m.NumVertices).CountsToOffsets().ToSubGeoVertexOffsetAttribute();
            attributeList.Add(vertexOffsets);

            // Compute the index offsets for each sub-geometry
            var indexOffsets = gs.Select(m => m.NumCorners).CountsToOffsets().ToSubGeoIndexOffsetAttribute();
            attributeList.Add(indexOffsets);

            // Add the renumbered index attribute
            if (first.GetAttributeIndex() != null)
            {
                var vertexOffset = 0;
                var cornerOffset = 0;
                var indices = new int[gs.Sum(ga => ga.NumCorners)];
                foreach (var ga in gs.ToEnumerable())
                {
                    var localIndices = ga.GetAttributeIndex().Data;
                    localIndices.Select(i => i + vertexOffset).CopyTo(indices, cornerOffset);
                    vertexOffset += ga.NumVertices;
                    cornerOffset += ga.NumCorners;
                }
                attributeList.Add(indices.ToIArray().ToIndexAttribute());
            }

            return attributeList.ToGeometryAttributes();
        }


        /// <summary>
        /// Applies vertex colors
        /// </summary>
        public static IGeometryAttributes Color(this IGeometryAttributes g, Vector4 color)
        {
            return g.Attributes
                .Where(a => a.Descriptor.Semantic != Semantic.Color)
                .Concat(Enumerable.Repeat(g.NumVertices.Select(_ => color).ToAttribute(new AttributeDescriptor(Association.assoc_vertex, Semantic.Color, DataType.dt_float32, 4)), 1))
                .ToGeometryAttributes();
        }
        /// <summary>
        /// Applies a transformation function to position attributes and another to normal attributes. When deforming, we may want to
        /// apply a similar deformation to the normals. For example a matrix can change the position, rotation, and scale of a geometry,
        /// but the only changes should be to the direction of the normal, not the length.
        /// </summary>
        public static IGeometryAttributes Deform(this IGeometryAttributes g, Func<Vector3, Vector3> positionTransform, Func<Vector3, Vector3> normalTransform)
            => g.Attributes.Select(
                a =>
                    (a.Descriptor.Semantic == "position" && a is GeometryAttribute<Vector3> p) ? p.Data.Select(positionTransform).ToAttribute(a.Descriptor) :
                    (a.Descriptor.Semantic == "normal" && a is GeometryAttribute<Vector3> n) ? n.Data.Select(normalTransform).ToAttribute(a.Descriptor) :
                    a)
            .ToGeometryAttributes();

        /// <summary>
        /// Applies a deformation to points, without changing the normals. For some transformation functions this can result in incorrect normals.
        /// </summary>
        public static IGeometryAttributes Deform(this IGeometryAttributes g, Func<Vector3, Vector3> positionTransform)
            => g.Attributes.Select(
                a =>
                    (a.Descriptor.Semantic == "position" && a is GeometryAttribute<Vector3> p) ? p.Data.Select(positionTransform).ToAttribute(a.Descriptor) :
                    a)
            .ToGeometryAttributes();

        /// <summary>
        /// Applies a transformation matrix
        /// </summary>
        public static IGeometryAttributes Transform(this IGeometryAttributes g, Matrix4x4 matrix)
            => g.Deform(v => v.Transform(matrix), v => v.TransformNormal(matrix));

        public static IGeometryAttributes SetPosition(this IGeometryAttributes g, IArray<Vector3> points)
            => g.SetAttribute(points.ToPositionAttribute());

        public static IGeometryAttributes SetAttribute(this IGeometryAttributes self, GeometryAttribute attr)
            => self.Attributes.Where(a => !a.Descriptor.Equals(attr.Descriptor)).Append(attr).ToGeometryAttributes();

        public static IGeometryAttributes SetAttribute<ValueT>(this IGeometryAttributes self, IArray<ValueT> values, AttributeDescriptor desc) where ValueT : unmanaged
            => self.SetAttribute(values.ToAttribute(desc));

        /// <summary>
        /// Leaves the vertex buffer intact and creates a new geometry that remaps all of the group, corner, and face data.
        /// The newFaces array is a list of indices into the old face array.
        /// Note: sub-geometries are lost.
        /// </summary>
        public static IGeometryAttributes RemapFaces(this IGeometryAttributes g, IArray<int> faceRemap)
            => g.RemapFacesAndCorners(faceRemap, g.FaceIndicesToCornerIndices(faceRemap));

        public static IEnumerable<GeometryAttribute> SetFaceSizeAttribute(this IEnumerable<GeometryAttribute> attributes, int numCornersPerFaces)
            => (numCornersPerFaces <= 0)
                   ? attributes
                   : attributes
                        .Where(attr => attr.Descriptor.Semantic != Semantic.FaceSize)
                        .Append(new[] { numCornersPerFaces }.ToObjectFaceSizeAttribute());      

        /// <summary>
        /// Low-level remap function. Maps faces and corners at the same time.
        /// In some cases, this is important (e.g. triangulating quads).
        /// Note: sub-geometries are lost.
        /// </summary>
        public static IGeometryAttributes RemapFacesAndCorners(this IGeometryAttributes g, IArray<int> faceRemap, IArray<int> cornerRemap, int numCornersPerFace = -1)
            => g.VertexAttributes()
                .Concat(g.NoneAttributes())
                .Concat(g.FaceAttributes().Select(attr => attr.Remap(faceRemap)))
                .Concat(g.EdgeAttributes().Select(attr => attr.Remap(cornerRemap)))
                .Concat(g.CornerAttributes().Select(attr => attr.Remap(cornerRemap)))
                .Concat(g.WholeGeometryAttributes())
                .SetFaceSizeAttribute(numCornersPerFace)
                .ToGeometryAttributes();

        /// <summary>
        /// Converts a quadrilateral mesh into a triangular mesh carrying over all attributes.
        /// </summary>
        public static IGeometryAttributes TriangulateQuadMesh(this IGeometryAttributes g)
        {
            if (g.NumCornersPerFace != 4) throw new Exception("Not a quad mesh");

            var cornerRemap = new int[g.NumFaces * 6];
            var faceRemap = new int[g.NumFaces * 2];
            var cur = 0;
            for (var i = 0; i < g.NumFaces; ++i)
            {
                cornerRemap[cur++] = i * 4 + 0;
                cornerRemap[cur++] = i * 4 + 1;
                cornerRemap[cur++] = i * 4 + 2;
                cornerRemap[cur++] = i * 4 + 0;
                cornerRemap[cur++] = i * 4 + 2;
                cornerRemap[cur++] = i * 4 + 3;

                faceRemap[i * 2 + 0] = i;
                faceRemap[i * 2 + 1] = i;
            }

            return g.RemapFacesAndCorners(faceRemap.ToIArray(), cornerRemap.ToIArray(), 3);
        }

        public static IGeometryAttributes CopyFaces(this IGeometryAttributes g, IArray<bool> keep)
            => g.CopyFaces(i => keep[i]);

        public static IGeometryAttributes CopyFaces(this IGeometryAttributes g, IArray<int> keep)
            => g.RemapFaces(keep);

        public static IGeometryAttributes CopyFaces(this IGeometryAttributes self, Func<int, bool> predicate)
            => self.RemapFaces(self.NumFaces.Select(i => i).IndicesWhere(predicate).ToIArray());

        public static IGeometryAttributes DeleteFaces(this IGeometryAttributes g, Func<int, bool> predicate)
            => g.CopyFaces(i => !predicate(i));

        public static IGeometryAttributes CopyFaces(this IGeometryAttributes g, int from, int count)
            => g.CopyFaces(i => i >= from && i < from + count);

        public static IArray<IGeometryAttributes> CopyFaceGroups<T>(this IGeometryAttributes g, int size)
            => g.NumFaces.DivideRoundUp(size).Select(i => CopyFaces(g, i * size, size));

        /// <summary>
        /// Updates the vertex buffer (e.g. after identifying unwanted faces) and the index
        /// buffer. Vertices are either re-ordered, removed, or deleted. Does not affect any other 
        /// </summary>
        public static IGeometryAttributes RemapVertices(this IGeometryAttributes g, IArray<int> newVertices, IArray<int> newIndices)
            => (new[] { newIndices.ToIndexAttribute() }
                .Concat(
                    g.VertexAttributes()
                    .Select(attr => attr.Remap(newVertices)))
                .Concat(g.NoneAttributes())
                .Concat(g.FaceAttributes())
                .Concat(g.EdgeAttributes())
                .Concat(g.CornerAttributes())
                .Concat(g.WholeGeometryAttributes())
                )
            .ToGeometryAttributes();

        /// <summary>
        /// The vertRemap is a list of vertices in the new vertex buffer, and where they came from. 
        /// This could be a reordering of the original vertex buffer, it could even be a repetition.
        /// It could also be some vertices were deleted, BUT if those vertices are still referenced
        /// then this will throw an exception.
        /// The values in the index buffer will change, but it will stay the same length.
        /// </summary>
        public static IGeometryAttributes RemapVertices(this IGeometryAttributes g, IArray<int> vertRemap)
        {
            var vertLookup = (-1).Repeat(g.NumVertices).ToArray();
            for (var i = 0; i < vertRemap.Count; ++i)
            {
                var oldVert = vertRemap[i];
                vertLookup[oldVert] = i;
            }

            var oldIndices = g.GetAttributeIndex()?.Data ?? g.NumVertices.Range();
            var newIndices = oldIndices.Select(i => vertLookup[i]).Evaluate();

            if (newIndices.Any(x => x == -1))
                throw new Exception("At least one of the indices references a vertex that no longer exists");

            return g.RemapVertices(vertRemap, newIndices);
        }

        /// <summary>
        /// For mesh g, create a new mesh from the passed selected faces,
        /// discarding un-referenced data and generating new index, vertex & face buffers.
        /// </summary>
        public static IGeometryAttributes SelectFaces(this IGeometryAttributes g, IArray<int> faces)
        {
            // Early exit, if all selected no need to do anything
            if (g.NumFaces == faces.Count)
                return g;

            // Early exit, if none selected no need to do anything
            if (faces.Count == 0)
                return null;

            // First, get all the indices for this array of faces
            var oldIndices = g.GetAttributeIndex();
            var oldSelIndices = new int[faces.Count * g.NumCornersPerFace];
            // var oldIndices = faces.SelectMany(f => f.Indices());
            for (int i = 0; i < faces.Count; i++)
            {
                for (int t = 0; t < 3; t++)
                    oldSelIndices[i * 3 + t] = oldIndices.Data[faces[i] * g.NumCornersPerFace + t];
            }

            // We need to create list of newIndices, and remapping
            // of oldVertices to newVertices
            var newIndices = (-1).Repeat(oldSelIndices.Length).ToArray();
            // Each index could potentially be in the new mesh
            var indexLookup = (-1).Repeat(oldIndices.ElementCount).ToArray();

            // Build mapping.  For each index, if the vertex it has
            // already been referred to has been mapped, use the 
            // mapped index.  Otherwise, remember that we need this vertex
            var numUsedVertices = 0;
            // remapping from old vert array => new vert array
            // Cache built of the old indices of vertices that are used
            // should be equivalent to indexLookup.Where(i => i != -1)
            var oldVertices = g.GetAttributePosition();
            var usedVertices = (-1).Repeat(oldVertices.ElementCount).ToArray();
            for (var i = 0; i < oldSelIndices.Length; ++i)
            {
                var oldIndex = oldSelIndices[i];
                var newIndex = indexLookup[oldIndex];
                if (newIndex < 0)
                {
                    // remapping from old vert array => new vert array
                    usedVertices[numUsedVertices] = oldIndex;
                    newIndex = indexLookup[oldIndex] = numUsedVertices++;
                }
                newIndices[i] = newIndex;
            }

            //var faceRemapping = faces.Select(f => f.Index);
            var vertRemapping = usedVertices.Take(numUsedVertices).ToIArray();
            var cornerRemapping = g.FaceIndicesToCornerIndices(faces);

            return g.VertexAttributes()
                .Select(attr => attr.Remap(vertRemapping))
                .Concat(g.NoneAttributes())
                .Concat(g.FaceAttributes().Select(attr => attr.Remap(faces)))
                .Concat(g.EdgeAttributes().Select(attr => attr.Remap(cornerRemapping)))
                .Concat(g.CornerAttributes().Select(attr => attr.Remap(cornerRemapping)))
                .Concat(g.WholeGeometryAttributes())
                .ToGeometryAttributes()
                .SetAttribute(newIndices.ToIndexAttribute());
        }

        public static G3D ToG3d(this IEnumerable<GeometryAttribute> attributes, G3dHeader? header = null)
            => new G3D(attributes, header);

        public static G3D ToG3d(this IArray<GeometryAttribute> attributes, G3dHeader? header = null)
            => attributes.ToEnumerable().ToG3d(header);

        public static IArray<int> IndexFlippedRemapping(this IGeometryAttributes g)
            => g.NumCorners.Select(c => ((c / g.NumCornersPerFace) + 1) * g.NumCornersPerFace - 1 - c % g.NumCornersPerFace);

        public static bool IsNormalAttribute(this GeometryAttribute attr)
            => attr.IsType<Vector3>() && attr.Descriptor.Semantic == "normal";

        public static IEnumerable<GeometryAttribute> FlipNormalAttributes(this IEnumerable<GeometryAttribute> self)
            => self.Select(attr => attr.IsNormalAttribute()
                ? attr.AsType<Vector3>().Data.Select(v => v.Inverse()).ToAttribute(attr.Descriptor)
                : attr);

        public static IGeometryAttributes FlipWindingOrder(this IGeometryAttributes g)
            => g.VertexAttributes()
                .Concat(g.NoneAttributes())
                .Concat(g.FaceAttributes())
                .Concat(g.EdgeAttributes().Select(attr => attr.Remap(g.IndexFlippedRemapping())))
                .Concat(g.CornerAttributes().Select(attr => attr.Remap(g.IndexFlippedRemapping())))
                .Concat(g.WholeGeometryAttributes())
                .FlipNormalAttributes()
                .ToGeometryAttributes();

        public static IGeometryAttributes DoubleSided(this IGeometryAttributes g)
            => g.Merge(g.FlipWindingOrder());
    }
}
