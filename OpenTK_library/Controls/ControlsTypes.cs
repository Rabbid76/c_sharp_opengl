using OpenTK.Mathematics; // Vector2, Vector3, Vector4, Matrix4

namespace OpenTK_library.Controls
{
    public interface IControls 
    {
        // TODO split: Interface Segregation Principle (ISP)
        // TODO int mode -> IMode ?

        void Start(int mode, Vector2 cursor_pos);
        void End(int mode, Vector2 cursor_pos);

        void MoveCursorTo(Vector2 cursor_pos);
        void MoveWheel(Vector2 cursor_pos, float delta);
        void Move(Vector3 move_vec);

        (Matrix4 matrix, bool changed) Update();
    }

    public enum NavigationMode { OFF, ORBIT, ROTATE };

    public delegate void UpdateMatrix(Matrix4 mat);
    public delegate Matrix4 GetMatrix();
    public delegate float[] GetViewRect();
    public delegate float GetDepthVal(Vector2 cursor_pos);
    public delegate Vector3 GetPivot(Vector2 cursor_pos);
    public delegate double GetTime();
}
