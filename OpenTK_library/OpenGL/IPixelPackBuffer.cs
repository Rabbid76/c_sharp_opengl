using System;

namespace OpenTK_library.OpenGL
{
    public interface IPixelPackBuffer : IDisposable
    {
        void Create<T_DATA>();

        float[] ReadDepth(int x, int y, int w = 1, int h = 1);
    }
}
