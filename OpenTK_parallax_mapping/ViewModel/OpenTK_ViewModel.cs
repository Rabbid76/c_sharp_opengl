using System;
using System.ComponentModel;
using System.Windows.Forms.Integration;
using System.Diagnostics;
using OpenTK_parallax_mapping.View;
using OpenTK_parallax_mapping.Model;
using OpenTK;                  // GLControl
using OpenTK.Graphics;         // GraphicsMode, Context
using OpenTK_libray_viewmodel.Control;


namespace OpenTK_parallax_mapping.ViewModel
{
    public class OpenTK_ViewModel
        : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private WindowsFormsHost _formsHost;
        private GLControl _glc;
        private GLControlViewModel _glc_vm;
        private OpenTK_Model _gl_model = new OpenTK_Model();

        public OpenTK_ViewModel()
        {
            _gl_model.ViewModel = this;
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

        private int _height_scale;
        public int HeightScale
        {
            get { return this._height_scale; }
            set { this._height_scale = value; this.OnPropertyChanged("HeightScale"); }
        }
    }
}
