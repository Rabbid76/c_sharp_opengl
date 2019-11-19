using System;
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

        public void Create2D(int cx, int cy, byte []pixel)
        {
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
