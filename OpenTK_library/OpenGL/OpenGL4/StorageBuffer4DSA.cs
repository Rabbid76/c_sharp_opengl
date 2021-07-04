﻿using System;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace OpenTK_library.OpenGL.OpenGL4
{
    internal class StorageBuffer4DSA : IStorageBuffer
    {
        private bool _disposed = false;
        private int _ssbo = 0;

        public int Object { get => this._ssbo; }

        public StorageBuffer4DSA()
        { }

        ~StorageBuffer4DSA()
        {
            GL.DeleteBuffer(this._ssbo);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                GL.DeleteBuffer(this._ssbo);
                this._ssbo = 0;
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        //! Create shader storage buffer object
        public void Create<T_DATA>(ref T_DATA data, IStorageBuffer.Usage usage = IStorageBuffer.Usage.Write)
        {
            int data_size = Marshal.SizeOf(default(T_DATA));
            IntPtr data_ptr = Marshal.AllocHGlobal(data_size);
            Marshal.StructureToPtr(data, data_ptr, false);

            BufferStorageFlags storage = BufferStorageFlags.DynamicStorageBit | BufferStorageFlags.MapPersistentBit;
            if (usage == IStorageBuffer.Usage.Write || usage == IStorageBuffer.Usage.ReadWrite)
                storage = storage | BufferStorageFlags.MapWriteBit;
            if (usage == IStorageBuffer.Usage.Read || usage == IStorageBuffer.Usage.ReadWrite)
                storage = storage | BufferStorageFlags.MapReadBit;

            GL.CreateBuffers(1, out this._ssbo);
            GL.NamedBufferStorage(this._ssbo, data_size, data_ptr, storage);

            Marshal.FreeHGlobal(data_ptr);
        }

        //! Bind to binding point
        public void Bind(int binding_point)
        {
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, binding_point, this._ssbo);
        }

        //! Update Buffer data
        public void Update<T_DATA>(ref T_DATA data) where T_DATA : struct
        {
            // TODO 
            // map buffer (`_buffer_specification_4`)

            int data_size = Marshal.SizeOf(default(T_DATA));

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, this._ssbo);
            GL.BufferSubData<T_DATA>(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, data_size, ref data);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }

        //! Update Buffer sub data
        public void Update(int offset, int data_size, IntPtr data_ptr)
        {
            IntPtr offset_as_ptr = new IntPtr(offset);

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, this._ssbo);
            GL.BufferSubData(BufferTarget.ShaderStorageBuffer, offset_as_ptr, data_size, data_ptr);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }
    }
}
