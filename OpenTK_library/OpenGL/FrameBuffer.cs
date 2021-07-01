using System;
using System.Collections.Generic;
using OpenTK.Mathematics;      // Color4       
using OpenTK.Graphics.OpenGL4; // GL
using OpenTK_library.OpenGL.OpenGL4DSA;

namespace OpenTK_library.OpenGL
{
    public class Framebuffer
    {
        // TODO update framebuffer size

        public enum Target { Read, Draw, ReadDraw};
        public enum Kind { renderbuffer, texture };
        public enum Format { RGBA_8, RGBA_F32 };

        private bool _disposed = false;
        private bool _buffer_specification_4 = true;

        IOpenGLObjectFactory openGLFactory = new OpenGLObjectFactory4DSA(); // TODO
        private int _fbo = 0;
        private List<Renderbuffer> _rbos;
        private List<ITexture> _tbos;
        private FramebufferErrorCode _error_code = FramebufferErrorCode.FramebufferComplete;
        private FramebufferStatus _status = FramebufferStatus.FramebufferComplete;
        private bool _valid;
        private int _cx = 0; 
        private int _cy = 0;
        private bool _depth = false;
        private bool _stencil = false;

        public int Object { get { return this._fbo; } }
        public List<Renderbuffer> Renderbuffers { get => this._rbos; }
        public List<ITexture> Textures { get => this._tbos; }

        public Framebuffer()
        { }

        ~Framebuffer()
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

        public static Framebuffer CreateFrambuffer(int cx, int cy, Kind kind, bool depth, bool stencil)
        {
            var fb = new Framebuffer();
            fb.Create(cx, cy, kind, Format.RGBA_8, depth, stencil);
            return fb;
        }
          
        // Crate framebuffer object
        public void Create(int cx, int cy, Kind kind, Format format, bool depth, bool stencil)
        {
            _cx = cx;
            _cy = cy;
            _depth = depth;
            _stencil = stencil;

            if (_buffer_specification_4)
            {
                GL.CreateFramebuffers(1, out this._fbo);

                if (kind == Kind.texture)
                {
                    this._tbos = new List<ITexture>();

                    var tbo_ca0 = openGLFactory.NewTexture();
                    this._tbos.Add(tbo_ca0);
                    ITexture.Format texture_format = ITexture.Format.RGBA_8;
                    if (format == Format.RGBA_F32)
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
                    this._rbos = new List<Renderbuffer>();

                    var rbo_ca0 = new Renderbuffer();
                    this._rbos.Add(rbo_ca0);
                    rbo_ca0.Create(cx, cy, false, false);
                    GL.NamedFramebufferRenderbuffer(this._fbo, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, rbo_ca0.Object);

                    if (depth || stencil)
                    {
                        var rbo_ds = new Renderbuffer();
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
            else 
            { 
                this._fbo = GL.GenFramebuffer();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, this._fbo);

                if (kind == Kind.texture)
                {
                    this._tbos = new List<ITexture>();

                    var tbo_ca0 = openGLFactory.NewTexture();
                    this._tbos.Add(tbo_ca0);
                    ITexture.Format texture_format = ITexture.Format.RGBA_8;
                    if (format == Format.RGBA_F32)
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
                    this._rbos = new List<Renderbuffer>();

                    var rbo_ca0 = new Renderbuffer();
                    this._rbos.Add(rbo_ca0);
                    rbo_ca0.Create(cx, cy, false, false);
                    GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, rbo_ca0.Object);
                    
                    if (depth || stencil)
                    {
                        var rbo_ds = new Renderbuffer();
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
        }

        // Bind to target
        public void Bind(Target target = Target.ReadDraw, bool set_viewport = false)
        {
            GL.Viewport(0, 0, this._cx, this._cy);

            FramebufferTarget target_type = FramebufferTarget.Framebuffer;
            if (target == Target.Read)
                target_type = FramebufferTarget.ReadFramebuffer;
            else if (target == Target.Draw)
                target_type = FramebufferTarget.DrawFramebuffer;
            GL.BindFramebuffer(target_type, this._fbo);
        }

        // Clear frambuffer
        public void Clear()
        {
            if (_buffer_specification_4)
            {
                float[] color = { 0, 0, 0, 0};
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
            else
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, this._fbo);

                ClearBufferMask mask = ClearBufferMask.ColorBufferBit;
                if (this._depth)
                    mask |= ClearBufferMask.DepthBufferBit;
                if (this._stencil)
                    mask |= ClearBufferMask.StencilBufferBit;
                GL.Clear(mask);
            }
        }

        public void Clear(Color4 clear_color)
        {
            if (_buffer_specification_4)
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
            else
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
        }

        // Blit to target
        public void Blit(Framebuffer draw_fbo, int x, int y, int w, int h, bool linear)
        {
            int fbo_d = draw_fbo == null ? 0 : draw_fbo.Object;
            Blit(fbo_d, x, y, w, h, linear);
        }

        public void Blit(int fbo_d, int x, int y, int w, int h, bool linear)
        {
            BlitFramebufferFilter filter = linear ? BlitFramebufferFilter.Linear : BlitFramebufferFilter.Nearest;

            if (_buffer_specification_4)
            {
                GL.BlitNamedFramebuffer(this._fbo, fbo_d, 0, 0, _cx, _cy, x, y, x + w, y + h, ClearBufferMask.ColorBufferBit, filter);
            }
            else
            {
                Bind(Target.Read);
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, fbo_d);
                GL.BlitFramebuffer(0, 0, _cx, _cy, x, y, x + w, y + h, ClearBufferMask.ColorBufferBit, filter);
            }
        }
    }
}
