﻿// MIT License
// Copyright (C) 2019 VIMaec LLC.
// Copyright (C) 2019 Ara 3D. Inc
// https://ara3d.com

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Vim.Math3d
{
    public partial struct Vector4 : ITransformable3D<Vector4>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4(Vector3 v, float w)
            : this(v.X, v.Y, v.Z, w)
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4(Vector2 v, float z, float w)
            : this(v.X, v.Y, z, w)
        { }

        /// <summary>
        /// Transforms a vector by the given matrix.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4 Transform(Matrix4x4 matrix)
            => new Vector4(
                X * matrix.M11 + Y * matrix.M21 + Z * matrix.M31 + W * matrix.M41,
                X * matrix.M12 + Y * matrix.M22 + Z * matrix.M32 + W * matrix.M42,
                X * matrix.M13 + Y * matrix.M23 + Z * matrix.M33 + W * matrix.M43,
                X * matrix.M14 + Y * matrix.M24 + Z * matrix.M34 + W * matrix.M44);

        public Vector3 XYZ => new Vector3(X, Y, Z);
        public Vector2 XY => new Vector2(X, Y);
    }

    public partial struct Vector3 : ITransformable3D<Vector3>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3(float x, float y)
            : this(x, y, 0)
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3(Vector2 xy, float z)
            : this(xy.X, xy.Y, z)
        { }
    
        /// <summary>
        /// Transforms a vector by the given matrix.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 Transform(Matrix4x4 matrix)
            => new Vector3(
                X * matrix.M11 + Y * matrix.M21 + Z * matrix.M31 + matrix.M41,
                X * matrix.M12 + Y * matrix.M22 + Z * matrix.M32 + matrix.M42,
                X * matrix.M13 + Y * matrix.M23 + Z * matrix.M33 + matrix.M43);

        /// <summary>
        /// Computes the cross product of two vectors.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 Cross(Vector3 vector2)
            => new Vector3(
                Y * vector2.Z - Z * vector2.Y,
                Z * vector2.X - X * vector2.Z,
                X * vector2.Y - Y * vector2.X);

        /// <summary>
        /// Returns the mixed product
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double MixedProduct(Vector3 v1, Vector3 v2)
            => Cross(v1).Dot(v2);

        /// <summary>
        /// Returns the reflection of a vector off a surface that has the specified normal.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 Reflect(Vector3 normal)
            => this - normal * Dot(normal) * 2f;

        /// <summary>
        /// Transforms a vector normal by the given matrix.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 TransformNormal(Matrix4x4 matrix)
            => new Vector3(
                X * matrix.M11 + Y * matrix.M21 + Z * matrix.M31,
                X * matrix.M12 + Y * matrix.M22 + Z * matrix.M32,
                X * matrix.M13 + Y * matrix.M23 + Z * matrix.M33);

        public Vector2 XY => new Vector2(X, Y);

        public Vector2 XZ => new Vector2(X, Z);

        public Vector2 YZ => new Vector2(Y, Z);

        public Vector3 XZY => new Vector3(X, Z, Y);

        public Vector3 ZXY => new Vector3(Z, X, Y);

        public Vector3 ZYX => new Vector3(Z, Y, Z);

        public Vector3 YXZ => new Vector3(Y, X, Z);

        public Vector3 YZX => new Vector3(Y, Z, X);
    }

    public partial struct Line : ITransformable3D<Line>, IPoints, IMappable<Line, Vector3>
    {
        public Vector3 Vector => B - A;
        public Ray Ray => new Ray(A, Vector);
        public float Length => A.Distance(B);
        public float LengthSquared => A.DistanceSquared(B);
        public Vector3 MidPoint => A.Average(B);
        public Line Normal => new Line(A, A + Vector.Normalize());
        public Line Inverse => new Line(B, A);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 Lerp(float amount) 
            => A.Lerp(B, amount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Line SetLength(float length) 
            => new Line(A, A + Vector.Along(length));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Line Transform(Matrix4x4 mat) 
            => new Line(A.Transform(mat), B.Transform(mat));

        public int NumPoints => 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetPoint(int n) => n == 0 ? A : B;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Line Map(Func<Vector3, Vector3> f) 
            => new Line(f(A), f(B));
    }

    public partial struct Vector2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double PointCrossProduct(Vector2 other) => X * other.Y - other.X * Y;

        /// <summary>
        /// Computes the cross product of two vectors.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Cross(Vector2 v2) => X * v2.Y - Y * v2.X;
    }

    public partial struct Line2D
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AABox2D BoundingBox() => AABox2D.Create(A, B);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double LinePointCrossProduct(Vector2 point)
        {
            var tmpLine = new Line2D(Vector2.Zero, B - A);
            var tmpPoint = point - A;
            return tmpLine.B.PointCrossProduct(tmpPoint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPointOnLine(Vector2 point)
            => Math.Abs(LinePointCrossProduct(point)) < Math3d.Constants.Tolerance;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPointRightOfLine(Vector2 point)
            => LinePointCrossProduct(point) < 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TouchesOrCrosses(Line2D other)
            => IsPointOnLine(other.A)
               || IsPointOnLine(other.B)
               || (IsPointRightOfLine(other.A) ^ IsPointRightOfLine(other.B));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(AABox2D thisBox, Line2D otherLine, AABox2D otherBox)
            => thisBox.Intersects(otherBox)
               && TouchesOrCrosses(otherLine)
               && otherLine.TouchesOrCrosses(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(Line2D other)
        {
            // Inspired by: https://martin-thoma.com/how-to-check-if-two-line-segments-intersect/
            return Intersects(BoundingBox(), other, other.BoundingBox());
        }
    }

    public partial struct Quad : ITransformable3D<Quad>, IPoints, IMappable<Quad, Vector3>
    {
        public Quad Transform(Matrix4x4 mat) => Map(x => x.Transform(mat));
        public int NumPoints => 4;
        public Vector3 GetPoint(int n) => n == 0 ? A : n == 1 ? B : n == 2 ? C : D;
        public Quad Map(Func<Vector3, Vector3> f) => new Quad(f(A), f(B), f(C), f(D));
    }

    public partial struct Triangle : ITransformable3D<Triangle>, IPoints, IMappable<Triangle, Vector3>
    {
        public Triangle Transform(Matrix4x4 mat) => Map(x => x.Transform(mat));
        public int NumPoints => 3;
        public Vector3 GetPoint(int n) => n == 0 ? A : n == 1 ? B : C;
        public Triangle Map(Func<Vector3, Vector3> f) => new Triangle(f(A), f(B), f(C));

        public float LengthA => A.Distance(B);
        public float LengthB => B.Distance(C);
        public float LengthC => C.Distance(A);

        public bool HasArea => A != B && B != C && C != A;

        public float Area => (B - A).Cross(C - A).Length() * 0.5f;

        public float Perimeter => LengthA + LengthB + LengthC;
        public Vector3 MidPoint => (A + B + C) / 3f;
        public Vector3 NormalDirection => (B - A).Cross(C - A);
        public Vector3 Normal => NormalDirection.Normalize();
        public Vector3 SafeNormal => NormalDirection.SafeNormalize();
        public AABox BoundingBox => AABox.Create(A, B, C);
        public Sphere BoundingSphere => Sphere.Create(A, B, C);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSliver(float tolerance = Constants.Tolerance)
            => LengthA <= tolerance || LengthB <= tolerance || LengthC <= tolerance;

        public Line Side(int n) => n == 0 ? AB : n == 1 ? BC : CA;
        public Vector3 Binormal => (B-A).SafeNormalize();
        public Vector3 Tangent => (C-A).SafeNormalize();

        public Line AB => new Line(A, B);
        public Line BC => new Line(B, C);
        public Line CA => new Line(C, A);

        public Line BA => AB.Inverse;
        public Line CB => BC.Inverse;
        public Line AC => CA.Inverse;
    }

    public partial struct DVector2
    {
        public Vector2 Vector2
            => new Vector2((float)X, (float)Y);
    }

    public partial struct DVector3
    {
        public Vector3 Vector3
            => new Vector3((float)X, (float)Y, (float)Z);
    }

    public partial struct DVector4
    {
        public Vector4 Vector4
            => new Vector4((float)X, (float)Y, (float) Z, (float)W);
    }

    public partial struct DAABox
    {
        public AABox AABox
            => new AABox(Min.Vector3, Max.Vector3);
    }

    public partial struct DQuaternion
    {
        public Quaternion Quaternion
            => new Quaternion((float)X, (float)Y, (float)Z, (float)W);

        public DVector4 DVector4
            => new DVector4(X, Y, Z, W);
    }
}
