﻿using OpenTK;
using OpenTK.Input;            // KeyboardState, Keyboard, Key
using OpenTK.Graphics;         // GameWindow, GraphicsMode, Context
using OpenTK.Graphics.OpenGL4; // GL

using OpenTK_library;
using OpenTK_library.Type;
using OpenTK_library.Controls;
using OpenTK_library.Mesh;
using OpenTK_library.OpenGL;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenTK_example_2
{
    // TODO $$$
    // - spin


    public class AppWindow
        : GameWindow
    {
        internal unsafe struct TLightSource
        {
            public fixed float _light_dir[4];
            public float _ambient;
            public float _diffuse;
            public float _specular;
            public float _shininess;

            public TLightSource(Vector4 light_dir, float ambient, float diffuse, float specular, float shininess)
            {
                this._ambient = ambient;
                this._diffuse = diffuse;
                this._specular = specular;
                this._shininess = shininess;
                this.lightDir = light_dir;
            }

            public Vector4 lightDir
            {
                get 
                { 
                    return new Vector4(this._light_dir[0], this._light_dir[1], this._light_dir[2], this._light_dir[3]); 
                }
                set
                {
                    float[] data = new float[] { value.X, value.Y, value.Z, value.W };
                    for (int i = 0; i < 4; ++i)
                        this._light_dir[i] = data[i];
                }
            }
        }

        private bool _disposed = false;

        private OpenTK_library.OpenGL.Version _version = new OpenTK_library.OpenGL.Version();
        private Extensions _extensions = new Extensions();
        private DebugCallback _debug_callback = new DebugCallback();

        private VertexArrayObject<float, uint> _test_vao;
        private OpenTK_library.OpenGL.Program _test_prog;
        private StorageBuffer<TMVP> _mvp_ssbo;
        private StorageBuffer<TLightSource> _light_ssbo;
        
        private Matrix4 _view = Matrix4.Identity;
        private Matrix4 _projection = Matrix4.Identity;
        private IControls _controls;
        double _period = 0;
        
        public AppWindow(int width, int height, string title)
            : base(width, height,
                new GraphicsMode(32, 24, 8, 8),
                title,
                GameWindowFlags.Default,
                DisplayDevice.Default,
                4,
                6,
                GraphicsContextFlags.Default | GraphicsContextFlags.Debug)
        { }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !this._disposed)
            {
                _light_ssbo.Dispose();
                _mvp_ssbo.Dispose();
                _test_vao.Dispose();
                _test_prog.Dispose();
                this._disposed = true;
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

            (float[] attributes, uint[] indices) = new TrefoilKnot(256, 16).Create();
            TVertexFormat[] format = {
                new TVertexFormat(0, 0, 3, 0, false),
                new TVertexFormat(0, 1, 3, 3, false),
                //new TVertexFormat(0, ?, 2, 6, false),
                new TVertexFormat(0, 2, 4, 8, false),
            };

            _test_vao = new VertexArrayObject<float, uint>();
            _test_vao.AppendVertexBuffer(0, 12, attributes);
            _test_vao.Create(format, indices);
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
                mat4 proj;
                mat4 view;
                mat4 model;
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

            layout(std430, binding = 2) buffer TLight
            {
                vec4  u_lightDir;
                float u_ambient;
                float u_diffuse;
                float u_specular;
                float u_shininess;
            } light_data;
      
            void main()
            {
                vec3 color = inData.col.rgb;

                // ambient part
                vec3 lightCol = light_data.u_ambient * color;
                vec3 normalV  = normalize( inData.nv );
                vec3 eyeV     = normalize( -inData.pos );
                vec3 lightV   = normalize( -light_data.u_lightDir.xyz );

                // diffuse part
                float NdotL   = max( 0.0, dot( normalV, lightV ) );
                lightCol     += NdotL * light_data.u_diffuse * color;

                // specular part
                vec3  halfV     = normalize( eyeV + lightV );
                float NdotH     = max( 0.0, dot( normalV, halfV ) );
                float kSpecular = ( light_data.u_shininess + 2.0 ) * pow( NdotH, light_data.u_shininess ) / ( 2.0 * 3.14159265 );
                lightCol       += kSpecular * light_data.u_specular * color;

                frag_color = vec4( lightCol.rgb, inData.col.a );
            }";

            this._test_prog = OpenTK_library.OpenGL.Program.VertexAndFragmentShaderProgram(vert_shader, frag_shader);
            this._test_prog.Generate();

            // Model view projection shader storage block objects and buffers
            TMVP mvp = new TMVP(Matrix4.Identity, Matrix4.Identity, Matrix4.Identity);
            this._mvp_ssbo = new StorageBuffer<TMVP>();
            this._mvp_ssbo.Create(ref mvp);
            this._mvp_ssbo.Bind(1);

            TLightSource light_source = new TLightSource(new Vector4(-1.0f, -0.5f, -2.0f, 0.0f), 0.2f, 0.8f, 0.8f, 10.0f);
            this._light_ssbo = new StorageBuffer<TLightSource>();
            this._light_ssbo.Create(ref light_source);
            this._light_ssbo.Bind(2);

            // states

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.CullFace(CullFaceMode.Back);

            // matrices and controller

            this._view = Matrix4.LookAt(0.0f, 0.0f, 1.5f, 0, 0, 0, 0, 1, 0);

            var spin = new ModelSpinningControls(
                () => { return this._period; },
                () => { return new float[] { 0, 0, (float)this.Width, (float)this.Height }; },
                () => { return this._view; }
            );
            spin.SetAttenuation(1.0f, 0.05f, 0.0f);
            this._controls = spin;

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

            this._period += this.RenderPeriod;

            (Matrix4 model_mat, bool update) = this._controls.Update();
            
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            this._test_prog.Use();

            TMVP mvp = new TMVP(model_mat, this._view, this._projection);
            this._mvp_ssbo.Update(ref mvp);

            _test_vao.Draw(36);

            Context.SwapBuffers();
            base.OnUpdateFrame(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            Vector2 wnd_pos = new Vector2((float)e.Mouse.X, (float)(this.Height - e.Mouse.Y));
            this._controls.Start(e.Mouse.LeftButton == ButtonState.Pressed ? 0 : 1, wnd_pos);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            Vector2 wnd_pos = new Vector2((float)e.Mouse.X, (float)(this.Height - e.Mouse.Y));
            this._controls.End(e.Mouse.LeftButton == ButtonState.Released ? 0 : 1, wnd_pos);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            Vector2 wnd_pos = new Vector2((float)e.Mouse.X, (float)(this.Height - e.Mouse.Y));
            this._controls.MoveCursorTo(wnd_pos);
        }
    }
}
