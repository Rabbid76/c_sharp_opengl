namespace OpenTK_library.OpenGL
{
    public abstract class IOpenGLObjectFactory
    {
        public IProgram VertexAndFragmentShaderProgram(string VertexShaderSource, string FragmentShaderSource)
        {
            (ShaderType, string)[] shader_source =
            { (ShaderType.VertexShader, VertexShaderSource), (ShaderType.FragmentShader, FragmentShaderSource) };
            return NewProgram(shader_source);
        }

        public IProgram VertexGeometryFragmentShaderProgram(string VertexShaderSource, string GeometryShaderSource, string FragmentShaderSource)
        {
            (ShaderType, string)[] shader_source =
            { (ShaderType.VertexShader, VertexShaderSource),
                  (ShaderType.GeometryShader, GeometryShaderSource),
                  (ShaderType.FragmentShader, FragmentShaderSource) };
            return NewProgram(shader_source);
        }

        public IProgram ComputeShaderProgram(string ComputeShaderSource)
        {
            (ShaderType, string)[] shader_source = { (ShaderType.ComputeShader, ComputeShaderSource) };
            return NewProgram(shader_source);
        }

        public abstract IProgram NewProgram((ShaderType, string)[] shader_source);

        public abstract IVertexArrayObject NewVertexArrayObject();

        public abstract ITexture NewTexture();
    }
}
