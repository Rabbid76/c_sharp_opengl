using System;
using OpenTK.Graphics.OpenGL4; // GL
using OpenTK.Mathematics;
using OpenTK_library.Type;
using OpenTK_library.Controls;
using OpenTK_library.OpenGL;
using OpenTK_library.OpenGL.OpenGL4;
using OpenTK_libray_viewmodel.Model;


namespace OpenTK_rubiks.Model
{
    public class Rubiks
        : IModel
    {
        public struct THitInfo
        {
            public int _side;        //!< main cube side
            public int _sub_cube;    //!< geometric sub cube index
            public int _mapped_cube; //!< actual (mapped) sub cube index
            public int _cube_side;   //!< side on mapped cube

            public THitInfo(int side, int sub_cube, int mapped_cube, int cube_side)
            {
                _side = side;
                _sub_cube = sub_cube;
                _mapped_cube = mapped_cube;
                _cube_side = cube_side;
            }
        };

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

        private IVertexArrayObject _vao;
        private IProgram _prog;
        private IStorageBuffer _mvp_ssbo;
        private IStorageBuffer _light_ssbo;
        private IStorageBuffer _rubiks_ssbo;
        private IPixelPackBuffer _depth_pack_buffer;

        private Matrix4 _view = Matrix4.Identity;
        private Matrix4 _projection = Matrix4.Identity;
        private RubiksMouseControlsProxy _controls;
        private RubiksControls _rubiks_cube;
        private double _period = 0;
        TMVP _mvp_data;

        /// <summary>initial hit information</summary>
        private THitInfo _start_hit = new THitInfo( -1, -1, -1, -1 );
        /// <summary>point of hit - intersection of line of sight and cube</summary>
        private Vector3 _hit_pt = Vector3.Zero;

        public Rubiks()
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
                _rubiks_ssbo.Dispose();
                _light_ssbo.Dispose();
                _mvp_ssbo.Dispose();
                _vao.Dispose();
                _prog.Dispose();
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IControls GetControls() => this._controls;

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

            // to do mesh buffer creator form multi indices mesh 
            // use in-source wave font file?

            float[] attributes =
            {
              // left
              -1.0f,   1.0f, -1.0f,    0.0f, 0.0f, 1.0f, 0.0f, 
              -1.0f,  -1.0f, -1.0f,    0.0f, 1.0f, 1.0f, 0.0f,
              -1.0f,  -1.0f,  1.0f,    1.0f, 1.0f, 1.0f, 0.0f,
              -1.0f,   1.0f,  1.0f,    1.0f, 0.0f, 1.0f, 0.0f,

               // right
               1.0f,  -1.0f, -1.0f,    0.0f, 0.0f, 2.0f, 0.0f, 
               1.0f,   1.0f, -1.0f,    0.0f, 1.0f, 2.0f, 0.0f,
               1.0f,   1.0f,  1.0f,    1.0f, 1.0f, 2.0f, 0.0f,
               1.0f,  -1.0f,  1.0f,    1.0f, 0.0f, 2.0f, 0.0f,

               // front
              -1.0f,  -1.0f, -1.0f,    0.0f, 0.0f, 3.0f, 0.0f, 
               1.0f,  -1.0f, -1.0f,    0.0f, 1.0f, 3.0f, 0.0f,
               1.0f,  -1.0f,  1.0f,    1.0f, 1.0f, 3.0f, 0.0f,
              -1.0f,  -1.0f,  1.0f,    1.0f, 0.0f, 3.0f, 0.0f,

              // back
               1.0f,   1.0f, -1.0f,    0.0f, 0.0f, 4.0f, 0.0f, 
              -1.0f,   1.0f, -1.0f,    0.0f, 1.0f, 4.0f, 0.0f,
              -1.0f,   1.0f,  1.0f,    1.0f, 1.0f, 4.0f, 0.0f,
               1.0f,   1.0f,  1.0f,    1.0f, 0.0f, 4.0f, 0.0f,

               // bottom
              -1.0f,   1.0f, -1.0f,    0.0f, 0.0f, 5.0f, 0.0f, 
               1.0f,   1.0f, -1.0f,    0.0f, 1.0f, 5.0f, 0.0f,
               1.0f,  -1.0f, -1.0f,    1.0f, 1.0f, 5.0f, 0.0f,
              -1.0f,  -1.0f, -1.0f,    1.0f, 0.0f, 5.0f, 0.0f,

               // top
              -1.0f,  -1.0f,  1.0f,    0.0f, 0.0f, 6.0f, 0.0f, 
               1.0f,  -1.0f,  1.0f,    0.0f, 1.0f, 6.0f, 0.0f,
               1.0f,   1.0f,  1.0f,    1.0f, 1.0f, 6.0f, 0.0f,
              -1.0f,   1.0f,  1.0f,    1.0f, 0.0f, 6.0f, 0.0f
            };

            uint[] indices =
            {
               0,  1,  2,  0,  2,  3, // front
               4,  5,  6,  4,  6,  7, // back
               8,  9, 10,  8, 10, 11, // left
              12, 13, 14, 12, 14, 15, // right
              16, 17, 18, 16, 18, 19, // bottom
              20, 21, 22, 20, 22, 23  // top
            };

            // create Vertex Array Object, Array Buffer Object and Element Array Buffer Object

            TVertexFormat[] format = {
                new TVertexFormat(0, 0, 3, 0, false),
                new TVertexFormat(0, 1, 4, 3, false),
            };

            _vao = openGLFactory.NewVertexArrayObject();
            _vao.AppendVertexBuffer(0, 7, attributes);
            _vao.Create(format, indices);
            _vao.Bind();

            // Create shader program

            string vert_shader = @"
            #version 460 core

            layout (location = 0) in vec3 inPos;
            layout (location = 1) in vec4 inAttr;

            out vec3  vertPos;
            out vec4  vertTex;
            out float highlight;

            layout (std430, binding = 1) buffer UB_MVP
            { 
                mat4 u_projection;
                mat4 u_view;
                mat4 u_model;
            };

            layout (std430, binding = 2) buffer UB_RUBIKS
            { 
                mat4 u_rubiks_model[27];
                int  u_cube_hit;
                int  u_side_hit;
            };

            void main()
            {
                vec4 tex     = inAttr;
                int  cube_i  = gl_InstanceID;
                int  color_i = int(tex.z + 0.5); 
                int  x_i     = cube_i % 3;
                int  y_i     = (cube_i % 9) / 3;
                int  z_i     = cube_i / 9;

                if ( color_i == 1 )
                    tex.z = x_i == 0 ? tex.z : 0.0;
                else if ( color_i == 2 )
                    tex.z = x_i == 2 ? tex.z : 0.0;
                else if ( color_i == 3 )
                    tex.z = y_i == 0 ? tex.z : 0.0;
                else if ( color_i == 4 )
                    tex.z = y_i == 2 ? tex.z : 0.0;
                else if ( color_i == 5 )
                    tex.z = z_i == 0 ? tex.z : 0.0;
                else if ( color_i == 6 )
                    tex.z = z_i == 2 ? tex.z : 0.0;

                mat4 model_view = u_view * u_model * u_rubiks_model[cube_i];
                vec4 vertex_pos = model_view * vec4(inPos, 1.0);

                vertPos     = vertex_pos.xyz;
                vertTex     = tex;
                //highlight   = tex.z > 0.5 && cube_i == u_cube_hit ? 1.0 : 0.0;	
                //highlight   = tex.z > 0.5 && color_i == u_side_hit ? 1.0 : 0.0;
                highlight   = tex.z > 0.5 && cube_i == u_cube_hit && color_i == u_side_hit ? 1.0 : 0.0;		

		        gl_Position = u_projection * vertex_pos;
            }";

            string frag_shader = @"
            #version 460 core

            in vec3  vertPos;
            in vec4  vertTex;
            in float highlight;

            out vec4 fragColor;

            vec4 color_table[7] = vec4[7](
                vec4(0.5, 0.5, 0.5, 1.0),
                vec4(1.0, 0.0, 0.0, 1.0),
                vec4(0.0, 1.0, 0.0, 1.0),
                vec4(0.0, 0.0, 1.0, 1.0),
                vec4(1.0, 0.5, 0.0, 1.0),
                vec4(1.0, 1.0, 0.0, 1.0),
                vec4(1.0, 0.0, 1.0, 1.0)
            );

            void main()
            {
                int color_i = int(vertTex.z + 0.5);

                vec4 color = color_table[color_i]; 
                color.rgb *= max(0.5, highlight);

                fragColor  = color;
            }";

            this._prog = openGLFactory.VertexAndFragmentShaderProgram(vert_shader, frag_shader);
            this._prog.Generate();

            // matrices

            this._view = Matrix4.LookAt(0.0f, -4.0f, 0.0f, 0, 0, 0, 0, 0, 1);

            float angle = 70.0f * (float)Math.PI / 180.0f;
            float aspect = (float)this._cx / (float)this._cy;
            this._projection = Matrix4.CreatePerspectiveFieldOfView(angle, aspect, 0.1f, 100.0f);

            // Model view projection shader storage block objects and buffers
            this._mvp_data = new TMVP(Matrix4.Identity, this._view, this._projection);
            this._mvp_ssbo = openGLFactory.NewStorageBuffer();
            this._mvp_ssbo.Create(ref this._mvp_data);
            this._mvp_ssbo.Bind(1);

            T_RUBIKS_DATA rubiks_data = rubiks_data = new T_RUBIKS_DATA();
            this._rubiks_ssbo = openGLFactory.NewStorageBuffer();
            this._rubiks_ssbo.Create(ref rubiks_data);
            this._rubiks_ssbo.Bind(2);

            TLightSource light_source = new TLightSource(new Vector4(-1.0f, -0.5f, -2.0f, 0.0f), 0.2f, 0.8f, 0.8f, 10.0f);
            this._light_ssbo = openGLFactory.NewStorageBuffer();
            this._light_ssbo.Create(ref light_source);
            this._light_ssbo.Bind(3);

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

            // controller

            var spin = new ModelSpinningControls(
                () => { return this._period; },
                () => { return new float[] { 0, 0, (float)this._cx, (float)this._cy }; },
                () => { return this._view; }
            );
            spin.SetAttenuation(1.0f, 0.05f, 0.0f);
            _controls = new RubiksMouseControlsProxy(spin);

            float offset = 2.0f * 1.1f;
            float scale = 1.0f / 3.0f;
            this._rubiks_cube = new RubiksControls(
                () => { return this._period; },
                offset, scale
            );

            int shuffles = 11;
            this._rubiks_cube.Shuffle(shuffles);
            double time_s = 1.0;
            _rubiks_cube.AnimationTime = time_s;
        }

