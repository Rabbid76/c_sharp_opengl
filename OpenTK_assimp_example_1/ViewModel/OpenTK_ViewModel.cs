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
using OpenTK_libray_viewmodel.Control;
using OpenTK.Wpf;

namespace OpenTK_assimp_example_1.ViewModel
{
    // TODO $$$ base class Model/Controls
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

        private OpenTK_View _form;
        private WindowsFormsHost _formsHost;
        private GLControl _glc;
        private GLControlViewModel _glc_vm;
        private GLWpfControl _glc2;
        private GLWpfControlViewModel _glc2_vm;
        private OpenTK_AssimpModel _gl_model = new OpenTK_AssimpModel();
        
        public OpenTK_ViewModel()
        {
            _gl_model.ViewModel = this;

            // [freakinpenguin/OpenTK - WPF](https://github.com/freakinpenguin/OpenTK-WPF)

            try
            {
                Controls = _gl_model.ControlsData();
                CurrentControl = Controls[1];
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

                //_glc2 = _form.OpenTkControl;
                //_glc2_vm = new GLWpfControlViewModel(_glc2, _gl_model);
            }
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
                    _glc_vm = new GLControlViewModel(_glc, _gl_model);
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
    }
}

