using OpenTK;
using OpenTK.Input;            // KeyboardState, Keyboard, Key
using OpenTK.Graphics;         // GameWindow, GraphicsMode, Context
using OpenTK.Graphics.OpenGL4; // GL

using OpenTK_library;

using System;
using System.Collections.Generic;

namespace OpenTK_orbit
{
    public class Orbit
        : GameWindow
    {
        internal unsafe struct TMVP
        {
            public fixed float _model[16];
            public fixed float _view[16];
            public fixed float _projection[16];

            public TMVP(Matrix4 model, Matrix4 view, Matrix4 projetion)
            {
                SetModel(model);
                SetView(view);
                SetProjection(projetion);
            }

            //public IntPtr view
            //{
            //    get { return new IntPtr(this._view); }
            //} 

            public void SetModel(Matrix4 m)
            {
                for (int i = 0; i < 16; ++i)
                    this._model[i] = m[i / 4, i % 4];
            }

            public void SetView(Matrix4 m)
            {
                for (int i = 0; i < 16; ++i)
                    this._view[i] = m[i / 4, i % 4];
            }

            public void SetProjection(Matrix4 m)
            {
                for (int i = 0; i < 16; ++i)
                    this._projection[i] = m[i / 4, i % 4];
            }
        }

        private bool _disposedValue = false;

        private GL_Version _version = new GL_Version();
        private GL_Extensions _extensions = new GL_Extensions();
        private GL_DebugCallback _debug_callback = new GL_DebugCallback();

        private GL_VertexArrayObject<float, uint> _test_vao;
        private GL_Program _test_prog;
        private GL_StorageBuffer<TMVP> _mvp_ssbo;

        public Orbit(int width, int height, string title)
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
                _mvp_ssbo.Dispose();
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
            // x      y      z      r     g     b     a
              -0.5f,  0.0f, -0.5f,  1.0f, 0.0f, 0.0f, 1.0f,
               0.5f,  0.0f, -0.5f,  1.0f, 1.0f, 0.0f, 1.0f,
               0.5f,  0.0f,  0.5f,  0.0f, 1.0f, 0.0f, 1.0f,
              -0.5f,  0.0f,  0.5f,  0.0f, 0.0f, 1.0f, 1.0f
            };

            uint[] iquad = { 0, 1, 2, 0, 2, 3 };

            GL_TVertexFormat[] format = {
                new GL_TVertexFormat(0, 0, 3, 0, false),
                new GL_TVertexFormat(0, 1, 4, 3, false),
            };

            _test_vao = new GL_VertexArrayObject<float, uint>();
            _test_vao.AppendVertexBuffer(0, 7, vquad);
            _test_vao.Create(format, iquad);
            _test_vao.Bind();

            // Create shader program

            string vert_shader = @"#version 460 core
            layout (location = 0) in vec4 a_pos;
            layout (location = 1) in vec4 a_color;
      
            layout (location = 0) out TVertexData
            {
                vec4 col;
            } outData;

            layout(std430, binding = 1) buffer MVP
            {
                mat4 model;
                mat4 view;
                mat4 proj;
            } mvp;

            void main()
            {
                outData.col = a_color;
                gl_Position = mvp.proj * mvp.view * mvp.model * a_pos; 
            }";

            string frag_shader = @"#version 460 core
            out vec4 frag_color;
            
            layout (location = 0) in TVertexData
            {
                vec4 col;
            } inData;
      
            void main()
            {
                frag_color = inData.col; 
            }";

            this._test_prog = new GL_Program(vert_shader, frag_shader);
            this._test_prog.Generate();

            // Model view projection shader storage block object
            TMVP mvp = new TMVP(Matrix4.Identity, Matrix4.Identity, Matrix4.Identity);
            this._mvp_ssbo = new GL_StorageBuffer<TMVP>();
            this._mvp_ssbo.Create(ref mvp);
            this._mvp_ssbo.Bind(1);

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

            this._test_prog.Use();

            float angle = 90.0f * (float)Math.PI / 180.0f;
            float aspect = (float)this.Width / (float)this.Height;
            Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(angle, aspect, 0.1f, 100.0f);
            Matrix4 view = Matrix4.LookAt(-1.0f, -2.0f, 1.0f, 0, 0, 0, 0, 0, 1);

            TMVP mvp = new TMVP(Matrix4.Identity, view, proj);
            //this._mvp_ssbo.Update(16*sizeof(float), 16*sizeof(float), mvp.view);
            this._mvp_ssbo.Update(ref mvp);

            _test_vao.Draw();

            Context.SwapBuffers();
            base.OnUpdateFrame(e);
        }
    }
}
