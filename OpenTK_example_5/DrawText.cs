using OpenTK;
using OpenTK.Input;            // KeyboardState, Keyboard, Key
using OpenTK.Graphics;         // GameWindow, GraphicsMode, Context
using OpenTK.Graphics.OpenGL4; // GL

using OpenTK_library;
using OpenTK_library.OpenGL;

using System;
using System.Drawing;
using System.IO;
using System.Reflection;

/// <summary>
/// [SharpFont](https://www.nuget.org/packages/SharpFont/)
/// [Robmaister/SharpFont](https://github.com/Robmaister/SharpFont)
/// [space-wizards/SharpFont](https://github.com/space-wizards/SharpFont)
/// </summary>

namespace OpenTK_example_5
{
    public class DrawText
        : GameWindow
    {
        private bool _disposedValue = false;

        private OpenTK_library.OpenGL.Version _version = new OpenTK_library.OpenGL.Version();
        private Extensions _extensions = new Extensions();
        private DebugCallback _debug_callback = new DebugCallback();

        private OpenTK_library.OpenGL.Program _text_prog;
        private FreeTypeFont _font;

        public DrawText(int width, int height, string title)
            : base(width, height, GraphicsMode.Default, title,
                GameWindowFlags.Default,
                DisplayDevice.Default,
                4,
                6,
                GraphicsContextFlags.Default | GraphicsContextFlags.Debug)
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
        protected override void OnLoad(EventArgs e)
        {
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
                fragColor = vec4(textColor.rgb*text, text)+1.0;
            }";

            this._text_prog = OpenTK_library.OpenGL.Program.VertexAndFragmentShaderProgram(vert_shader, frag_shader);
            this._text_prog.Generate();
            this._text_prog.Use();

            // load font
            _font = new FreeTypeFont(32);

            base.OnLoad(e);
        }

        //! On resize
        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, this.Width, this.Height);
            base.OnResize(e);
        }

        //! On update window
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            KeyboardState input = Keyboard.GetState();
            if (input.IsKeyDown(Key.Escape))
            {
                Exit();
            }

            Matrix4 projectionM = Matrix4.CreateScale(new Vector3(1f/this.Width, 1f/this.Height, 1.0f));

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            this._text_prog.Use();
            GL.UniformMatrix4(1, false, ref projectionM);

            GL.Uniform3(2, new Vector3(0.5f, 0.8f, 0.2f));
            _font.RenderText("This is sample text", 25.0f, 50.0f, 1.0f, new Vector2(1f, 0f));

            GL.Uniform3(2, new Vector3(0.3f, 0.7f, 0.9f));
            _font.RenderText("(C) LearnOpenGL.com", 100.0f, 200.0f, 0.5f, new Vector2(1.0f, -0.25f));

            Context.SwapBuffers();
            base.OnUpdateFrame(e);
        }
    }
}
