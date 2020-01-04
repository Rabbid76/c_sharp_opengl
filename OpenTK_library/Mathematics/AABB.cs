using System;
using OpenTK; // Vector2, Vector3, Vector4, Matrix4

using static System.Math;
using static OpenTK_library.Mathematics.Operations;

namespace OpenTK_library.Mathematics
{
    public class AABB
    {
        private bool _valid = false;
        private Vector3 _min = new Vector3();
        private Vector3 _max = new Vector3();

        public AABB()
        { }

        public AABB(Vector3 min, Vector3 max)
        {
            _min = new Vector3(min);
            _max = new Vector3(max);
            _valid = true;
        }

        public bool Valid { get => _valid; }

        public Vector3 Min
        {
            get => _min;
            set { _min = value; UpdateMax(); }
        }

        public Vector3 Max
        {
            get => _max;
            set { _max = value; UpdateMin(); }
        }

        public Vector3 Center { get => (_min + _max) / 2.0f; }

        public Vector3 Size { get => _max - _min; }

        public float Diagonal { get => Size.Length; }

        public static AABB operator | (AABB b, Vector3 v)
        {
            if (b.Valid)
            {
                b._min.X = Min(v.X, b._min.X);
                b._min.Y = Min(v.Y, b._min.Y);
                b._min.Z = Min(v.Z, b._min.Z);
                b._max.X = Max(v.X, b._max.X);
                b._max.Y = Max(v.Y, b._max.Y);
                b._max.Z = Max(v.Z, b._max.Z);
            }
            else
            {
                b._valid = true;
                b._min = new Vector3(v);
                b._max = new Vector3(v);
            }
            return b;
        }

        public static AABB operator | (AABB b1, AABB b2)
        {
            return b1 | b2.Min | b2.Max;
        }

        public AABB Transform(Matrix4 m)
        {
            _min = TransformPoint(_min, m);
            _max = TransformPoint(_max, m);
            SortMinMax();
            return this;
        }

        private (float min, float max) SortMinMax(float a, float b) => a <= b ? (a, b) : (a, b);

        private void SortMinMax()
        {
            (_min.X, _max.X) = SortMinMax(_min.X, _max.X);
            (_min.Y, _max.Y) = SortMinMax(_min.Y, _max.Y);
            (_min.Z, _max.Z) = SortMinMax(_min.Z, _max.Z);
        }

        private void UpdateMax()
        {
            _max.X = Max(_min.X, _max.X);
            _max.Y = Max(_min.Y, _max.Y);
            _max.Z = Max(_min.Z, _max.Z);
        }

        private void UpdateMin()
        {
            _min.X = Min(_min.X, _max.X);
            _min.Y = Min(_min.Y, _max.Y);
            _min.Z = Min(_min.Z, _max.Z);
        }
    }
}
