
//! Creating a Window
//! [https://opentk.net/learn/chapter1/1-creating-a-window.html]
//!
//! .NET Framework 2.0
//! TOOLS / NuGet / Package Manager Console
//! Install-Package OpenTK

//! Hello Triangle
//! [https://opentk.net/learn/chapter1/2-hello-triangle.html]

using OpenTK;
using OpenTK.Input;            // KeyboardState, Keyboard, Key
using OpenTK.Graphics;         // GameWindow, GraphicsMode, Context
using OpenTK.Graphics.OpenGL4; // GL

using OpenTK_library;
using OpenTK_library.OpenGL;

using System;
using System.Collections.Generic;

namespace OpenTK_example_1
{
    public class Game
        : GameWindow
    {
        private bool _disposedValue = false;

        private OpenTK_library.OpenGL.Version _version = new OpenTK_library.OpenGL.Version();
        private Extensions _extensions = new Extensions();
        private DebugCallback _debug_callback = new DebugCallback();

        private VertexArrayObject<float, uint> _test_vao;
        private OpenTK_library.OpenGL.Program _test_prog;

        public Game(int width, int height, string title)
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
            // x      y     z      r     g     b     a
              -1.0f, -1.0f, 0.0f,  1.0f, 0.0f, 0.0f, 1.0f, 
               1.0f, -1.0f, 0.0f,  1.0f, 1.0f, 0.0f, 1.0f,
               1.0f,  1.0f, 0.0f,  0.0f, 1.0f, 0.0f, 1.0f,
              -1.0f,  1.0f, 0.0f,  0.0f, 0.0f, 1.0f, 1.0f
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
            uniform mat4 u_transform;

            void main()
            {
                v_color     = a_color;
                gl_Position = u_transform * a_pos; 
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

            base.OnLoad(e);
        }

        //! On resize
        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, this.Width, this.Height);
            base.OnResize(e);
        }

        private double angle = 0.0;

        //! On update window
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            KeyboardState input = Keyboard.GetState();
            if (input.IsKeyDown(Key.Escape))
            {
                Exit();
            }
            
            GL.Clear(ClearBufferMask.ColorBufferBit);

            int transformLocation = GL.GetUniformLocation(this._test_prog.Object, "u_transform");

            double diagonal = Math.Sqrt(this.Width * this.Width + this.Height * this.Height);
            double dia_angle1 = Math.Atan2(this.Height, this.Width) + angle * Math.PI / 180;
            double dia_angle2 = Math.Atan2(this.Height, -this.Width) + angle * Math.PI / 180;
            double rot_w = Math.Max(Math.Abs(diagonal * Math.Cos(dia_angle1)), Math.Abs(diagonal * Math.Cos(dia_angle2)));
            double rot_h = Math.Max(Math.Abs(diagonal * Math.Sin(dia_angle1)), Math.Abs(diagonal * Math.Sin(dia_angle2)));
            double scale = Math.Min(this.Width / rot_w, this.Height / rot_h);

            Matrix4 transformMatrix =
                Matrix4.CreateScale((float)scale) *
                Matrix4.CreateScale(this.Width, this.Height, 1.0f) *
                Matrix4.CreateRotationZ((float)(angle * Math.PI / 180)) *
                Matrix4.CreateScale(1.0f / this.Width, 1.0f / this.Height, 1.0f);
               
            angle += 1;

            GL.UniformMatrix4(transformLocation, false, ref transformMatrix);

            _test_vao.Draw();
            
            Context.SwapBuffers();
            base.OnUpdateFrame(e);
        }
    }
}
