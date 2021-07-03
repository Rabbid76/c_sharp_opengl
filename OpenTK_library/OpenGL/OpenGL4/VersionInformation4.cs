using System;
using OpenTK.Graphics.OpenGL4;

namespace OpenTK_library.OpenGL.OpenGL4
{
    internal class VersionInformation4 : IVersionInformation
    {
        private readonly Action<string> _log;
        private string _vendor;
        private string _renderer;
        private string _version;
        private string _glsl_version;
        private int _major;
        private int _minor;

        public VersionInformation4(Action<string> log)
        {
            _log = log;
        }

        // Get OpenGL version information
        public void Retrieve()
        {
            this._vendor = GL.GetString(StringName.Vendor);
            this._renderer = GL.GetString(StringName.Renderer);
            this._version = GL.GetString(StringName.Version);
            this._glsl_version = GL.GetString(StringName.ShadingLanguageVersion);
            this._major = GL.GetInteger(GetPName.MajorVersion);
            this._minor = GL.GetInteger(GetPName.MinorVersion);

            _log("OpenGL vendor:   " + this._vendor);
            _log("OpenGL renderer: " + this._renderer);
            _log("OpenGL version:  " + this._version);
            _log("GLSL   version:  " + this._glsl_version);
            _log("OpenGL " + this._major.ToString() + "." + this._minor.ToString());
        }
    }
}
