using OpenTK;                  // Matrix4
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
                this.model = model;
                this.view = view;
                this.projetion = projetion;
            }

            public Matrix4 model
            {
                get
                {
                    return new Matrix4(_model[0], _model[1], _model[2], _model[3],
                                       _model[4], _model[5], _model[6], _model[7],
                                       _model[8], _model[9], _model[10], _model[11],
                                       _model[12], _model[13], _model[14], _model[15]);
                }

                set
                {
                    for (int i = 0; i < 16; ++i)
                        this._model[i] = value[i / 4, i % 4];
                }
            }

            public Matrix4 view
            {
                get
                { 
                    return new Matrix4(_view[0], _view[1], _view[2], _view[3],
                                       _view[4], _view[5], _view[6], _view[7],
                                       _view[8], _view[9], _view[10], _view[11],
                                       _view[12], _view[13], _view[14], _view[15]);
                }

                set
                {
                    for (int i = 0; i < 16; ++i)
                        this._view[i] = value[i / 4, i % 4];
                }
            }

            public Matrix4 projetion
            {
                get
                {
                    return new Matrix4(_projection[0], _projection[1], _projection[2], _projection[3],
                                       _projection[4], _projection[5], _projection[6], _projection[7],
                                       _projection[8], _projection[9], _projection[10], _projection[11],
                                       _projection[12], _projection[13], _projection[14], _projection[15]);
                }

                set
                {
                    for (int i = 0; i < 16; ++i)
                        this._projection[i] = value[i / 4, i % 4];
                }
            }
        }

        private bool _disposedValue = false;

        private GL_Version _version = new GL_Version();
        private GL_Extensions _extensions = new GL_Extensions();
        private GL_DebugCallback _debug_callback = new GL_DebugCallback();

        private GL_VertexArrayObject<float, uint> _test_vao;
        private GL_Program _test_prog;
        private GL_StorageBuffer<TMVP> _mvp_ssbo;
        private GL_PixelPackBuffer<float> _depth_pack_buffer;

        private Matrix4 _view = Matrix4.Identity;
        private Matrix4 _projection = Matrix4.Identity;
        private NavigationController _navigate;
        private float _wheel_pos = 0.0f;

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
                _depth_pack_buffer.Dispose();
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

            float[] v = { -1, -1, 1, 1, -1, 1, 1, 1, 1, -1, 1, 1, -1, -1, -1, 1, -1, -1, 1, 1, -1, -1, 1, -1 };
            float[] c = { 1.0f, 0.0f, 0.0f, 1.0f, 0.5f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f };
            float[] n = { 0, 0, 1, 1, 0, 0, 0, 0, -1, -1, 0, 0, 0, 1, 0, 0, -1, 0 };
            int[] ec = {0, 1, 2, 3, 1, 5, 6, 2, 5, 4, 7, 6, 4, 0, 3, 7, 3, 2, 6, 7, 1, 0, 4, 5};
            int[] es = { 0, 1, 2, 0, 2, 3 };
            List<float> attr_array = new List<float>();
            for (int si = 0; si < 6; ++si)
            {
                for(int vi = 0; vi <6; ++ vi)
                {
                    int ci = es[vi];
                    int i = si * 4 + ci;
                    attr_array.AddRange(new float[] { v[ec[i] * 3], v[ec[i] * 3 + 1], v[ec[i] * 3 + 2] });
                    attr_array.AddRange(new float[] { n[si * 3], n[si * 3 + 1], n[si * 3 + 2] });
                    attr_array.AddRange(new float[] { c[si * 3], c[si * 3 + 1], c[si * 3 + 2], 1 });
                }
            }
            
            uint[] icube = {};

            GL_TVertexFormat[] format = {
                new GL_TVertexFormat(0, 0, 3, 0, false),
                new GL_TVertexFormat(0, 1, 3, 3, false),
                new GL_TVertexFormat(0, 2, 4, 6, false),
            };

            _test_vao = new GL_VertexArrayObject<float, uint>();
            _test_vao.AppendVertexBuffer(0, 10, attr_array.ToArray());
            _test_vao.Create(format, icube);
            _test_vao.Bind();

            // Create shader program

            string vert_shader = @"#version 460 core
            layout (location = 0) in vec4 a_pos;
            layout (location = 1) in vec3 a_nv;
            layout (location = 2) in vec4 a_color;
      
            layout (location = 0) out TVertexData
            {
                vec3 pos;
                vec3 nv;
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
                mat4 mv_mat     = mvp.view * mvp.model;
                mat3 normal_mat = inverse(transpose(mat3(mv_mat))); 

                outData.nv   = normalize(normal_mat * a_nv);
                outData.col  = a_color;
                vec4 viewPos = mv_mat * a_pos;
                outData.pos  = viewPos.xyz / viewPos.w;
                gl_Position  = mvp.proj * viewPos;
            }";

            string frag_shader = @"#version 460 core
            out vec4 frag_color;
            
            layout (location = 0) in TVertexData
            {
                vec3 pos;
                vec3 nv;
                vec4 col;
            } inData;
      
            void main()
            {
                frag_color = inData.col; 
            }";

            this._test_prog = new GL_Program(vert_shader, frag_shader);
            this._test_prog.Generate();

            // Model view projection shader storage block objects and buffers
            TMVP mvp = new TMVP(Matrix4.Identity, Matrix4.Identity, Matrix4.Identity);
            this._mvp_ssbo = new GL_StorageBuffer<TMVP>();
            this._mvp_ssbo.Create(ref mvp);
            this._mvp_ssbo.Bind(1);

            this._depth_pack_buffer = new GL_PixelPackBuffer<float>();
            this._depth_pack_buffer.Create();

            // states

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.CullFace(CullFaceMode.Back);

            // matrices and controller

            this._view = Matrix4.LookAt(-2.0f, -4.0f, 2.0f, 0, 0, 0, 0, 0, 1);

            _navigate = new NavigationController(
                () => { return new float[] { 0, 0, (float)this.Width, (float)this.Height }; },
                () => { return this._view; },
                () => { return this._projection; },
                this.GetDepth,
                (cursor_pos) => { return new Vector3(0, 0, 0);  }
            );

            base.OnLoad(e);
        }

        //! On resize
        protected override void OnResize(EventArgs e)
        {
            float angle = 90.0f * (float)Math.PI / 180.0f;
            float aspect = (float)this.Width / (float)this.Height;
            this._projection = Matrix4.CreatePerspectiveFieldOfView(angle, aspect, 0.1f, 100.0f);

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

    
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            this._test_prog.Use();

            TMVP mvp = new TMVP(Matrix4.Identity, this._view, this._projection);
            this._mvp_ssbo.Update(ref mvp);

            _test_vao.Draw(36);

            Context.SwapBuffers();
            base.OnUpdateFrame(e);
        }

        // get depth on fragment
        private float GetDepth(Vector2 cursor_pos)
        {
            int x = (int)cursor_pos.X;
            int y = this.Height - (int)cursor_pos.Y;
            float[] depth_data = _depth_pack_buffer.ReadDepth(x, y);
            float depth = depth_data.Length > 0 ? depth_data[0] : 1.0f;
            if (depth == 1.0f)
            {
                Vector3 pt_drag = new Vector3();
                Vector4 clip_pos_h = new Vector4(pt_drag, 1.0f);
                clip_pos_h = Vector4.Transform(clip_pos_h, this._view);
                clip_pos_h = Vector4.Transform(clip_pos_h, this._projection);
                Vector3 ndc_pos = new Vector3(clip_pos_h.X / clip_pos_h.W, clip_pos_h.Y / clip_pos_h.W, clip_pos_h.Z / clip_pos_h.W);
                if (ndc_pos.Z > -1 && ndc_pos.Z < 1)
                    depth = ndc_pos.Z * 0.5f + 0.5f;
            }

            return depth;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            Vector2 wnd_pos = new Vector2((float)e.Mouse.X, (float)(this.Height - e.Mouse.Y));
            if (e.Mouse.RightButton == ButtonState.Pressed)
            {
                this._navigate.StartPan(wnd_pos);
            }
            if (e.Mouse.LeftButton == ButtonState.Pressed)
            {
                //this._navigate.StartOrbit(wnd_pos, NavigationMode.ORBIT);
                this._navigate.StartOrbit(wnd_pos, NavigationMode.ROTATE);
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            Vector2 wnd_pos = new Vector2((float)e.Mouse.X, (float)(this.Height - e.Mouse.Y));
            if (e.Mouse.RightButton == ButtonState.Released)
            {
                this._navigate.EndPan(wnd_pos);
            }
            if (e.Mouse.LeftButton == ButtonState.Released)
            {
                this._navigate.EndOrbit(wnd_pos);
            }
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            Vector2 wnd_pos = new Vector2((float)e.Mouse.X, (float)(this.Height - e.Mouse.Y));
            (Matrix4 view_mat, bool update) = this._navigate.MoveCursorTo(wnd_pos);
            this._view = view_mat;
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            Vector2 wnd_pos = new Vector2((float)e.Mouse.X, (float)(this.Height - e.Mouse.Y));
            float direction = e.Mouse.WheelPrecise - this._wheel_pos;
            this._wheel_pos = e.Mouse.WheelPrecise;
            (Matrix4 view_mat, bool update) = this._navigate.MoveOnLineOfSight(wnd_pos, direction);
            this._view = view_mat;
        }
    }
}
