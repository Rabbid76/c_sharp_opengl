
//! Creating a Window
//! [https://opentk.net/learn/chapter1/1-creating-a-window.html]
//!
//! .NET Framework 2.0
//! TOOLS / NuGet / Package Manager Console
//! Install-Package OpenTK

//! Hello Triangle
//! [https://opentk.net/learn/chapter1/2-hello-triangle.html]

using OpenTK;
using OpenTK.Input;            // KeyboardState, Keyboard, Key
using OpenTK.Graphics;         // GameWindow, GraphicsMode, Context
using OpenTK.Graphics.OpenGL4; // GL


using System;
using System.Runtime.InteropServices;

namespace OpenTK_example_1
{
  public class Game
    : GameWindow
    , IDisposable
  {
    bool _disposedValue = false;

    Shader _test_prog;

    int _test_vao;
    int _test_vbo;
    int _test_ibo;


    public Game(int width, int height, string title)
      : base(width, height, GraphicsMode.Default, title,
             GameWindowFlags.Default,
             DisplayDevice.Default,
             4,
             6,
             GraphicsContextFlags.Default | GraphicsContextFlags.Debug)
    { }

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (disposing && !this._disposedValue)
      {
        _test_prog.Dispose();

        int[] buffers = { this._test_vbo, this._test_ibo };
        GL.DeleteBuffers(2, buffers);
        GL.DeleteVertexArray(_test_vao);

        this._disposedValue = true;
      }
    }

    // Callback for OpenGL debug message
    public static void DebugProc(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
    {
      string message_str = Marshal.PtrToStringAnsi(message);
      Console.WriteLine(message_str);
    }

    //! On load window (once)
    protected override void OnLoad(EventArgs e)
    {
      // debug callback

      GL.DebugMessageCallback(DebugProc, IntPtr.Zero);

      // filter: all debug messages on
      GL.DebugMessageControl(DebugSourceControl.DontCare, DebugTypeControl.DontCare, DebugSeverityControl.DontCare, 0, new int[0], true);

      // filter: only error messages
      //GL.DebugMessageControl(DebugSourceControl.DontCare, DebugTypeControl.DontCare, DebugSeverityControl.DontCare, 0, new int[0], false);
      //GL.DebugMessageControl(DebugSourceControl.DebugSourceApi, DebugTypeControl.DebugTypeError, DebugSeverityControl.DontCare, 0, new int[0], false);

      GL.Enable(EnableCap.DebugOutput);
      GL.Enable(EnableCap.DebugOutputSynchronous);
      GL.DebugMessageInsert(DebugSourceExternal.DebugSourceApplication, DebugType.DebugTypeMarker, 0, DebugSeverity.DebugSeverityNotification, -1, "Debug output enabled");

      // create Vertex Array Object, Array Buffer Object and Element Array Buffer Object

      float[] vquad = 
      {
      // x      y     z      r     g     b     a
        -0.5f, -0.5f, 0.0f,  1.0f, 0.0f, 0.0f, 1.0f, 
         0.5f, -0.5f, 0.0f,  1.0f, 1.0f, 0.0f, 1.0f,
         0.5f,  0.5f, 0.0f,  0.0f, 1.0f, 0.0f, 1.0f,
        -0.5f,  0.5f, 0.0f,  0.0f, 0.0f, 1.0f, 1.0f
      };

      uint [] iquad = { 0, 1, 2, 0, 2, 3 };

      this._test_vao = GL.GenVertexArray();
      this._test_vbo = GL.GenBuffer();
      this._test_ibo = GL.GenBuffer();

      GL.BindBuffer(BufferTarget.ArrayBuffer, this._test_vbo);
      GL.BufferData(BufferTarget.ArrayBuffer, vquad.Length * sizeof(float), vquad, BufferUsageHint.StaticDraw);

      GL.BindVertexArray(this._test_vao);

      GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 7 * sizeof(float), 0);
      GL.EnableVertexAttribArray(0);
      GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 7 * sizeof(float), 3 * sizeof(float));
      GL.EnableVertexAttribArray(1);

      GL.BindBuffer(BufferTarget.ElementArrayBuffer, this._test_ibo);
      GL.BufferData(BufferTarget.ElementArrayBuffer, iquad.Length * sizeof(uint), iquad, BufferUsageHint.StaticDraw);

      // Create shader program

      string vert_shader = @"#version 460 core
      layout (location = 0) in vec4 a_pos;
      layout (location = 1) in vec4 a_color;
      
      out vec4 v_color;

      void main()
      {
          v_color     = a_color;
          gl_Position = a_pos; 
      }";

      string frag_shader = @"#version 460 core
      out vec4 frag_color;
      in  vec4 v_color;
      
      void main()
      {
          frag_color = v_color; 
      }";

      this._test_prog = new Shader(vert_shader, frag_shader);
      this._test_prog.Generate();

      this._test_prog.Use();

      // states

      GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

      base.OnLoad(e);
    }

    //! On resize
    protected override void OnResize(EventArgs e)
    {
      GL.Viewport(0, 0, this.Width,this.Height);
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


      GL.Clear(ClearBufferMask.ColorBufferBit);

      GL.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedInt, 0);

      Context.SwapBuffers();
      base.OnUpdateFrame(e);
    }
  }
}
