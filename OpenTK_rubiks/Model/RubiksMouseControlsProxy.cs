using OpenTK.Mathematics;
using OpenTK_library.Controls;

namespace OpenTK_rubiks.Model
{
    public enum TMode { roatate, change };

    public class RubiksMouseControlsProxy
        : IControls
    {
        private ModelSpinningControls _controls;
        Vector2 _wnd_cursor_pos = Vector2.Zero;

        /// <summary>manipulation mode (rotate or change)</summary>
        private TMode _mode = TMode.roatate;
        /// <summary>cube was hit</summary>
        private bool _hit = false;

        public TMode Mode => _mode;

        public bool Hit 
        {
            get => _hit;
            set => _hit = value;
        }

        public Vector2 WndCursorPos => _wnd_cursor_pos;

        public RubiksMouseControlsProxy(ModelSpinningControls controls) => _controls = controls;

        private bool IsLeft(int mode) => mode == 0;

        public void Start(int button_mode, Vector2 cursor_pos)
        {
            if (IsLeft(button_mode))
            {
                if (_mode == TMode.roatate)
                    _controls.Start(0, cursor_pos);
                else
                    _hit = true;
            }
        }

        public void End(int button_mode, Vector2 cursor_pos)
        {
            if (IsLeft(button_mode))
            {
                if (_mode == TMode.roatate)
                    this._controls.End(0, cursor_pos);
            }
            else
            {
                this._controls.ToogleRotate();
                _mode = this._controls.AutoRotate ? TMode.roatate : TMode.change;
            }
        }

        public void MoveCursorTo(Vector2 cursor_pos)
        {
            _wnd_cursor_pos = cursor_pos;
            this._controls.UpdatePosition(cursor_pos);
        }

        public void MoveWheel(Vector2 cursor_pos, float delta) => _controls.MoveWheel(cursor_pos, delta);

        public void Move(Vector3 move_vec) => _controls.Move(move_vec);

        public (Matrix4 matrix, bool changed) Update() => _controls.Update();
    }
}
