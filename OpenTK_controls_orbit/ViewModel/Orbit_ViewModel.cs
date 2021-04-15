﻿using System.ComponentModel;
using OpenTK_controls_orbit.View;
using OpenTK_controls_orbit.Model;
using OpenTK_libray_viewmodel.Control;
using OpenTK.Wpf;

/// <summary>
/// See [Integrating WPF and Microsoft Kinect SDK with OpenTK](http://igordcard.blogspot.com/2011/12/integrating-wpf-and-kinect-with-opentk.html)
/// </summary>

namespace OpenTK_controls_orbit.ViewModel
{
    class Orbit_ViewModel
        : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private OpenTK_View _form;
        private GLWpfControl _glc;
        private GLWpfControlViewModel _glc_vm;
        private Orbit_Model _gl_model = new Orbit_Model();

        public Orbit_ViewModel()
        { }

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
