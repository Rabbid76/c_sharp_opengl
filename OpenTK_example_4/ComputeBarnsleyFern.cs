using System;
using OpenTK.Mathematics;      // Vector2, Vector3, Vector4, Matrix4
using OpenTK.Graphics.OpenGL4; // GL
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK_library.OpenGL;
using OpenTK_library.OpenGL.OpenGL4;

namespace OpenTK_exmaple_4
{
    /// <summary>
    /// [Barnsley fern](https://en.wikipedia.org/wiki/Barnsley_fern)
    /// </summary>
    public class ComputeBarnsleyFern
        : GameWindow
    {
        internal unsafe struct TCoordinate
        {
            public fixed float _coordiante[4];
           
            public TCoordinate(Vector2 coord)
            {
                this.coordiante = coord;
            }

            public Vector2 coordiante
            {
                get
                {
                    return new Vector2(this._coordiante[0], this._coordiante[1]);
                }
                set
                {
                    this._coordiante[0] = value.X;
                    this._coordiante[1] = value.Y;
                    this._coordiante[2] = 0.0f;
                    this._coordiante[3] = 0.0f;
                }
            }
        }

        private IOpenGLObjectFactory openGLFactory = new OpenGLObjectFactory4();
        private bool _disposedValue = false;

        private IVersionInformation _version;
        private IExtensionInformation _extensions;
        private IDebugCallback _debug_callback;

        private IProgram _compute_prog;
        private IStorageBuffer _coord_ssbo;
        private IFramebuffer _fbo;
        private int _image_cx = 512;
        private int _image_cy = 512;
        private int _frame = 0;
        private Random _rand = new Random();

        public static ComputeBarnsleyFern New(int width, int height)
        {
            GameWindowSettings setting = new GameWindowSettings();
            NativeWindowSettings nativeSettings = new NativeWindowSettings();
            nativeSettings.Size = new OpenTK.Mathematics.Vector2i(width, height);
            nativeSettings.API = ContextAPI.OpenGL;
            return new ComputeBarnsleyFern(setting, nativeSettings);
        }

        public ComputeBarnsleyFern(GameWindowSettings setting, NativeWindowSettings nativeSettings)
            : base(setting, nativeSettings)
        { }

        public ComputeBarnsleyFern(int width, int height, string title)
            : base(
                  new GameWindowSettings()
                  {
                  },
                  new NativeWindowSettings()
                  {
                      Size = new OpenTK.Mathematics.Vector2i(width, height),
                      Title = title,
                      APIVersion = new System.Version(4, 6),
                      API = ContextAPI.OpenGL,
                  })
        { }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !this._disposedValue)
            {
                _fbo.Dispose();
                _coord_ssbo.Dispose();
                _compute_prog.Dispose();
                this._disposedValue = true;
            }
            base.Dispose(disposing);
        }

        //! On load window (once)
        protected override void OnLoad()
        {
            _version = openGLFactory.NewVersionInformation(Console.WriteLine);
            _extensions = openGLFactory.NewExtensionInformation();
            _debug_callback = openGLFactory.NewDebugCallback(Console.WriteLine);

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
            layout(rgba32f, binding = 1) writeonly uniform image2D img_output;

            layout(location = 1) uniform vec4  u_color;
            layout(location = 2) uniform float u_margin;
            layout(location = 3) uniform float u_random; 

            layout(std430, binding = 1) buffer TCoord1
            {
                vec4 pos;
            } coord_inout;

            vec4[7] constants = vec4[7](
                vec4(0.0,  0.85,  0.2,   -0.15),  
                vec4(0.0,  0.04, -0.26,   0.28),  
                vec4(0.0, -0.04,  0.23,   0.26),  
                vec4(0.16, 0.85,  0.22,   0.24),  
                vec4(0.0,  0.0,   0.0,    0.0 ),  
                vec4(0.0,  1.6,   1.6,    0.44),  
                vec4(1.0, 86.0,  93.0,  100.0 ));  

            void main() {
  
                  vec2 dims = vec2(imageSize(img_output)); // fetch image dimensions

                  int i = 0; 
                  for (; i < 4; ++i)
                  {
                      if (u_random*100.0 < constants[6][i])
                          break;
                  }
                  i = min(i, 3);

                  vec2 fern = vec2(
                      constants[0][i] * coord_inout.pos.x + constants[1][i] * coord_inout.pos.y + constants[4][i],
                      constants[2][i] * coord_inout.pos.x + constants[3][i] * coord_inout.pos.y + constants[5][i]);
                  coord_inout.pos.xy = fern;
                  
                  vec2 pixel_coords = vec2(
                      (fern.x + 2.1820 + u_margin) * dims.x / (2.1820 + 2.6558 + 2.0 * u_margin),  
                      dims.y - (-fern.y + 9.9983 + u_margin) * dims.y / (9.9983 + 2.0 * u_margin));
                  
                  // output to a specific pixel in the image
                  imageStore(img_output, ivec2(pixel_coords), u_color);
            }";

            this._compute_prog = openGLFactory.ComputeShaderProgram(compute_shader);
            this._compute_prog.Generate();

            // Model view projection shader storage block objects and buffers
            _coord_ssbo = openGLFactory.NewStorageBuffer();
            TCoordinate coord_data = new TCoordinate(Vector2.Zero);
            this._coord_ssbo.Create(ref coord_data, IStorageBuffer.Usage.ReadWrite);
            this._coord_ssbo.Bind(1);
            
            // framebuffers

            _fbo = openGLFactory.NewFramebuffer();
            _fbo.Create(_image_cx, _image_cy, IFramebuffer.Kind.texture, IFramebuffer.Format.RGBA_F32, true, false);
            _fbo.Clear(new Color4(0.2f, 0.1f, 0.0f, 1.0f));

            // states

            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);

            base.OnLoad();
        }

        //! On update window
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (this._disposedValue)
                return;

            int i_read = (this._frame % 2) == 0 ? 1 : 0;
            int i_write = (this._frame % 2) == 0 ? 1 : 0;

            GL.Viewport(0, 0, this.Size.X, this.Size.Y);

            _fbo.Textures[0].BindImage(1, ITexture.Access.Write);
            
            float margin = 0.5f;
            Color4 color = new Color4(0.5f, 1.0f, 0.25f, 1.0f);
            float random_number = (float)_rand.Next(1000) / 1000.0f;
            GL.ProgramUniform4(this._compute_prog.Object, 1, color);
            GL.ProgramUniform1(this._compute_prog.Object, 2, margin);
            GL.ProgramUniform1(this._compute_prog.Object, 3, random_number);

            _compute_prog.Use();
            this._frame++;

            GL.DispatchCompute(1, 1, 1);
            //GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit); // alternative:  MemoryBarrierFlags.AllBarrierBits;
            GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);

            /* DEBUG output
            var data_ptr = GL.MapNamedBufferRange(this._coord_ssbo.Object, IntPtr.Zero, 8, BufferAccessMask.MapReadBit);
            float[] managedArray = new float[2];
            Marshal.Copy(data_ptr, managedArray, 0, 2);
            GL.UnmapNamedBuffer(this._coord_ssbo.Object);
            Console.WriteLine("data x: " + managedArray[0].ToString() + " y: " + managedArray[1].ToString());
            */

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Be aware, this won't work if the target framebuffer is a multisampling framebuffer
            if (this.Size.X > this.Size.Y)
                _fbo.Blit(null, (this.Size.X - this.Size.Y) / 2, 0, this.Size.Y, this.Size.Y, true);
            else
                _fbo.Blit(null, 0, (this.Size.Y - this.Size.X) / 2, this.Size.X, this.Size.X, true);

            Context.SwapBuffers();
            base.OnUpdateFrame(e);
        }
    }
}
