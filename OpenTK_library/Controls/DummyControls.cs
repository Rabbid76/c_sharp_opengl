using OpenTK; // Vector2, Vector3, Vector4, Matrix4

namespace OpenTK_library.Controls
{
    public class DummyControls
        : IControls
    {
        public DummyControls()
        { }

        public (Matrix4 matrix, bool changed) Update()
        {
            return (matrix: Matrix4.Identity, changed: false);
        }

        public void Start(int mode, Vector2 cursor_pos)
        { }

        public void End(int mode, Vector2 cursor_pos)
        { }

        public void MoveCursorTo(Vector2 cursor_pos)
        { }

        public void Move(Vector3 move_vec)
        { }

        public void MoveWheel(Vector2 cursor_pos, float delta)
        { }
    }
}

