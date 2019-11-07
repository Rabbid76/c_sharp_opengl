﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK; // Vector2, Vector3, Vector4, Matrix4

namespace OpenTK_library
{
    public enum NavigationMode { OFF, ORBIT, ROTATE };

    public delegate Matrix4 GetMatrix();
    public delegate float[] GetViewRect();
    public delegate float GetDepthVal(Vector2 cursor_pos);
    public delegate Vector3 GetPivot(Vector2 cursor_pos);

    public class NavigationController
    {
        GetViewRect _get_view_rect;
        GetMatrix _get_view_mat;
        GetMatrix _get_proj_mat;
        GetDepthVal _get_depth_val;
        GetPivot _get_pivot;

        bool _pan = false;
        Vector3 _pan_start = new Vector3(0, 0, 1);
        NavigationMode _orbit = NavigationMode.OFF;
        Vector3 _orbit_start = new Vector3(0, 0, 1);
        Vector3 _pivot_world = new Vector3(0, 0, 0);

        protected (Matrix4 matrix, Matrix4 inverse) projection
        {
            get
            {
                Matrix4 m = this._get_proj_mat();
                return (matrix: m, inverse: m.Inverted());
            }
        }

        protected (Matrix4 matrix, Matrix4 inverse) view
        {
            get
            {
                Matrix4 m = this._get_view_mat();
                return (matrix: m, inverse: m.Inverted());
            }
        }

        protected (Matrix4 matrix, Matrix4 inverse) window
        {
            get
            {
                float[] vp_rect = this.viewport_rect;
                Matrix4 inv_wnd = Matrix4.CreateTranslation(-1, -1, -1);
                inv_wnd = Matrix4.CreateScale(2 / vp_rect[2], 2 / vp_rect[3], 2) * inv_wnd; // OpenTK `*`-operator is reversed!
                inv_wnd = Matrix4.CreateTranslation(vp_rect[0], vp_rect[1], 0) * inv_wnd; // OpenTK `*`-operator is reversed!
                return (matrix: inv_wnd.Inverted(), inverse: inv_wnd);
            }
        }

        protected float[] viewport_rect
        {
            get { return this._get_view_rect(); }
        }

        protected float Depth(Vector2 cursor_pos)
        {
            return this._get_depth_val(cursor_pos);
        }

        protected Vector3 PivotWorld(Vector2 cursor_pos)
        {
            return this._get_pivot(cursor_pos);
        }

        protected Vector2 WindowToNDC(Vector2 window_pos)
        {
            float[] vp_rect = this.viewport_rect;
            return new Vector2(
                (window_pos.X + vp_rect[0]) * 2 / vp_rect[2] - 1,
                (window_pos.Y + vp_rect[1]) * 2 / vp_rect[3] - 1);
        }

        protected Vector3 WindowToNDC(Vector3 window_pos)
        {
            float[] vp_rect = this.viewport_rect;
            return new Vector3(
                (window_pos.X + vp_rect[0]) * 2 / vp_rect[2] - 1,
                (window_pos.Y + vp_rect[1]) * 2 / vp_rect[3] - 1,
                window_pos.Z * 2 - 1);
        }

        protected Vector3 UnProject(Vector3 wnd_pos)
        {
            (Matrix4 mat_proj, Matrix4 inv_proj) = projection;
            (Matrix4 mat_view, Matrix4 inv_view) = view;
            (Matrix4 mat_wnd, Matrix4 inv_wnd) = window;

            //float[] vp_rect = this.viewport_rect;
            //Vector4 pos_h = new Vector4(wnd_pos.X * 2 / (vp_rect[2] - vp_rect[0]) - 1, wnd_pos.Y * 2 / (vp_rect[3] - vp_rect[1]) - 1, wnd_pos.Z * 2 -1, 1.0f);

            Vector4 pos_h = new Vector4(wnd_pos.X, wnd_pos.Y, wnd_pos.Z, 1.0f);
            pos_h = Vector4.Transform(pos_h, inv_wnd);
            pos_h = Vector4.Transform(pos_h, inv_proj);
            pos_h = Vector4.Transform(pos_h, inv_view);

            Vector3 world_pos = new Vector3(pos_h.X / pos_h.W, pos_h.Y / pos_h.W, pos_h.Z / pos_h.W);
            return world_pos;
        }

