using System;
using OpenTK.Mathematics;      // Vector2, Vector3, Vector4, Matrix4
using OpenTK.Graphics.OpenGL4; // GL
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK_library.OpenGL;
using OpenTK_library.OpenGL.OpenGL4;

namespace OpenTK_example_5
{
    public class DrawText
        : GameWindow
    {
        private IOpenGLObjectFactory openGLFactory = new OpenGLObjectFactory4();
        private bool _disposedValue = false;

        private IVersionInformation _version;
        private IExtensionInformation _extensions;
        private IDebugCallback _debug_callback;

        private IProgram _text_prog;
        private FreeTypeFont _font;

        public static DrawText New(int width, int height)
        {
            GameWindowSettings setting = new GameWindowSettings();
            NativeWindowSettings nativeSettings = new NativeWindowSettings();
            nativeSettings.Size = new OpenTK.Mathematics.Vector2i(width, height);
            nativeSettings.API = ContextAPI.OpenGL;
            return new DrawText(setting, nativeSettings);
        }

        public DrawText(GameWindowSettings setting, NativeWindowSettings nativeSettings)
            : base(setting, nativeSettings)
        { }

        public DrawText(int width, int height, string title)
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
                      NumberOfSamples = 8,
                  })
        { }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !this._disposedValue)
            {
                _text_prog.Dispose();
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

            // Create shader program

            string vert_shader = @"#version 460

            layout (location = 0) in vec2 in_pos;
            layout (location = 1) in vec2 in_uv;

            out vec2 vUV;

            layout (location = 0) uniform mat4 model;
            layout (location = 1) uniform mat4 projection;
            
            void main()
            {
                vUV         = in_uv.xy;
		        gl_Position = projection * model * vec4(in_pos.xy, 0.0, 1.0);
            }";

            string frag_shader = @"#version 460

            in vec2 vUV;

            layout (binding=0) uniform sampler2D u_texture;

             layout (location = 2) uniform vec3 textColor;

            out vec4 fragColor;

            void main()
            {
                vec2 uv = vUV.xy;
                float text = texture(u_texture, uv).r;
                fragColor = vec4(textColor.rgb*text, text);
            }";

            this._text_prog = openGLFactory.VertexAndFragmentShaderProgram(vert_shader, frag_shader);
            this._text_prog.Generate();
            this._text_prog.Use();

            // load font
            _font = new FreeTypeFont(32);

            base.OnLoad();
        }

        //! On update window
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            Matrix4 projectionM = Matrix4.CreateScale(new Vector3(1f/this.Size.X, 1f/this.Size.Y, 1.0f));
            projectionM = Matrix4.CreateOrthographicOffCenter(0.0f, this.Size.X, this.Size.Y, 0.0f, -1.0f, 1.0f);

            GL.Viewport(0, 0, this.Size.X, this.Size.Y);
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.Enable(EnableCap.Blend);
            //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.BlendFunc(0, BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            this._text_prog.Use();
            GL.UniformMatrix4(1, false, ref projectionM);

            GL.Uniform3(2, new Vector3(0.5f, 0.8f, 0.2f));
            _font.RenderText("This is sample text", 25.0f, 50.0f, 1.2f, new Vector2(1f, 0f));

            GL.Uniform3(2, new Vector3(0.3f, 0.7f, 0.9f));
            _font.RenderText("(C) LearnOpenGL.com", 50.0f, 200.0f, 0.9f, new Vector2(1.0f, -0.25f));

            Context.SwapBuffers();
            base.OnUpdateFrame(e);
        }
    }
}
