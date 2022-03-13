using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using OpenTK.Wpf;
using OpenTK_libray_viewmodel.Model;
using OpenTK.Mathematics;

/// <summary>
/// [opentk/GLWpfControl](https://github.com/opentk/GLWpfControl)
/// </summary>

namespace OpenTK_libray_viewmodel.Control
{
    public class GLWpfControlViewModel
    {
        //private GraphicsContext _context;
        //private IWindowInfo _windowInfo;
        private GLWpfControl _glc;
        private IModel _model;
        private double _cx = 0;
        private double _cy = 0;
        private bool _initiliazed = false;
        private bool _disposed = false;
        private Stopwatch _stopWatch = new Stopwatch();

        double Width { get => _cx;  }
        double Height { get => _cy; }

        public GLWpfControlViewModel(GLWpfControl glc, IModel model)
        {
            _glc = glc;
            _model = model;

            Window window = Window.GetWindow(_glc.Parent);
            window.Closing += new CancelEventHandler(GLC_OnDestroy);

            _glc.SizeChanged += new SizeChangedEventHandler(GLC_OnSiceChanged);
            _glc.Render += GLC_OnPaint;
            _glc.MouseDown += new MouseButtonEventHandler(GLC_OnMouseDown);
            _glc.MouseUp += new MouseButtonEventHandler(GLC_OnMouseUp);
            _glc.MouseMove += new MouseEventHandler(GLC_OnMouseMove);
            _glc.MouseWheel += new MouseWheelEventHandler(GLC_OnMouseWheel);
            
            //GraphicsMode mode = new GraphicsMode(32, 24, 8, 8);
            //var gl_ctrl = new GLControl(mode, 4, 6, GraphicsContextFlags.Default | GraphicsContextFlags.Debug);
            //gl_ctrl.CreateControl();
            //this._windowInfo = gl_ctrl.WindowInfo;
            //this._context = new GraphicsContext(mode, this._windowInfo);

            var settings = new GLWpfControlSettings()
            {
                MajorVersion = 4,
                MinorVersion = 6,
                GraphicsContextFlags = OpenTK.Windowing.Common.ContextFlags.Default | OpenTK.Windowing.Common.ContextFlags.Debug,
                // ContextToUse  = _context
                //UseDeviceDpi = false
            };
           
            _glc.Start(settings);
        }

        public int Framebuffer => _glc.Framebuffer;


        protected void GLC_OnDestroy(object sender, EventArgs e)
        {
            if (_disposed)
                return;
            _disposed = true;

            if (this._model != null)
                this._model.Dispose();
        }

        protected void GLC_OnSiceChanged(object sender, SizeChangedEventArgs e)
        {
            // [...]
        }

        protected void GLC_OnPaint(System.TimeSpan timespawn)
        {
            _cx = _glc.FrameBufferWidth;
            _cy = _glc.FrameBufferHeight;

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
