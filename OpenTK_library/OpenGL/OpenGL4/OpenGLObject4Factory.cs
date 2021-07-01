namespace OpenTK_library.OpenGL.OpenGL4
{
    public class OpenGLObject4Factory : IOpenGLObjectFactory
    {
        public override IProgram NewProgram((ShaderType, string)[] shader_source) =>
            new Program4(shader_source);

        public override IVertexArrayObject NewVertexArrayObject() =>
            new VertexArrayObject4();

        public override ITexture NewTexture() =>
            new Texture4();
    }
}
