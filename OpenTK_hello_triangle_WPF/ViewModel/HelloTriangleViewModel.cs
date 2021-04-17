using OpenTK_hello_triangle_WPF.View;
using OpenTK_hello_triangle_WPF.Model;
using OpenTK.Wpf;
using System;
using System.ComponentModel;
using System.Windows;

namespace OpenTK_hello_triangle_WPF.ViewModel
{
    public class HelloTriangleViewModel
    {
        private bool disposed;
        HelloTriangleView view;

        public HelloTriangleView View
        {
            get => view;
            set
            {
                view = value;
                Initialize();
            }
        }

        public GLWpfControl GLWpfControl => View.gl_control;

        public HelloTriangle Model { get; } = new HelloTriangle();

        void Initialize()
        {
            if (GLWpfControl == null)
                return;

            Window window = Window.GetWindow(View);
            window.Closing += new CancelEventHandler(GLWpfControlOnDestroy);
            GLWpfControl.SizeChanged += new SizeChangedEventHandler(GLWpfControlOnSiceChanged);
            GLWpfControl.Render += GLWpfControlOnRendder;

            GLWpfControl.Start(
                new GLWpfControlSettings()
                {
                    MajorVersion = 4,
                    MinorVersion = 6,
                    GraphicsContextFlags = OpenTK.Windowing.Common.ContextFlags.Default | OpenTK.Windowing.Common.ContextFlags.Debug,
                });

            Model.Create();
        }

        protected void GLWpfControlOnDestroy(object sender, EventArgs e)
        {
            if (disposed)
                return;
            disposed = true;
            Model.Dispose(true);
        }

        protected void GLWpfControlOnSiceChanged(object sender, SizeChangedEventArgs e)
        {
            Model.Resize(e.NewSize);
        }

        protected void GLWpfControlOnRendder(System.TimeSpan timespawn)
        {
            Model.Render();
        }
    }
}
