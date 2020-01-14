﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using OpenTK; // Vector2, Vector3, Vector4, Matrix4
using OpenTK.Graphics.OpenGL4; // GL

using OpenTK_assimp_example_1.ViewModel;

using OpenTK_library;
using OpenTK_library.Type;
using OpenTK_library.Mesh;
using OpenTK_library.Controls;
using OpenTK_library.OpenGL;
using OpenTK_library_assimp.Builder;
using OpenTK_libray_viewmodel.Model;

namespace OpenTK_assimp_example_1.Model
{
    public class OpenTK_AssimpModel
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

        private OpenTK_ViewModel _viewmodel;
        private int _controls_id = 0;
        private Dictionary<string, string> _model_names = new Dictionary<string, string>();
        private Dictionary<int, OpenTK_library.Scene.Model> _models = new Dictionary<int, OpenTK_library.Scene.Model>();
        private int _model_id = 0;
        private bool _disposed = false;
        private int _cx = 0;
        private int _cy = 0;
        private float _far = 100.0f;
        private OpenTK_library.OpenGL.Version _version = new OpenTK_library.OpenGL.Version();
        private Extensions _extensions = new Extensions();
        private DebugCallback _debug_callback = new DebugCallback();

        private OpenTK_library.Scene.Model _model;
        private OpenTK_library.OpenGL.Program _test_prog;
        private StorageBuffer<TMVP> _mvp_ssbo;
        private StorageBuffer<TLightSource> _light_ssbo;
        private PixelPackBuffer<float> _depth_pack_buffer;

        private Matrix4 _view = Matrix4.Identity;
        private Matrix4 _projection = Matrix4.Identity;
        private Matrix4 _model_center = Matrix4.Identity;
        private IControls _controls;
        double _period = 0;

        public OpenTK_AssimpModel()
        {
            List<string> names = new List<string>();

            //Assembly assembly = Assembly.GetExecutingAssembly();
            //names = new List<string>(assembly.GetManifestResourceNames());

            string working_directory = Directory.GetCurrentDirectory();
            if (Directory.Exists(working_directory))
                names = new List<string>(Directory.GetFiles(working_directory, "*.obj", SearchOption.AllDirectories));

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
                _depth_pack_buffer.Dispose();
                _light_ssbo.Dispose();
                _mvp_ssbo.Dispose();
                _test_prog.Dispose();
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

        public IControls GetControls() => _controls;

        public float GetScale() => _model.SceneBox.MaxSize;

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
            _debug_callback.Init();

            // Create shader program

            string vert_shader = @"#version 460 core
            layout (location = 0) in vec4 a_pos;
            layout (location = 1) in vec3 a_nv;
            layout (location = 2) in vec2 a_uv;
      
            layout (location = 0) out TVertexData
            {
                vec3 pos;
                vec3 nv;
                vec2 uv;
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
                outData.uv   = a_uv;
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
                vec2 uv;
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
                // vec3 color = vec3(fract(inData.uv * 10.0), 0.0);
                vec3 color = inData.nv;

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

                frag_color = vec4( lightCol.rgb, 1.0 );
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

            this._depth_pack_buffer = new PixelPackBuffer<float>();
            this._depth_pack_buffer.Create();

            // states

            GL.Viewport(0, 0, this._cx, this._cy);
            GL.ClearColor(System.Drawing.Color.Beige);
            //GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Enable(EnableCap.DepthTest);
            //GL.Enable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.CullFace(CullFaceMode.Back);

            // matrices

            this._view = Matrix4.LookAt(0.0f, -1.5f, 0, 0, 0, 0, 0, 0, 1);

            float angle = 90.0f * (float)Math.PI / 180.0f;
            float aspect = (float)this._cx / (float)this._cy;
            this._projection = Matrix4.CreatePerspectiveFieldOfView(angle, aspect, 0.1f, _far);
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
                    _model = _models[_model_id];
                }
                else
                {
                    string resource_name = _model_names[_viewmodel.CurrentModel.Text];
                    //Assembly assembly = Assembly.GetExecutingAssembly();
                    //Stream resource_stream = assembly.GetManifestResourceStream(resource_name);
                    //_model = Model.Create(resource_stream);
                    _model = AssimpModel.Create(resource_name);
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
                this._view = Matrix4.LookAt(0, -size * 0.8f, 0, 0, 0, 0, 0, 0, 1);
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
                var keyboardState = OpenTK.Input.Keyboard.GetState();
                Vector3 move_vec = new Vector3(0, 0, 0);
                if (keyboardState[OpenTK.Input.Key.W])
                    move_vec.Y += 1.0f;
                if (keyboardState[OpenTK.Input.Key.S])
                    move_vec.Y -= 1.0f;
                if (keyboardState[OpenTK.Input.Key.D])
                    move_vec.X += 1.0f;
                if (keyboardState[OpenTK.Input.Key.A])
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

                float angle = 90.0f * (float)Math.PI / 180.0f;
                float aspect = (float)this._cx / (float)this._cy;
                this._projection = Matrix4.CreatePerspectiveFieldOfView(angle, aspect, 0.1f, _far);
            }

            Matrix4 model_mat = Matrix4.Identity;
            bool update = false;
            if (_controls_id == 0)
                (model_mat, update) = this._controls.Update();

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            this._test_prog.Use();

            model_mat = _model_center * model_mat; // OpenTK `*`-operator is reversed
            TMVP mvp = new TMVP(model_mat, this._view, this._projection);
            this._mvp_ssbo.Update(ref mvp);

            if (_model != null)
                DrawModel(_model.Root, model_mat);
        }

        private void DrawModel(OpenTK_library.Scene.ModelNode node, Matrix4 model_matrix)
        {
            Matrix4 node_model_matrix = model_matrix * node.ModelMatrix; // OpenTK `*`-operator is reversed

            // update matrices
            if (node.Meshs.Count > 0)
            {
                TMVP mvp = new TMVP(node_model_matrix, this._view, this._projection);
                this._mvp_ssbo.Update(ref mvp);
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
                clip_pos_h = Vector4.Transform(clip_pos_h, this._view);
                clip_pos_h = Vector4.Transform(clip_pos_h, this._projection);
                Vector3 ndc_pos = new Vector3(clip_pos_h.X / clip_pos_h.W, clip_pos_h.Y / clip_pos_h.W, clip_pos_h.Z / clip_pos_h.W);
                if (ndc_pos.Z > -1 && ndc_pos.Z < 1)
                    depth = ndc_pos.Z * 0.5f + 0.5f;
            }

            return depth;
        }
    }
}

