﻿using System;
using System.Windows;
using OpenTK.Graphics.OpenGL4;

namespace OpenTK_hello_triangle_WPF.Model
{
    public class HelloTriangle
    {
        private bool disposed;
        private int vertex_array_object;
        private int vertex_buffer_object;
        private int program;

        public void Dispose(bool disposing)
        {
            if (!disposing || disposed)
                return;
            disposed = true;
            GL.DeleteProgram(program);
            GL.DeleteBuffer(vertex_buffer_object);
            GL.DeleteVertexArray(vertex_array_object);
        }

        public void Create()
        {
            // Equilateral triangle https://en.wikipedia.org/wiki/Equilateral_triangle
            float[] triangle =
            {
            // x        y      z      r     g     b    
              -0.866f, -0.75f, 0.0f,  1.0f, 0.0f, 0.0f,
               0.866f, -0.75f, 0.0f,  1.0f, 1.0f, 0.0f,
               0.0f,    0.75f, 0.0f,  0.0f, 0.0f, 1.0f,
            };

            vertex_array_object = GL.GenVertexArray();
            vertex_buffer_object = GL.GenBuffer();

            GL.BindVertexArray(vertex_array_object);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertex_buffer_object);
            GL.BufferData(BufferTarget.ArrayBuffer, triangle.Length * sizeof(float), triangle, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);

            string vertex_shader_code = @"#version 460 core
            layout (location = 0) in vec4 a_pos;
            layout (location = 1) in vec4 a_color;
      
            out vec4 v_color;

            void main()
            {
                v_color     = a_color;
                gl_Position = a_pos; 
            }";

            string fragment_shader_code = @"#version 460 core
            out vec4 frag_color;
            in  vec4 v_color;
      
            void main()
            {
                frag_color = v_color; 
            }";

            int vertex_shader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertex_shader, vertex_shader_code);
            GL.CompileShader(vertex_shader);
            string info_log_vertex = GL.GetShaderInfoLog(vertex_shader);
            if (!string.IsNullOrEmpty(info_log_vertex))
                Console.WriteLine(info_log_vertex);

            int fragment_shader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragment_shader, fragment_shader_code);
            GL.CompileShader(fragment_shader);
            string info_log_fragment = GL.GetShaderInfoLog(fragment_shader);
            if (!string.IsNullOrEmpty(info_log_fragment))
                Console.WriteLine(info_log_fragment);

            program = GL.CreateProgram();
            GL.AttachShader(program, vertex_shader);
            GL.AttachShader(program, fragment_shader);
            GL.LinkProgram(program);
            string info_log_program = GL.GetProgramInfoLog(program);
            if (!string.IsNullOrEmpty(info_log_program))
                Console.WriteLine(info_log_program);
            GL.DetachShader(program, vertex_shader);
            GL.DetachShader(program, fragment_shader);
            GL.DeleteShader(vertex_shader);
            GL.DeleteShader(fragment_shader);

            GL.UseProgram(program);
        }

        public void Resize(Size newSize)
        {
            GL.Viewport(0, 0, (int)newSize.Width, (int)newSize.Height);
        }

        public void Render()
        {
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }
    }
}
