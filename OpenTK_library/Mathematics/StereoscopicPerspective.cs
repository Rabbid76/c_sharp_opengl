using OpenTK; // Vector2, Vector3, Vector4, Matrix4

using static System.Math;

namespace OpenTK_library.Mathematics
{
    public class StereoscopicPerspective
    {
        public enum TSide { Left, Right };

        Matrix4[] _views = { Matrix4.Identity, Matrix4.Identity };
        Matrix4[] _projection = { Matrix4.Identity, Matrix4.Identity };

        public Matrix4 View(TSide side) { return _views[side == TSide.Left ? 0 : 1]; }
        public Matrix4 Projection(TSide side) { return _projection[side == TSide.Left ? 0 : 1]; }

        Matrix4 _original_view = Matrix4.Identity;
        float _fov_y = (float)PI / 3.0f;
        float _aspect = 16.0f / 9.0f;
        float _near = 0.1f;
        float _far = 10.0f;
        float _eye_dist = 0.0f;
        float _fcocal = 1.0f;

        public StereoscopicPerspective(Matrix4 view, float fov_y, float aspect, float near, float far, float eye_dist, float fcocal)
        {
            _original_view = view;
            _fov_y = fov_y;
            _aspect = aspect;
            _near = near;
            _far = far;
            _eye_dist = eye_dist;
            _fcocal = fcocal;

            ComputeEyes();
        }

        private void ComputeEyes()
        {
            _views[0] = Operations.Clone(_original_view);
            _views[1] = Operations.Clone(_original_view);
            _projection[0] = _projection[1] = Matrix4.CreatePerspectiveFieldOfView(_fov_y, _aspect, _near, _far);

            Matrix4 inverse_view = _original_view.Inverted();
            Vector3 line_of_sight = -inverse_view.Row2.Xyz;
            Vector3 x_axis = inverse_view.Row0.Xyz;
            Vector3 origin = inverse_view.Row3.Xyz;
            Vector3 target = origin + line_of_sight * _fcocal;
            float y = _fcocal * (float)Tan(_fov_y / 2.0f);
            float x = y * _aspect;
            float near_scale = _near / _fcocal;
            float top = y * near_scale;
            float bottom = -y * near_scale;

            for (int i = 0; i < 2; ++i)
            {
                bool is_left = i == 0;
                float side_dist = _eye_dist / 2.0f * (is_left ? -1.0f : 1.0f);
                var eye_orgin = new Vector4(origin + x_axis * side_dist, 1.0f);

                Matrix4 eye_orientation = Operations.Clone(inverse_view);
                eye_orientation.Row3 = eye_orgin;
                _views[i] = eye_orientation.Inverted();

                float left = (-x - side_dist) * near_scale;
                float right = (x - side_dist) * near_scale;
                _projection[i] = Matrix4.CreatePerspectiveOffCenter(left, right, bottom, top, _near, _far);
            }
        }
    }
}
