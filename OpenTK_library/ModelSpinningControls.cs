﻿using System;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK; // Vector2, Vector3, Vector4, Matrix4

namespace OpenTK_library
{
    public class ModelSpinningControls
        : BaseControls
    {
        GetTime _get_time;
        GetViewRect _get_view_rect;
        Matrix4 _orbit_mat = Matrix4.Identity;
        Matrix4 _current_orbit_mat = Matrix4.Identity;
        Matrix4 _model_mat = Matrix4.Identity;
        Matrix4 _current_model_mat = Matrix4.Identity;
        bool _mouse_drag = false;
        bool _auto_spin = false;
        bool _auto_rotate = true;
        Vector2 _mouse_start = new Vector2();
        Vector3 _mouse_drag_axis = new Vector3();
        float _mouse_drag_angle = 0;
        double _mouse_drag_time = 0;
        bool _auto_rotate_mode = true;
        bool _hit = false;
        Vector2 _mouse = new Vector2();
        bool _active = true;
        double _drag_start_T = 0;
        double _rotate_start_T = 0;
        float[] _attenuation = new float[] { 0, 0, 0 };

        public ModelSpinningControls(GetTime get_time, GetViewRect view_rect)
        {
            this._get_time = get_time;
            this._get_view_rect = view_rect;
            this._drag_start_T = this._rotate_start_T = this.time;
        }

        // get the render time in seconds
        private double time { get { return this._get_time(); } }

        protected float[] viewport_rect { get { return this._get_view_rect(); } }

        public Matrix4 orbit { get { return this.orbitMatrix; } }

        public Matrix4 orbitMatrix
        {
            get
            {
                return (this._mouse_drag || (this._auto_rotate && this._auto_spin)) ?
                    (this._current_model_mat * this._current_orbit_mat) : // OpenTK `*`-operator is reversed
                    this._orbit_mat;
            }
        }

        public Matrix4 autoModelMatrix
        {
            get
            {
                return this._auto_rotate ?
                    (this._current_model_mat * this._model_mat) : // OpenTK `*`-operator is reversed
                    this._model_mat;
            }
        }

        public ModelSpinningControls SetAttenuation(float att_const, float att_linear, float att_quad)
        {
            this._attenuation = new float[] { att_const, att_linear, att_quad };
            return this;
        }

        public ModelSpinningControls Update()
        {
            double current_T = this.time;
            this._current_model_mat = Matrix4.Identity;
            if (this._mouse_drag)
            {
                this._current_orbit_mat = CreateRotate(this._mouse_drag_angle, this._mouse_drag_axis);
            }
            else if (this._auto_rotate)
            {
                if (this._auto_spin)
                {
                    if (this._mouse_drag_time > 0)
                    {
                        float angle = this._mouse_drag_angle * (float)((current_T - this._rotate_start_T) / this._mouse_drag_time);
                        if (Math.Abs(this._attenuation[0]) > 0)
                            angle /= this._attenuation[0] + this._attenuation[1] * angle + this._attenuation[2] * angle * angle;
                        this._current_orbit_mat = CreateRotate(angle, this._mouse_drag_axis);
                    }
                }
                else
                {
                    float auto_angle_x = Fract((float)(current_T - this._rotate_start_T) / 13.0f) * 2.0f * (float)Math.PI;
                    float auto_angle_y = Fract((float)(current_T - this._rotate_start_T) / 17.0f) * 2.0f * (float)Math.PI;
                    this._current_model_mat =
                        CreateRotate(auto_angle_y, new Vector3(0, 1, 0)) *
                        CreateRotate(auto_angle_x, new Vector3(1, 0, 0)) *
                        this._current_model_mat; // OpenTK `*`-operator is reversed
                }
            }

            return this;
        }

        private ModelSpinningControls ChangeMotionMode(bool drag, bool spin, bool auto)
        {
            bool new_drag = drag;
            bool new_auto = new_drag ? false : auto;
            bool new_spin = new_auto ? spin : false;
            bool change = this._mouse_drag != new_drag || this._auto_rotate != new_auto || this._auto_spin != new_spin;
            if (change == false)
                return this;
            if (new_drag && this._mouse_drag == false)
            {
                this._drag_start_T = this.time;
                this._mouse_drag_angle = 0;
                this._mouse_drag_time = 0;
            }
            if (new_auto && this._auto_rotate == false)
                this._rotate_start_T = this.time;
            this._mouse_drag = new_drag;
            this._auto_rotate = new_auto;
            this._auto_spin = new_spin;
            this._orbit_mat = this._orbit_mat * this._current_orbit_mat; // OpenTK `*`-operator is reversed
            this._current_orbit_mat = Matrix4.Identity;
            this._model_mat = this._model_mat * this._current_model_mat; // OpenTK `*`-operator is reversed
            this._current_model_mat = Matrix4.Identity;
            return this;
        }

        public ModelSpinningControls MosueDown(Vector2 mouse_pos, bool left)
        {
            this._mouse = mouse_pos;
            this._hit = false;
            if (left)
            {
                if (this._auto_rotate_mode)
                {
                    this._mouse_start = this._mouse;
                    this.ChangeMotionMode(true, false, false);
                }
                else
                {
                    this._hit = true;
                }
            }
            return this;
        }

        public ModelSpinningControls MosueUp(Vector2 mouse_pos, bool left)
        {
            this._mouse = mouse_pos;
            this._hit = false;
            if (left && this._active)
            {
                if (this._auto_rotate_mode)
                {
                    this.ChangeMotionMode(false, true, true);
                }
            }
            else if (left == false)
            {
                //this.ChangeMotionMode( false, false, this._auto_rotate == false );
                //this._auto_rotate_mode = this._auto_rotate;
                this._active = !this._active;
            }
            return this;
        }

        public ModelSpinningControls MosueMove(Vector2 mouse_pos)
        {
            if (this._active == false && this._hit == false)
            {
                return this;
            }
            this._mouse = mouse_pos;
            float[] vp_rect = this.viewport_rect;
            Vector2 dist = Vector2.Subtract(this._mouse, this._mouse_start);
            Vector2 vp_dia = new Vector2(vp_rect[2] - vp_rect[0], vp_rect[3] - vp_rect[1]);
            float dx = dist.X / vp_dia.X;
            float dy = dist.Y / vp_dia.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (this._mouse_drag && len > 0)
            {
                this._mouse_drag_angle = (float)Math.PI * len;
                this._mouse_drag_axis = new Vector3(-dy / len, dx / len, 0);
                this._mouse_drag_time = this.time - this._drag_start_T;
            }
            return this;
        }
    }
}
