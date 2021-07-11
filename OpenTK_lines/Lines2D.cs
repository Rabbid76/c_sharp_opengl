using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK_library.OpenGL;
using OpenTK_library.OpenGL.OpenGL4;
using System;
using System.Linq;
using System.Collections.Generic;

namespace OpenTK_lines
{
    public class Lines2D
    : GameWindow
    {
        private IOpenGLObjectFactory openGLFactory = new OpenGLObjectFactory4();
        private bool _disposedValue = false;

        private IVersionInformation _version;
        private IExtensionInformation _extensions;
        private IDebugCallback _debug_callback;

        private IProgram _test_prog;
        private IStorageBuffer _lineBuffer1;
        private int _no_vertices_1;
        private IStorageBuffer _lineBuffer2;
        private int _no_vertices_2;
        private int mvpLocation, resolutionLocation, thicknessLocation, colorLocation;

        public static Lines2D New(int width, int height)
        {
            GameWindowSettings setting = new GameWindowSettings();
            NativeWindowSettings nativeSettings = new NativeWindowSettings();
            nativeSettings.Size = new Vector2i(width, height);
            nativeSettings.API = ContextAPI.OpenGL;
            return new Lines2D(setting, nativeSettings);
        }

        public Lines2D(GameWindowSettings setting, NativeWindowSettings nativeSettings)
            : base(setting, nativeSettings)
        { }

        public Lines2D(int width, int height, string title)
            : base(
                  new GameWindowSettings()
                  {
                  },
                  new NativeWindowSettings()
                  {
                      Size = new Vector2i(width, height),
                      Title = title,
                      APIVersion = new Version(4, 6),
                      API = ContextAPI.OpenGL,
                      NumberOfSamples = 8,
                  })
        { }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !this._disposedValue)
            {
                _test_prog.Dispose();
                _lineBuffer1.Dispose();
                _disposedValue = true;
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

            float[] vquad =
            {
            // x      y     z      r     g     b     a
              -1.0f, -1.0f, 0.0f,  1.0f, 0.0f, 0.0f, 1.0f,
               1.0f, -1.0f, 0.0f,  1.0f, 1.0f, 0.0f, 1.0f,
               1.0f,  1.0f, 0.0f,  0.0f, 1.0f, 0.0f, 1.0f,
              -1.0f,  1.0f, 0.0f,  0.0f, 0.0f, 1.0f, 1.0f
            };

            uint[] iquad = { 0, 1, 2, 0, 2, 3 };

            TVertexFormat[] format = {
                new TVertexFormat(0, 0, 3, 0, false),
                new TVertexFormat(0, 1, 4, 3, false),
            };

            int vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            // Create shader program

            string vert_shader = @"
            #version 460 core

            layout(std430, binding = 0) buffer TVertex
            {
               vec4 vertex[]; 
            };

            uniform mat4  u_mvp;
            uniform vec2  u_resolution;
            uniform float u_thickness;
            
            void main()
            {
                int line_i = gl_VertexID / 6;
                int tri_i  = gl_VertexID % 6;
                vec4 va[4];
                for (int i=0; i<4; ++i)
                {
                    va[i] = u_mvp * vertex[line_i+i];
                    va[i].xyz /= va[i].w;
                    va[i].xy = (va[i].xy + 1.0) * 0.5 * u_resolution;
                }
                vec2 v_line  = normalize(va[2].xy - va[1].xy);
                vec2 nv_line = vec2(-v_line.y, v_line.x);
    
                vec4 pos;
                if (tri_i == 0 || tri_i == 1 || tri_i == 3)
                {
                    vec2 v_pred  = normalize(va[1].xy - va[0].xy);
                    vec2 v_miter = normalize(nv_line + vec2(-v_pred.y, v_pred.x));
                    pos = va[1];
                    pos.xy += v_miter * u_thickness * (tri_i == 1 ? -0.5 : 0.5) / dot(v_miter, nv_line);
                }
                else
                {
                    vec2 v_succ  = normalize(va[3].xy - va[2].xy);
                    vec2 v_miter = normalize(nv_line + vec2(-v_succ.y, v_succ.x));
                    pos = va[2];
                    pos.xy += v_miter * u_thickness * (tri_i == 5 ? 0.5 : -0.5) / dot(v_miter, nv_line);
                }
                pos.xy = pos.xy / u_resolution * 2.0 - 1.0;
                pos.xyz *= pos.w;
                gl_Position = pos;
            }";

            string frag_shader = @"
            #version 460 core
            
            out vec4 fragColor;
            uniform vec4 u_color;
            
            void main()
            {
                fragColor = u_color;
            }";

            _test_prog = openGLFactory.VertexAndFragmentShaderProgram(vert_shader, frag_shader);
            _test_prog.Generate();

            mvpLocation = GL.GetUniformLocation(this._test_prog.Object, "u_mvp");
            resolutionLocation = GL.GetUniformLocation(this._test_prog.Object, "u_resolution");
            thicknessLocation = GL.GetUniformLocation(this._test_prog.Object, "u_thickness");
            colorLocation = GL.GetUniformLocation(this._test_prog.Object, "u_color");
            _test_prog.Use();

            float[] p0 = { -1.0f, -1.0f, 0.0f, 1.0f };
            float[] p1 = { 1.0f, -1.0f, 0.0f, 1.0f };
            float[] p2 = { 1.0f, 1.0f, 0.0f, 1.0f };
            float[] p3 = { -1.0f, 1.0f, 0.0f, 1.0f };
            float[] line1 = p3.Concat(p0).Concat(p1).Concat(p2).Concat(p3).Concat(p0).Concat(p1).ToArray();
            _lineBuffer1 = openGLFactory.NewStorageBuffer();
            _lineBuffer1.Create(line1);
            _no_vertices_1 = line1.Length / 4;

            var ll2 = new List<float>();
            for (int u = -8; u <= 368; u += 8)
            {
                double a = u * Math.PI / 180.0;
                double c = Math.Cos(a), s = Math.Sin(a);
                ll2.AddRange(new float[]{ (float)c, (float)s, 0.0f, 1.0f});
            }
            float[] line2 = ll2.ToArray();
            _lineBuffer2 = openGLFactory.NewStorageBuffer();
            _lineBuffer2.Create(line2);
            _no_vertices_2 = line2.Length / 4;

            // states

            GL.ClearColor(0.8f, 0.9f, 1.0f, 1.0f);

            base.OnLoad();
        }

