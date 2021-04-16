using System.ComponentModel;
using OpenTK_rubiks.View;
using OpenTK_rubiks.Model;
using OpenTK_libray_viewmodel.Control;
using OpenTK.Wpf;

/// <summary>
/// [opentk/GLWpfControl](https://github.com/opentk/GLWpfControl)
/// </summary>

namespace OpenTK_rubiks.ViewModel
{
    public class Rubiks_ViewModel
        : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private OpenTK_View _form;
        private GLWpfControl _glc;
        private GLWpfControlViewModel _glc_vm;
        private Rubiks _gl_model = new Rubiks();

        public Rubiks_ViewModel()
        {}

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
