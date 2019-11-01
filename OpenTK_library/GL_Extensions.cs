using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4; // GL

namespace OpenTK_library
{
    public class GL_Extensions
    {
        private List<string> _extensions = new List<string>();

        public GL_Extensions()
        { }

        // Get OpenGL extension list
        public void Retrieve()
        {
            int no_extensions = GL.GetInteger(GetPName.NumExtensions);
            for (int i = 0; i < no_extensions; ++i)
            {
                string extension_name = GL.GetString(StringNameIndexed.Extensions, i);
                _extensions.Add(extension_name);
            }
        }
    }
}
