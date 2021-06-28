namespace OpenTK_library.OpenGL.OpenGL4DSA
{
    public class OpenGLObjectFactory4DSA : IOpenGLObjectFactory
    {
        public override IProgram NewProgram((ShaderType, string)[] shader_source) =>
            new Program4DSA(shader_source);
    }
}
