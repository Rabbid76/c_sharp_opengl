using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;

// TODO: map shader type sources / spire-v
// create spire-v asynchronously

namespace OpenTK_library.OpenGL.OpenGL4
{
    internal class Program4 : IProgram
    {
        private bool _disposed = false;

        public int Object { get => _program; }
        public bool Valid { get => _valid; }

        private int _program = 0;
        private bool _valid = false;

        List<(ShaderType, string)> _shader_source = new List<(ShaderType, string)>();

        public Program4((ShaderType, string)[] shader_source)
        {
            foreach (var shader in shader_source)
                this._shader_source.Add(shader);
        }

        ~Program4()
        {
            GL.DeleteProgram(this._program);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                GL.DeleteProgram(this._program);
                this._program = 0;
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // install program
        public void Use()
        {
            GL.UseProgram(this._program);
        }

        // generate a shader program
        public bool Generate()
        {
            this._program = GL.CreateProgram();

            List<int> shader_list = new List<int>();
            foreach (var shader in this._shader_source)
            {
                int shader_object = this.GenerateShader(shader.Item1, shader.Item2);
                this.CompileShader(shader_object);
                GL.AttachShader(this._program, shader_object);
                shader_list.Add(shader_object);
            }

            GL.LinkProgram(this._program);
            string infoLogProg = GL.GetProgramInfoLog(this._program);
            if (infoLogProg != System.String.Empty)
            {
                System.Console.WriteLine(infoLogProg);
                return false; // TODO exception
            }

            foreach (var shader_object in shader_list)
            {
                GL.DetachShader(this._program, shader_object);
                GL.DeleteShader(shader_object);
            }

            _valid = true;
            return true;
        }

        //! generate a shader object with a specific type
        private int GenerateShader(ShaderType type, string source)
        {
            int shader = GL.CreateShader(Type(type));
            GL.ShaderSource(shader, source);
            return shader;
        }

        //! compile a shader object 
        private bool CompileShader(int shader)
        {
            GL.CompileShader(shader);

            string infoLogVert = GL.GetShaderInfoLog(shader);
            if (infoLogVert != System.String.Empty)
            {
                System.Console.WriteLine(infoLogVert);
                return false; // TODO exception
            }
            return true;
        }

        static IDictionary<ShaderType, OpenTK.Graphics.OpenGL4.ShaderType> shaderTypeMap
            = new Dictionary<ShaderType, OpenTK.Graphics.OpenGL4.ShaderType>
            {
                { ShaderType.VertexShader, OpenTK.Graphics.OpenGL4.ShaderType.VertexShader },
                { ShaderType.FragmentShader, OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader },
                { ShaderType.GeometryShader, OpenTK.Graphics.OpenGL4.ShaderType.GeometryShader },
                { ShaderType.TessEvaluationShader, OpenTK.Graphics.OpenGL4.ShaderType.TessEvaluationShader },
                { ShaderType.TessControlShader, OpenTK.Graphics.OpenGL4.ShaderType.TessControlShader },
                { ShaderType.ComputeShader, OpenTK.Graphics.OpenGL4.ShaderType.ComputeShader },
            };

        private static OpenTK.Graphics.OpenGL4.ShaderType Type(ShaderType type) =>
            shaderTypeMap[type];
    }
}
