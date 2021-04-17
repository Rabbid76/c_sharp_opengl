using System;
using System.Windows.Forms;

namespace OpenTK_hello_triangle_windows_forms
{
    public partial class HelloTriangleForm : Form
    {
        private HelloTriangle model;
        private Timer _timer = null!;

        public HelloTriangleForm()
        {
            InitializeComponent();
        }

        private void HelloTriangleForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            model.Dispose(true);
        }

        private void glControl_Load(object sender, EventArgs e)
        {
            // Make sure that when the GLControl is resized or needs to be painted,
            // we update our projection matrix or re-render its contents, respectively.
            glControl.Resize += glControl_Resize;
            glControl.Paint += glControl_Paint;

           
            model = new HelloTriangle();
            glControl.MakeCurrent();
            model.Create();

            // Ensure that the viewport and projection matrix are set correctly initially.
            glControl_Resize(glControl, EventArgs.Empty);

            // Redraw the screen every 1/20 of a second.
            _timer = new Timer();
            _timer.Tick += (sender, e) => Render();
            _timer.Interval = 10;   // 1000 ms per sec / 10 ms per frame = 100 FPS
            _timer.Start();
        }

        private void glControl_Resize(object? sender, EventArgs e)
        {
            glControl.MakeCurrent();

            if (glControl.ClientSize.Height == 0)
                glControl.ClientSize = new System.Drawing.Size(glControl.ClientSize.Width, 1);

            model.Resize(glControl.ClientSize);
        }

        private void glControl_Paint(object sender, PaintEventArgs e)
        {
            Render();
        }

        private void Render()
        {
            glControl.MakeCurrent();
            model.Render();
            glControl.SwapBuffers();
        }
    }
}
