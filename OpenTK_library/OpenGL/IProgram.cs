using System;

namespace OpenTK_library.OpenGL
{
    public interface IProgram : IDisposable
    {
        public int Object { get; }

        public bool Valid { get; }

        void Use();

        bool Generate();
    }
}
