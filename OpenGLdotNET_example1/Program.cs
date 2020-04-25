using System;
using GLFW;
using OpenGL;
using System.Runtime.InteropServices;

namespace OpenGLdotNET_example1
{
    class Program
    {
        static void Main(string[] args)
        {
            Glfw.Init();

            Glfw.WindowHint(Hint.ContextVersionMajor, 4);
            Glfw.WindowHint(Hint.ContextVersionMinor, 6);
            Glfw.WindowHint(Hint.OpenglProfile, Profile.Compatibility);
            Window window = Glfw.CreateWindow(1080, 720, "Yeet", Monitor.None, Window.None);

            // `Gl.Initialize()` has to be don before `Glfw.MakeContextCurrent(window)`
            // [How Do I Initialize OpenGL.NET with GLFW.Net?](https://stackoverflow.com/questions/61318104/how-do-i-initialize-opengl-net-with-glfw-net/61319044?noredirect=1#comment108476826_61319044)
            Gl.Initialize();
            Glfw.MakeContextCurrent(window);

            var v = Gl.GetString(StringName.Version);
            Console.WriteLine(v);

            uint vao = Gl.CreateVertexArray();
            Gl.BindVertexArray(vao);

            uint vbo = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            
            var vertices = new float[] { -0.5f, -0.5f, 0.5f, -0.5f, 0.0f, 0.5f };
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)(4 * vertices.Length), null, BufferUsage.StaticDraw);
   
            IntPtr unmanagedPointer = Marshal.AllocHGlobal(4 * vertices.Length);
            Marshal.Copy(vertices, 0, unmanagedPointer, vertices.Length);
            Gl.BufferSubData(BufferTarget.ArrayBuffer, new IntPtr(0), (uint)(4 * vertices.Length), unmanagedPointer);
            Marshal.FreeHGlobal(unmanagedPointer);
            
            //Gl.BufferSubData(BufferTarget.ArrayBuffer, new IntPtr(0), (uint)(4 * vertices.Length), vertices);

            Gl.VertexAttribPointer(0, 2, VertexAttribType.Float, false, 0, null);

            while (!Glfw.WindowShouldClose(window))
            {
                Glfw.PollEvents();

                Gl.ClearColor(0.0f, 1.0f, 1.0f, 1.0f);
                Gl.Clear(ClearBufferMask.ColorBufferBit);
                Gl.BindVertexArray(vao);
                Gl.EnableVertexAttribArray(0);
                Gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
                Gl.DisableVertexAttribArray(0);
                Gl.BindVertexArray(0);

                Glfw.SwapBuffers(window);
            }

            Glfw.DestroyWindow(window);
            Glfw.Terminate();
        }
    }
}
