using System;
using System.Drawing;

namespace OpenTK_library.OpenGL
{
    public interface ITexture : IDisposable
    {
        enum Format { RGBA_8, RGBA_F32, Depth, DepthStencil };
        enum Access { Read, Write, ReadWrite };

        int Object { get; }
        int CX { get; }
        int CY { get; }
        bool Depth { get;  }
        bool Stencil { get; }
        Format InternalFormat { get; }

        void Create2D(Bitmap bm);

        void Create2D(int cx, int cy, byte[] pixel);

        void Create2D(int cx, int cy, ITexture.Format format);

        void Bind(int binding_point);

        void BindImage(int binding_point, Access access);
    }
}
