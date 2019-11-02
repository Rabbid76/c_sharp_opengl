using System;
using OpenTK.Graphics.OpenGL4; // GL, ShaderType


// TODO: map shader type sources / spire-v
// create spire-v asynchronously

namespace OpenTK_library
{
    public class GL_Program
        : IDisposable
    {
        private bool _disposed = false;

        public int program
        {
            get { return this._program; }
        }
        private int _program = 0;

        private string _vert_source; //!< vertex shader source (TODO abstract, map)
        private string _frag_source; //!< vertex shader source (TODO abstract, map)

        public GL_Program(string VertexShaderSource, string FragmentShaderSource)
        {
            this._vert_source = VertexShaderSource;
            this._frag_source = FragmentShaderSource;
        }

        ~GL_Program()
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
            int vert_shader = this.GenerateShader(ShaderType.VertexShader, _vert_source);
            int frag_shader = this.GenerateShader(ShaderType.FragmentShader, _frag_source);

            this.CompileShader(vert_shader);
            this.CompileShader(frag_shader);

            this._program = GL.CreateProgram();

            GL.AttachShader(this._program, vert_shader);
            GL.AttachShader(this._program, frag_shader);

            GL.LinkProgram(this._program);
            string infoLogProg = GL.GetProgramInfoLog(this._program);
            if (infoLogProg != System.String.Empty)
            {
              System.Console.WriteLine(infoLogProg);
              return false; // TODO exception
            }

            GL.DetachShader(this._program, vert_shader);
            GL.DetachShader(this._program, frag_shader);
            GL.DeleteShader(vert_shader);
            GL.DeleteShader(frag_shader);

            return true;
        }

        //! generate a shader object with a specific type
        private int GenerateShader(ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);
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
    }
}
