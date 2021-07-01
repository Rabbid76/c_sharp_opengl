// TODO remove namespace OpenGL4DSA 
// OpenGLFactory4(dsa, imuutabletexture, vaoseparateformat)

namespace OpenTK_library.OpenGL.OpenGL4DSA
{
    public class OpenGLObjectFactory4DSA : IOpenGLObjectFactory
    {
        public override IProgram NewProgram((ShaderType, string)[] shader_source) =>
            new Program4DSA(shader_source);

        public override IVertexArrayObject NewVertexArrayObject() =>
            new VertexArrayObject4DSA();

        public override ITexture NewTexture() =>
            new Texture4Immutable();
    }
}
