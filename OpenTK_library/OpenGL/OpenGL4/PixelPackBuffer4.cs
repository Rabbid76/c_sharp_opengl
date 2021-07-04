using System;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace OpenTK_library.OpenGL.OpenGL4
{
    internal class PixelPackBuffer4 : IPixelPackBuffer
    {
        private bool _disposed = false;
        private int _ppbo = 0;

        public PixelPackBuffer4()
        { }

        ~PixelPackBuffer4()
        {
            GL.DeleteBuffer(this._ppbo);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                GL.DeleteBuffer(this._ppbo);
                this._ppbo = 0;
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Create<T_DATA>()
        {
            int data_size = Marshal.SizeOf(default(T_DATA));

            this._ppbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.PixelPackBuffer, this._ppbo);
            GL.BufferData(BufferTarget.PixelPackBuffer, data_size, IntPtr.Zero, BufferUsageHint.StreamCopy);
            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
        }

        public float[] ReadDepth(int x, int y, int w = 1, int h = 1)
        {
            float[] depth = new float[w * h];

            GL.ReadnPixels<float>(x, y, w, h, PixelFormat.DepthComponent, PixelType.Float, w * h * sizeof(float), depth);

            /*
            TODO : That doesn't work, but at the moment it's completely unclear what causes the issue.
                   There is nothing complicate on this OpenGL instructions (in c++ this seems to work).
                   Is it cause because the depth buffer is read?
                   Is it cause because of `IntPtr.Zero`?
            
            GL.ReadBuffer(ReadBufferMode.Front);

            GL.BindBuffer(BufferTarget.PixelPackBuffer, this._ppbo);
            GL.ReadPixels(x, y, w, h, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            
            IntPtr buffer_data = GL.MapBuffer(BufferTarget.PixelPackBuffer, BufferAccess.ReadOnly);
            Marshal.Copy(buffer_data, depth, 0, w*h);
            GL.UnmapBuffer(BufferTarget.PixelPackBuffer);
            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);

            Console.WriteLine(depth[0].ToString());
            */

            return depth;
        }
    }
}
