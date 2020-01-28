using OpenTK; // Vector2, Vector3, Vector4, Matrix4

using static System.Math;

namespace OpenTK_library.Mathematics
{
    public static class Operations
    {
        # region Numeric

        public static double Fract(double val) => val - Truncate(val);

        public static double Clamp(double val, double min, double max) => Max(min, Min(min, val));

        #endregion

        #region Angle

        public static double Radians(double val) => val * PI / 180.0;

        public static double Degrees(double val) => val * 180.0 / PI;
        
        #endregion

        #region Matrix
        
        public static Vector3 PerspectiveDivide(Vector4 v) => v.Xyz / v.W;

        public static Vector3 TransformPoint(Vector3 v, Matrix4 m) => PerspectiveDivide(Vector4.Transform(new Vector4(v.X, v.Y, v.Z, 1.0f), m));
        public static Vector3 TransformVector(Vector3 v, Matrix4 m) => Vector4.Transform(new Vector4(v.X, v.Y, v.Z, 0.0f), m).Xyz;

        #endregion

        #region Matrix

        public static Matrix4 Clone(Matrix4 m)
        {
            return new Matrix4(
                m[0, 0], m[0, 1], m[0, 2], m[0, 3],
                m[1, 0], m[1, 1], m[1, 2], m[1, 3],
                m[2, 0], m[2, 1], m[2, 2], m[2, 3],
                m[3, 0], m[3, 1], m[3, 2], m[3, 3]);
        }

        public static Matrix4 CreateRotate(float angle, Vector3 axis)
        {
            if (axis.X == 0.0f && axis.Y == 0.0f && axis.Z == 0.0f)
                return Matrix4.Identity;

            Vector3 norm_axis = axis.Normalized();
            float x = norm_axis.X;
            float y = norm_axis.Y;
            float z = norm_axis.Z;
            float c = (float)Cos(-angle);
            float s = (float)Sin(-angle);

            return new Matrix4(
              x * x * (1.0f - c) + c, x * y * (1.0f - c) - z * s, x * z * (1.0f - c) + y * s, 0.0f,
              y * x * (1.0f - c) + z * s, y * y * (1.0f - c) + c, y * z * (1.0f - c) - x * s, 0.0f,
              z * x * (1.0f - c) - y * s, z * y * (1.0f - c) + x * s, z * z * (1.0f - c) + c, 0.0f,
              0.0f, 0.0f, 0.0f, 1.0f);
        }
        #endregion

        #region EulerAngles
        // [Pitch, yaw, and roll](https://simple.wikipedia.org/wiki/Pitch,_yaw,_and_roll)
        // [Maths - Conversion Quaternion to Euler](https://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/)

        public static float Pitch(Quaternion q)
        {
            const float epsi = 0.0001f;
            float y = 2.0f * (q.Y * q.Z + q.W * q.X);
            float x = q.W * q.W - q.X * q.X - q.Y * q.Y + q.Z * q.Z;

            float pitch = (float)((Abs(q.X) < epsi && Abs(q.Y) < epsi) ? 2.0 * Atan2(q.X, q.W) : Atan2(y, x));
            return pitch;
        }

        public static float Yaw(Quaternion q)
        {
            float yaw = (float)Asin(Clamp(-2.0f * (q.X * q.Z - q.W * q.Y), -1.0f, 1.0f));
            return yaw;
        }

        public static float Roll(Quaternion q)
        {
            float roll = (float)Atan2(2.0f * (q.X * q.Y + q.W * q.Z), q.W * q.W + q.X * q.X - q.Y * q.Y - q.Z * q.Z);
            return roll;
        }

        #endregion
    }
}
