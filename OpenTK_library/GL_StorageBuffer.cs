﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4; // GL, ShaderType

namespace OpenTK_library
{
    public class GL_StorageBuffer<T_DATA>
        : IDisposable
        where T_DATA : struct
    {
        private bool _disposed = false;
        private bool _buffer_specification_4 = true;

        private int _ssbo = 0;

        public GL_StorageBuffer()
        { }

        ~GL_StorageBuffer()
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
        public void Create(ref T_DATA data)
        {
            int data_size = Marshal.SizeOf(default(T_DATA));
            IntPtr data_ptr = Marshal.AllocHGlobal(data_size);
            Marshal.StructureToPtr(data, data_ptr, false);

            if (_buffer_specification_4)
            {
                GL.CreateBuffers(1, out this._ssbo);
                BufferStorageFlags storage = BufferStorageFlags.DynamicStorageBit | BufferStorageFlags.MapWriteBit | BufferStorageFlags.MapPersistentBit;
                GL.NamedBufferStorage(this._ssbo, data_size, data_ptr, storage);
            }
            else
            {
                this._ssbo = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, this._ssbo);
                GL.BufferData(BufferTarget.ShaderStorageBuffer, data_size, data_ptr, BufferUsageHint.DynamicDraw);
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
            }

            Marshal.FreeHGlobal(data_ptr);
        }

        //! Bind to binding point
        public void Bind(int binding_point)
        {
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, binding_point, this._ssbo);
        }

        //! Update Buffer data
        public void Update(ref T_DATA data)
        {
            // TODO 
            // map buffer (`_buffer_specification_4`)

            int data_size = Marshal.SizeOf(default(T_DATA));
            IntPtr data_ptr = Marshal.AllocHGlobal(data_size);
            Marshal.StructureToPtr(data, data_ptr, false);

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, this._ssbo);
            GL.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, data_size, data_ptr);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

            Marshal.FreeHGlobal(data_ptr);
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
