using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4; // GL
using OpenTK.Mathematics;
using OpenTK_library.Controls;
using OpenTK_library.OpenGL;
using OpenTK_library.OpenGL.OpenGL4;
using OpenTK_libray_viewmodel.Model;
using OpenTK_compute_raytracing.ViewModel;

namespace OpenTK_compute_raytracing.Model
{
    public class RayTracing_Model
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
        private RayTracing_ViewModel _viewmodel;
        private bool _disposed = false;
        private int _cx = 0;
        private int _cy = 0;
        private IVersionInformation _version;
        private IExtensionInformation _extensions;
        private IDebugCallback _debug_callback;

        private IProgram _compute_prog;
        private List<IFramebuffer> _fbos;
        private int _image_cx = 512; //1024;
        private int _image_cy = 512; //1024;
        private int _frame = 0;
        double _period = 0;
        private IControls _controls = new DummyControls();

        public IControls GetControls() => _controls;

        public float GetScale() => 1.0f;

        public RayTracing_ViewModel ViewModel
        {
            get => _viewmodel;
            set => _viewmodel = value;
        }

        public int DefaultFramebuffer => _viewmodel.DefaultFramebuffer;

        public RayTracing_Model()
        {
            _version = openGLFactory.NewVersionInformation(Console.WriteLine);
            _extensions = openGLFactory.NewExtensionInformation();
            _debug_callback = openGLFactory.NewDebugCallback(Console.WriteLine);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                foreach (var fbo in _fbos)
                    fbo.Dispose();
                _fbos.Clear();
                _compute_prog.Dispose();
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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

            // create Vertex Array Object, Array Buffer Object and Element Array Buffer Object

            // ...

            // Create shader program

            string compute_shader =
            @"#version 460

            layout(local_size_x = 1, local_size_y = 1) in;
            layout(rgba32f, binding = 1) readonly uniform image2D img_input;
            layout(rgba32f, binding = 2) writeonly uniform image2D img_output;

            layout(location = 1) uniform int iFrame; 
            layout(location = 2) uniform float iTime; 

             // All code here is by Zavie (https://www.shadertoy.com/view/4sfGDB#)

            /*

            This shader is an attempt at porting smallpt to GLSL.

            See what it's all about here:
            http://www.kevinbeason.com/smallpt/

            The code is based in particular on the slides by David Cline.

            Some differences:

            - For optimization purposes, the code considers there is
              only one light source (see the commented loop)
            - Russian roulette and tent filter are not implemented

            I spent quite some time pulling my hair over inconsistent
            behavior between Chrome and Firefox, Angle and native. I
            expect many GLSL related bugs to be lurking, on top of
            implementation errors. Please Let me know if you find any.

            --
            Zavie

            */

            // Play with the following value to change quality.
            // You want as many samples as your GPU can bear. :)
            #define MAXDEPTH 4

            // Uncomment to see how many samples never reach a light source
            //#define DEBUG

            // Not used for now
            #define DEPTH_RUSSIAN 2

            #define PI 3.14159265359
            #define DIFF 0
            #define SPEC 1
            #define REFR 2
            #define NUM_SPHERES 9

            float seed = 0.;
            float rand() { return fract(sin(seed++)*43758.5453123); }

            struct Ray { vec3 o, d; };
            struct Sphere {
                float r;
                vec3 p, e, c;
                int refl;
            };

            Sphere lightSourceVolume = Sphere(20., vec3(50., 81.6, 81.6), vec3(12.), vec3(0.), DIFF);
            Sphere spheres[NUM_SPHERES];
            void initSpheres() {
                spheres[0] = Sphere(1e5, vec3(-1e5+1., 40.8, 81.6), vec3(0.), vec3(.75, .25, .25), DIFF);
                spheres[1] = Sphere(1e5, vec3( 1e5+99., 40.8, 81.6),vec3(0.), vec3(.25, .25, .75), DIFF);
                spheres[2] = Sphere(1e5, vec3(50., 40.8, -1e5),     vec3(0.), vec3(.75), DIFF);
                spheres[3] = Sphere(1e5, vec3(50., 40.8,  1e5+170.),vec3(0.), vec3(0.), DIFF);
                spheres[4] = Sphere(1e5, vec3(50., -1e5, 81.6),     vec3(0.), vec3(.75), DIFF);
                spheres[5] = Sphere(1e5, vec3(50.,  1e5+81.6, 81.6),vec3(0.), vec3(.75), DIFF);
                spheres[6] = Sphere(16.5, vec3(27., 16.5, 47.),     vec3(0.), vec3(1.), SPEC);
                spheres[7] = Sphere(16.5, vec3(73., 16.5, 78.),     vec3(0.), vec3(.7, 1., .9), REFR);
                spheres[8] = Sphere(600., vec3(50., 681.33, 81.6),  vec3(12.), vec3(0.), DIFF);
            }

            float intersect(Sphere s, Ray r) {
                vec3 op = s.p - r.o;
                float t, epsilon = 1e-3, b = dot(op, r.d), det = b * b - dot(op, op) + s.r * s.r;
                if (det < 0.) return 0.; else det = sqrt(det);
                return (t = b - det) > epsilon ? t : ((t = b + det) > epsilon ? t : 0.);
            }

            int intersect(Ray r, out float t, out Sphere s, int avoid) {
                int id = -1;
                t = 1e5;
                s = spheres[0];
                for (int i = 0; i < NUM_SPHERES; ++i) {
                    Sphere S = spheres[i];
                    float d = intersect(S, r);
                    if (i!=avoid && d!=0. && d<t) { t = d; id = i; s=S; }
                }
                return id;
            }

            vec3 jitter(vec3 d, float phi, float sina, float cosa) {
                vec3 w = normalize(d), u = normalize(cross(w.yzx, w)), v = cross(w, u);
                return (u*cos(phi) + v*sin(phi)) * sina + w * cosa;
            }

            vec3 radiance(Ray r) {
                vec3 acc = vec3(0.);
                vec3 mask = vec3(1.);
                int id = -1;
                for (int depth = 0; depth < MAXDEPTH; ++depth) {
                    float t;
                    Sphere obj;
                    if ((id = intersect(r, t, obj, id)) < 0) break;
                    vec3 x = t * r.d + r.o;
                    vec3 n = normalize(x - obj.p), nl = n * sign(-dot(n, r.d));

                    //vec3 f = obj.c;
                    //float p = dot(f, vec3(1.2126, 0.7152, 0.0722));
                    //if (depth > DEPTH_RUSSIAN || p == 0.) if (rand() < p) f /= p; else { acc += mask * obj.e * E; break; }

                    if (obj.refl == DIFF) {
                        float r2 = rand();
                        vec3 d = jitter(nl, 2.*PI*rand(), sqrt(r2), sqrt(1. - r2));
                        vec3 e = vec3(0.);
                        //for (int i = 0; i < NUM_SPHERES; ++i)
                        {
                            // Sphere s = sphere(i);
                            // if (dot(s.e, vec3(1.)) == 0.) continue;

                            // Normally we would loop over the light sources and
                            // cast rays toward them, but since there is only one
                            // light source, that is mostly occluded, here goes
                            // the ad hoc optimization:
                            Sphere s = lightSourceVolume;
                            int i = 8;

                            vec3 l0 = s.p - x;
                            float cos_a_max = sqrt(1. - clamp(s.r * s.r / dot(l0, l0), 0., 1.));
                            float cosa = mix(cos_a_max, 1., rand());
                            vec3 l = jitter(l0, 2.*PI*rand(), sqrt(1. - cosa*cosa), cosa);

                            if (intersect(Ray(x, l), t, s, id) == i) {
                                float omega = 2. * PI * (1. - cos_a_max);
                                e += (s.e * clamp(dot(l, n),0.,1.) * omega) / PI;
                            }
                        }
                        float E = 1.;//float(depth==0);
                        acc += mask * obj.e * E + mask * obj.c * e;
                        mask *= obj.c;
                        r = Ray(x, d);
                    } else if (obj.refl == SPEC) {
                        acc += mask * obj.e;
                        mask *= obj.c;
                        r = Ray(x, reflect(r.d, n));
                    } else {
                        float a=dot(n,r.d), ddn=abs(a);
                        float nc=1., nt=1.5, nnt=mix(nc/nt, nt/nc, float(a>0.));
                        float cos2t=1.-nnt*nnt*(1.-ddn*ddn);
                        r = Ray(x, reflect(r.d, n));
                        if (cos2t>0.) {
                            vec3 tdir = normalize(r.d*nnt + sign(a)*n*(ddn*nnt+sqrt(cos2t)));
                            float R0=(nt-nc)*(nt-nc)/((nt+nc)*(nt+nc)),
                                c = 1.-mix(ddn,dot(tdir, n),float(a>0.));
                            float Re=R0+(1.-R0)*c*c*c*c*c,P=.25+.5*Re,RP=Re/P,TP=(1.-Re)/(1.-P);
                            if (rand()<P) { mask *= RP; }
                            else { mask *= obj.c*TP; r = Ray(x, tdir); }
                        }
                    }
                }
                return acc;
            }

            void main() {
  
                  // get index in global work group i.e x,y position
                  ivec2 pixel_coords = ivec2(gl_GlobalInvocationID.xy);
                  ivec2 dims         = imageSize(img_output); // fetch image dimensions

                  vec2 fragCoord   = vec2(pixel_coords);
                  vec2 iResolution = vec2(dims);
  
                  initSpheres();
                  vec2 st = fragCoord.xy / iResolution.xy;
                  seed = iTime + iResolution.y * fragCoord.x / iResolution.x + fragCoord.y / iResolution.y;
                  vec2 uv = 2. * fragCoord.xy / iResolution.xy - 1.;
                  vec3 camPos = vec3((2. * .5*iResolution.xy / iResolution.xy - 1.) * vec2(48., 40.) + vec2(50., 40.8), 169.);
                  vec3 cz = normalize(vec3(50., 40., 81.6) - camPos);
                  vec3 cx = vec3(1., 0., 0.);
                  vec3 cy = normalize(cross(cx, cz)); cx = cross(cz, cy);

                  // Moving average (multipass code)
                  vec3 color = imageLoad(img_input, pixel_coords).rgb * float(iFrame);
                  color += radiance(Ray(camPos, normalize(.53135 * (iResolution.x/iResolution.y*uv.x * cx + uv.y * cy) + cz)));
                  vec4 fragColor = vec4(color/float(iFrame + 1), 1.);                  

                  imageStore(img_output, pixel_coords, fragColor);
            }";

            this._compute_prog = openGLFactory.ComputeShaderProgram(compute_shader);
            this._compute_prog.Generate();

            // framebuffers

            _fbos = new List<IFramebuffer>();
            _fbos.Add(openGLFactory.NewFramebuffer());
            _fbos[0].Create(_image_cx, _image_cy, IFramebuffer.Kind.texture, IFramebuffer.Format.RGBA_F32, true, false);
            _fbos[0].Clear();
            _fbos.Add(openGLFactory.NewFramebuffer());
            _fbos[1].Create(_image_cx, _image_cy, IFramebuffer.Kind.texture, IFramebuffer.Format.RGBA_F32, true, false);
            _fbos[1].Clear();

            // states

            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
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
            }

            int i_read = (this._frame % 2) == 0 ? 1 : 0;
            int i_write = (this._frame % 2) == 0 ? 0 : 1;

            _fbos[i_read].Textures[0].BindImage(1, ITexture.Access.Read);
            _fbos[i_write].Textures[0].BindImage(2, ITexture.Access.Write);
            
            GL.ProgramUniform1(_compute_prog.Object, 1, this._frame);
            GL.ProgramUniform1(_compute_prog.Object, 2, (float)this._period);

            _compute_prog.Use();
            this._frame++;

            GL.DispatchCompute(_image_cx, _image_cy, 1);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit); // alternative:  MemoryBarrierFlags.AllBarrierBits;

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, DefaultFramebuffer);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Be aware, this won't work if the target framebuffer is a multisampling framebuffer
            if (this._cx > this._cy)
                _fbos[i_write].Blit(DefaultFramebuffer, (this._cx - this._cy) / 2, 0, this._cy, this._cy, false);
            else
                _fbos[i_write].Blit(DefaultFramebuffer, 0, (this._cy - this._cx) / 2, this._cx, this._cx, false);
        }
    }
}