        public void Draw(int cx, int cy, double app_t)
        {
            this._period = app_t;

            bool resized = this._cx != cx || this._cy != cy;
            if (resized)
            {
                this._cx = cx;
                this._cy = cy;
                GL.Viewport(0, 0, this._cx, this._cy);

                float angle = 70.0f * (float)Math.PI / 180.0f;
                float aspect = (float)this._cx / (float)this._cy;
                this._projection = Matrix4.CreatePerspectiveFieldOfView(angle, aspect, 0.1f, 100.0f);
                this._mvp_data.projetion = this._projection;
            }

            this._rubiks_cube.Update();
            Render();
        }

        void Render( )
        {
            UpdateRenderData();

            this._mvp_ssbo.Update(ref this._mvp_data);
            this._mvp_ssbo.Bind(1);
            this._rubiks_ssbo.Update(ref _rubiks_cube.Data);
            this._rubiks_ssbo.Bind(2);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            this._prog.Use();
            _vao.DrawInstanced(27, 36);
        }

        void UpdateRenderData()
        {
            (Matrix4 model, bool update) = this._controls.Update();
            this._mvp_data.model = model;

            this._rubiks_cube.Data.cube_hit = -1;
            this._rubiks_cube.Data.side_hit = 0;

            if (_rubiks_cube.AnimationPending || _controls.Mode != TMode.change || _controls.Hit == false)
                _start_hit = new THitInfo(-1, -1, -1, -1);

            if (_rubiks_cube.AnimationPending || _controls.Mode != TMode.change)
            {
                _controls.Hit = false;
                _rubiks_cube.ResetHit();
                return;
            }

            float cube_offset = _rubiks_cube.Offset;
            float cube_scale = _rubiks_cube.Scale;

            // intersect ray with the side of the cube
            //
            // Is it possible get which surface of cube will be click in OpenGL?
            // [https://stackoverflow.com/questions/45893277/is-it-possble-get-which-surface-of-cube-will-be-click-in-opengl/45946943#45946943]
            //
            // How to recover view space position given view space depth value and ndc xy
            // [https://stackoverflow.com/questions/11277501/how-to-recover-view-space-position-given-view-space-depth-value-and-ndc-xy/46118945#46118945]


            // calculate the NDC position of the cursor on the far plane and the camera position

            float w = (float)this._cx;
            float h = (float)this._cy;
            float ndc_x = 2.0f * _controls.WndCursorPos.X / w - 1.0f;
            float ndc_y = 2.0f * _controls.WndCursorPos.Y / h - 1.0f;

            Matrix4 modelview = model * this._view;  // OpenTK `*`-operator is reversed
            Matrix4 inverse_modelview = modelview.Inverted();
            Matrix4 inverse_projection = this._projection.Inverted();

            THitInfo new_hit = new THitInfo(-1, -1, -1, -1);
            if (Math.Abs(ndc_x) < 1.0f && Math.Abs(ndc_y) < 1.0f)
            {
                // calculate a ray from the eye position along the line of sight through the cursor position

                Vector4 ndc_cursor_far = new Vector4(ndc_x, ndc_y, 1.0f, 1.0f); // z = 1.0 -> far plane
                Vector4 view_cursor = ndc_cursor_far * inverse_projection;

                Vector4 view_r0 = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
                Vector4 view_r1 = new Vector4(view_cursor.Xyz / view_cursor.W, 1.0f);

                Vector4 model_r0 = view_r0 * inverse_modelview; // OpenTK `*`-operator is reversed
                Vector4 model_r1 = view_r1 * inverse_modelview; // OpenTK `*`-operator is reversed

                Vector3 r0_ray = model_r0.Xyz;
                Vector3 d_ray = Vector3.Normalize(model_r1.Xyz - r0_ray);

                if (_controls.Hit && _start_hit._mapped_cube >= 0)
                {
                    _rubiks_cube.Data.cube_hit = _start_hit._mapped_cube;
                    _rubiks_cube.Data.side_hit = _start_hit._cube_side + 1;

                    // get 2nd point on intersection plane
                    float dist;
                    Vector3 xpt;
                    if (_rubiks_cube.IntersectSidePlane(r0_ray, d_ray, _start_hit._side, out dist, out xpt) == false)
                        return;

                    // check if the length of the vector, which is defined by the 2 intersection points, exceeds the threshold  
                    Vector3 hover_dir = xpt - _hit_pt;
                    float threshold_dist = 2.0f * cube_scale * 0.75f;
                    if (hover_dir.Length < threshold_dist)
                        return;
                    float[] s = new float[] { Math.Abs(hover_dir.X), Math.Abs(hover_dir.Y), Math.Abs(hover_dir.Z) };

                    // get rotation direction vector
                    int max_i = s[0] > s[1] ? (s[0] > s[2] ? 0 : 2) : (s[1] > s[2] ? 1 : 2);
                    Vector3 rot_dir = new Vector3(0.0f, 0.0f, 0.0f);
                    rot_dir[max_i] = hover_dir[max_i] < 0.0f ? -1.0f : 1.0f;

                    // TODO $$$ check if the component of `hover_dir` in the rotation direction (`rot_dir`) is greater as a specific threshold

                    // get side direction
                    Vector3 side_dir = new Vector3(0.0f, 0.0f, 0.0f);
                    side_dir[_start_hit._side / 2] = (_start_hit._side % 2 != 0) ? 1.0f : -1.0f;

                    // get rotation axis, row and direction
                    Vector3 rot_axis = Vector3.Cross(rot_dir, side_dir);
                    ChangeOperation op = new ChangeOperation(rot_axis, _start_hit._sub_cube);

                    // change the cube
                    _controls.Hit = false;
                    _start_hit = new THitInfo(-1, -1, -1, -1);
                    _rubiks_cube.Change(op);
                    return;
                }

                // find the nearest intersection of a side of the cube and the ray

                Vector3 isect_pt;
                if (_rubiks_cube.Intersect(r0_ray, d_ray, out new_hit._side, out isect_pt) == false)
                    new_hit._side = -1;

                // get intersected sub cube
                if (_rubiks_cube.IntersectedSubCube(new_hit._side, isect_pt, out new_hit._sub_cube, out new_hit._mapped_cube) == false)
                {
                    new_hit._sub_cube = -1;
                    new_hit._mapped_cube = -1;
                }

                // get the side on the intersected sub cube
                if (_rubiks_cube.IntersectedSubCubeSide(new_hit._side, new_hit._mapped_cube, out new_hit._cube_side) == false)
                    new_hit._cube_side = -1;

                // set the hit data
                _rubiks_cube.Data.cube_hit = new_hit._mapped_cube;
                _rubiks_cube.Data.side_hit = new_hit._cube_side + 1;

                if (_controls.Hit)
                {
                    _start_hit = new_hit;
                    _hit_pt = isect_pt;
                }
            }
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
