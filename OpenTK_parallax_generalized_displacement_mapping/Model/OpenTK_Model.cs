using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4; // GL
using OpenTK.Mathematics;
using OpenTK_parallax_generalized_displacement_mapping.ViewModel;
using OpenTK_library.Type;
using OpenTK_library.Mesh;
using OpenTK_library.Controls;
using OpenTK_library.Generator;
using OpenTK_library.OpenGL;
using OpenTK_library.OpenGL.OpenGL4;
using OpenTK_libray_viewmodel.Model;

namespace OpenTK_parallax_generalized_displacement_mapping.Model
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
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

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
                vec3 world_pos;
                vec3 world_nv;
                vec2 uv;
            } out_data;

            layout(std430, binding = 1) buffer MVP
            {
                mat4 proj;
                mat4 view;
                mat4 model;
            } mvp;

            void main()
            {
                vec4 worldPos      = mvp.model * vec4(inPos, 1.0);
                out_data.world_pos = worldPos.xyz / worldPos.w;
                out_data.world_nv  = normalize( mat3(mvp.model) * inNV );
                out_data.uv        = inUV;
            }";

            string geo_shader = @"#version 460 core
            
            layout( triangles ) in;
            layout( triangle_strip, max_vertices = 15 ) out;

            in TVertexData
            {
                vec3 world_pos;
                vec3 world_nv;
                vec2 uv;
            } inData[];

            out TGeometryData
            {
                vec3  pos;
                vec3  nv;
                vec3  tv;
                vec3  bv;
                vec3  uvh;
                vec4  d;
                float clip;
            } outData;

            layout(std430, binding = 1) buffer MVP
            {
                mat4 proj;
                mat4 view;
                mat4 model;
            } mvp;

            layout(location=1) uniform vec4  u_clipPlane;
            layout(location=2) uniform float u_displacement_scale;

            void main()
            {
                // tangent space
                //vec3  p_dA       = vsPos[1].xyz - vsPos[0].xyz;
                //vec3  p_dB       = vsPos[2].xyz - vsPos[0].xyz;
                //vec2  tc_dA      = inData[1].uv - inData[0].uv;
                //vec2  tc_dB      = inData[2].uv - inData[0].uv;
                //float texDet     = determinant( mat2( tc_dA, tc_dB ) );
                //outData.vsTV     = ( tc_dB.y * p_dA - tc_dA.y * p_dB ) / texDet;
                //outData.vsBVsign = sign(texDet);

                vec3 world_pos_up[3];
                for (int i = 0; i < 3; ++ i)
                    world_pos_up[i] = inData[i].world_pos + inData[i].world_nv * u_displacement_scale;

                vec3 view_nv[3];
                vec3 view_pos[3];
                vec3 view_pos_up[3];
                for (int i = 0; i < 3; ++ i)
                {
                    vec4 viewPos   = mvp.view * vec4(inData[i].world_pos, 1.0);
                    view_nv[i]     = normalize(mat3(mvp.view) * inData[i].world_nv);
                    view_pos[i]    = viewPos.xyz;
                    view_pos_up[i] = view_pos[i] + view_nv[i] * u_displacement_scale;
                    //view_pos_up[i] = (mvp.view * vec4(world_pos_up[i], 1.0)).xyz;
                }

                // tangent space
                // Followup: Normal Mapping Without Precomputed Tangents [http://www.thetenthplanet.de/archives/1180]
                vec3  dp1  = view_pos[1].xyz - view_pos[0].xyz;
                vec3  dp2  = view_pos[2].xyz - view_pos[0].xyz;
                vec2  duv1 = inData[1].uv.xy - inData[0].uv.xy;
                vec2  duv2 = inData[2].uv.xy - inData[0].uv.xy;

                vec3 nv[3];
                vec3 tv[3];
                vec3 bv[3];
                for ( int i=0; i < 3; ++i )
                {
                    vec3 dp2perp = cross(dp2, view_nv[i]); 
                    vec3 dp1perp = cross(view_nv[i], dp1);
        
                    nv[i] = view_nv[i] * u_displacement_scale;
                    tv[i] = dp2perp * duv1.x + dp1perp * duv2.x;
                    bv[i] = dp2perp * duv1.y + dp1perp * duv2.y;
                }

    
                // distance to opposite planes
                float d[3];
                float d_up[3];
                float d_opp[3];
                float d_opp_up[3];
                float d_top[3];
                for ( int i0=0; i0 < 3; ++i0 )
                {
                  d[i0]    = length(view_pos[i0].xyz);
                  d_up[i0] = length(view_pos_up[i0].xyz);

                  int i1 = (i0+1) % 3; 
                  int i2 = (i0+2) % 3; 
                  vec3 edge    = view_pos[i2].xyz - view_pos[i1].xyz;
                  vec3 edge_up = view_pos_up[i2].xyz - view_pos_up[i1].xyz;
                  vec3 up      = view_nv[i1].xyz + view_nv[i2].xyz;

                  // intersect the view ray trough a corner point of the prism (with triangular base)
                  // with the opposite side face of the prism
                  //
                  // d = dot(P0 - R0, N) / dot(D, N)
                  //
                  // R0 : point on the ray
                  // D  : direction of the ray
                  // P0 : point on the plane
                  // N  : norma vector of the plane
                  // d  :: distance from R0 to the intersection with the plane along D
      
                  //vec3  R0      = vec3(view_pos[i0].xy, 0.0); // for orthographic projection
                  //vec3  D       = vec3(0.0, 0.0, -1.0); // for orthographic projection
                  vec3  R0      = vec3(0.0); // for persepctive projection
                  vec3  D       = normalize(view_pos[i0].xyz); // for persepctive projection
                  vec3  N       = normalize(cross(edge, up));
                  vec3  P0      = (view_pos[i1].xyz+view_pos[i2].xyz)/2.0;
                  d_opp[i0]     = dot(P0 - R0, N) / dot(D, N);

                  //vec3  R0_up   = vec2(view_pos_up[i0].xyz, 0.0); // for orthographic projection
                  //vec3  D_up  = vec3(0.0, 0.0, -1.0); // for orthographic projection
                  vec3  R0_up   = vec3(0.0); // for persepctive projection 
                  vec3  D_up    = normalize(view_pos_up[i0].xyz); // for persepctive projection
                  vec3  N_up    = normalize(cross(edge_up, up));
                  vec3  P0_up   = (view_pos_up[i1].xyz+view_pos_up[i2].xyz)/2.0;
                  d_opp_up[i0]  = dot(P0_up - R0_up, N_up) / dot(D_up, N_up);

                  //vec3  N_top   = view_nv[i0];
                  vec3  N_top   = normalize(view_nv[0]+view_nv[1]+view_nv[2]);
                  vec3  P0_top  = (view_pos_up[0].xyz + view_pos_up[1].xyz + view_pos_up[2].xyz)/3.0;
                  d_top[i0]     = dot(P0_top - R0, N_top) / dot(D, N_top);
                }

                vec4 clipPlane = vec4(normalize(u_clipPlane.xyz), u_clipPlane.w);

                for ( int i=0; i < 3; ++i )
                {
                    outData.nv   = nv[i];
                    outData.tv   = tv[i];
                    outData.bv   = bv[i];
                    outData.pos  = view_pos[i];
                    outData.uvh  = vec3(inData[i].uv, 0.0);
                    outData.d    = vec4( i==0 ? d_opp[i] : d[i], i==1 ? d_opp[i] : d[i], i==2 ? d_opp[i] : d[i], d_top[i] );
                    outData.clip = dot(vec4(inData[i].world_pos, 1.0), clipPlane);
                    gl_Position  = mvp.proj * vec4( outData.pos, 1.0 );
                    EmitVertex();
                }
                EndPrimitive();

                vec3 cpt_tri = (view_pos[0] + view_pos[1] + view_pos[2]) / 3.0;
                for ( int i0=0; i0 < 3; ++i0 )
                {
                    int i1 = (i0+1) % 3;
                    int i2 = (i0+2) % 3; 

                    vec3 cpt_edge    = (view_pos[i0] + view_pos[i1]) / 2.0;
                    vec3 dir_to_edge = cpt_edge - cpt_tri; // direction from thge center of the triangle to the edge

                    vec3 edge    = view_pos[i1] - view_pos[i0];
                    vec3 nv_edge  = nv[i0] + nv[i1];
                    vec3 nv_side = cross(edge, nv_edge); // normal vector of a side of the prism
                    nv_side *= sign(dot(nv_side, dir_to_edge)); // orentate the normal vector out of the center of the triangle

                    // a front face is a side of the prism, where the normal vector is directed against the view vector
                    float frontface = sign(dot(cpt_edge, -nv_side));

                    float d_opp0, d_opp1, d_opp_up0, d_opp_up1;
                    if ( frontface > 0.0 )
                    {
                        d_opp0    = max(d[i0], d_opp[i0]);
                        d_opp1    = max(d[i1], d_opp[i1]);
                        d_opp_up0 = max(d_up[i0], d_opp_up[i0]);
                        d_opp_up1 = max(d_up[i1], d_opp_up[i1]);
                    }
                    else
                    {
                        d_opp0    = min(d[i0], d_opp[i0]);
                        d_opp1    = min(d[i1], d_opp[i1]);
                        d_opp_up0 = min(d_up[i0], d_opp_up[i0]);
                        d_opp_up1 = min(d_up[i1], d_opp_up[i1]);
                    }

                    outData.nv   = nv[i0];
                    outData.tv   = tv[i0];
                    outData.bv   = bv[i0];
                    outData.pos  = view_pos[i0];
                    outData.uvh  = vec3(inData[i0].uv, 0.0);
                    outData.d    = vec4(d_opp0, d[i0], frontface, d_top[i0]);
                    outData.clip = dot(vec4(inData[i0].world_pos, 1.0), clipPlane);
                    gl_Position  = mvp.proj * vec4( outData.pos, 1.0 );
                    EmitVertex();

                    outData.nv   = nv[i1];
                    outData.tv   = tv[i1];
                    outData.bv   = bv[i1];
                    outData.pos  = view_pos[i1];
                    outData.uvh  = vec3(inData[i1].uv, 0.0);
                    outData.d    = vec4(d[i1], d_opp1, frontface, d_top[i1]);
                    outData.clip = dot(vec4(inData[i1].world_pos, 1.0), clipPlane);
                    gl_Position  = mvp.proj * vec4( outData.pos, 1.0 );
                    EmitVertex();

                    outData.nv   = nv[i0];
                    outData.tv   = tv[i0];
                    outData.bv   = bv[i0];
                    outData.pos  = view_pos_up[i0];
                    outData.uvh  = vec3(inData[i0].uv, 1.0);
                    outData.d    = vec4(d_opp_up0, d_up[i0], frontface, 0.0);
                    outData.clip = dot(vec4(world_pos_up[i0], 1.0), clipPlane);
                    gl_Position  = mvp.proj * vec4( outData.pos, 1.0 );
                    EmitVertex();

                    outData.nv   = nv[i1];
                    outData.tv   = tv[i1];
                    outData.bv   = bv[i1];
                    outData.pos  = view_pos_up[i1];
                    outData.uvh  = vec3(inData[i1].uv, 1.0);
                    outData.d    = vec4(d_up[i1], d_opp_up1, frontface, 0.0);
                    outData.clip = dot(vec4(world_pos_up[i1], 1.0), clipPlane);
                    gl_Position  = mvp.proj * vec4( outData.pos, 1.0 );
                    EmitVertex();

                    EndPrimitive();
                }
            }";

            string frag_shader = @"#version 460 core
            //#define NORMAL_MAP_TEXTURE
            #define NORMAL_MAP_QUALITY 1

            in TGeometryData
            {
                vec3  pos;
                vec3  nv;
                vec3  tv;
                vec3  bv;
                vec3  uvh;
                vec4  d;
                float clip;
            } in_data;

            out vec4 fragColor;

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

            layout(std430, binding = 1) buffer MVP
            {
                mat4 proj;
                mat4 view;
                mat4 model;
            } mvp;

            layout(location=1) uniform vec4 u_clipPlane;

            #if defined(NORMAL_MAP_TEXTURE)
            uniform sampler2D u_normal_map;
            #endif

            float CalculateHeight( in vec2 texCoords )
            {
                float height = texture( u_displacement_map, texCoords ).x;
                return clamp( height, 0.0, 1.0 );
            }

            vec2 GetHeightAndCone( in vec2 texCoords )
            {
                vec2 h_and_c = texture( u_displacement_map, texCoords ).rg;
                return clamp( h_and_c, 0.0, 1.0 );
            }

            vec4 CalculateNormal( in vec2 texCoords )
            {
            #if defined(NORMAL_MAP_TEXTURE)
                float height = CalculateHeight( texCoords );
                vec3  tempNV = texture( u_normal_map, texCoords ).xyz * 2.0 / 1.0;
                return vec4( normalize( tempNV ), height );
            #else
                vec2 texOffs = 1.0 / vec2(textureSize( u_displacement_map, 0 ).xy);
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

            vec3 Parallax( in float frontFace, in vec3 texCoord, in vec3 tbnP0, in vec3 tbnP1, in vec3 tbnStep )
            {   
                // inverse height map: -1 for inverse height map or 1 if not inverse
                // height maps of back faces base triangles are inverted
                float back_face = step(0.0, -frontFace); 
                vec3 texC0 = texCoord.xyz + tbnP0 + back_face * vec3(tbnStep.xy, 0.0);
                vec3 texC1 = texCoord.xyz + tbnP1 + back_face * vec3(tbnStep.xy, 0.0);

                // sample steps and quality
                vec2  quality_range  = u_parallax_quality;
                float quality        = mix( quality_range.x, quality_range.y, 1.0 - abs(normalize(tbnStep).z) );
                float numSteps       = clamp( quality * 50.0, 1.0, 50.0 );
                int   numBinarySteps = int( clamp( quality * 10.0, 1.0, 10.0 ) );

                // change of the height per step
                float bumpHeightStep = (texC0.z-texC1.z) / (numSteps-1.0);

                float bestBumpHeight = texC1.z;
                float mapHeight      = 1.0;
                for ( int i = 0; i < int( numSteps ); ++ i )
                {
                    mapHeight = back_face + frontFace * CalculateHeight( mix(texC0.xy, texC1.xy, (bestBumpHeight-texC0.z)/(texC1.z-texC0.z)) );
                    if ( mapHeight >= bestBumpHeight || bestBumpHeight > 1.0 )
                        break;
                    bestBumpHeight += bumpHeightStep;   
                } 

                if ( texCoord.z < 0.0001 || bestBumpHeight >= 0.0 ) // if not a silhouett 
                {
                    // binary steps, starting at the previous sample point 
                    bestBumpHeight -= bumpHeightStep;
                    for ( int i = 0; i < numBinarySteps; ++ i )
                    {
                        bumpHeightStep *= 0.5;
                        bestBumpHeight += bumpHeightStep;
                        mapHeight       = back_face + frontFace * CalculateHeight( mix(texC0.xy, texC1.xy, (bestBumpHeight-texC0.z)/(texC1.z-texC0.z)) );
                        bestBumpHeight -= ( bestBumpHeight < mapHeight ) ? bumpHeightStep : 0.0;
                    }

                    // final linear interpolation between the last to heights 
                    bestBumpHeight += bumpHeightStep * clamp( ( bestBumpHeight - mapHeight ) / abs(bumpHeightStep), 0.0, 1.0 );
                }

                // set displaced texture coordiante and intersection height
                vec2 texC  = mix(texC0.xy, texC1.xy, (bestBumpHeight-texC0.z)/(texC1.z-texC0.z));
                mapHeight  = bestBumpHeight;
    
                return vec3(texC.xy, mapHeight);
            }

            void main()
            {
                vec3  objPosEs    = in_data.pos;
                vec3  objNormalEs = in_data.nv;
                vec3  texCoords   = in_data.uvh.stp;
                float frontFace   = (texCoords.p > 0.0) ? 1.0 : (gl_FrontFacing ? 1.0 : -1.0); // TODO $$$ sign(dot(N,objPosEs));
    
                //vec3  tangentEs    = normalize( tangentVec - normalEs * dot(tangentVec, normalEs ) );
                //mat3  tbnMat       = mat3( tangentEs, binormalSign * cross( normalEs, tangentEs ), normalEs );

                // tangent space
                // Followup: Normal Mapping Without Precomputed Tangents [http://www.thetenthplanet.de/archives/1180]
                //   If backface, then the normal vector is downwards the (co-)tangent space.
                //   In this case the normal has to be mirrored to make the parallax algorithm prpper work.
                vec3  N           = frontFace * objNormalEs;  
                vec3  T           = in_data.tv;
                vec3  B           = in_data.bv;
                float invmax      = inversesqrt(max(dot(T, T), dot(B, B)));
                mat3  tbnMat      = mat3(T * invmax, B * invmax, N * invmax);
                mat3  inv_tbnMat  = inverse( tbnMat );

                // distances to the sides of the prism
                bool  is_silhouette    = texCoords.p > 0.0001;
                bool  silhouette_front = in_data.d.z > 0.0;
                float df = length( objPosEs );
                float d0;
                float d1;
                if ( is_silhouette == false )
                {
                    if ( frontFace > 0.0 )
                    {
                        d1 = 0.0;
                        d0 = min(min(in_data.d.x, in_data.d.y), in_data.d.z) - df; // TODO $$$ * 0.9
                    }
                    else
                    {
                        d0 = 0.0;
                        d1 = max(max(in_data.d.x, in_data.d.y), in_data.d.z) - df;
                    }
                }
                else
                {
                    d1 = min(in_data.d.x, in_data.d.y) - df;
                    d0 = max(in_data.d.x, in_data.d.y) - df;
                }

                // intersection points
                vec3  V  = objPosEs / df;
                vec3  P0 = V * d0;
                vec3  P1 = V * d1;
   
                vec3  tbnP0        = inv_tbnMat * P0;
                vec3  tbnP1        = inv_tbnMat * P1;
                vec3  tbnDir       = normalize(inv_tbnMat * objPosEs);
                vec3  tbnTopMax    = tbnDir / tbnDir.z;

                // geometry situation
                float base_height  = texCoords.p;                     // intersection level (height) on the silhouette (side of prism geometry)
                bool  is_up_isect  = is_silhouette && tbnDir.z > 0.0; // upwards intersection on potential silhouette (side of prism geometry)

                // sample start and end height (level)
                float delta_height0 = is_up_isect ? 1.05*(1.0-base_height) : base_height; // TODO $$$ 1.05 ??? 
                float delta_height1 = is_up_isect ? 0.0 : (base_height - 1.0);

                // sample distance
                //vec3 texDist = tbnDir / abs(tbnDir.z); // (z is negative) the direction vector points downwards int tangent-space
                vec3 texDist = is_silhouette == false ? tbnDir / abs(tbnDir.z) : tbnDir / max(abs(tbnDir.z), 0.5*length(tbnDir.xy));
                vec3 tbnStep = vec3(texDist.xy, sign(tbnDir.z));

                // start and end of samples
                tbnP0 = delta_height0 * tbnStep; // sample end - bottom of prism 
                tbnP1 = delta_height1 * tbnStep; // sample start - top of prism 
                if ( is_silhouette )
                {
                    if ( silhouette_front )
                    {
                        tbnP1 = vec3(0.0);
                    }
                    else
                    {
                        tbnP0 = vec3(0.0);
                    }
                }

                vec3  newTexCoords = abs(u_displacement_scale) < 0.001 ? vec3(texCoords.st, 0.0) : Parallax( frontFace, texCoords.stp, tbnP0, tbnP1, tbnStep );
                vec3  tex_offst    = newTexCoords.stp-texCoords.stp;
    
                // slihouett discard (clipping)
                if ( is_silhouette )
                {
                    if ( newTexCoords.z > 1.000001 ||                // clip at top plane of the prism
                         newTexCoords.z < 0.0 ||                    // clip at bottom plane of the prism
                         dot(tex_offst, tbnDir)*in_data.d.z < 0.0 ) // clip back side faces at the back and clip front side faces at the front
                        discard;
                    if ( silhouette_front == false && is_up_isect )
                        discard;
                }
    
                vec3  displ_vec      = tbnMat * tex_offst/invmax;
                vec3  view_pos_displ = objPosEs + displ_vec;
                texCoords.st         = newTexCoords.xy;

            #define DEBUG_CLIP
            #define DEBUG_CLIP_DISPLACED

            #if defined (DEBUG_CLIP)
                vec4  modelPos       = inverse(mvp.view) * vec4(view_pos_displ, 1.0);
                vec4  clipPlane      = vec4(normalize(u_clipPlane.xyz), u_clipPlane.w);
            #if defined (DEBUG_CLIP_DISPLACED)
                float clip_dist      = dot(modelPos, clipPlane);
            #else
                float clip_dist      = in_data.clip;
            #endif
                if ( clip_dist < 0.0 )
                    discard;
            #endif
    
                vec4  normalVec = CalculateNormal( texCoords.st );
                // If back face, then the height map has been inverted (except cone step map). This causes that the normalvector has to be adapted.
                normalVec.xy *= frontFace;
                //vec3  nvMappedEs = normalize( tbnMat * normalVec.xyz );
                vec3  nvMappedEs = normalize( transpose(inv_tbnMat) * normalVec.xyz ); // TODO $$$ evaluate `invmax`?

                vec3 color = texture( u_texture, texCoords.st ).rgb;

                // ambient part
                vec3 lightCol = light_data.u_ambient * color;

                // diffuse part
                vec3  normalV = normalize( nvMappedEs );
                vec3  lightV  = -normalize(mat3(mvp.view) * light_data.u_lightDir.xyz);
                float NdotL   = max( 0.0, dot( normalV, lightV ) );
                lightCol     += NdotL * light_data.u_diffuse * color;
    
                // specular part
                vec3  eyeV      = normalize( -objPosEs );
                vec3  halfV     = normalize( eyeV + lightV );
                float NdotH     = max( 0.0, dot( normalV, halfV ) );
                float kSpecular = ( light_data.u_shininess + 2.0 ) * pow( NdotH, light_data.u_shininess ) / ( 2.0 * 3.14159265 );
                lightCol       += kSpecular * light_data.u_specular * color;

                fragColor = vec4( lightCol.rgb, 1.0 );

                vec4 proj_pos_displ = mvp.proj * vec4(view_pos_displ.xyz, 1.0);
                float depth = 0.5 + 0.5 * proj_pos_displ.z / proj_pos_displ.w;

                gl_FragDepth = depth;

            //#define DEBUG_FRONT_SILHOUETTES
            //#define DEBUG_BACK_SILHOUETTES
            //#define DEBUG_DEPTH

            #if defined(DEBUG_FRONT_SILHOUETTES)
                if ( texCoords.p < 0.0001 )
                    discard;
                if ( in_data.d.z < 0.0 )
                    discard;
                fragColor = vec4(vec2(in_data.d.xy-df), in_data.d.z, 1.0);
                //fragColor = vec4(vec2(d1), in_data.d.z, 1.0);
            #endif

            #if defined(DEBUG_BACK_SILHOUETTES)
                if ( texCoords.p < 0.0001 )
                    discard;
                if ( in_data.d.z > 0.0 )
                    discard;
                fragColor = vec4(vec2(df-in_data.d.xy), -in_data.d.z, 1.0);
                //fragColor = vec4(vec2(-d0), -in_data.d.z, 1.0);
            #endif

            #if defined(DEBUG_DEPTH)
                fragColor = vec4( vec3(1.0-depth), 1.0 );
            #endif
            }";

            this._parallax_prog = openGLFactory.VertexGeometryFragmentShaderProgram(vert_shader, geo_shader, frag_shader);
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
