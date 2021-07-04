using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4; // GL
using OpenTK.Mathematics;
using OpenTK_prallax_cone_step_mapping.ViewModel;
using OpenTK_library.Type;
using OpenTK_library.Mesh;
using OpenTK_library.Controls;
using OpenTK_library.Generator;
using OpenTK_library.OpenGL;
using OpenTK_library.OpenGL.OpenGL4;
using OpenTK_libray_viewmodel.Model;

namespace OpenTK_prallax_cone_step_mapping.Model
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

        private List<TextureGenerator> _generators;
        private List<ITexture> _tbos;
        private int _image_cx = 512; //1024;
        private int _image_cy = 512; //1024;
        private IVertexArrayObject _cube_vao;
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
            set { _viewModel = value; }
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
                _parallax_prog.Dispose();
                foreach (var tbo in _tbos)
                    tbo.Dispose();
                _tbos.Clear();
                foreach (var generrator in _generators)
                    generrator.Dispose();
                _generators.Clear();
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

            // Create textures objects

            _tbos = new List<ITexture>();
            _tbos.Add(openGLFactory.NewTexture());
            _tbos[0].Create2D(_image_cx, _image_cy, ITexture.Format.RGBA_8);
            _tbos.Add(openGLFactory.NewTexture());
            _tbos[1].Create2D(_image_cx, _image_cy, ITexture.Format.RGBA_F32);
            _tbos.Add(openGLFactory.NewTexture());
            _tbos[2].Create2D(_image_cx, _image_cy, ITexture.Format.RGBA_F32);
            GL.TextureParameter(_tbos[2].Object, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TextureParameter(_tbos[2].Object, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            
            // Create generators

            this._generators = new List<TextureGenerator>();
            this._generators.Add(new TextureGenerator(openGLFactory, TextureGenerator.TType.texture_test1, new ITexture[] { _tbos[0] }));
            this._generators.Add(new TextureGenerator(openGLFactory, TextureGenerator.TType.heightmap_test1, new ITexture[] { _tbos[1] }));
            this._generators.Add(new TextureGenerator(openGLFactory, TextureGenerator.TType.cone_step_map, new ITexture[] { _tbos[2] }, new ITexture[] { _tbos[1] }));

            // Create textures

            foreach (var generator in this._generators)
                generator.Generate();
          
            // Create shader program

            string vert_shader = @"#version 460 core
            
            layout (location = 0) in vec3 inPos;
            layout (location = 1) in vec3 inNV;
            layout (location = 2) in vec2 inUV;

            out TVertexData
            {
                vec3  pos;
                vec3  nv;
                vec2  uv;
                float clip;
            } out_data;

            layout(std430, binding = 1) buffer MVP
            {
                mat4 proj;
                mat4 view;
                mat4 model;
            } mvp;

            layout(location=1) uniform vec4 u_clipPlane;

            void main()
            {
                vec3 modelNV   = mat3( mvp.model ) * normalize( inNV );
                out_data.nv    = mat3( mvp.view ) * modelNV;
                out_data.uv    = inUV;
                vec4 worldPos  = mvp.model * vec4( inPos, 1.0 );
                vec4 viewPos   = mvp.view * worldPos;
                out_data.pos   = viewPos.xyz / viewPos.w;
                gl_Position    = mvp.proj * viewPos;
                vec4 clipPlane = vec4(normalize(u_clipPlane.xyz), u_clipPlane.w);
                out_data.clip  = dot(worldPos, clipPlane);
            }";

            string frag_shader = @"#version 460 core
            //#define NORMAL_MAP_TEXTURE
            #define NORMAL_MAP_QUALITY 1

            in TVertexData
            {
                vec3  pos;
                vec3  nv;
                vec2  uv;
                float clip;
            } in_data;

            out vec4 fragColor;

            layout(std430, binding = 1) buffer MVP
            {
                mat4 proj;
                mat4 view;
                mat4 model;
            } mvp;

            layout(location=1) uniform vec4 u_clipPlane;

            layout(std430, binding = 2) buffer TLight
            {
                vec4  u_lightDir;
                float u_ambient;
                float u_diffuse;
                float u_specular;
                float u_shininess;
            } light_data;

            layout(binding=1)  uniform sampler2D u_texture;
            layout(binding=2)  uniform sampler2D u_displacement_map;
            layout(location=2) uniform float     u_displacement_scale;
            layout(location=3) uniform vec2      u_parallax_quality;

            #if defined(NORMAL_MAP_TEXTURE)
            uniform sampler2D u_normal_map;
            #endif

            vec2 GetHeightAndCone( in vec2 texCoords )
            {
                vec2 h_and_c = texture( u_displacement_map, texCoords ).rg;
                return clamp( h_and_c, 0.0, 1.0 );
            }

            vec4 CalculateNormal( in vec2 texCoords )
            {
            #if defined(NORMAL_MAP_TEXTURE)
                float height = GetHeight( texCoords );
                vec3  tempNV = texture( u_normal_map, texCoords ).xyz * 2.0 / 1.0;
                return vec4( normalize( tempNV ), height );
            #else
                vec2 texOffs = 1.0 / textureSize( u_displacement_map, 0 ).xy;
                vec2 scale   = 1.0 / texOffs;
            #if NORMAL_MAP_QUALITY > 1
                float hx[9];
                hx[0] = texture( u_displacement_map, texCoords.st + texOffs * vec2(-1.0, -1.0) ).r;
                hx[1] = texture( u_displacement_map, texCoords.st + texOffs * vec2( 0.0, -1.0) ).r;
                hx[2] = texture( u_displacement_map, texCoords.st + texOffs * vec2( 1.0, -1.0) ).r;
                hx[3] = texture( u_displacement_map, texCoords.st + texOffs * vec2(-1.0,  0.0) ).r;
                hx[4] = texture( u_displacement_map, texCoords.st ).r;
                hx[5] = texture( u_displacement_map, texCoords.st + texOffs * vec2( 1.0, 0.0) ).r;
                hx[6] = texture( u_displacement_map, texCoords.st + texOffs * vec2(-1.0, 1.0) ).r;
                hx[7] = texture( u_displacement_map, texCoords.st + texOffs * vec2( 0.0, 1.0) ).r;
                hx[8] = texture( u_displacement_map, texCoords.st + texOffs * vec2( 1.0, 1.0) ).r;
                vec2  deltaH = vec2(hx[0]-hx[2] + 2.0*(hx[3]-hx[5]) + hx[6]-hx[8], hx[0]-hx[6] + 2.0*(hx[1]-hx[7]) + hx[2]-hx[8]); 
                float h_mid  = hx[4];
            #elif NORMAL_MAP_QUALITY > 0
                float h_mid  = texture( u_displacement_map, texCoords.st ).r;
                float h_xa   = texture( u_displacement_map, texCoords.st + texOffs * vec2(-1.0,  0.0) ).r;
                float h_xb   = texture( u_displacement_map, texCoords.st + texOffs * vec2( 1.0,  0.0) ).r;
                float h_ya   = texture( u_displacement_map, texCoords.st + texOffs * vec2( 0.0, -1.0) ).r;
                float h_yb   = texture( u_displacement_map, texCoords.st + texOffs * vec2( 0.0,  1.0) ).r;
                vec2  deltaH = vec2(h_xa-h_xb, h_ya-h_yb); 
            #else
                vec4  heights = textureGather( u_displacement_map, texCoords, 0 );
                vec2  deltaH  = vec2(dot(heights, vec4(1.0, -1.0, -1.0, 1.0)), dot(heights, vec4(-1.0, -1.0, 1.0, 1.0)));
                float h_mid   = heights.w; 
            #endif
                return vec4( normalize( vec3( deltaH * scale, 1.0 ) ), h_mid );
            #endif 
            }

            // the super fast version
            // (change number of iterations at run time)
            float intersect_cone_fixed(in vec2 dp, in vec3 ds)
            {
                // the 'not Z' component of the direction vector
                // (requires that the vector ds was normalized!)
                float iz = sqrt(1.0 - ds.z * ds.z);
                // my starting location (is at z=1,
                // and moving down so I don't have
                // to invert height maps)
                // texture lookup (and initialized to starting location)
                vec4 t;
                // scaling distance along vector ds
                float sc;
                // the ds.z component is positive!
                // (headed the wrong way, since
                // I'm using heightmaps)
                // find the initial location and height
                t = texture(u_displacement_map, dp);
                // right, I need to take one step.
                // I use the current height above the texture,
                // and the information about the cone-ratio
                // to size a single step. So it is fast and
                // precise! (like a coneified version of
                // 'space leaping', but adapted from voxels)
                sc = (1.0 - ds.z * sc - t.r) / (ds.z + iz / (t.g * t.g));
                // and repeat a few (4x) times
                t = texture(u_displacement_map, dp + ds.xy * sc);
                sc += (1.0 - ds.z * sc - t.r) / (ds.z + iz / (t.g * t.g));
                t = texture(u_displacement_map, dp + ds.xy * sc);
                sc += (1.0 - ds.z * sc - t.r) / (ds.z + iz / (t.g * t.g));
                t = texture(u_displacement_map, dp + ds.xy * sc);
                sc += (1.0 - ds.z * sc - t.r) / (ds.z + iz / (t.g * t.g));
                t = texture(u_displacement_map, dp + ds.xy * sc);
                sc += (1.0 - ds.z * sc - t.r) / (ds.z + iz / (t.g * t.g));
                // and another five!
                t = texture(u_displacement_map, dp + ds.xy * sc);
                sc += (1.0 - ds.z * sc - t.r) / (ds.z + iz / (t.g * t.g));
                t = texture(u_displacement_map, dp + ds.xy * sc);
                sc += (1.0 - ds.z * sc - t.r) / (ds.z + iz / (t.g * t.g));
                t = texture(u_displacement_map, dp + ds.xy * sc);
                sc += (1.0 - ds.z * sc - t.r) / (ds.z + iz / (t.g * t.g));
                t = texture(u_displacement_map, dp + ds.xy * sc);
                sc += (1.0 - ds.z * sc - t.r) / (ds.z + iz / (t.g * t.g));
                t = texture(u_displacement_map, dp + ds.xy * sc);
                sc += (1.0 - ds.z * sc - t.r) / (ds.z + iz / (t.g * t.g));
                // return the vector length needed to hit the height-map
                return (sc);
            }

            // (and you can do LOD by changing 'conesteps' based on size/distance, etc.)
            float intersect_cone_loop(in vec2 dp, in vec3 ds)
            {
                float maxBumpHeight = u_displacement_scale;

                const int conesteps = 10; // ???
                                          // the 'not Z' component of the direction vector
                                          // (requires that the vector ds was normalized!)
                float iz = sqrt(1.0 - ds.z * ds.z);
                // my starting location (is at z=1,
                // and moving down so I don't have
                // to invert height maps)
                // texture lookup (and initialized to starting location)
                vec4 t;
                // scaling distance along vector ds
                float sc = 0.0;
                //t=texture2D(stepmap,dp);
                //return (max(0.0,-(t.b-0.5)*ds.x-(t.a-0.5)*ds.y));
                // the ds.z component is positive!
                // (headed the wrong way, since
                // I'm using heightmaps)
                // adaptive (same speed as it averages the same # steps)
                //for (int i = int(float(conesteps)*(0.5+iz)); i > 0; --i)
                // fixed
                for (int i = conesteps; i > 0; --i)
                {
                    // find the new location and height
                    t = texture(u_displacement_map, dp + ds.xy * sc);
                    t.r = t.r * maxBumpHeight;
                    t.g = t.g / maxBumpHeight;

                    // right, I need to take one step.
                    // I use the current height above the texture,
                    // and the information about the cone-ratio
                    // to size a single step. So it is fast and
                    // precise! (like a coneified version of
                    // 'space leaping', but adapted from voxels)
                    sc += (1.0 - ds.z * sc - t.r) / (ds.z + iz / (t.g * t.g));
                }
                // return the vector length needed to hit the height-map
                return (sc);
            }

            // slowest, but best quality
            float intersect_cone_exact(in vec2 dp, in vec3 ds)
            {
                vec2 texsize = textureSize(u_displacement_map, 0);

                // minimum feature size parameter
                float w = 1.0 / max(texsize.x, texsize.y);
                // the 'not Z' component of the direction vector
                // (requires that the vector ds was normalized!)
                float iz = sqrt(1.0 - ds.z * ds.z);
                // my starting location (is at z=1,
                // and moving down so I don't have
                // to invert height maps)
                // texture lookup
                vec4 t;
                // scaling distance along vector ds
                float sc = 0.0;
                // the ds.z component is positive!
                // (headed the wrong way, since
                // I'm using heightmaps)
                // find the starting location and height
                t = texture(u_displacement_map, dp);
                while (1.0 - ds.z * sc > t.r)
                {
                    // right, I need to take one step.
                    // I use the current height above the texture,
                    // and the information about the cone-ratio
                    // to size a single step. So it is fast and
                    // precise! (like a coneified version of
                    // 'space leaping', but adapted from voxels)
                    sc += w + (1.0 - ds.z * sc - t.r) / (ds.z + iz / (t.g * t.g));
                    // find the new location and height
                    t = texture(u_displacement_map, dp + ds.xy * sc);
                }
                // back off one step
                sc -= w;
                // return the vector length needed to hit the height-map
                return (sc);
            }

            // Parallax Occlusion Mapping in GLSL [http://sunandblackcat.com/tipFullView.php?topicid=28]
            vec3 ConeStep(in float frontFace, in vec3 texDir3D, in vec2 texCoord)
            {
                float maxBumpHeight = 1.0;
                vec2 quality_range = u_parallax_quality;

                // [Determinante](https://de.wikipedia.org/wiki/Determinante)
                // A x B = A.x * B.y - A.y * B.x = dot(A, vec2(B.y,-B.x)) = det(mat2(A,B))

                // [How do you detect where two line segments intersect?](https://stackoverflow.com/questions/563198/how-do-you-detect-where-two-line-segments-intersect)
                vec2 R = normalize(vec2(length(texDir3D.xy), texDir3D.z));
                vec2 P = R * maxBumpHeight / texDir3D.z;

                vec2 tex_size = textureSize(u_displacement_map, 0).xy;
                vec2 min_tex_step = normalize(texDir3D.xy) / tex_size;
                float min_step = length(min_tex_step) * 1.0 / R.x;

                float t = 0.0;
                const int max_no_of_steps = int(5.0 + quality_range * 45.0);
                for (int i = 0; i < max_no_of_steps; ++i)
                {
                    vec3 sample_pt = vec3(texCoord.xy, maxBumpHeight) + texDir3D * t;

                    vec2 h_and_c = GetHeightAndCone(sample_pt.xy);
                    float h = h_and_c.x * maxBumpHeight;
                    float c = h_and_c.y * h_and_c.y / maxBumpHeight;

                    vec2 C = P + R * t;
                    if (C.y <= h)
                        break;

                    vec2 Q = vec2(C.x, h);
                    vec2 S = normalize(vec2(c, 1.0));
                    float new_t = dot(Q - P, vec2(S.y, -S.x)) / dot(R, vec2(S.y, -S.x));
                    t = max(t + min_step, new_t);
                }

                vec2 texC = texCoord.xy + texDir3D.xy * t;
                float mapHeight = GetHeightAndCone(texC.xy).x - step(frontFace, 0.0);
                return vec3(texC.xy, mapHeight);
            }

            void main()
            {
                vec3 objPosEs = in_data.pos;
                vec3 objNormalEs = in_data.nv;
                vec2 texCoords = in_data.uv.st;
                float frontFace = gl_FrontFacing ? 1.0 : -1.0; // TODO $$$ sign(dot(N,objPosEs));
                                                               //vec3  normalEs     = frontFace * normalize( objNormalEs );

                // orthonormal tangent space matrix
                //vec3  p_dx         = dFdx( objPosEs );
                //vec3  p_dy         = dFdy( objPosEs );
                //vec2  tc_dx        = dFdx( texCoords );
                //vec2  tc_dy        = dFdy( texCoords );
                //float texDet       = determinant( mat2( tc_dx, tc_dy ) );
                //vec3  tangentVec   = ( tc_dy.y * p_dx - tc_dx.y * p_dy ) / abs( texDet );
                //vec3  tangentEs    = normalize( tangentVec - normalEs * dot(tangentVec, normalEs ) );
                //mat3  tbnMat       = mat3( sign( texDet ) * tangentEs, cross( normalEs, tangentEs ), normalEs );

                // Followup: Normal Mapping Without Precomputed Tangents [http://www.thetenthplanet.de/archives/1180]
                vec3 N = frontFace * normalize(objNormalEs);
                vec3 dp1 = dFdx(objPosEs);
                vec3 dp2 = dFdy(objPosEs);
                vec2 duv1 = dFdx(texCoords);
                vec2 duv2 = dFdy(texCoords);
                vec3 dp2perp = cross(dp2, N);
                vec3 dp1perp = cross(N, dp1);
                vec3 T = dp2perp * duv1.x + dp1perp * duv2.x;
                vec3 B = dp2perp * duv1.y + dp1perp * duv2.y;
                float invmax = inversesqrt(max(dot(T, T), dot(B, B)));
                mat3 tbnMat = mat3(T * invmax, B * invmax, N * u_displacement_scale);
                mat3 inv_tbnMat = inverse(tbnMat);

                vec3 texDir3D = normalize(inv_tbnMat * objPosEs);
                vec3 newTexCoords = abs(u_displacement_scale) < 0.001 ? vec3(texCoords.st, 0.0) : ConeStep(frontFace, texDir3D, texCoords.st);

                //float depth_displ    = length(tbnMat * (newTexCoords.z * texDir3D.xyz / abs(texDir3D.z))); 
                //vec3  view_pos_displ = objPosEs - depth_displ * normalize(objPosEs);
                vec3 displ_vec = tbnMat * (newTexCoords.z * texDir3D.xyz / abs(texDir3D.z));
                vec3 view_pos_displ = objPosEs - displ_vec;
                vec4 modelPos = inverse(mvp.view) * vec4(view_pos_displ, 1.0);
                vec4 clipPlane = vec4(normalize(u_clipPlane.xyz), u_clipPlane.w);
                float clip_dist = dot(modelPos, clipPlane);
                //float clip_dist      = in_data.clip;
                if (clip_dist < 0.0)
                    discard;

                texCoords.st = newTexCoords.xy;
                vec4 normalVec = CalculateNormal(texCoords);
                //vec3  nvMappedEs   = normalize( tbnMat * normalVec.xyz );
                vec3 nvMappedEs = u_displacement_scale < 0.001 ? normalize(objNormalEs) : (normalize(transpose(inv_tbnMat) * normalVec.xyz));

                //vec3 color = in_data.col;
                vec3 color = texture(u_texture, texCoords.st).rgb;

                // ambient part
                vec3 lightCol = light_data.u_ambient * color;

                // diffuse part
                vec3 normalV = normalize(nvMappedEs);
                vec3 lightV = -normalize(mat3(mvp.view) * light_data.u_lightDir.xyz);
                float NdotL = max(0.0, dot(normalV, lightV));
                lightCol += NdotL * light_data.u_diffuse * color;

                // specular part
                vec3 eyeV = normalize(-objPosEs);
                vec3 halfV = normalize(eyeV + lightV);
                float NdotH = max(0.0, dot(normalV, halfV));
                float kSpecular = (light_data.u_shininess + 2.0) * pow(NdotH, light_data.u_shininess) / (2.0 * 3.14159265);
                lightCol += kSpecular * light_data.u_specular * color;

                fragColor = vec4(lightCol.rgb, 1.0);

                vec4 proj_pos_displ = mvp.proj * vec4(view_pos_displ.xyz, 1.0);
                float depth = 0.5 + 0.5 * proj_pos_displ.z / proj_pos_displ.w;
                gl_FragDepth = u_displacement_scale < 0.001 ? gl_FragCoord.z : depth;

                //fragColor = vec4( vec3(1.0-depth), 1.0 );
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

            // no face culling, because of clipping
            //GL.Enable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.CullFace(CullFaceMode.Back);

            // matrices and controller

            this._view = Matrix4.LookAt(0.0f, 0.0f, 2.5f, 0, 0, 0, 0, 1, 0);

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
            ViewModel.HeightScale = 50;
            ViewModel.QualityScale = 50;
            ViewModel.ClipScale = 50;
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

            this._tbos[0].Bind(1);
            this._tbos[2].Bind(2);

            float clip_scale = (float)ViewModel.ClipScale / 100.0f;
            float height_scale = (float)ViewModel.HeightScale / 500.0f;
            float quality_scale = (float)ViewModel.QualityScale / 100.0f;

            GL.Uniform4(1, -2.0f, -1.0f, -2.0f, clip_scale * 1.7321f);
            GL.Uniform1(2, height_scale);
            GL.Uniform2(3, quality_scale, (float)1.0);

            TMVP mvp = new TMVP(model_mat, this._view, this._projection);
            this._mvp_ssbo.Update(ref mvp);

            _cube_vao.Draw(36);
        }
    }
}