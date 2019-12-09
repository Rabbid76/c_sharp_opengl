using System;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4; // GL, ShaderType

namespace OpenTK_library.OpenGL
{
    public class Texture
        : IDisposable
    {
        private bool _disposed = false;
        private bool _buffer_specification_4 = true;

        private int _tbo = 0;
        private int _cx = 0;
        private int _cy = 0;
        private bool _depth = false;
        private bool _stencil = false;

        public int Object { get { return this._tbo; } }

        public Texture()
        { }

        ~Texture()
        {
            GL.DeleteTexture(this._tbo);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                GL.DeleteTexture(this._tbo);
                this._tbo = 0;
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Create2D(Bitmap bm)
        {
            byte[] textur_image = new byte[bm.Width * bm.Height * 4];

            // TODO $$$ improve that nested loops

            for (int x = 0; x < bm.Width; ++x)
            {
                for (int y = 0; y < bm.Height; ++y)
                {
                    int i = (y * bm.Width + x) * 4;
                    Color c = bm.GetPixel(x, y);
                    textur_image[i + 0] = c.R;
                    textur_image[i + 1] = c.G;
                    textur_image[i + 2] = c.B;
                    textur_image[i + 3] = c.A;
                }
            }
            /*
            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                textur_image = ms.ToArray();
            }
            */

            Create2D(bm.Width, bm.Height, textur_image);
        }

        public void Create2D(int cx, int cy, byte []pixel)
        {
            _cx = cx;
            _cy = cy;
            _depth = false;
            _stencil = false;

            if (_buffer_specification_4)
            {
                // [What's the DSA version of glTexImage2D?](https://gamedev.stackexchange.com/questions/134177/whats-the-dsa-version-of-glteximage2d)

                GL.CreateTextures(TextureTarget.Texture2D, 1, out this._tbo);
                //GL.TextureStorage2D(this._tbo, 1, SizedInternalFormat.Rgba8, cx, cy);
                //int base_level = 0;
                //GL.TextureParameter(this._tbo, TextureParameterName.TextureBaseLevel, (int)0);
                
                GL.BindTexture(TextureTarget.Texture2D, this._tbo);
                GL.TexImage2D<byte>(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, cx, cy, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixel);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.GenerateTextureMipmap(this._tbo);
            }
            else
            {
                this._tbo = GL.GenTexture();
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, this._tbo);
                GL.TexImage2D<byte>(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, cx, cy, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixel);
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            }
        }

        public void Create2D(int cx, int cy, bool depth, bool stencil)
        {
            if (/*depth &&*/ stencil)
                Create2D(cx, cy, PixelInternalFormat.DepthStencil, PixelFormat.DepthStencil, PixelType.UnsignedByte);
            else if (depth)
                Create2D(cx, cy, PixelInternalFormat.DepthComponent, PixelFormat.DepthComponent, PixelType.Float);
            else
                Create2D(cx, cy, PixelInternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte);
        }

        public void Create2D(int cx, int cy, PixelInternalFormat internalFormat, PixelFormat format, PixelType type)
        {
            _cx = cx;
            _cy = cy;
            _depth = false;
            _stencil = false;

            if (false /*_buffer_specification_4*/)
            {
                // [What's the DSA version of glTexImage2D?](https://gamedev.stackexchange.com/questions/134177/whats-the-dsa-version-of-glteximage2d)
                // TODO $$$
            }
            else
            {
                this._tbo = GL.GenTexture();
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, this._tbo);
                GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, cx, cy, 0, format, type, IntPtr.Zero );
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            }
        }

        public void Bind(int binding_point)
        {
            if (_buffer_specification_4)
            {
                GL.BindTextureUnit(binding_point, this._tbo);
            }
            else
            {
                GL.ActiveTexture(TextureUnit.Texture0 + binding_point);
                GL.BindTexture(TextureTarget.Texture2D, this._tbo);
            }
        }
    }
}
