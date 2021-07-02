using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace OpenTK_library.OpenGL
{
    public interface IFramebuffer : IDisposable
    {
        enum Target { Read, Draw, ReadDraw };
        enum Kind { renderbuffer, texture };
        enum Format { RGBA_8, RGBA_F32 };

        int Object { get; }

        List<IRenderbuffer> Renderbuffers { get; }
        List<ITexture> Textures { get; }

        void Create(int cx, int cy, Kind kind, Format format, bool depth, bool stencil);
        void Bind(IFramebuffer.Target target = IFramebuffer.Target.ReadDraw, bool set_viewport = false);

        void Clear();

        void Clear(Color4 clear_color);

        void Blit(IFramebuffer draw_fbo, int x, int y, int w, int h, bool linear);

        void Blit(int fbo_d, int x, int y, int w, int h, bool linear);
    }
}
