using System;
using System.ComponentModel;
using System.Windows.Forms.Integration;
using System.Diagnostics;
using System.Collections.Generic;
using OpenTK_assimp_example_1.View;
using OpenTK_assimp_example_1.Model;
using OpenTK;                  // GLControl
using OpenTK.Input;            // KeyboardState, Keyboard, Key
using OpenTK.Graphics;         // GraphicsMode, Context

namespace OpenTK_assimp_example_1.ViewModel
{
    public class Model
        : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected internal void OnPropertyChanged(string propertyname)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
        }

        private string _text;
        private string _number;

        public Model()
        { }
        public Model(string text, string number)
        {
            this._text = text;
            this._number = number;
        }

        public string ModelText
        {
            get { return this._text; }
            set
            {
                this._text = value;
                OnPropertyChanged(nameof(ModelText));
            }
        }

        public string ModelNumber
        {
            get { return _number; }
            set
            {
                _number = value;
                OnPropertyChanged(nameof(ModelNumber));
            }
        }
    }

    public class Controls
        : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected internal void OnPropertyChanged(string propertyname)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
        }

        private string _text;
        private string _number;

        public Controls()
        { }
        public Controls(string text, string number)
        {
            this._text = text;
            this._number = number;
        }

        public string ControlsText
        {
            get { return this._text; }
            set
            {
                this._text = value;
                OnPropertyChanged(nameof(ControlsText));
            }
        }

        public string ControlsNumber
        {
            get { return _number; }
            set
            {
                _number = value;
                OnPropertyChanged(nameof(ControlsNumber));
            }
        }
    }

    public class OpenTK_ViewModel
        : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private List<Model> _models = new List<Model>();
        public List<Model> Models
        {
            get { return _models; }
            set
            {
                _models = value;
                OnPropertyChanged(nameof(Models));
            }
        }
        private Model _currentModel;
        public Model CurrentModel
        {
            get { return _currentModel; }
            set
            {
                _currentModel = value;
                OnPropertyChanged(nameof(CurrentModel));
            }
        }

        private List<Controls> _controls = new List<Controls>();
        public List<Controls> Controls
        {
            get { return _controls; }
            set
            {
                _controls = value;
                OnPropertyChanged(nameof(Controls));
            }
        }
        private Controls _currentControl;
        public Controls CurrentControl
        {
            get { return _currentControl; }
            set
            {
                _currentControl = value;
                OnPropertyChanged(nameof(CurrentControl));
            }
        }

        WindowsFormsHost _formsHost;
        GLControl _glc;
        private OpenTK_AssimpModel _gl_model = new OpenTK_AssimpModel();
        private int _cx = 0;
        private int _cy = 0;
        private Stopwatch _stopWatch = new Stopwatch();

        public OpenTK_ViewModel()
        {
            _gl_model.ViewModel = this;
            Models = _gl_model.ModelsData();
            Controls = _gl_model.ControlsData();
            CurrentModel = Models[0];
            CurrentControl = Controls[0];
        }

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
                _gl_model.MouseDown(wnd_pos, e.Button == System.Windows.Forms.MouseButtons.Left);
        }

        protected void GLC_OnMouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Vector2 wnd_pos = new Vector2((float)e.X, (float)(this._cy - e.Y));
            if (_gl_model != null)
                _gl_model.MouseUp(wnd_pos, e.Button == System.Windows.Forms.MouseButtons.Left);
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