        public static Matrix4 CreateRotate(float angle, Vector3 axis)
        {
            Vector3 norm_axis = axis.Normalized();
            float x = norm_axis.X;
            float y = norm_axis.Y;
            float z = norm_axis.Z;
            float c = (float)Math.Cos(-angle);
            float s = (float)Math.Sin(-angle);

            return new Matrix4(
              x* x*(1.0f - c) + c,     x* y*(1.0f - c) - z * s, x* z*(1.0f - c) + y * s, 0.0f,
              y* x*(1.0f - c) + z * s, y* y*(1.0f - c) + c,     y* z*(1.0f - c) - x * s, 0.0f,
              z* x*(1.0f - c) - y * s, z* y*(1.0f - c) + x * s, z* z*(1.0f - c) + c,     0.0f,
              0.0f,                    0.0f,                    0.0f,                    1.0f );
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

        public NavigationController(GetViewRect view_rect, GetMatrix view, GetMatrix proj, GetDepthVal depth, GetPivot pivot)
        {
            this._get_view_rect = view_rect;
            this._get_view_mat = view;
            this._get_proj_mat = proj;
            this._get_depth_val = depth;
            this._get_pivot = pivot;
        }

        public void StartPan(Vector2 cursor_pos)
        {
            this._pan = true;
            this._pan_start = new Vector3(cursor_pos.X, cursor_pos.Y, Depth(cursor_pos));
        }

        public void EndPan(Vector2 cursor_pos)
        {
            this._pan = false;
        }

        public void StartOrbit(Vector2 cursor_pos, NavigationMode mode = NavigationMode.ORBIT)
        {
            this._orbit = mode;
            this._orbit_start = new Vector3(cursor_pos.X, cursor_pos.Y, Depth(cursor_pos));
            this._pivot_world = this.PivotWorld(cursor_pos);
        }

        public void EndOrbit(Vector2 cursor_pos)
        {
            this._orbit = NavigationMode.OFF;
        }

        public (Matrix4 matrix, bool changed) MoveOnLineOfSight(Vector2 cursor_pos, float delta)
        {
            // get view, projection and window matrix
            //(Matrix4 mat_proj, Matrix4 inv_proj) = projection;
            (Matrix4 mat_view, Matrix4 inv_view) = view;
            //(Matrix4 mat_wnd, Matrix4 inv_wnd) = window;

            // get world space position on view ray
            Vector3 pt_world = this.UnProject(new Vector3(cursor_pos.X, cursor_pos.Y, 1));

            // get view position
            Vector3 eye = new Vector3(inv_view[3, 0], inv_view[3, 1], inv_view[3, 2]);

            // get "zoom" direction and amount
            Vector3 ray_cursor = (pt_world - eye).Normalized();

            // translate view position and update view matrix
            inv_view = inv_view * Matrix4.CreateTranslation(ray_cursor * delta); // OpenTK `*`-operator is reversed

            // return new view matrix
            return (matrix: inv_view.Inverted(), changed: true);
        }

        public (Matrix4 matrix, bool changed) MoveCursorTo(Vector2 cursor_pos)
        {
            bool view_changed = false;

            // get view matrix
            (Matrix4 mat_view, Matrix4 inv_view) = view;

            if (this._pan)
            {
                // get drag start and end
                Vector3 wnd_from = this._pan_start;
                Vector3 wnd_to = new Vector3(cursor_pos.X, cursor_pos.Y, this._pan_start.Z);
                this._pan_start = wnd_to;
                
                // get calculate drag start and world coordinates
                Vector3 pt_world_from = this.UnProject(wnd_from);
                Vector3 pt_world_to = this.UnProject(wnd_to);

                // calculate drag world translation
                Vector3 world_vec = pt_world_to - pt_world_from;

                // translate view position and update view matrix
                inv_view = inv_view * Matrix4.CreateTranslation(world_vec * -1); // OpenTK `*`-operator is reversed
                mat_view = inv_view.Inverted();
                view_changed = true;
            }
            else if (this._orbit == NavigationMode.ORBIT)
            {
                // get the drag start and end
                Vector3 wnd_from = this._orbit_start;
                Vector3 wnd_to = new Vector3(cursor_pos.X, cursor_pos.Y, this._orbit_start.Z);
                this._orbit_start = wnd_to;

                // calculate the pivot, rotation axis and angle
                Vector3 pivot_w = PivotWorld(cursor_pos);
                Vector3 pivot = new Vector3(Vector4.Transform(new Vector4(pivot_w.X, pivot_w.Y, pivot_w.Z, 1), mat_view));
                Vector3 orbit_dir = wnd_to - wnd_from;

                // calculate the rotation matrix and the rotation around the pivot 
                Vector3 axis = new Vector3(-orbit_dir.Y, orbit_dir.X, 0);
                Vector2 dir = new Vector2(orbit_dir.X, orbit_dir.Y);
                Matrix4 rot_pivot = CreateRotate(pivot, axis, dir, dir);
                
                // transform and update view matrix
                mat_view = mat_view * rot_pivot;  // OpenTK `*`-operator is reversed
                view_changed = true;
            }
            else if (this._orbit == NavigationMode.ROTATE)
            {
                // get the drag start and end
                Vector3 wnd_from = this._orbit_start;
                Vector3 wnd_to = new Vector3(cursor_pos.X, cursor_pos.Y, this._orbit_start.Z);
                this._orbit_start = wnd_to;

                // calculate the pivot, rotation axis and angle
                Vector3 pivot_w = PivotWorld(cursor_pos);
                Vector3 pivot = new Vector3(Vector4.Transform(new Vector4(pivot_w.X, pivot_w.Y, pivot_w.Z, 1), mat_view));
                Vector3 orbit_dir = wnd_to - wnd_from;

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
                    rot_pivot_x = CreateRotate(pivot, axis_x, orbit_dir_x, orbit_vec_x);
                }

                // calculate the rotation matrix around the world space up vector through the pivot
                Matrix4 rot_pivot_up = Matrix4.Identity;
                if (Vector2.Distance(orbit_vec_x, new Vector2(0, 0)) > 0.5)
                {
                    Vector2 orbit_dir_up = new Vector2(1, 0);
                    Vector3 axis_up = new Vector3(0, 0, 1);
                    rot_pivot_up = CreateRotate(pivot_w, axis_up, orbit_dir_up, orbit_vec_up);
                }

                // transform and update view matrix
                mat_view = rot_pivot_up * mat_view * rot_pivot_x;  // OpenTK `*`-operator is reversed
                view_changed = true;
            }

            // return new view matrix
            return (matrix: mat_view, changed: view_changed);
        }
    }
}