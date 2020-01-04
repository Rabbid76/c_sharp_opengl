using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK; // Vector2, Vector3, Vector4, Matrix4

using static OpenTK_library.Mathematics.Operations;

namespace OpenTK_library.Controls
{
    public interface IControls 
    {
        // TODO split: Interface Segregation Principle (ISP)
        // TODO int mode -> IMode ?

        void Start(int mode, Vector2 cursor_pos);
        void End(int mode, Vector2 cursor_pos);

        (Matrix4 matrix, bool changed) MoveCursorTo(Vector2 cursor_pos);
        (Matrix4 matrix, bool changed) MoveWheel(Vector2 cursor_pos, float delta);
        (Matrix4 matrix, bool changed) Move(Vector3 move_vec);

        (Matrix4 matrix, bool changed) Update();
    }

    public enum NavigationMode { OFF, ORBIT, ROTATE };

    public delegate Matrix4 GetMatrix();
    public delegate float[] GetViewRect();
    public delegate float GetDepthVal(Vector2 cursor_pos);
    public delegate Vector3 GetPivot(Vector2 cursor_pos);
    public delegate double GetTime();
}
