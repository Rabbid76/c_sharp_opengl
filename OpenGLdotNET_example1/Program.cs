using System;
using glfw3;
using OpenGL;

namespace OpenGLdotNET_example1
{
    class Program
    {
        static void Main(string[] args)
        {
            Glfw.Init();

            Glfw.WindowHint(0x00022002, 4);
            Glfw.WindowHint(0x00022003, 4);
            Glfw.WindowHint(0x00022008, 0x00032002);
            GLFWwindow window = Glfw.CreateWindow(1080, 720, "Yeet", null, null);

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
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)(4 * vertices.Length), vertices, BufferUsage.StaticDraw);
            Gl.VertexAttribPointer(0, 2, VertexAttribType.Float, false, 0, null);

            while (Glfw.WindowShouldClose(window) != 1)
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
