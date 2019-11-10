using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Input;
using OpenTK_WPF_example_1.View;
using OpenTK_WPF_example_1.Model;
using OpenTK;                  // GLControl
using OpenTK.Input;            // KeyboardState, Keyboard, Key
using OpenTK.Graphics;         // GraphicsMode, Context

/// <summary>
/// See [Integrating WPF and Microsoft Kinect SDK with OpenTK](http://igordcard.blogspot.com/2011/12/integrating-wpf-and-kinect-with-opentk.html)
/// </summary>

namespace OpenTK_WPF_example_1.ViewModel
{
    class OpenTK_ViewModel
        : INotifyPropertyChanged
    {
        public OpenTK_View Formular { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        GLControl _glc;
        private OpenTK_Model _gl_model = new OpenTK_Model();

        public OpenTK_ViewModel()
        {

        }

        public GLControl GLC
        {
            get
            {
                if (_glc == null)
                {
                    // Create the GLControl.
                    _glc = new GLControl(GraphicsMode.Default, 4, 6, GraphicsContextFlags.Default | GraphicsContextFlags.Debug);

                    // Assign Load and Paint events of GLControl.
                    _glc.Load += new EventHandler(glc_Load);
                    _glc.Paint += new System.Windows.Forms.PaintEventHandler(glc_Paint);
                    _glc.HandleDestroyed += new EventHandler(glc_Destroy);
                }
                return _glc;
            }
        }

        protected internal void OnPropertyChanged(string propertyname)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
        }

        public void glc_Load(object sender, EventArgs e)
        {
            int cx = _glc.Width;
            int cy = _glc.Height;
            _gl_model.Setup(cx, cy);
        }

        public void glc_Destroy(object sender, EventArgs e)
        {
            _gl_model.Dispose();
        }

        public void glc_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            int cx = _glc.Width;
            int cy = _glc.Height;
            _gl_model.Draw(cx, cy);
            _glc.SwapBuffers();
        }
    }
}
