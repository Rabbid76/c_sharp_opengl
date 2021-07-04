using System;

namespace OpenTK_library.OpenGL
{
    public interface IStorageBuffer : IDisposable
    {
        public enum Usage { Read, Write, ReadWrite };

        public int Object { get; }

        void Create<T_DATA>(ref T_DATA data, Usage usage = Usage.Write);

        void Bind(int binding_point);

        void Update<T_DATA>(ref T_DATA data) where T_DATA : struct;

        void Update(int offset, int data_size, IntPtr data_ptr);
    }
}
