using System.ComponentModel;
using OpenTK_parallax_relief_mapping.View;
using OpenTK_parallax_relief_mapping.Model;
using OpenTK_libray_viewmodel.Control;
using OpenTK.Wpf;

/// <summary>
/// [opentk/GLWpfControl](https://github.com/opentk/GLWpfControl)
/// </summary>

namespace OpenTK_parallax_relief_mapping.ViewModel
{
    public class OpenTK_ViewModel
        : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private OpenTK_View _form;
        private GLWpfControl _glc;
        private GLWpfControlViewModel _glc_vm;
        private OpenTK_Model _gl_model = new OpenTK_Model();

        public OpenTK_ViewModel()
        {
            _gl_model.ViewModel = this;
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

        private int _height_scale;
        public int HeightScale
        {
            get { return this._height_scale; }
            set { this._height_scale = value; this.OnPropertyChanged("HeightScale"); }
        }
    }
}
