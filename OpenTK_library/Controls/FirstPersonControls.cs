using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK; // Vector2, Vector3, Vector4, Matrix4

namespace OpenTK_library.Controls
{
    public class FirstPersonControls
        : BaseControls
    {
        GetViewRect _get_view_rect;
        GetMatrix _get_view_mat;
        NavigationMode _mode = NavigationMode.OFF;
        Vector2 _rotate_start = new Vector2(0, 0);

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

        public FirstPersonControls(GetViewRect view_rect, GetMatrix view)
        {
            this._get_view_rect = view_rect;
            this._get_view_mat = view;
        }

        public void StartRotate(Vector2 cursor_pos, NavigationMode mode = NavigationMode.ROTATE)
        {
            this._mode = mode;
            this._rotate_start = new Vector2(cursor_pos.X, cursor_pos.Y);
        }

        public void EndRotate(Vector2 cursor_pos)
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
            Matrix4 rot_mat = CreateRotate(angle, axis);
            Matrix4 rot_pivot = Matrix4.CreateTranslation(-pivot) * rot_mat * Matrix4.CreateTranslation(pivot); // OpenTK `*`-operator is reversed

            return rot_pivot;
        }

        public (Matrix4 matrix, bool changed) MoveCursorTo(Vector2 cursor_pos)
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
                if (Vector2.Distance(orbit_vec_x, new Vector2(0, 0)) > 0.5)
                {
                    Vector2 orbit_dir_up = new Vector2(1, 0);
                    Vector3 axis_up = new Vector3(0, 0, 1);
                    rot_pivot_up = CreateRotate(pivot_world, axis_up, orbit_dir_up, orbit_vec_up);
                }

                // transform and update view matrix
                mat_view = rot_pivot_up * mat_view * rot_pivot_x;  // OpenTK `*`-operator is reversed
                view_changed = true;
            }

            // return new view matrix
            return (matrix: mat_view, changed: view_changed);
        }

        public (Matrix4 matrix, bool changed) Move(Vector3 move_vec)
        {
            bool view_changed = false;

            // get view matrix
            (Matrix4 mat_view, Matrix4 inv_view) = view;

            view_changed = true;
            Matrix4 trans_mat = Matrix4.CreateTranslation(new Vector3(-move_vec.X, move_vec.Z, move_vec.Y));
            mat_view = mat_view * trans_mat; // OpenTK `*`-operator is reversed

            // return new view matrix
            return (matrix: mat_view, changed: view_changed);
        }
    }
}
