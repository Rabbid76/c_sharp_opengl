using System.ComponentModel;
using OpenTK_stereoscopic_example_1.View;
using OpenTK_stereoscopic_example_1.Model;
using OpenTK_libray_viewmodel.Control;
using OpenTK.Wpf;
using WpfViewModelModule;
using System.Collections.Generic;
using System;

/// <summary>
/// [opentk/GLWpfControl](https://github.com/opentk/GLWpfControl)
/// </summary>

namespace OpenTK_stereoscopic_example_1.ViewModel
{
    public class Anaglyphs
        : ComboBoxViewModel
    {
        public Anaglyphs() : base(nameof(Anaglyphs)) { }
        public Anaglyphs(string text, string number) : base(nameof(Anaglyphs), text, number) { }
    }

    public class Model
        : ComboBoxViewModel
    {
        public Model() : base(nameof(Model)) { }
        public Model(string text, string number) : base(nameof(Model), text, number) { }
    }

    public class Controls
        : ComboBoxViewModel
    {
        public Controls() : base(nameof(Controls)) { }
        public Controls(string text, string number) : base(nameof(Controls), text, number) { }
    }

    public class OpenTK_ViewModel
        : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        private List<Anaglyphs> _anaglyphs = new List<Anaglyphs>();
        public List<Anaglyphs> Anaglyphs
        {
            get { return _anaglyphs; }
            set
            {
                _anaglyphs = value;
                OnPropertyChanged(nameof(Anaglyphs));
            }
        }
        private Anaglyphs _currentAnaglyph;
        public Anaglyphs CurrentAnaglyph
        {
            get { return _currentAnaglyph; }
            set
            {
                _currentAnaglyph = value;
                OnPropertyChanged(nameof(CurrentAnaglyph));
            }
        }

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

        private int _eyet_scale;
        public int EyeScale
        {
            get { return this._eyet_scale; }
            set { this._eyet_scale = value; this.OnPropertyChanged("EyeScale"); }
        }

        private int _focal_scale;
        public int FocalScale
        {
            get { return this._focal_scale; }
            set { this._focal_scale = value; this.OnPropertyChanged("FocalScale"); }
        }

        public int DefaultFramebuffer => _glc.Framebuffer;

        private OpenTK_View _form;
        private GLWpfControl _glc;
        private GLWpfControlViewModel _glc_vm;
        private OpenTK_AssimpModel _gl_model = new OpenTK_AssimpModel();
        
        public OpenTK_ViewModel()
        {
            _gl_model.ViewModel = this;

            // [freakinpenguin/OpenTK - WPF](https://github.com/freakinpenguin/OpenTK-WPF)

            try
            {
                Anaglyphs = _gl_model.AnaglyphsData();
                CurrentAnaglyph = Anaglyphs[Anaglyphs.Count > 2 ? 2 : 0];
            }
            catch (Exception ex)
            {
                Console.WriteLine("error determining anaglyphs: " + ex.Message);
            }

            try
            {
                Controls = _gl_model.ControlsData();
                CurrentControl = Controls[0];
            }
            catch (Exception ex)
            {
                Console.WriteLine("error determining controls: " + ex.Message);
            }

            try
            {
                Models = _gl_model.ModelsData();
                CurrentModel = Models[0];
            }
            catch (Exception ex)
            {
                Console.WriteLine("error reading models: " + ex.Message);
            }
        }

        public OpenTK_View Form
        {
            get { return _form; }
            set 
            { 
                _form = value;
                _glc = _form.gl_control;
                _glc_vm = new GLWpfControlViewModel(_glc, _gl_model);
            }
        }

        protected internal void OnPropertyChanged(string propertyname)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
        }
    }
}

