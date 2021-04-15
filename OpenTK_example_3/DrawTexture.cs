using OpenTK.Graphics.OpenGL4; // GL
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK_library.OpenGL;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace OpenTK_example_3
{
    public class DrawTexture
        : GameWindow
    {
        private bool _disposedValue = false;

        private OpenTK_library.OpenGL.Version _version = new OpenTK_library.OpenGL.Version();
        private Extensions _extensions = new Extensions();
        private DebugCallback _debug_callback = new DebugCallback();

        private VertexArrayObject<float, uint> _test_vao;
        private OpenTK_library.OpenGL.Program _test_prog;
        private Texture _test_texture;

        public static DrawTexture New(int width, int height)
        {
            GameWindowSettings setting = new GameWindowSettings();
            NativeWindowSettings nativeSettings = new NativeWindowSettings();
            nativeSettings.Size = new OpenTK.Mathematics.Vector2i(width, height);
            nativeSettings.API = ContextAPI.OpenGL;
            return new DrawTexture(setting, nativeSettings);
        }

        public DrawTexture(GameWindowSettings setting, NativeWindowSettings nativeSettings)
            : base(setting, nativeSettings)
        { }

        public DrawTexture(int width, int height, string title)
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
                      NumberOfSamples = 8,
                  })
        { }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !this._disposedValue)
            {
                _test_texture.Dispose();
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
            // x      y     z      u     v    
              -0.5f, -0.5f, 0.0f,  0.0f, 1.0f,
               0.5f, -0.5f, 0.0f,  1.0f, 1.0f,
               0.5f,  0.5f, 0.0f,  1.0f, 0.0f,
              -0.5f,  0.5f, 0.0f,  0.0f, 0.0f
            };

            uint[] iquad = { 0, 1, 2, 0, 2, 3 };

            TVertexFormat[] format = {
                new TVertexFormat(0, 0, 3, 0, false),
                new TVertexFormat(0, 1, 2, 3, false),
            };

            _test_vao = new VertexArrayObject<float, uint>();
            _test_vao.AppendVertexBuffer(0, 5, vquad);
            _test_vao.Create(format, iquad);

            // Create texture

            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] names = assembly.GetManifestResourceNames();
            Stream resource_stream = assembly.GetManifestResourceStream("OpenTK_example_3.Resource.background.jpg");
            
            _test_texture = new Texture();
            _test_texture.Create2D(new Bitmap(resource_stream));

            // Create shader program

            string vert_shader = @"#version 460 core
            layout (location = 0) in vec4 a_pos;
            layout (location = 1) in vec2 a_uv;
      
            out vec2 v_uv;

            void main()
            {
                v_uv        = a_uv;
                gl_Position = a_pos; 
            }";

            string frag_shader = @"#version 460 core
            out vec4 frag_color;
            in  vec2 v_uv;
            layout(binding = 7) uniform sampler2D u_texture; 
      
            void main()
            {
                frag_color = texture(u_texture, v_uv).rgba; 
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

            _test_texture.Bind(7);
            _test_vao.Draw();

            Context.SwapBuffers();
            base.OnUpdateFrame(e);
        }
    }
}
