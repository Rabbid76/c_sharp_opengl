using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL4; // GL
using OpenTK.Mathematics;
using OpenTK_stereoscopic_example_1.ViewModel;
using OpenTK_library.Type;
using OpenTK_library.Mathematics;
using OpenTK_library.MeshBuilder;
using OpenTK_library.Controls;
using OpenTK_library.OpenGL;
using OpenTK_library.OpenGL.OpenGL4;
using OpenTK_library_assimp.Builder;
using OpenTK_libray_viewmodel.Model;

namespace OpenTK_stereoscopic_example_1.Model
{
    // Anaglyph stereo 3D
    //
    // [Anaglyph 3D] (https://de.wikipedia.org/wiki/Anaglyph_3D)  
    // [Anaglyph Methods Comparison] (http://3dtv.at/Knowhow/AnaglyphComparison_en.aspx)
    // [Half Color Anaglyph] (https://ixora.io/projects/camera-3D/half-color-anaglyph/)  
    // [Color Anaglyph / Dubois Anaglyph] (http://stereo.jpn.org/eng/stphmkr/help/stereo_04.htm)
    // [Creating anaglyphs from left/right image pairs] (https://www.triplespark.net/render/stereo/anaglyph/)  
    // [Optimised Anaglyph] (http://stereo.jpn.org/eng/stphmkr/help/stereo_13.htm)  
    // 
    // Books and papers:
    // 
    // [Creating a Color Anaglyph from a Pseudo-Stereo Pair of Images] (https://www.zuj.edu.jo/conferences/ICIT09/PaperList/Papers/Image%20and%20Signal%20Processing/578.pdf)
    // [Adaptive Generation of Color Anaglyph] (https://link.springer.com/chapter/10.1007/978-3-642-39759-2_3)  
    // [Engineering Optics] (https://books.google.at/books?id=dk8S7ki4NPcC&pg=PA475&lpg=PA475&dq=colored+anaglyph&source=bl&ots=q6mraZMVSL&sig=ACfU3U137qUigxPQr1-RMBGAf71qCvzFbA&hl=de&sa=X&ved=2ahUKEwiKurTu-rznAhXSWxUIHVuKC5g4ChDoATAHegQIChAB#v=onepage&q=colored%20anaglyph&f=false) 

    // TODO: increase depth effect by darkening far and brightening near 

