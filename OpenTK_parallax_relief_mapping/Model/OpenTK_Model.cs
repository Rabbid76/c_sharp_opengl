using System;
using System.IO;
using System.Reflection;
using System.Drawing;
using OpenTK.Graphics.OpenGL4; // GL
using OpenTK.Mathematics;
using OpenTK_parallax_relief_mapping.ViewModel;
using OpenTK_library.Type;
using OpenTK_library.Mesh;
using OpenTK_library.Controls;
using OpenTK_library.OpenGL;
using OpenTK_library.OpenGL.OpenGL4;
using OpenTK_libray_viewmodel.Model;

namespace OpenTK_parallax_relief_mapping.Model
{
    /// <summary>
    /// [Bump Mapping with javascript and glsl](https://stackoverflow.com/questions/51988629/bump-mapping-with-javascript-and-glsl/51990812#51990812)
    /// </summary>

    public class OpenTK_Model
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

        private IVertexArrayObject _cube_vao;
        private ITexture _texture;
        private ITexture _normalmap;
        private ITexture _displacementmap;
        private IProgram _parallax_prog;
        private IStorageBuffer _mvp_ssbo;
        private IStorageBuffer _light_ssbo;

        private Matrix4 _view = Matrix4.Identity;
        private Matrix4 _projection = Matrix4.Identity;
        private ModelSpinningControls _spin;
        double _period = 0;

        private OpenTK_ViewModel _viewModel;
        public OpenTK_ViewModel ViewModel
        {
            get { return _viewModel; }
            set { _viewModel = value;  }
        }

        public OpenTK_Model()
        {
            _version = openGLFactory.NewVersionInformation(Console.WriteLine);
            _extensions = openGLFactory.NewExtensionInformation();
            _debug_callback = openGLFactory.NewDebugCallback(Console.WriteLine);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _light_ssbo.Dispose();
                _mvp_ssbo.Dispose();
                _cube_vao.Dispose();
                _texture.Dispose();
                _normalmap.Dispose();
                _displacementmap.Dispose();
                _parallax_prog.Dispose();
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IControls GetControls() => this._spin;

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
                new TVertexFormat(0, 2, 2, 6, false),
                //new TVertexFormat(0, 2, 4, 8, false),
            };

            _cube_vao = openGLFactory.NewVertexArrayObject();
            _cube_vao.AppendVertexBuffer(0, 12, attributes);
            _cube_vao.Create(format, indices);
            _cube_vao.Bind();

            // Create textures

            Assembly assembly = Assembly.GetExecutingAssembly();
            //string[] names = assembly.GetManifestResourceNames();
            Stream textue_stream = assembly.GetManifestResourceStream("OpenTK_parallax_relief_mapping.Resource.woodtiles.jpg");
            Stream normalmap_stream = assembly.GetManifestResourceStream("OpenTK_parallax_relief_mapping.Resource.toy_box_normal.png");
            Stream displacementmap_stream = assembly.GetManifestResourceStream("OpenTK_parallax_relief_mapping.Resource.toy_box_disp.png");

            _texture = openGLFactory.NewTexture();
            _texture.Create2D(new Bitmap(textue_stream));
            _normalmap = openGLFactory.NewTexture();
            _normalmap.Create2D(new Bitmap(normalmap_stream));
            _displacementmap = openGLFactory.NewTexture();
            _displacementmap.Create2D(new Bitmap(displacementmap_stream));

            // Create shader program

            string vert_shader = @"#version 460 core
            layout (location = 0) in vec4 a_pos;
            layout (location = 1) in vec3 a_nv;
            layout (location = 2) in vec2 a_uv;
      
            layout (location = 0) out TVertexData
            {
                vec3 w_pos;
                vec3 w_nv;
                vec2 uv;
                vec3 eye_pos;
            } outData;

            layout(std430, binding = 1) buffer MVP
            {
                mat4 proj;
                mat4 view;
                mat4 model;
            } mvp;

            void main()
            {
                mat3 normal_mat = inverse(transpose(mat3(mvp.model))); 

                outData.w_nv    = normalize(normal_mat * a_nv);
                outData.uv      = a_uv;
                vec4 worldPos   = mvp.model * a_pos;
                outData.w_pos   = worldPos.xyz / worldPos.w;
                outData.eye_pos = inverse(mvp.view)[3].xyz;
                gl_Position     = mvp.proj * mvp.view * mvp.model * a_pos;
            }";

            string frag_shader = @"#version 460 core
            out vec4 frag_color;
            
            layout (location = 0) in TVertexData
            {
                vec3 w_pos;
                vec3 w_nv;
                vec2 uv;
                vec3 eye_pos;
            } inData;

            layout(std430, binding = 2) buffer TLight
            {
                vec4  u_lightDir;
                float u_ambient;
                float u_diffuse;
                float u_specular;
                float u_shininess;
            } light_data;

            layout(binding=1) uniform sampler2D u_diffuse;
            layout(binding=2) uniform sampler2D u_normal_map;
            layout(binding=3) uniform sampler2D u_displacement_map;
            layout(location=1) uniform float u_height_scale;

