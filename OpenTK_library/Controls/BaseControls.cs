using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK; // Vector2, Vector3, Vector4, Matrix4

namespace OpenTK_library.Controls
{
    public class BaseControls
    {
        public enum NavigationMode { OFF, ORBIT, ROTATE };

        public delegate Matrix4 GetMatrix();
        public delegate float[] GetViewRect();
        public delegate float GetDepthVal(Vector2 cursor_pos);
        public delegate Vector3 GetPivot(Vector2 cursor_pos);
        public delegate double GetTime();

        public static Matrix4 CreateRotate(float angle, Vector3 axis)
        {
            if (axis.X == 0.0f && axis.Y == 0.0f && axis.Z == 0.0f)
                return Matrix4.Identity;

            Vector3 norm_axis = axis.Normalized();
            float x = norm_axis.X;
            float y = norm_axis.Y;
            float z = norm_axis.Z;
            float c = (float)Math.Cos(-angle);
            float s = (float)Math.Sin(-angle);

            return new Matrix4(
              x * x * (1.0f - c) + c, x * y * (1.0f - c) - z * s, x * z * (1.0f - c) + y * s, 0.0f,
              y * x * (1.0f - c) + z * s, y * y * (1.0f - c) + c, y * z * (1.0f - c) - x * s, 0.0f,
              z * x * (1.0f - c) - y * s, z * y * (1.0f - c) + x * s, z * z * (1.0f - c) + c, 0.0f,
              0.0f, 0.0f, 0.0f, 1.0f);
        }

        public static float Fract(float value)
        {
            return value - (float)Math.Truncate(value);
        }

        #region EulerAngles
        // [Pitch, yaw, and roll](https://simple.wikipedia.org/wiki/Pitch,_yaw,_and_roll)
        // [Maths - Conversion Quaternion to Euler](https://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/)

        public static float Pitch(Quaternion q)
        {
            const float epsi = 0.0001f;
            float y = 2.0f * (q.Y * q.Z + q.W * q.X);
            float x = q.W * q.W - q.X * q.X - q.Y * q.Y + q.Z * q.Z;
            
            float pitch = (float)((Math.Abs(q.X) < epsi && Math.Abs(q.Y) < epsi) ? 2.0 * Math.Atan2(q.X, q.W) : Math.Atan2(y, x));
            return pitch;
        }

        public static float Yaw(Quaternion q)
        {
            float yaw = (float)Math.Asin(Math.Min(Math.Max(-2.0f * (q.X * q.Z - q.W * q.Y), -1.0f), 1.0f));
            return yaw;
        }

        public static float Roll(Quaternion q)
        {
            float roll = (float)Math.Atan2(2.0f * (q.X * q.Y + q.W * q.Z), q.W * q.W + q.X * q.X - q.Y * q.Y - q.Z * q.Z);
            return roll;
        }

        #endregion
    }
}
