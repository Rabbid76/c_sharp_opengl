using System;

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

        public IFramebuffer CreateFrambuffer(int cx, int cy, IFramebuffer.Kind kind, bool depth, bool stencil)
        {
            var fb = NewFramebuffer();
            fb.Create(cx, cy, kind, IFramebuffer.Format.RGBA_8, depth, stencil);
            return fb;
        }

        public abstract IVersionInformation NewVersionInformation(Action<string> log);

        public abstract IExtensionInformation NewExtensionInformation();

        public abstract IDebugCallback NewDebugCallback(Action<string> log);

        public abstract IProgram NewProgram((ShaderType, string)[] shader_source);

        public abstract IVertexArrayObject NewVertexArrayObject();

        public abstract ITexture NewTexture();

        public abstract IFramebuffer NewFramebuffer();

        public abstract IRenderbuffer NewRenderbuffer();

        public abstract IStorageBuffer NewStorageBuffer();

        public abstract IPixelPackBuffer NewPixelPackBuffer();
    }
}