        private double angle = 0.0;

        //! On update window
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            GL.Viewport(0, 0, Size.X, Size.Y);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            float aspect = (float)Size.X / (float)Size.Y;
            Matrix4 projectionMatrix = Matrix4.CreateOrthographicOffCenter(-aspect, aspect, -1.0f, 1.0f, -10.0f, 10.0f);

            GL.Uniform1(thicknessLocation, 20.0f);
            GL.Uniform2(resolutionLocation, (float)Size.X, (float)Size.Y);

            Matrix4 transformMatrix =
                Matrix4.CreateScale(0.5f, 0.5f, 1.0f) *
                Matrix4.CreateTranslation(-0.6f, 0.0f, 0.0f);
            Matrix4 mvp = transformMatrix * projectionMatrix;
            GL.UniformMatrix4(mvpLocation, false, ref mvp);

            _lineBuffer1.Bind(0);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.Uniform4(colorLocation, 0.7f, 0.7f, 0.7f, 1.0f);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6 * (_no_vertices_1 - 3));
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.Uniform4(colorLocation, 0.0f, 0.0f, 0.0f, 1.0f);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6 * (_no_vertices_1 - 3));

            transformMatrix =
                Matrix4.CreateScale(0.5f, 0.5f, 1.0f) *
                Matrix4.CreateTranslation(0.6f, 0.0f, 0.0f);
            mvp = transformMatrix * projectionMatrix;
            GL.UniformMatrix4(mvpLocation, false, ref mvp);

            _lineBuffer2.Bind(0);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.Uniform4(colorLocation, 0.7f, 0.7f, 0.7f, 1.0f);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6 * (_no_vertices_2 - 3));
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.Uniform4(colorLocation, 0.0f, 0.0f, 0.0f, 1.0f);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6 * (_no_vertices_2 - 3));

            Context.SwapBuffers();
            base.OnUpdateFrame(e);
        }
    }
}
