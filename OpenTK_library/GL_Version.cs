using System;
using OpenTK.Graphics.OpenGL4; // GL

namespace OpenTK_library
{
    public class GL_Version
    {
        private string _vendor;
        private string _renderer;
        private string _version;
        private string _glsl_version;
        private int _major;
        private int _minor;

        public GL_Version()
        {}

        // Get OpenGL version information
        public void Retrieve()
        {
            this._vendor = GL.GetString(StringName.Vendor);
            this._renderer = GL.GetString(StringName.Renderer);
            this._version = GL.GetString(StringName.Version);
            this._glsl_version = GL.GetString(StringName.ShadingLanguageVersion);
            this._major = GL.GetInteger(GetPName.MajorVersion);
            this._minor = GL.GetInteger(GetPName.MinorVersion);

            Console.WriteLine("OpenGL vendor:   " + this._vendor);
            Console.WriteLine("OpenGL renderer: " + this._renderer);
            Console.WriteLine("OpenGL version:  " + this._version);
            Console.WriteLine("GLSL   version:  " + this._glsl_version);
            Console.WriteLine("OpenGL " + this._major.ToString() + "." + this._minor.ToString());
        }
    }
}
