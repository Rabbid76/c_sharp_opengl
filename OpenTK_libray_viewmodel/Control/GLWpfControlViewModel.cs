using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using OpenTK;
using OpenTK.Graphics;         // GraphicsMode, Context
using OpenTK.Wpf;
using OpenTK_libray_viewmodel.Model;

// [jayhf/OpenTkControl](https://github.com/jayhf/OpenTkControl)
// [freakinpenguin/OpenTK-WPF](https://github.com/freakinpenguin/OpenTK-WPF)
// [varon/GLWpfControl](https://github.com/varon/GLWpfControl)

namespace OpenTK_libray_viewmodel.Control
{
    public class GLWpfControlViewModel
    {
        private GLControl _glc_;
        private GLWpfControl _glc;
        private IModel _model;
        private double _cx = 0;
        private double _cy = 0;
        private bool _initiliazed = false;
        private Stopwatch _stopWatch = new Stopwatch();

        double Width { get => _cx;  }
        double Height { get => _cy; }

        public GLWpfControlViewModel(GLWpfControl glc, IModel model)
        {
            _glc = glc;
            _model = model;

            // Assign Load and Paint events of GLControl.

            _glc.Unloaded += new RoutedEventHandler(GLC_OnUnload);

            _glc.Render += GLC_OnPaint;
            _glc.MouseDown += new MouseButtonEventHandler(GLC_OnMouseDown);
            _glc.MouseUp += new MouseButtonEventHandler(GLC_OnMouseUp);
            _glc.MouseMove += new MouseEventHandler(GLC_OnMouseMove);
            _glc.MouseWheel += new MouseWheelEventHandler(GLC_OnMouseWheel);

            //_glc.HandleDestroyed += new EventHandler(GLC_OnDestroy);
            //_glc.Initialized += new EventHandler(GLC_OnLoad);
            //_glc.SizeChanged

            GraphicsMode mode = new GraphicsMode(32, 24, 8, 8);
            _glc_ = new GLControl(mode, 4, 6, GraphicsContextFlags.Default | GraphicsContextFlags.Debug);
            _glc_.CreateControl();
            var info = _glc_.WindowInfo;

            _glc_.HandleDestroyed += new EventHandler(GLC_OnDestroy);

            var settings = new GLWpfControlSettings();
            settings.MajorVersion = 4;
            settings.MinorVersion = 6;
            settings.GraphicsContextFlags = GraphicsContextFlags.Default | GraphicsContextFlags.Debug;
            settings.ContextToUse = new GraphicsContext(mode, info);
            
            _glc.Start(settings);
        }

        /*
        protected void GLC_OnLoad(object sender, EventArgs e)
        {
            _cx = _glc.Width;
            _cy = _glc.Height;
            if (this._model != null)
                this._model.Setup((int)_cx, (int)_cy);
            _stopWatch.Start();
        }
        */

        protected void GLC_OnUnload(object sender, RoutedEventArgs e)
        {
            _model.Dispose();
        }

        protected void GLC_OnDestroy(object sender, EventArgs e)
        {
            if (this._model != null)
                this._model.Dispose();
        }

        protected void GLC_OnPaint(System.TimeSpan timespawn)
        {
            // TODO use timespawn

            _cx = _glc.ActualWidth;
            _cy = _glc.ActualHeight;

            if (_initiliazed == false)
            {
                _initiliazed = true;
                if (this._model != null)
                    this._model.Setup((int)_cx, (int)_cy);
                _stopWatch.Start();
            }

            var span = _stopWatch.Elapsed;
            double app_t = span.TotalMilliseconds / 1000.0;

            if (this._model != null)
                this._model.Draw((int)_cx, (int)_cy, app_t);
        }

        protected void GLC_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var controls = _model.GetControls();
            if (controls == null)
                return;

            var position = e.GetPosition(_glc);
            Vector2 wnd_pos = new Vector2((float)position.X, (float)(this.Height - position.Y));
            int mode = e.ChangedButton == MouseButton.Left ? 0 : 1;
            controls.Start(mode, wnd_pos);
        }

        protected void GLC_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            var controls = _model.GetControls();
            if (controls == null)
                return;

            var position = e.GetPosition(_glc);
            Vector2 wnd_pos = new Vector2((float)position.X, (float)(this.Height - position.Y));
            int mode = e.ChangedButton == MouseButton.Left ? 0 : 1;
            controls.End(mode, wnd_pos);
        }

        protected void GLC_OnMouseMove(object sender, MouseEventArgs e)
        {
            var controls = _model.GetControls();
            if (controls == null)
                return;

            var position = e.GetPosition(_glc);
            Vector2 wnd_pos = new Vector2((float)position.X, (float)(this.Height - position.Y));
            controls.MoveCursorTo(wnd_pos);
        }

        protected void GLC_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var controls = _model.GetControls();
            if (controls == null)
                return;

            var position = e.GetPosition(_glc);
            Vector2 wnd_pos = new Vector2((float)position.X, (float)(this.Height - position.Y));
            float distance = _model.GetScale();
            controls.MoveWheel(wnd_pos, (float)e.Delta * 0.001f * distance);
        }
    }
}
