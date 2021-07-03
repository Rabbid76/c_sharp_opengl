using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace OpenTK_library.OpenGL.OpenGL4
{
    internal class Framebuffer4 : IFramebuffer
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

        public Framebuffer4(IOpenGLObjectFactory openGLFactory)
        {
            this.openGLFactory = openGLFactory;
        }

        ~Framebuffer4()
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

            this._fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, this._fbo);

            if (kind == IFramebuffer.Kind.texture)
            {
                this._tbos = new List<ITexture>();

                var tbo_ca0 = openGLFactory.NewTexture();
                this._tbos.Add(tbo_ca0);
                ITexture.Format texture_format = ITexture.Format.RGBA_8;
                if (format == IFramebuffer.Format.RGBA_F32)
                    texture_format = ITexture.Format.RGBA_F32;
                tbo_ca0.Create2D(cx, cy, texture_format);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, tbo_ca0.Object, 0);

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
                    GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachment, TextureTarget.Texture2D, tbo_ds.Object, 0);
                }
            }
            else // if (kind == Kind.renderbuffer)
            {
                this._rbos = new List<IRenderbuffer>();

                var rbo_ca0 = openGLFactory.NewRenderbuffer();
                this._rbos.Add(rbo_ca0);
                rbo_ca0.Create(cx, cy, false, false);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, rbo_ca0.Object);

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
                    GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, attachment, RenderbufferTarget.Renderbuffer, rbo_ds.Object);
                }
            }

            _error_code = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            _valid = _error_code == FramebufferErrorCode.FramebufferComplete;

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
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

        // Clear framebuffer
        public void Clear()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, this._fbo);

            ClearBufferMask mask = ClearBufferMask.ColorBufferBit;
            if (this._depth)
                mask |= ClearBufferMask.DepthBufferBit;
            if (this._stencil)
                mask |= ClearBufferMask.StencilBufferBit;
            GL.Clear(mask);

        }

        public void Clear(Color4 clear_color)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, this._fbo);

            ClearBufferMask mask = ClearBufferMask.ColorBufferBit;
            if (this._depth)
                mask |= ClearBufferMask.DepthBufferBit;
            if (this._stencil)
                mask |= ClearBufferMask.StencilBufferBit;

            GL.ClearColor(clear_color);
            GL.Clear(mask);
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
            Bind(IFramebuffer.Target.Read);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, fbo_d);
            GL.BlitFramebuffer(0, 0, _cx, _cy, x, y, x + w, y + h, ClearBufferMask.ColorBufferBit, filter);
        }
    }
}
