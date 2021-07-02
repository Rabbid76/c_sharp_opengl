using System;

namespace OpenTK_library.OpenGL
{
    public interface IRenderbuffer : IDisposable
    {
        int Object { get; }

        void Create(int cx, int cy, bool depth, bool stencil);
    }
}
