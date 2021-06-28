using OpenTK_library.OpenGL.OpenGL4;

namespace OpenTK_library.OpenGL.OpenGL4DSA
{
    internal class Program4DSA : Program4
    {
        public Program4DSA((ShaderType, string)[] shader_source)
            : base(shader_source) 
        { }
    }
}
