using System;
using OpenTK.Graphics.OpenGL4; // GL

namespace OpenTK_library.OpenGL
{
    public class Renderbuffer
        : IDisposable
    {
        private bool _disposed = false;
        private bool _buffer_specification_4 = true;

        private int _rbo = 0;
        private int _cx = 0;
        private int _cy = 0;
        RenderbufferStorage _internal_format = RenderbufferStorage.Rgba8;

        public int Object { get { return this._rbo; } }

        public Renderbuffer()
        { }

        ~Renderbuffer()
        {
            GL.DeleteBuffer(this._rbo);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                GL.DeleteBuffer(this._rbo);
                this._rbo = 0;
                this._disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        //!// Crate renderbuffer object
        public void Create(int cx, int cy, bool depth, bool stencil)
        {
            this._cx = cx;
            this._cy = cy;

            this._internal_format = RenderbufferStorage.Rgba8;
            if (depth && stencil)
                this._internal_format = RenderbufferStorage.DepthStencil;
            else if (depth)
                this._internal_format = RenderbufferStorage.DepthComponent;
            else if (stencil)
                this._internal_format = RenderbufferStorage.StencilIndex8;

            if (this._buffer_specification_4)
            {
                GL.CreateRenderbuffers(1, out this._rbo);
                GL.NamedRenderbufferStorage(this._rbo, _internal_format, _cx, _cy);
            }
            else
            {
                this._rbo = GL.GenRenderbuffer();
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, this._rbo);
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, _internal_format, _cx, _cy);
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
            }
        }
    }
}
