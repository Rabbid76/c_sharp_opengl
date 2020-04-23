﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Input;            // KeyboardState, Keyboard, Key
using OpenTK.Graphics;         // GameWindow, GraphicsMode, Context
using OpenTK.Graphics.OpenGL4; // GL
using OpenTK_library;
using OpenTK_library.OpenGL;

namespace OpenTK_windows_forms_example_1
{
    public partial class Form1 : Form
    {
        private OpenTK_library.OpenGL.Version _version = new OpenTK_library.OpenGL.Version();
        private Extensions _extensions = new Extensions();
        private DebugCallback _debug_callback = new DebugCallback();

        private VertexArrayObject<float, uint> _test_vao;
        private OpenTK_library.OpenGL.Program _test_prog;

        public Form1()
        {
            InitializeComponent();
        }

        private void OnLoadGL(object sender, EventArgs e)
        {
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
              -0.5f, -0.5f, 0.0f,  1.0f, 0.0f, 0.0f, 1.0f,
               0.5f, -0.5f, 0.0f,  1.0f, 1.0f, 0.0f, 1.0f,
               0.5f,  0.5f, 0.0f,  0.0f, 1.0f, 0.0f, 1.0f,
              -0.5f,  0.5f, 0.0f,  0.0f, 0.0f, 1.0f, 1.0f
            };

            uint[] iquad = { 0, 1, 2, 0, 2, 3 };

            TVertexFormat[] format = {
                new TVertexFormat(0, 0, 3, 0, false),
                new TVertexFormat(0, 1, 4, 3, false),
            };

            _test_vao = new VertexArrayObject<float, uint>();
            _test_vao.AppendVertexBuffer(0, 7, vquad);
            _test_vao.Create(format, iquad);

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

            this._test_prog = OpenTK_library.OpenGL.Program.VertexAndFragmentShaderProgram(vert_shader, frag_shader);
            this._test_prog.Generate();

            this._test_prog.Use();

            // states

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        }

        private void OnPaintGL(object sender, PaintEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
            
            _test_vao.Draw();

            GL.Flush();
            glControl1.SwapBuffers();
        }

        private void OnResizeGL(object sender, EventArgs e)
        {
            GL.Viewport(0, 0, this.Width, this.Height);
        }

        private void OnClosing(object sender, FormClosingEventArgs e)
        {
            _test_vao.Dispose();
            _test_prog.Dispose();
        }
    }
}