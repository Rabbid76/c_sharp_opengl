using System;
using System.Drawing;
using OpenTK.Graphics.OpenGL4;

namespace OpenTK_library.OpenGL.OpenGL4
{
    internal class Texture4Immutable : ITexture
    {
        private bool _disposed = false;
        
        private int _tbo = 0;
        private int _cx = 0;
        private int _cy = 0;
        private bool _depth = false;
        private bool _stencil = false;
        private ITexture.Format _foramt = ITexture.Format.RGBA_8;

        public int Object { get => this._tbo; }
        public int CX { get => this._cx; }
        public int CY { get => this._cy; }
        public bool Depth { get => this._depth; }
        public bool Stencil { get => this._stencil; }
        public ITexture.Format InternalFormat { get => this._foramt; }

        public Texture4Immutable()
        { }

        ~Texture4Immutable()
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

        public void Create2D(int cx, int cy, byte[] pixel)
        {
            _cx = cx;
            _cy = cy;
            _depth = false;
            _stencil = false;
            _foramt = ITexture.Format.RGBA_8;

            float maxTextureMaxAnisotropy = GL.GetFloat((GetPName)0x84FF);
            float textureMaxAnisotropy = maxTextureMaxAnisotropy;

            GL.CreateTextures(TextureTarget.Texture2D, 1, out this._tbo);
            GL.TextureParameter(this._tbo, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TextureParameter(this._tbo, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TextureParameter(this._tbo, (TextureParameterName)0x84FE, textureMaxAnisotropy);
            GL.TextureStorage2D(this._tbo, 1, SizedInternalFormat.Rgba8, cx, cy);
            GL.TextureSubImage2D<byte>(this._tbo, 0, 0, 0, cx, cy, PixelFormat.Rgba, PixelType.UnsignedByte, pixel);
            GL.GenerateTextureMipmap(this._tbo); // TODO $$$ mipmaps for immutable texture?
        }

        public void Create2D(int cx, int cy, ITexture.Format format)
        {
            _foramt = format;

            switch (format)
            {
                case ITexture.Format.RGBA_8: Create2D(cx, cy, SizedInternalFormat.Rgba8); break;
                case ITexture.Format.RGBA_F32: Create2D(cx, cy, SizedInternalFormat.Rgba32f); break;

                // TODO $$$ depth and stencil format for depth and stencil
                case ITexture.Format.Depth: Create2D(cx, cy, PixelInternalFormat.DepthComponent, PixelFormat.DepthComponent, PixelType.Float); break;
                case ITexture.Format.DepthStencil: Create2D(cx, cy, PixelInternalFormat.DepthStencil, PixelFormat.DepthStencil, PixelType.UnsignedInt248); break;
            }
        }

        private void Create2D(int cx, int cy, SizedInternalFormat internalFormat)
        {
            _cx = cx;
            _cy = cy;
            _depth = false;
            _stencil = false;

            GL.CreateTextures(TextureTarget.Texture2D, 1, out this._tbo);
            GL.TextureStorage2D(this._tbo, 1, internalFormat, cx, cy);
            GL.TextureParameter(this._tbo, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TextureParameter(this._tbo, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            //GL.TextureParameter(this._tbo, (TextureParameterName)0x84FE, 16);
        }

        private void Create2D(int cx, int cy, PixelInternalFormat internalFormat, PixelFormat format, PixelType type)
        {
            _cx = cx;
            _cy = cy;
            _depth = false;
            _stencil = false;

            this._tbo = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, this._tbo);
            GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, cx, cy, 0, format, type, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            //GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)0x84FE, 16);
        }

        // bind the texture to target (for texture sampler)
        public void Bind(int binding_point)
        {
            GL.BindTextureUnit(binding_point, this._tbo);
        }

        // bind the texture for image load and store operation
        public void BindImage(int binding_point, ITexture.Access access)
        {
            TextureAccess tex_access = TextureAccess.ReadWrite;
            switch (access)
            {
                case ITexture.Access.Read: tex_access = TextureAccess.ReadOnly; break;
                case ITexture.Access.Write: tex_access = TextureAccess.WriteOnly; break;
                case ITexture.Access.ReadWrite: tex_access = TextureAccess.ReadWrite; break;
            }

            SizedInternalFormat tex_format = SizedInternalFormat.Rgba32f;
            switch (_foramt)
            {
                case ITexture.Format.RGBA_8: tex_format = SizedInternalFormat.Rgba8; break;
                case ITexture.Format.RGBA_F32: tex_format = SizedInternalFormat.Rgba32f; break;
            }

            GL.BindImageTexture(binding_point, Object, 0, false, 0, tex_access, tex_format);
        }
    }
}
