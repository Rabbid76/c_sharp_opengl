﻿using System;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Forms.Integration;
using System.Diagnostics;
using OpenTK;                  // GLControl
using OpenTK.Input;            // KeyboardState, Keyboard, Key
using OpenTK.Graphics;         // GraphicsMode, Context
using OpenTK_rubiks.View;
using OpenTK_rubiks.Model;

namespace OpenTK_rubiks.ViewModel
{
    public class Rubiks_ViewModel
        : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        WindowsFormsHost _formsHost;
        GLControl _glc;
        private Rubiks _gl_model = new Rubiks();
        private int _cx = 0;
        private int _cy = 0;
        private Stopwatch _stopWatch = new Stopwatch();

        public Rubiks_ViewModel()
        { }

        public WindowsFormsHost GLHostControl
        {
            // [Created Bindable WindowsFormsHost, but child update is not being reflected to control](https://stackoverflow.com/questions/11510031/created-bindable-windowsformshost-but-child-update-is-not-being-reflected-to-co)
            // <ContentControl x:Name="host" Margin="10" Grid.ColumnSpan="2" Content="{Binding GLHostControl}" />
            get
            {
                if (_glc == null)
                {
                    // Create the GLControl.
                    GraphicsMode mode = new GraphicsMode(32, 24, 8, 8);
                    _glc = new GLControl(mode, 4, 6, GraphicsContextFlags.Default | GraphicsContextFlags.Debug);

                    // Assign Load and Paint events of GLControl.
                    _glc.Load += new EventHandler(GLC_OnLoad);
                    _glc.HandleDestroyed += new EventHandler(GLC_OnDestroy);
                    _glc.Paint += new System.Windows.Forms.PaintEventHandler(GLC_OnPaint);
                    _glc.MouseDown += GLC_OnMouseDown;
                    _glc.MouseUp += GLC_OnMouseUp;
                    _glc.MouseMove += GLC_OnMouseMove;
                    _glc.MouseWheel += GLC_OnMouseWheel;
                }
                if (_formsHost == null)
                {
                    _formsHost = new WindowsFormsHost();
                    _formsHost.Child = _glc;
                }
                return _formsHost;
            }
        }

        protected internal void OnPropertyChanged(string propertyname)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
        }

        protected void GLC_OnLoad(object sender, EventArgs e)
        {
            _cx = _glc.Width;
            _cy = _glc.Height;
            if (this._gl_model != null)
                this._gl_model.Setup(_cx, _cy);
            _stopWatch.Start();
        }

        protected void GLC_OnDestroy(object sender, EventArgs e)
        {
            if (this._gl_model != null)
                this._gl_model.Dispose();
        }

        protected void GLC_OnPaint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            var span = _stopWatch.Elapsed;
            double app_t = span.TotalMilliseconds / 1000.0;

            _cx = _glc.Width;
            _cy = _glc.Height;
            if (this._gl_model != null)
                this._gl_model.Draw(_cx, _cy, app_t);
            this._glc.SwapBuffers();
            this._glc.Invalidate();
        }

        protected void GLC_OnMouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Vector2 wnd_pos = new Vector2((float)e.X, (float)(this._cy - e.Y));
            if (_gl_model != null)
                _gl_model.MouseDown(wnd_pos, e.Button == System.Windows.Forms.MouseButtons.Left, e.Button == System.Windows.Forms.MouseButtons.Right);
        }

        protected void GLC_OnMouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Vector2 wnd_pos = new Vector2((float)e.X, (float)(this._cy - e.Y));
            if (_gl_model != null)
                _gl_model.MouseUp(wnd_pos, e.Button == System.Windows.Forms.MouseButtons.Left, e.Button == System.Windows.Forms.MouseButtons.Right);
        }

        protected void GLC_OnMouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Vector2 wnd_pos = new Vector2((float)e.X, (float)(this._cy - e.Y));
            if (_gl_model != null)
                _gl_model.MouseMove(wnd_pos);
        }

        protected void GLC_OnMouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Vector2 wnd_pos = new Vector2((float)e.X, (float)(this._cy - e.Y));
            if (_gl_model != null)
                _gl_model.MouseWheel(wnd_pos, e.Delta);
        }
    }
}