using OpenTK;
using OpenTK.Input;            // KeyboardState, Keyboard, Key
using OpenTK.Graphics;         // GameWindow, GraphicsMode, Context
using OpenTK.Graphics.OpenGL4; // GL

using OpenTK_library;
using OpenTK_library.OpenGL;

using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

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

        public DrawTexture(int width, int height, string title)
            : base(width, height, GraphicsMode.Default, title,
                GameWindowFlags.Default,
                DisplayDevice.Default,
                4,
                6,
                GraphicsContextFlags.Default | GraphicsContextFlags.Debug)
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
        protected override void OnLoad(EventArgs e)
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
            //string[] names = assembly.GetManifestResourceNames();
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

            base.OnLoad(e);
        }

        //! On resize
        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, this.Width, this.Height);
            base.OnResize(e);
        }

        //! On update window
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            KeyboardState input = Keyboard.GetState();
            if (input.IsKeyDown(Key.Escape))
            {
                Exit();
            }


            GL.Clear(ClearBufferMask.ColorBufferBit);

            _test_texture.Bind(7);
            _test_vao.Draw();

            Context.SwapBuffers();
            base.OnUpdateFrame(e);
        }
    }
}