            vec2 ParallaxMapping (vec2 texCoord, vec3 viewDir)
            {
                float numLayers = 32.0 - 31.0 * abs(dot(vec3(0.0, 0.0, 1.0), viewDir));
                float layerDepth = 1.0 / numLayers;

                vec2 P = viewDir.xy / viewDir.z * u_height_scale;
                vec2 deltaTexCoords = P / numLayers;
                vec2 currentTexCoords = texCoord;

                float currentLayerDepth = 0.0;
                float currentDepthMapValue = texture2D(u_displacement_map, currentTexCoords).r;
                for (int i=0; i<32; ++ i)
                {
                    if (currentLayerDepth >= currentDepthMapValue)
                        break;
                    currentTexCoords -= deltaTexCoords;
                    currentDepthMapValue = texture2D(u_displacement_map, currentTexCoords).r;
                    currentLayerDepth += layerDepth;
                }

                vec2 prevTexCoords = currentTexCoords + deltaTexCoords;
                float afterDepth = currentDepthMapValue - currentLayerDepth;
                float beforeDepth = texture2D(u_displacement_map, prevTexCoords).r - currentLayerDepth + layerDepth;

                float weight = afterDepth / (afterDepth - beforeDepth);
                return prevTexCoords * weight + currentTexCoords * (1.0 - weight);
            }
      
            void main()
            {
                vec3  N       = normalize(inData.w_nv);
                vec3  dp1     = dFdx( inData.w_pos );
                vec3  dp2     = dFdy( inData.w_pos );
                vec2  duv1    = dFdx( inData.uv );
                vec2  duv2    = dFdy( inData.uv );
                vec3  dp2perp = cross(dp2, N);
                vec3  dp1perp = cross(N, dp1);
                vec3  T       = dp2perp * duv1.x + dp1perp * duv2.x;
                vec3  B       = dp2perp * duv1.y + dp1perp * duv2.y;
                float invmax  = inversesqrt(max(dot(T, T), dot(B, B)));
                mat3  tm      = mat3(T * invmax, B * invmax, N);
                mat3  tbn_inv = mat3(vec3(tm[0].x, tm[1].x, tm[2].x), vec3(tm[0].y, tm[1].y, tm[2].y), vec3(tm[0].z, tm[1].z, tm[2].z));

                vec3 view_dir = tbn_inv * normalize(inData.w_pos - inData.eye_pos);
                vec2 uv = ParallaxMapping(inData.uv, view_dir);
                if (uv.x > 1.0 || uv.y > 1.0 || uv.x < 0.0 || uv.y < 0.0)
                    discard;

                vec4 color   = texture(u_diffuse, uv.xy);
                vec3 normalV = texture2D(u_normal_map, uv.st).xyz * 2.0 - 1.0;
                normalV      = normalize(vec3(normalV.xy, normalV.z / max(0.001, 10.0 * u_height_scale)));

                // ambient part
                vec3 lightCol = light_data.u_ambient * color.rgb;
                vec3 eyeV     = normalize(inData.eye_pos - inData.w_pos);
                vec3 lightV   = tbn_inv * normalize( -light_data.u_lightDir.xyz );

                // diffuse part
                float NdotL   = max( 0.0, dot( normalV, lightV ) );
                lightCol     += NdotL * light_data.u_diffuse * color.rgb;

                // specular part
                vec3  halfV     = normalize( eyeV + lightV );
                float NdotH     = max( 0.0, dot( normalV, halfV ) );
                float kSpecular = ( light_data.u_shininess + 2.0 ) * pow( NdotH, light_data.u_shininess ) / ( 2.0 * 3.14159265 );
                lightCol       += kSpecular * light_data.u_specular * color.rgb;
 
                frag_color = vec4( lightCol.rgb, color.a );
            }";

            this._parallax_prog = openGLFactory.VertexAndFragmentShaderProgram(vert_shader, frag_shader);
            this._parallax_prog.Generate();

            // Model view projection shader storage block objects and buffers
            TMVP mvp = new TMVP(Matrix4.Identity, Matrix4.Identity, Matrix4.Identity);
            this._mvp_ssbo = openGLFactory.NewStorageBuffer();
            this._mvp_ssbo.Create(ref mvp);
            this._mvp_ssbo.Bind(1);

            TLightSource light_source = new TLightSource(new Vector4(-1.0f, -0.5f, -2.0f, 0.0f), 0.2f, 0.8f, 0.8f, 10.0f);
            this._light_ssbo = openGLFactory.NewStorageBuffer();
            this._light_ssbo.Create(ref light_source);
            this._light_ssbo.Bind(2);

            // states

            GL.Viewport(0, 0, this._cx, this._cy);
            //GL.ClearColor(System.Drawing.Color.Beige);
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.CullFace(CullFaceMode.Back);

            // matrices and controller

            this._view = Matrix4.LookAt(0.0f, 0.0f, 3.0f, 0, 0, 0, 0, 1, 0);

            float angle = 90.0f * (float)Math.PI / 180.0f;
            float aspect = (float)this._cx / (float)this._cy;
            this._projection = Matrix4.CreatePerspectiveFieldOfView(angle, aspect, 0.1f, 100.0f);

            this._spin = new ModelSpinningControls(
                () => { return this._period; },
                () => { return new float[] { 0, 0, (float)this._cx, (float)this._cy }; },
                () => { return this._view; }
            );
            this._spin.SetAttenuation(1.0f, 0.05f, 0.0f);

            // properties 
            ViewModel.HeightScale = 100;
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

                float angle = 90.0f * (float)Math.PI / 180.0f;
                float aspect = (float)this._cx / (float)this._cy;
                this._projection = Matrix4.CreatePerspectiveFieldOfView(angle, aspect, 0.1f, 100.0f);
            }

            this._spin.Update();
            Matrix4 model_mat = this._spin.autoModelMatrix * this._spin.orbit; // OpenTK `*`-operator is reversed

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            this._parallax_prog.Use();

            this._texture.Bind(1);
            this._normalmap.Bind(2);
            this._displacementmap.Bind(3);
            float height_scale = (float)ViewModel.HeightScale / 1000.0f;
            GL.Uniform1(1, height_scale);

            TMVP mvp = new TMVP(model_mat, this._view, this._projection);
            this._mvp_ssbo.Update(ref mvp);

            _cube_vao.Draw(36);
        }
    }
}
