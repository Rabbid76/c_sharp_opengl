
//! Creating a Window
//! [https://opentk.net/learn/chapter1/1-creating-a-window.html]
//!
//! .NET Framework 2.0
//! TOOLS / NuGet / Package Manager Console
//! Install-Package OpenTK

//! Hello Triangle
//! [https://opentk.net/learn/chapter1/2-hello-triangle.html]

using OpenTK.Graphics.OpenGL4; // GL
using OpenTK_library.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;


namespace OpenTK_example_1
{
    public class Game
        : GameWindow
    {
        private bool _disposedValue = false;

        private Version _version = new Version();
        private Extensions _extensions = new Extensions();
        private DebugCallback _debug_callback = new DebugCallback();

        private VertexArrayObject<float, uint> _test_vao;
        private OpenTK_library.OpenGL.Program _test_prog;

        public static Game New(int width, int height)
        {
            GameWindowSettings setting = new GameWindowSettings();
            NativeWindowSettings nativeSettings = new NativeWindowSettings();
            nativeSettings.Size = new OpenTK.Mathematics.Vector2i(width, height);
            nativeSettings.API = ContextAPI.OpenGL;
            return new Game(setting, nativeSettings);
        }

        public Game(GameWindowSettings setting, NativeWindowSettings nativeSettings)
            : base(setting, nativeSettings)
        { }

        public Game(int width, int height, string title)
            : base(
                  new GameWindowSettings()
                  {
                      // IsMultiThreaded
                      // RenderFrequency
                      // UpdateFrequency
                  },
                  new NativeWindowSettings()
                  {
                      Size = new OpenTK.Mathematics.Vector2i(width, height),
                      // Location
                      // WindowBorder
                      // WindowState
                      // StartVisible
                      // StartFocused
                      Title = title,
                      // CurrentMonitor
                      APIVersion = new System.Version(4, 6),
                      // AutoLoadBindings
                      // Flags
                      // Profile
                      API = ContextAPI.OpenGL
                      // IsEventDriven
                      // Icon
                      // SharedContext
                      // IsFullscreen
                      // NumberOfSamples
                  })
            { }

        protected override void Dispose(bool disposing) 
        {
            if (disposing && !this._disposedValue)
            {
                _test_vao.Dispose();
                _test_prog.Dispose();
                this._disposedValue = true;
            }
            base.Dispose(disposing);
        }

        //! On load window (once)
        protected override void OnLoad()
        {
            // Version strings
            _version.Retrieve();

            // Get OpenGL extensions
            _extensions.Retrieve();
            
            // Debug callback
            _debug_callback.Init();
      
            // create Vertex Array Object, Array Buffer Object and Element Array Buffer Object

            float[] vquad = 
            {
            // x      y     z      r     g     b     a
              -0.5f, -0.5f, 0.0f,  1.0f, 0.0f, 0.0f, 1.0f, 
               0.5f, -0.5f, 0.0f,  1.0f, 1.0f, 0.0f, 1.0f,
               0.5f,  0.5f, 0.0f,  0.0f, 1.0f, 0.0f, 1.0f,
              -0.5f,  0.5f, 0.0f,  0.0f, 0.0f, 1.0f, 1.0f
            };

            uint [] iquad = { 0, 1, 2, 0, 2, 3 };

            TVertexFormat[] format = {
                new TVertexFormat(0, 0, 3, 0, false),
                new TVertexFormat(0, 1, 4, 3, false),
            };

            _test_vao = new VertexArrayObject<float, uint>();
            _test_vao.AppendVertexBuffer(0, 7, vquad);
            _test_vao.Create(format, iquad);

            // Create shader program

            string vert_shader = @"#version 460 core
            layout (location = 0) in vec4 a_pos;
            layout (location = 1) in vec4 a_color;
      
            out vec4 v_color;

            void main()
            {
                v_color     = a_color;
                gl_Position = a_pos; 
            }";

            string frag_shader = @"#version 460 core
            out vec4 frag_color;
            in  vec4 v_color;
      
            void main()
            {
                frag_color = v_color; 
            }";

            this._test_prog = OpenTK_library.OpenGL.Program.VertexAndFragmentShaderProgram(vert_shader, frag_shader);
            this._test_prog.Generate();

            this._test_prog.Use();

            // states

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            base.OnLoad();
        }

        //! On update window
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            GL.Viewport(0, 0, this.Size.X, this.Size.Y);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            _test_vao.Draw();
            
            Context.SwapBuffers();
            base.OnUpdateFrame(e);
        }
    }
}
