using System;
using System.Collections.Generic;
using OpenTK.Mathematics;     
using OpenTK.Graphics.OpenGL4;

namespace OpenTK_library.OpenGL.OpenGL4
{
    internal class Framebuffer4DSA : IFramebuffer
    {
        // TODO update framebuffer size

        private bool _disposed = false;
        
        private readonly IOpenGLObjectFactory openGLFactory;
        private int _fbo = 0;
        private List<IRenderbuffer> _rbos;
        private List<ITexture> _tbos;
        private FramebufferErrorCode _error_code = FramebufferErrorCode.FramebufferComplete;
        private FramebufferStatus _status = FramebufferStatus.FramebufferComplete;
        private bool _valid;
        private int _cx = 0;
        private int _cy = 0;
        private bool _depth = false;
        private bool _stencil = false;

        public int Object { get => this._fbo; }
        public List<IRenderbuffer> Renderbuffers { get => this._rbos; }
        public List<ITexture> Textures { get => this._tbos; }

        public Framebuffer4DSA(IOpenGLObjectFactory openGLFactory)
        {
            this.openGLFactory = openGLFactory;
        }

        ~Framebuffer4DSA()
        {
            GL.DeleteFramebuffer(this._fbo);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                if (this._rbos != null)
                {
                    foreach (var rbo in this._rbos)
                        rbo.Dispose();
                    _rbos.Clear();
                }
                if (this._tbos != null)
                {
                    foreach (var tbo in this._tbos)
                        tbo.Dispose();
                    _tbos.Clear();
                    GL.DeleteFramebuffer(this._fbo);
                }
                this._fbo = 0;
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Crate framebuffer object
        public void Create(int cx, int cy, IFramebuffer.Kind kind, IFramebuffer.Format format, bool depth, bool stencil)
        {
            _cx = cx;
            _cy = cy;
            _depth = depth;
            _stencil = stencil;

            GL.CreateFramebuffers(1, out this._fbo);

            if (kind == IFramebuffer.Kind.texture)
            {
                this._tbos = new List<ITexture>();

                var tbo_ca0 = openGLFactory.NewTexture();
                this._tbos.Add(tbo_ca0);
                ITexture.Format texture_format = ITexture.Format.RGBA_8;
                if (format == IFramebuffer.Format.RGBA_F32)
                    texture_format = ITexture.Format.RGBA_F32;
                tbo_ca0.Create2D(cx, cy, texture_format);
                GL.NamedFramebufferTexture(this._fbo, FramebufferAttachment.ColorAttachment0, tbo_ca0.Object, 0);

                if (depth || stencil)
                {
                    var tbo_ds = openGLFactory.NewTexture();
                    this._tbos.Add(tbo_ds);
                    FramebufferAttachment attachment;
                    if (depth && stencil == false)
                    {
                        tbo_ds.Create2D(cx, cy, ITexture.Format.Depth);
                        attachment = FramebufferAttachment.DepthAttachment;
                    }
                    else if (depth == false && stencil)
                    {
                        tbo_ds.Create2D(cx, cy, ITexture.Format.DepthStencil);
                        attachment = FramebufferAttachment.StencilAttachment;
                    }
                    else
                    {
                        tbo_ds.Create2D(cx, cy, ITexture.Format.DepthStencil);
                        attachment = FramebufferAttachment.DepthStencilAttachment;
                    }
                    GL.NamedFramebufferTexture(this._fbo, attachment, tbo_ds.Object, 0);
                }
            }
            else // if (kind == Kind.renderbuffer)
            {
                this._rbos = new List<IRenderbuffer>();

                var rbo_ca0 = openGLFactory.NewRenderbuffer();
                this._rbos.Add(rbo_ca0);
                rbo_ca0.Create(cx, cy, false, false);
                GL.NamedFramebufferRenderbuffer(this._fbo, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, rbo_ca0.Object);

                if (depth || stencil)
                {
                    var rbo_ds = openGLFactory.NewRenderbuffer();
                    this._rbos.Add(rbo_ds);
                    rbo_ds.Create(cx, cy, depth, stencil);
                    FramebufferAttachment attachment = FramebufferAttachment.DepthStencilAttachment;
                    if (depth && stencil == false)
                        attachment = FramebufferAttachment.DepthAttachment;
                    else if (depth == false && stencil)
                        attachment = FramebufferAttachment.StencilAttachment;
                    GL.NamedFramebufferRenderbuffer(this._fbo, attachment, RenderbufferTarget.Renderbuffer, rbo_ds.Object);
                }
            }

            _status = GL.CheckNamedFramebufferStatus(this._fbo, FramebufferTarget.Framebuffer);
            _valid = _status == FramebufferStatus.FramebufferComplete;
        }

        // Bind to target
        public void Bind(IFramebuffer.Target target = IFramebuffer.Target.ReadDraw, bool set_viewport = false)
        {
            GL.Viewport(0, 0, this._cx, this._cy);

            FramebufferTarget target_type = FramebufferTarget.Framebuffer;
            if (target == IFramebuffer.Target.Read)
                target_type = FramebufferTarget.ReadFramebuffer;
            else if (target == IFramebuffer.Target.Draw)
                target_type = FramebufferTarget.DrawFramebuffer;
            GL.BindFramebuffer(target_type, this._fbo);
        }

        // Clear frambuffer
        public void Clear()
        {
            float[] color = { 0, 0, 0, 0 };
            GL.ClearNamedFramebuffer(this._fbo, ClearBuffer.Color, 0, color);
            if (this._depth)
            {
                float depth = 1.0f;
                GL.ClearNamedFramebuffer(this._fbo, ClearBuffer.Depth, 0, ref depth);
            }
            if (this._stencil)
            {
                int stencil = 0;
                GL.ClearNamedFramebuffer(this._fbo, ClearBuffer.Stencil, 0, ref stencil);
            }
        }

        public void Clear(Color4 clear_color)
        {
            float[] color = { clear_color.R, clear_color.G, clear_color.B, clear_color.A };
            GL.ClearNamedFramebuffer(this._fbo, ClearBuffer.Color, 0, color);
            if (this._depth)
            {
                float depth = 1.0f;
                GL.ClearNamedFramebuffer(this._fbo, ClearBuffer.Depth, 0, ref depth);
            }
            if (this._stencil)
            {
                int stencil = 0;
                GL.ClearNamedFramebuffer(this._fbo, ClearBuffer.Stencil, 0, ref stencil);
            }
        }

        // Blit to target
        public void Blit(IFramebuffer draw_fbo, int x, int y, int w, int h, bool linear)
        {
            int fbo_d = draw_fbo == null ? 0 : draw_fbo.Object;
            Blit(fbo_d, x, y, w, h, linear);
        }

        public void Blit(int fbo_d, int x, int y, int w, int h, bool linear)
        {
            BlitFramebufferFilter filter = linear ? BlitFramebufferFilter.Linear : BlitFramebufferFilter.Nearest;
            GL.BlitNamedFramebuffer(this._fbo, fbo_d, 0, 0, _cx, _cy, x, y, x + w, y + h, ClearBufferMask.ColorBufferBit, filter);
        }
    }
}
