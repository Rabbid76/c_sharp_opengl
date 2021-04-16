using System.ComponentModel;
using OpenTK_compute_raytracing.View;
using OpenTK_compute_raytracing.Model;
using OpenTK_libray_viewmodel.Control;
using OpenTK.Wpf;

/// <summary>
/// [opentk/GLWpfControl](https://github.com/opentk/GLWpfControl)
/// </summary>

namespace OpenTK_compute_raytracing.ViewModel
{
    public class RayTracing_ViewModel
        : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private RayTracingView _form;
        private GLWpfControl _glc;
        private GLWpfControlViewModel _glc_vm;
        private RayTracing_Model _gl_model = new RayTracing_Model();

        public int DefaultFramebuffer => _glc.Framebuffer;

        public RayTracing_ViewModel()
        {
            _gl_model.ViewModel = this;
        }

        public RayTracingView Form
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
