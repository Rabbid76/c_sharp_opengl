﻿using System;
using OpenTK.Mathematics; // Vector2, Vector3, Vector4, Matrix4
using OpenTK_library.Mathematics;

using static OpenTK_library.Mathematics.Operations;

namespace OpenTK_library.Controls
{
    public class FirstPersonControls
        : IControls
    {
        GetViewRect _get_view_rect;
        GetMatrix _get_view_mat;
        UpdateMatrix _set_view_mat;
        NavigationMode _mode = NavigationMode.OFF;
        Vector2 _rotate_start = new Vector2(0, 0);
        bool _view_changed = false;
        Matrix4 _current_view_mat = Matrix4.Identity;

        protected (Matrix4 matrix, Matrix4 inverse) view
        {
            get
            {
                Matrix4 m = this._get_view_mat();
                return (matrix: m, inverse: m.Inverted());
            }
        }

        protected float[] viewport_rect
        {
            get { return this._get_view_rect(); }
        }

        public FirstPersonControls(GetViewRect view_rect, GetMatrix view, UpdateMatrix update)
        {
            this._get_view_rect = view_rect;
            this._get_view_mat = view;
            this._set_view_mat = update;
            _current_view_mat = this._get_view_mat();
        }

        public (Matrix4 matrix, bool changed) Update()
        {
            return (matrix: _current_view_mat, changed: _view_changed);
        }

        public void Start(int mode, Vector2 cursor_pos)
        {
            this._mode = mode == 0 ? NavigationMode.ROTATE : NavigationMode.ORBIT;
            this._rotate_start = new Vector2(cursor_pos.X, cursor_pos.Y);
        }

        public void End(int mode, Vector2 cursor_pos)
        {
            this._mode = NavigationMode.OFF;
        }

        public Matrix4 CreateRotate(Vector3 pivot, Vector3 axis, Vector2 window_dir, Vector2 window_vec)
        {
            // get the viewport rectangle
            float[] vp_rect = this.viewport_rect;

            // Get the rotation axis and angle
            Vector2 dist_vec = new Vector2(window_vec.X / (vp_rect[2] - vp_rect[0]), window_vec.Y / (vp_rect[3] - vp_rect[1]));
            float angle = Vector2.Dot(window_dir.Normalized(), dist_vec) * (float)Math.PI;

            // calculate the rotation matrix and the rotation around the pivot 
            Matrix4 rot_mat = Operations.CreateRotate(angle, axis);
            Matrix4 rot_pivot = Matrix4.CreateTranslation(-pivot) * rot_mat * Matrix4.CreateTranslation(pivot); // OpenTK `*`-operator is reversed

            return rot_pivot;
        }

        public void MoveCursorTo(Vector2 cursor_pos)
        {
            bool view_changed = false;

            // get view matrix
            (Matrix4 mat_view, Matrix4 inv_view) = view;

            if (this._mode == NavigationMode.ROTATE)
            {
                // get the drag start and end
                Vector2 wnd_from = this._rotate_start;
                Vector2 wnd_to = new Vector2(cursor_pos.X, cursor_pos.Y);
                this._rotate_start = wnd_to;

                // calculate the pivot, rotation axis and angle
                Vector3 pivot_world = inv_view.Row3.Xyz;
                Vector3 pivot_view = new Vector3(0, 0, 0);
                Vector2 orbit_dir = wnd_to - wnd_from;

                // get the projection of the up vector to the view port 
                // TODO

                // calculate the rotation components for the rotation around the view space x axis and the world up vector
                Vector2 orbit_vec_x = new Vector2(0, orbit_dir.Y);
                Vector2 orbit_vec_up = new Vector2(orbit_dir.X, 0);

                // calculate the rotation matrix around the view space x axis through the pivot
                Matrix4 rot_pivot_x = Matrix4.Identity;
                if (Vector2.Distance(orbit_vec_x, new Vector2(0, 0)) > 0.5)
                {
                    Vector2 orbit_dir_x = new Vector2(0, 1);
                    Vector3 axis_x = new Vector3(-1, 0, 0);
                    rot_pivot_x = CreateRotate(pivot_view, axis_x, orbit_dir_x, orbit_vec_x);
                }

                // calculate the rotation matrix around the world space up vector through the pivot
                Matrix4 rot_pivot_up = Matrix4.Identity;
                if (Vector2.Distance(orbit_vec_up, new Vector2(0, 0)) > 0.5)
                {
                    Vector2 orbit_dir_up = new Vector2(1, 0);
                    Vector3 axis_up = new Vector3(0, 0, 1);
                    rot_pivot_up = CreateRotate(pivot_world, axis_up, orbit_dir_up, orbit_vec_up);
                }

                // transform and update view matrix
                mat_view = rot_pivot_up * mat_view * rot_pivot_x;  // OpenTK `*`-operator is reversed
                view_changed = true;
            }

            _view_changed = view_changed;
            _current_view_mat = mat_view;
            if (_view_changed)
                _set_view_mat(_current_view_mat);
        }

        public void Move(Vector3 move_vec)
        {
            // get view matrix
            (Matrix4 mat_view, Matrix4 inv_view) = view;

            Matrix4 trans_mat = Matrix4.CreateTranslation(new Vector3(-move_vec.X, move_vec.Z, move_vec.Y));
            mat_view = mat_view * trans_mat; // OpenTK `*`-operator is reversed

            _view_changed = true;
            _current_view_mat = mat_view;
            if (_view_changed)
                _set_view_mat(_current_view_mat);
        }

        public void MoveWheel(Vector2 cursor_pos, float delta)
        {
            Move(new Vector3(0.0f, 0.0f, delta));
        }
    }
}
