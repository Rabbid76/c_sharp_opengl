using OpenTK.Mathematics;      // Vector2, Vector3, Vector4, Matrix4
using OpenTK.Graphics.OpenGL4; // GL
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK_library.Controls;
using OpenTK_library.OpenGL;
using OpenTK_library.OpenGL.OpenGL4;
using OpenTK_library.Generator;
using System;
using System.Collections.Generic;

namespace OpenTK_compute_conestepmap
{
    public class AppWindow
        : GameWindow
    {
        private IOpenGLObjectFactory openGLFactory = new OpenGLObjectFactory4();
        private bool _disposed = false;
        private int _cx = 0;
        private int _cy = 0;
        private IVersionInformation _version;
        private IExtensionInformation _extensions;
        private IDebugCallback _debug_callback;

        private List<TextureGenerator> _generators;
        private List<IFramebuffer> _fbos;
        private int _image_cx = 512; //1024;
        private int _image_cy = 512; //1024;
        private int _frame = 0;
        double _period = 0;
        private IControls _controls = new DummyControls();

        public IControls GetControls() => _controls;

        public float GetScale() => 1.0f;

        public static AppWindow New(int width, int height)
        {
            GameWindowSettings setting = new GameWindowSettings();
            NativeWindowSettings nativeSettings = new NativeWindowSettings();
            nativeSettings.Size = new OpenTK.Mathematics.Vector2i(width, height);
            nativeSettings.API = ContextAPI.OpenGL;
            return new AppWindow(setting, nativeSettings);
        }

        public AppWindow(GameWindowSettings setting, NativeWindowSettings nativeSettings)
            : base(setting, nativeSettings)
        { }

        public AppWindow(int width, int height, string title)
            : base(
                  new GameWindowSettings()
                  {
                  },
                  new NativeWindowSettings()
                  {
                      Size = new OpenTK.Mathematics.Vector2i(width, height),
                      Title = title,
                      APIVersion = new System.Version(4, 6),
                      API = ContextAPI.OpenGL,
                      NumberOfSamples = 0,
                  })
        { }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !this._disposed)
            {
                foreach (var fbo in _fbos)
                    fbo.Dispose();
                _fbos.Clear();
                foreach (var generrator in _generators)
                    generrator.Dispose();
                _generators.Clear();
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        //! On load window (once)
        protected override void OnLoad()
        {
            _version = openGLFactory.NewVersionInformation(Console.WriteLine);
            _extensions = openGLFactory.NewExtensionInformation();
            _debug_callback = openGLFactory.NewDebugCallback(Console.WriteLine);

            // Version strings
            _version.Retrieve();

            // Get OpenGL extensions
            _extensions.Retrieve();

            // Debug callback
            _debug_callback.Init();

            // create Vertex Array Object, Array Buffer Object and Element Array Buffer Object

            // [...]

            // Create shader program

            // [...]

            // framebuffers

            _fbos = new List<IFramebuffer>();
            _fbos.Add(openGLFactory.NewFramebuffer());
            _fbos[0].Create(_image_cx, _image_cy, IFramebuffer.Kind.texture, IFramebuffer.Format.RGBA_F32, true, false);
            _fbos[0].Clear();
            _fbos.Add(openGLFactory.NewFramebuffer());
            _fbos[1].Create(_image_cx, _image_cy, IFramebuffer.Kind.texture, IFramebuffer.Format.RGBA_F32, true, false);
            _fbos[1].Clear();
            _fbos.Add(openGLFactory.NewFramebuffer());
            _fbos[2].Create(_image_cx, _image_cy, IFramebuffer.Kind.texture, IFramebuffer.Format.RGBA_F32, true, false);
            _fbos[2].Clear();

            // create generators
            this._generators = new List<TextureGenerator>();
            this._generators.Add(new TextureGenerator(openGLFactory, TextureGenerator.TType.texture_test1, new ITexture[] { _fbos[0].Textures[0] }));
            this._generators.Add(new TextureGenerator(openGLFactory, TextureGenerator.TType.heightmap_test1, new ITexture[] { _fbos[1].Textures[0] }));
            this._generators.Add(new TextureGenerator(openGLFactory, TextureGenerator.TType.cone_step_map, new ITexture[] { _fbos[2].Textures[0] }, new ITexture[] { _fbos[1].Textures[0] }));

            foreach (var generator in this._generators)
                generator.GenetateProgram();

            // states

            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
        }

        //! On update window
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (this._disposed)
                return;
            this._period += 0.001; // TODO [...]

            bool resized = this._cx != this.Size.X || this._cy != this.Size.Y;
            if (resized)
            {
                this._cx = this.Size.X;
                this._cy = this.Size.Y;
                GL.Viewport(0, 0, this._cx, this._cy);
            }

            if (_frame < 3)
                this._generators[_frame].Generate();
            this._frame++;


            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Be aware, this won't work if the target framebuffer is a multisampling framebuffer
            List<Vector2> pts = new List<Vector2>();
            int side_len = 0;
            if (this._cx >= this._cy * 1.5)
            {
                side_len = Math.Min(this._cy, this._cx / 3);
                int offset_x = (this._cx - side_len * 3) / 2;
                int offset_y = (this._cy - side_len) / 2;
                pts.Add(new Vector2(offset_x, offset_y));
                pts.Add(new Vector2(offset_x + side_len, offset_y));
                pts.Add(new Vector2(offset_x + side_len * 2, offset_y));
            }
            else if (this._cy >= this._cx * 1.5)
            {
                side_len = Math.Min(this._cx, this._cy / 3);
                int offset_x = (this._cx - side_len) / 2;
                int offset_y = (this._cy - side_len * 3) / 2;
                pts.Add(new Vector2(offset_x, offset_y));
                pts.Add(new Vector2(offset_x, offset_y + side_len));
                pts.Add(new Vector2(offset_x, offset_y + side_len * 2));
            }
            else if (this._cx > this._cy)
            {
                side_len = this._cy / 2;
                pts.Add(new Vector2((this._cx - side_len) / 2, 0));
                pts.Add(new Vector2((this._cx - 2 * side_len) / 2, side_len));
                pts.Add(new Vector2((this._cx - 2 * side_len) / 2 + side_len, side_len));
            }
            else
            {
                side_len = this._cx / 2;
                pts.Add(new Vector2(side_len / 2, (this._cy - 2 * side_len) / 2));
                pts.Add(new Vector2(0, (this._cy - 2 * side_len) / 2 + side_len));
                pts.Add(new Vector2(side_len, (this._cy - 2 * side_len) / 2 + side_len));
            }

            for (int i = 0; i < 3; ++i)
                _fbos[i].Blit(null, (int)pts[i].X, (int)pts[i].Y, side_len, side_len, false);

            Context.SwapBuffers();
            base.OnUpdateFrame(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            // [...]
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            // [...]
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            // [...]
        }
    }
}
