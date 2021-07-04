using System;
using OpenTK.Graphics.OpenGL4; // GL
using OpenTK_library.Type;
using OpenTK_library.Mesh;
using OpenTK_library.Controls;
using OpenTK_library.OpenGL;
using OpenTK_library.OpenGL.OpenGL4;
using OpenTK_libray_viewmodel.Model;
using OpenTK.Mathematics;

namespace OpenTK_controls_firstperson.Model
{
    public class Scene_Model
        : IModel
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

        private IOpenGLObjectFactory openGLFactory = new OpenGLObjectFactory4();
        private bool _disposed = false;
        private int _cx = 0;
        private int _cy = 0;
        private IVersionInformation _version;
        private IExtensionInformation _extensions;
        private IDebugCallback _debug_callback;

        private IVertexArrayObject _test_vao;
        private IProgram _test_prog;
        private IStorageBuffer _mvp_ssbo;
        private IStorageBuffer _light_ssbo;
        private IPixelPackBuffer _depth_pack_buffer;

        private Matrix4 _view = Matrix4.Identity;
        private Matrix4 _projection = Matrix4.Identity;
        private IControls _controls;
        private double _period = 0;

        public Scene_Model()
        {
            _version = openGLFactory.NewVersionInformation(Console.WriteLine);
            _extensions = openGLFactory.NewExtensionInformation();
            _debug_callback = openGLFactory.NewDebugCallback(Console.WriteLine);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _depth_pack_buffer.Dispose();
                _light_ssbo.Dispose();
                _mvp_ssbo.Dispose();
                _test_vao.Dispose();
                _test_prog.Dispose();
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IControls GetControls() => _controls;

        public float GetScale() => 1.0f;

        public void Setup(int cx, int cy)
        {
            this._cx = cx;
            this._cy = cy;

            // Version strings
            _version.Retrieve();

            // Get OpenGL extensions
            _extensions.Retrieve();

            // Debug callback
            _debug_callback.Init();

            // create Vertex Array Object, Array Buffer Object and Element Array Buffer Object

            (float[] attributes, uint[] indices) = new Cube().Create();
            TVertexFormat[] format = {
                new TVertexFormat(0, 0, 3, 0, false),
                new TVertexFormat(0, 1, 3, 3, false),
                //new TVertexFormat(0, ?, 2, 6, false),
                new TVertexFormat(0, 2, 4, 8, false),
            };

            _test_vao = openGLFactory.NewVertexArrayObject();
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

            this._test_prog = openGLFactory.VertexAndFragmentShaderProgram(vert_shader, frag_shader);
            this._test_prog.Generate();

            // Model view projection shader storage block objects and buffers
            TMVP mvp = new TMVP(Matrix4.Identity, Matrix4.Identity, Matrix4.Identity);
            this._mvp_ssbo = openGLFactory.NewStorageBuffer();
            this._mvp_ssbo.Create(ref mvp);
            this._mvp_ssbo.Bind(1);

            TLightSource light_source = new TLightSource(new Vector4(-1.0f, -0.5f, -2.0f, 0.0f), 0.2f, 0.8f, 0.8f, 10.0f);
            this._light_ssbo = openGLFactory.NewStorageBuffer();
            this._light_ssbo.Create(ref light_source);
            this._light_ssbo.Bind(2);

            this._depth_pack_buffer = openGLFactory.NewPixelPackBuffer();
            this._depth_pack_buffer.Create<float>();

            // states

            GL.Viewport(0, 0, this._cx, this._cy);
            //GL.ClearColor(System.Drawing.Color.Beige);
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.CullFace(CullFaceMode.Back);

            // matrices and controller

            this._view = Matrix4.LookAt(0.0f, -4.0f, 0.0f, 0, 0, 0, 0, 0, 1);

            float angle = 90.0f * (float)Math.PI / 180.0f;
            float aspect = (float)this._cx / (float)this._cy;
            this._projection = Matrix4.CreatePerspectiveFieldOfView(angle, aspect, 0.1f, 100.0f);

            _controls = new FirstPersonControls(
                () => { return new float[] { 0, 0, (float)this._cx, (float)this._cy }; },
                () => { return this._view; },
                (Matrix4 view) => { this._view = view; }
            );
        }

        public void Draw(int cx, int cy, double app_t)
        {
            double delta_t = app_t - this._period;
            this._period = app_t;

            bool resized = this._cx != cx || this._cy != cy;
            if (resized)
            {
                this._cx = cx;
                this._cy = cy;
                GL.Viewport(0, 0, this._cx, this._cy);

                float angle = 90.0f * (float)Math.PI / 180.0f;
                float aspect = (float)this._cx / (float)this._cy;
                this._projection = Matrix4.CreatePerspectiveFieldOfView(angle, aspect, 0.1f, 100.0f);
            }

            Vector3 move_vec = new Vector3(0, 0, 0);
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Up))
                move_vec.Y += 1.0f;
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Down))
                move_vec.Y -= 1.0f;
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Right))
                move_vec.X += 1.0f;
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Left))
                move_vec.X -= 1.0f;
            move_vec *= (float)delta_t;

            this._controls.Move(move_vec);
         
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            this._test_prog.Use();

            TMVP mvp = new TMVP(Matrix4.Identity, this._view, this._projection);
            this._mvp_ssbo.Update(ref mvp);

            _test_vao.Draw(36);
        }

        // get depth on fragment
        private float GetDepth(Vector2 cursor_pos)
        {
            int x = (int)cursor_pos.X;
            int y = this._cy - (int)cursor_pos.Y;
            float[] depth_data = _depth_pack_buffer.ReadDepth(x, y);
            float depth = depth_data.Length > 0 ? depth_data[0] : 1.0f;
            if (depth == 1.0f)
            {
                Vector3 pt_drag = new Vector3();
                Vector4 clip_pos_h = new Vector4(pt_drag, 1.0f);
                clip_pos_h = Vector4.TransformRow(clip_pos_h, this._view);
                clip_pos_h = Vector4.TransformRow(clip_pos_h, this._projection);
                Vector3 ndc_pos = new Vector3(clip_pos_h.X / clip_pos_h.W, clip_pos_h.Y / clip_pos_h.W, clip_pos_h.Z / clip_pos_h.W);
                if (ndc_pos.Z > -1 && ndc_pos.Z < 1)
                    depth = ndc_pos.Z * 0.5f + 0.5f;
            }

            return depth;
        }
    }
}

