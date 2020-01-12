using System;
using System.Diagnostics;
using OpenTK;
using OpenTK_libray_viewmodel.Model;

namespace OpenTK_libray_viewmodel.Control
{
    public class GLControlViewModel
    {
        private GLControl _glc;
        private IModel _model;
        private int _cx = 0;
        private int _cy = 0;
        private Stopwatch _stopWatch = new Stopwatch();

        public GLControlViewModel(GLControl glc, IModel model)
        {
            _glc = glc;
            _model = model;

            // Assign Load and Paint events of GLControl.
            _glc.Load += new EventHandler(GLC_OnLoad);
            _glc.HandleDestroyed += new EventHandler(GLC_OnDestroy);
            _glc.Paint += new System.Windows.Forms.PaintEventHandler(GLC_OnPaint);
            _glc.MouseDown += GLC_OnMouseDown;
            _glc.MouseUp += GLC_OnMouseUp;
            _glc.MouseMove += GLC_OnMouseMove;
            _glc.MouseWheel += GLC_OnMouseWheel;
        }

        protected void GLC_OnLoad(object sender, EventArgs e)
        {
            _cx = _glc.Width;
            _cy = _glc.Height;
            if (this._model != null)
                this._model.Setup(_cx, _cy);
            _stopWatch.Start();
        }

        protected void GLC_OnDestroy(object sender, EventArgs e)
        {
            if (this._model != null)
                this._model.Dispose();
        }

        protected void GLC_OnPaint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            var span = _stopWatch.Elapsed;
            double app_t = span.TotalMilliseconds / 1000.0;

            _cx = _glc.Width;
            _cy = _glc.Height;
            if (this._model != null)
                this._model.Draw(_cx, _cy, app_t);
            this._glc.SwapBuffers();
            this._glc.Invalidate();
        }

        protected void GLC_OnMouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            var controls = _model.GetControls();
            if (controls == null)
                return;
            
            Vector2 wnd_pos = new Vector2((float)e.X, (float)(this._cy - e.Y));
            int mode = e.Button == System.Windows.Forms.MouseButtons.Left ? 0 : 1;
            controls.Start(mode, wnd_pos);
        }

        protected void GLC_OnMouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            var controls = _model.GetControls();
            if (controls == null)
                return;

            Vector2 wnd_pos = new Vector2((float)e.X, (float)(this._cy - e.Y));
            int mode = e.Button == System.Windows.Forms.MouseButtons.Left ? 0 : 1;
            controls.End(mode, wnd_pos);
        }

        protected void GLC_OnMouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            var controls = _model.GetControls();
            if (controls == null)
                return;

            Vector2 wnd_pos = new Vector2((float)e.X, (float)(this._cy - e.Y));
            controls.MoveCursorTo(wnd_pos);
        }

        protected void GLC_OnMouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            var controls = _model.GetControls();
            if (controls == null)
                return;

            Vector2 wnd_pos = new Vector2((float)e.X, (float)(this._cy - e.Y));
            float distance = _model.GetScale();
            controls.MoveWheel(wnd_pos, (float)e.Delta * 0.001f * distance);
        }
    }
}
