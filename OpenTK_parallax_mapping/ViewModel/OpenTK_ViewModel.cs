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

        private OpenTK_View _form;
        private GLWpfControlEx _glc;
        private GLWpfControlViewModelEx _glc_vm;
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
                _glc = _form.OpenTkControl;
                _glc_vm = new GLWpfControlViewModelEx(_glc, _gl_model);
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
