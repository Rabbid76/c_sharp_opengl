using System;

namespace OpenTK_library.OpenGL.OpenGL4
{
    public class OpenGLObjectFactory4 : IOpenGLObjectFactory
    {
        public bool dsa = true;
        public bool vaoSeparateFormat = true;
        public bool immutableTexture = true;

        public override IVersionInformation NewVersionInformation(Action<string> log) =>
            new VersionInformation4(log);

        public override IExtensionInformation NewExtensionInformation() =>
            new ExtensionInformation4();

        public override IDebugCallback NewDebugCallback(Action<string> log) =>
            new DebugCallback4(log);

        public override IProgram NewProgram((ShaderType, string)[] shader_source) =>
            new Program4(shader_source);

        public override IVertexArrayObject NewVertexArrayObject() =>
            vaoSeparateFormat ? new VertexArrayObject4SeparateFormat() : new VertexArrayObject4();

        public override ITexture NewTexture() =>
            immutableTexture ? new Texture4Immutable() : new Texture4();

        public override IFramebuffer NewFramebuffer() =>
            dsa ? new Framebuffer4DSA(this) : new Framebuffer4(this);

        public override IRenderbuffer NewRenderbuffer() =>
            dsa ? new Renderbuffer4DSA() : new Renderbuffer4();

        public override IStorageBuffer NewStorageBuffer() =>
            dsa ? new StorageBuffer4DSA() : new StorageBuffer4();

        public override IPixelPackBuffer NewPixelPackBuffer() =>
            dsa ? new PixelPackBuffer4DSA() : new PixelPackBuffer4();
    }
}
