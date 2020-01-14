using System;
using System.ComponentModel;
using System.Windows.Forms.Integration;
using System.Diagnostics;
using OpenTK_compute_raytracing.View;
using OpenTK_compute_raytracing.Model;
using OpenTK;                  // GLControl
using OpenTK.Graphics;         // GraphicsMode, Context
using OpenTK_libray_viewmodel.Control;

namespace OpenTK_compute_raytracing.ViewModel
{
    class RayTracing_ViewModel
        : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private WindowsFormsHost _formsHost;
        private GLControl _glc;
        private GLControlViewModel _glc_vm;
        private RayTracing_Model _gl_model = new RayTracing_Model();

        public RayTracing_ViewModel()
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
                    GraphicsMode mode = new GraphicsMode(32, 24, 8, 0);
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