    public class OpenTK_AssimpModel
        : IModel
    {
        private static readonly string _name_trefoil_knot = "trefoil knot";

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
        private OpenTK_ViewModel _viewmodel;
        private int _anaglyph_model_id = 0;
        private int _controls_id = 0;
        private Dictionary<string, string> _model_names = new Dictionary<string, string>();
        private Dictionary<int, OpenTK_library.Scene.Model> _models = new Dictionary<int, OpenTK_library.Scene.Model>();
        private int _model_id = 0;
        private bool _disposed = false;
        private int _cx = 0;
        private int _cy = 0;
        private float _far = 100.0f;
        private IVersionInformation _version;
        private IExtensionInformation _extensions;
        private IDebugCallback _debug_callback;

        private OpenTK_library.Scene.Model _model;
        private IProgram _draw_prog;
        private IProgram _stereo_prog;
        private IStorageBuffer _vp_ssbo;
        private IStorageBuffer _model_ssbo;
        private IStorageBuffer _light_ssbo;
        private IPixelPackBuffer _depth_pack_buffer;
        private List<IFramebuffer> _fbos;
        private IVertexArrayObject _quad_vao;

        private Matrix4 _view = Matrix4.Identity;
        private Matrix4 _projection = Matrix4.Identity;
        private Matrix4 _model_center = Matrix4.Identity;
        private IControls _controls;
        double _period = 0;

        private float _eye_scale = 0.10f / 100.0f;
        private float _focal_scale = 1.0f / 200.0f;

        private float EyeScale
        {
            get => (float)ViewModel.EyeScale * _eye_scale;
            set => ViewModel.EyeScale = (int)(value / _eye_scale);
        }

        private float FocalScale
        {
            get => (float)ViewModel.FocalScale * _focal_scale;
            set => ViewModel.FocalScale = (int)(value / _focal_scale);
        }

        public OpenTK_AssimpModel()
        {
            _version = openGLFactory.NewVersionInformation(Console.WriteLine);
            _extensions = openGLFactory.NewExtensionInformation();
            _debug_callback = openGLFactory.NewDebugCallback(Console.WriteLine);

            // add "built-in" meshs
            _model_names[_name_trefoil_knot] = _name_trefoil_knot;

            // get wavefront files
            List<string> names = new List<string>();

            //Assembly assembly = Assembly.GetExecutingAssembly();
            //names = new List<string>(assembly.GetManifestResourceNames());

            string working_directory = Directory.GetCurrentDirectory();
            if (Directory.Exists(working_directory))
                names = new List<string>(Directory.GetFiles(working_directory, "*.obj", SearchOption.AllDirectories));

            // add wavefront models
            foreach (var name in new List<string>(names))
            {
                if (name.EndsWith(".obj"))
                {
                    var user_name = name.Substring(0, name.Length - 4);
                    var pos = user_name.LastIndexOfAny(new char[]{'.', '\\'});
                    user_name = user_name.Substring(pos + 1, user_name.Length - pos - 1);
                    _model_names[user_name] = name;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                foreach (var model in _models)
                    model.Value.Dispose();
                _models.Clear();
                if (_fbos != null)
                {
                    foreach (var fbo in _fbos)
                        fbo.Dispose();
                    _fbos.Clear();
                }
                _depth_pack_buffer.Dispose();
                _quad_vao.Dispose();
                _light_ssbo.Dispose();
                _vp_ssbo.Dispose();
                _model_ssbo.Dispose();
                _draw_prog.Dispose();
                _stereo_prog.Dispose();
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public OpenTK_ViewModel ViewModel
        {
            get => _viewmodel;
            set => _viewmodel = value; 
        }

        public int DefaultFramebuffer => _viewmodel.DefaultFramebuffer;

        public IControls GetControls() => _controls;

        public float GetScale() => _model.SceneBox.MaxSize;

        public List<ViewModel.Anaglyphs> AnaglyphsData()
        {
            var anaglyphs = new List<ViewModel.Anaglyphs>();
            anaglyphs.Add(new ViewModel.Anaglyphs("Ture", "0"));
            anaglyphs.Add(new ViewModel.Anaglyphs("Gray", "1"));
            anaglyphs.Add(new ViewModel.Anaglyphs("Color", "2"));
            anaglyphs.Add(new ViewModel.Anaglyphs("Half color", "3"));
            anaglyphs.Add(new ViewModel.Anaglyphs("Optimized", "4"));
            anaglyphs.Add(new ViewModel.Anaglyphs("Dubois", "5"));
            return anaglyphs;
        }

        public List<ViewModel.Model> ModelsData()
        {
            var model = new List<ViewModel.Model>();
            int id = 0;
            foreach (var mode_name in _model_names)
            {
                model.Add(new ViewModel.Model(mode_name.Key, id.ToString()));
                id++;
            }
            return model;
        }

        public List<ViewModel.Controls> ControlsData()
        {
            var controls = new List<ViewModel.Controls>();
            controls.Add(new ViewModel.Controls("spin", "0"));
            controls.Add(new ViewModel.Controls("pan rotate zoom", "1"));
            controls.Add(new ViewModel.Controls("first person", "2"));
            return controls;
        }

        public void Setup(int cx, int cy)
        {
            this._cx = cx;
            this._cy = cy;

            // Version strings
            _version.Retrieve();

            // Get OpenGL extensions
            _extensions.Retrieve();

            // Debug callback
            _debug_callback.Init(true);

            // Create draw shader program

            string vert_shader_draw = @"#version 460 core
            layout (location = 0) in vec4 a_pos;
            layout (location = 1) in vec3 a_nv;
            layout (location = 2) in vec2 a_uv;
      
            layout (location = 0) out TVertexData
            {
                vec3 pos;
                vec3 nv;
                vec2 uv;
                vec3 lightV;
            } outData;

            layout(std430, binding = 1) buffer TVP
            {
                mat4 proj;
                mat4 view;
            } vp;

            layout(std430, binding = 2) buffer TModel
            {
                mat4 model;
            } model;

            layout(std430, binding = 3) buffer TLight
            {
                vec4  u_lightDir;
                float u_ambient;
                float u_diffuse;
                float u_specular;
                float u_shininess;
            } light_data;

            void main()
            {
                mat4 mv_mat     = vp.view * model.model;
                mat3 normal_mat = inverse(transpose(mat3(mv_mat))); 

                //outData.lightV = mat3(vp.view) * -light_data.u_lightDir.xyz;
                outData.lightV = -light_data.u_lightDir.xyz;
                outData.nv     = normalize(normal_mat * a_nv);
                outData.uv     = a_uv;
                vec4 viewPos   = mv_mat * a_pos;
                outData.pos    = viewPos.xyz / viewPos.w;
                gl_Position    = vp.proj * viewPos;
            }";

            string frag_shader_draw = @"#version 460 core
            out vec4 frag_color;
            
            layout (location = 0) in TVertexData
            {
                vec3 pos;
                vec3 nv;
                vec2 uv;
                vec3 lightV;
            } inData;

            layout(std430, binding = 3) buffer TLight
            {
                vec4  u_lightDir;
                float u_ambient;
                float u_diffuse;
                float u_specular;
                float u_shininess;
            } light_data;
      
            void main()
            {
                // vec3 color = vec3(fract(inData.uv * 10.0), 0.0);
                vec3 color = inData.nv;
                //vec3 color = vec3(1.0);

                // ambient part
                vec3 lightCol = light_data.u_ambient * color;
                vec3 normalV  = normalize( inData.nv );
                vec3 eyeV     = normalize( -inData.pos );
                vec3 lightV   = normalize( inData.lightV );

                // diffuse part
                float NdotL   = max( 0.0, dot( normalV, lightV ) );
                lightCol     += NdotL * light_data.u_diffuse * color;

                // specular part
                vec3  halfV     = normalize( eyeV + lightV );
                float NdotH     = max( 0.0, dot( normalV, halfV ) );
                float kSpecular = ( light_data.u_shininess + 2.0 ) * pow( NdotH, light_data.u_shininess ) / ( 2.0 * 3.14159265 );
                lightCol       += kSpecular * light_data.u_specular * color;

                frag_color = vec4( lightCol.rgb, 1.0 );
            }";

            this._draw_prog = openGLFactory.VertexAndFragmentShaderProgram(vert_shader_draw, frag_shader_draw);
            this._draw_prog.Generate();

            // Model view projection shader storage block objects and buffers
            TVP vp = new TVP(Matrix4.Identity, Matrix4.Identity);
            this._vp_ssbo = openGLFactory.NewStorageBuffer();
            this._vp_ssbo.Create(ref vp);
            this._vp_ssbo.Bind(1);

            TMat44 model = new TMat44(Matrix4.Identity);
            this._model_ssbo = openGLFactory.NewStorageBuffer();
            this._model_ssbo.Create(ref model);
            this._model_ssbo.Bind(2);

            TLightSource light_source = new TLightSource(new Vector4(-1.0f, -1.0f, -5.0f, 0.0f), 0.0f, 1.0f, 0.0f, 10.0f);
            this._light_ssbo = openGLFactory.NewStorageBuffer();
            this._light_ssbo.Create(ref light_source);
            this._light_ssbo.Bind(3);

            this._depth_pack_buffer = openGLFactory.NewPixelPackBuffer();
            this._depth_pack_buffer.Create<float>();

            // Create stereoscopic filter shader program

            string vert_shader_stereo = @"#version 460 core
            layout (location = 0) in vec2 a_pos;
      
            layout (location = 0) out TVertexData
            {
                vec3 pos;
                vec2 uv;
            } outData;

            void main()
            {
                outData.uv  = a_pos.xy * 0.5 + 0.5;
                gl_Position = vec4(a_pos.xy, 0.0, 1.0);
            }";

            string frag_shader_stereo = @"#version 460 core
            out vec4 frag_color;
            
            layout (location = 0) in TVertexData
            {
                vec3 pos;
                vec2 uv;
            } inData;

            layout (binding = 1) uniform sampler2D u_left_color;
            layout (binding = 2) uniform sampler2D u_right_color;

            layout (location = 10) uniform int u_anaglyph_mode;

            // rational depth [0, 1] -> linear depth [0, 1]
            float LinearizePerspectiveDepth( float depth, float near, float far)
            {
                  float  z = depth * 2.0 - 1.0;
                  float  linear_depth = (2.0 * near * far / (far + near - z * (far-near)) - near) / (far-near);
                  return linear_depth;
            }

            void main()
            {
                // compute texture coordinate
                vec2 uv = inData.uv;

                // get colors and depths for the left and right eye
                vec4  left_color  = texture2D(u_left_color, uv);
                vec4  right_color = texture2D(u_right_color, uv);
                //float left_depth  = texture2D(u_left_depth, uv).x;
                //float right_depth = texture2D(u_right_depth, uv).x;

                // compute depth dependent scale factors
                //float left_linear_depth  = u_perspective == 0 ? left_depth : LinearizePerspectiveDepth(left_depth, u_depthrange.x, u_depthrange.y);
                //float right_linear_depth = u_perspective == 0 ? left_depth : LinearizePerspectiveDepth(right_depth, u_depthrange.x, u_depthrange.y);

                // scale the colors for the left an right eye
                //left_color.rgb  *= mix(u_colorscale.x, u_colorscale.y, left_linear_depth);
                //right_color.rgb *= mix(u_colorscale.x, u_colorscale.y, right_linear_depth);

                vec3  luminance  = vec3(0.299, 0.587, 0.114);
                //vec3  luminance  = vec3(0.2126, 0.7152, 0.0722);
                float left_gray  = dot(left_color.rgb, luminance);
                float right_gray = dot(right_color.rgb, luminance);

                vec3 final_color;

                if (u_anaglyph_mode == 0)
                {
                    // True Anaglyphs
                    final_color = vec3(left_gray, 0.0, right_gray);
                }
                else if (u_anaglyph_mode == 1)
                {
                    // Gray Anaglyphs
                    final_color = vec3(left_gray, right_gray, right_gray);
                }
                else if (u_anaglyph_mode == 2)
                {
                    // Color Anaglyphs
                    final_color = vec3(left_color.r, right_color.gb);
                }
                else if (u_anaglyph_mode == 3)
                {
                    // Half Color Anaglyphs
                    final_color = vec3(left_gray, right_color.gb);
                }
                else if (u_anaglyph_mode == 4)
                {
                    // Optimized Anaglyphs (no red color component)
                    final_color = vec3(dot(left_color.gb, vec2(0.7, 0.3)) * 1.5, right_color.gb);
                }
                else // if (u_anaglyph_mode == 5)
                {
                    // Dubois Anaglyphs
                    mat3 left_mat  = mat3( 0.456, -0.040,  0.015,     0.500, -0.038, -0.021,    0.176, -0.016, -0.005);
                    mat3 right_mat = mat3(-0.043,  0.378, -0.072,    -0.088,  0.734, -0.113,   -0.002, -0.018,  1.226);
                    final_color    = vec3(left_mat * left_color.rgb + right_mat * right_color.rgb);
                }

                frag_color = vec4(final_color, 1.0);
            }";

            this._stereo_prog = openGLFactory.VertexAndFragmentShaderProgram(vert_shader_stereo, frag_shader_stereo);
            this._stereo_prog.Generate();

            // quad vertex array

            float[] attributes = { -1, -1, 1, -1, 1, 1, -1, 1 };
            uint[] indices = { 0, 1, 2, 0, 2, 3 };
            TVertexFormat[] format = {new TVertexFormat(0, 0, 2, 0, false )};
            _quad_vao = openGLFactory.NewVertexArrayObject();
            _quad_vao.AppendVertexBuffer(0, 2, attributes);
            _quad_vao.Create(format, indices);
            _quad_vao.Bind();

            // states

            GL.Viewport(0, 0, this._cx, this._cy);
            //GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.ClearColor(System.Drawing.Color.Beige);

            // matrices

            this._view = Matrix4.LookAt(0.0f, -1.5f, 0, 0, 0, 0, 0, 0, 1);

            float angle = 90.0f * (float)Math.PI / 180.0f;
            float aspect = (float)this._cx / (float)this._cy;
            this._projection = Matrix4.CreatePerspectiveFieldOfView(angle, aspect, 0.1f, _far);

            // properties 
            EyeScale = 0.1f;
            FocalScale = 0.5f;
        }

        private void LoadCurrentModel()
        {
            try
            {
                int model_id = Int32.Parse(_viewmodel.CurrentModel.Number);
                if (_model != null && model_id == _model_id)
                    return;
                _model_id = model_id;

                if (_models.ContainsKey(_model_id))
                {
                    // get model from cache
                    _model = _models[_model_id];
                }
                else
                {
                    string resource_name = _model_names[_viewmodel.CurrentModel.Text];

                    if (resource_name == _name_trefoil_knot)
                    {
                        // Create trefoil knot model
                        _model = TrefoilKnotModel.Create(openGLFactory, 256, 32);
                    }
                    else
                    {
                        // Create "Assimp" Model
                        
                        //Assembly assembly = Assembly.GetExecutingAssembly();
                        //Stream resource_stream = assembly.GetManifestResourceStream(resource_name);
                        //_model = Model.Create(resource_stream);
                        _model = AssimpModel.Create(openGLFactory, resource_name);
                    }
                    _models[_model_id] = _model;
                }

                // view matrix

                var cpt = _model.SceneBox.Center;
                var size = _model.SceneBox.Diagonal;
                // set new far plane
                this._far = size * 2.0f;
                // force set viewport and projection
                this._cx = 0;
                this._cy = 0;
                // set new view matrix
                this._view = Matrix4.LookAt(0, -size * 0.6f, 0, 0, 0, 0, 0, 0, 1);
                // set model matrix
                this._model_center = Matrix4.CreateTranslation(-cpt) * Matrix4.CreateRotationX((float)Math.PI / 2.0f);
            }
            catch (Exception)
            { }
        }

        private void SelectCurrentControls()
        {
            try
            {
                int controls_id = Int32.Parse(_viewmodel.CurrentControl.Number);
                if (_controls == null || controls_id != _controls_id)
                {
                    _controls_id = controls_id;
                    switch (_controls_id)
                    {
                        default:
                        case 0:
                            var spin = new ModelSpinningControls(
                                () => { return this._period; },
                                () => { return new float[] { 0, 0, (float)this._cx, (float)this._cy }; },
                                () => { return this._view; }
                            );
                            spin.SetAttenuation(1.0f, 0.05f, 0.0f);
                            this._controls = spin;
                            break;

                        case 1:
                            _controls = new NavigationControls(
                                () => { return new float[] { 0, 0, (float)this._cx, (float)this._cy }; },
                                () => { return this._view; },
                                () => { return this._projection; },
                                this.GetDepth,
                                (cursor_pos) => { return new Vector3(0, 0, 0); },
                                (Matrix4 view) => { this._view = view; }
                            );
                            break;

                        case 2:
                            _controls = new FirstPersonControls(
                                () => { return new float[] { 0, 0, (float)this._cx, (float)this._cy }; },
                                () => { return this._view; },
                                (Matrix4 view) => { this._view = view; }
                            );
                            break;
                    }
                }
            }
            catch (Exception)
            { }
        }

        public void Draw(int cx, int cy, double app_t)
        {
            double delta_t = app_t - this._period;
            this._period = app_t;

            // select controls
            SelectCurrentControls();

            if (_controls_id == 2)
            {
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

                float distance = _model.SceneBox.MaxSize;
                this._controls.Move(move_vec * distance);
            }

            // load model
            LoadCurrentModel();

            // setup viewport and projection
            bool resized = this._cx != cx || this._cy != cy;
            if (resized)
            {
                this._cx = cx;
                this._cy = cy;
                GL.Viewport(0, 0, this._cx, this._cy);

                // framebuffers
                // TODO anti aliased framebuffer?
                // TODO update framebuffer size

                if (_fbos != null)
                {
                    foreach (var fbo in _fbos)
                        fbo.Dispose();
                    _fbos.Clear();
                }

                _fbos = new List<IFramebuffer>();
                _fbos.Add(openGLFactory.NewFramebuffer());
                _fbos[0].Create(cx, cy, IFramebuffer.Kind.texture, IFramebuffer.Format.RGBA_F32, true, false);
                _fbos[0].Clear();
                _fbos.Add(openGLFactory.NewFramebuffer());
                _fbos[1].Create(cx, cy, IFramebuffer.Kind.texture, IFramebuffer.Format.RGBA_F32, true, false);
                _fbos[1].Clear();
            }

            Matrix4 model_mat = Matrix4.Identity;
            bool update = false;
            if (_controls_id == 0)
                (model_mat, update) = this._controls.Update();

            // render scene

            GL.Enable(EnableCap.DepthTest);
            //GL.Enable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.CullFace(CullFaceMode.Back);

            this._draw_prog.Use();

            // for both eyes
            for (int i = 0; i < 2; ++i)
            {
                var side = i == 0 ? StereoscopicPerspective.TSide.Left : StereoscopicPerspective.TSide.Right;

                Matrix4 view_mat = model_mat * this._view; // OpenTK `*`-operator is reversed
                float viewdist = view_mat.Row3.Xyz.Length;
                float fov_y = 90.0f * (float)Math.PI / 180.0f;
                float aspect = (float)this._cx / (float)this._cy;
                float eye_dist = EyeScale;
                float focyl_dist = Math.Max(0.1f, FocalScale * 2.0f * viewdist);
                var perspective = new StereoscopicPerspective(view_mat, fov_y, aspect, 0.1f, _far, eye_dist, focyl_dist);
                
                TVP vp = new TVP(perspective.View(side), perspective.Projection(side));
                this._vp_ssbo.Update(ref vp);

                _fbos[i].Bind();
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                if (_model != null)
                    DrawModel(_model.Root, _model_center);
            }

            // stereo filter

            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, DefaultFramebuffer);
            //GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            _stereo_prog.Use();
            _fbos[0].Textures[0].Bind(1);
            _fbos[1].Textures[0].Bind(2);

            int anaglyph_model_id = Int32.Parse(_viewmodel.CurrentAnaglyph.Number); ;
            if (_anaglyph_model_id != anaglyph_model_id)
            {
                _anaglyph_model_id = anaglyph_model_id;
                GL.Uniform1(10, _anaglyph_model_id);
            }


        _quad_vao.Draw();
        }

        private void DrawModel(OpenTK_library.Scene.ModelNode node, Matrix4 model_matrix)
        {
            Matrix4 node_model_matrix = node.ModelMatrix * model_matrix; // OpenTK `*`-operator is reversed

            // update matrices
            if (node.Meshs.Count > 0)
            {
                if (node.ModelSSBONeedsupdate)
                    node.UpdateModel(node_model_matrix);
                node.ModelSSBO.Bind(2);
            }

            // draw meshes
            foreach (var mesh in node.Meshs)
            {
                var vao = mesh.VertexArray;
                vao.Draw();
            }

            // draw children
            foreach (var child_node in node.Children)
            {
                DrawModel(child_node, node_model_matrix);
            }
        }

        private float GetDepth(Vector2 cursor_pos)
        {
            int x = (int)cursor_pos.X;
            int y = this._cy - (int)cursor_pos.Y;
            float[] depth_data = _depth_pack_buffer.ReadDepth(x, y);
            float depth = depth_data.Length > 0 ? depth_data[0] : 1.0f;

            // TODO $$$
            bool valid_depth = depth != 1.0f && depth != 0.0f;
            
            if (valid_depth == false)
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

