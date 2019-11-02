
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

using System;
using System.Collections.Generic;

namespace OpenTK_example_1
{
    public class Game
        : GameWindow
    {
        private bool _disposedValue = false;

        private GL_Version _version = new GL_Version();
        private GL_Extensions _extensions = new GL_Extensions();
        private GL_DebugCallback _debug_callback = new GL_DebugCallback();

        private GL_VertexArrayObject<float, uint> _test_vao;
        private GL_Program _test_prog;

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
              -0.5f, -0.5f, 0.0f,  1.0f, 0.0f, 0.0f, 1.0f, 
               0.5f, -0.5f, 0.0f,  1.0f, 1.0f, 0.0f, 1.0f,
               0.5f,  0.5f, 0.0f,  0.0f, 1.0f, 0.0f, 1.0f,
              -0.5f,  0.5f, 0.0f,  0.0f, 0.0f, 1.0f, 1.0f
            };

            uint [] iquad = { 0, 1, 2, 0, 2, 3 };

            GL_TVertexFormat[] format = {
                new GL_TVertexFormat(0, 0, 3, 0, false),
                new GL_TVertexFormat(0, 1, 4, 3, false),
            };

            _test_vao = new GL_VertexArrayObject<float, uint>();
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

            this._test_prog = new GL_Program(vert_shader, frag_shader);
            this._test_prog.Generate();

            this._test_prog.Use();

            // states

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            base.OnLoad(e);
        }

        //! On resize
        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, this.Width,this.Height);
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

            _test_vao.Draw();
            
            Context.SwapBuffers();
            base.OnUpdateFrame(e);
        }
    }
}
