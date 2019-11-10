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
    }
}
