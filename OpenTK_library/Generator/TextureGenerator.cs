using System;
using System.Collections.Generic;
using OpenTK_library.OpenGL;
using OpenTK_library.OpenGL.OpenGL4;
using OpenTK.Graphics.OpenGL4; // GL

namespace OpenTK_library.Generator
{
    public class TextureGenerator
        : IDisposable
    {
        private readonly IOpenGLObjectFactory _openGLFactory;
        private bool _disposed = false;

        public enum TType
        { 
            texture_test1,  //! test texture with multiple colors
            heightmap_test1, //! test height map corresponding to `texture_test1`
            cone_step_map
        }

        private IProgram _compute_prog;
        TType _type;
        private List<ITexture> _out_textures;
        private List<ITexture> _in_textures;
        
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _compute_prog?.Dispose();
                this._disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~TextureGenerator()
        {}

        public TextureGenerator(IOpenGLObjectFactory openGLFactory, TType type, ITexture[] out_textures, ITexture[] in_textures = null)
        {
            _openGLFactory = openGLFactory;
            _type = type;
            _out_textures = new List<ITexture>(out_textures);
            if (in_textures != null)
                _in_textures = new List<ITexture>(in_textures);
        }

        public bool Generate()
        {
            if (_out_textures == null || _out_textures.Count == 0)
                return false;
            if (GenetateProgram() == false)
                return false;

            int i = 1;
            if (_out_textures != null)
            {
                foreach (var tob in _out_textures)
                    tob.BindImage(i++, ITexture.Access.Write);
            }
            if (_in_textures != null)
            {
                foreach (var tob in _in_textures)
                    tob.BindImage(i++, ITexture.Access.Read);
            }

            _compute_prog.Use();
            GL.DispatchCompute(_out_textures[0].CX, _out_textures[0].CY, 1);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit); // alternative:  MemoryBarrierFlags.AllBarrierBits;
            GL.UseProgram(0);

            return true;
        }

        public bool GenetateProgram()
        {
            if (_compute_prog != null)
                return _compute_prog.Valid;

            string source_code = AssembleShaderCode();
            if (source_code == null || source_code == "")
                return false;

            _compute_prog = _openGLFactory.ComputeShaderProgram(source_code);
            return _compute_prog.Generate();
        }

        private string AssembleShaderCode()
        {
            string source_code = "";
            switch (_type)
            {
                case TType.texture_test1: source_code = Code_Texture_Test1(); break;
                case TType.heightmap_test1: source_code = Code_HeightMap_Test1(); break;
                case TType.cone_step_map: source_code = Code_ConeStepMap(); break;
            }
            if (source_code == null || source_code == "")
                return "";

            string final_code = "#version 460\n";

            List<ITexture> all_textures = new List<ITexture>();
            all_textures?.AddRange(_out_textures);
            if (_in_textures != null)
                all_textures?.AddRange(_in_textures);
            for (int i=0; i < all_textures.Count; ++i)
            {
                string format_str = "rgba8";
                switch (all_textures[i].InternalFormat)
                {
                    case ITexture.Format.RGBA_8: format_str = "rgba8"; break;
                    case ITexture.Format.RGBA_F32: format_str = "rgba32f"; break;
                }
                final_code += "#define IMAGE_FORMAT_" + i.ToString() + " " + format_str + "\n";
            }

            final_code += source_code;

            return final_code;
        }

        private string Code_Texture_Test1()
        {
            string compute_shader =
            @"layout(local_size_x = 1, local_size_y = 1) in;
            layout(IMAGE_FORMAT_0, binding = 1) writeonly uniform image2D img_output;
            
            vec4 CreateTexture(vec2 uv)
            {
                vec2 uv_inner = (uv-0.5) * 1.05;
                if (abs(uv_inner.x) > 0.5 || abs(uv_inner.y) > 0.5)
                    return vec4(vec3(0.5), 1.0);

                vec2 uv_tile = uv_inner;
                if (uv_tile.x < 0.0) uv_tile.x += 0.5;
                if (uv_tile.y < 0.0) uv_tile.y += 0.5;
                uv_tile *= 2.0;

                vec3 color = vec3(0.0);
                if (uv_tile.x < 1.0/3.0)
                {
                    if (uv_tile.y < 1.0/3.0)
                        color = vec3(1.0, 0.0, 0.0);
                    else if (uv_tile.y < 2.0/3.0)
                        color = vec3(1.0, 0.5, 0.0);
                    else
                        color = vec3(1.0, 1.0, 0.0);
                }
                else if (uv_tile.x < 2.0/3.0)
                {
                    if (uv_tile.y < 1.0/3.0)
                        color = vec3(1.0, 0.0, 1.0);
                    else if (uv_tile.y < 2.0/3.0)
                        color = vec3(0.5);
                    else
                        color = vec3(0.0, 1.0, 0.0);
                }
                else
                {
                    if (uv_tile.y < 1.0/3.0)
                        color = vec3(0.0, 0.0, 1.0);
                    else if (uv_tile.y < 2.0/3.0)
                        color = vec3(1.0);
                    else
                        color = vec3(0.1);
                }

                return vec4(color.rgb, 1.0);
            }

            void main() {
  
                  // get index in global work group i.e x,y position
                  ivec2 pixel_coords = ivec2(gl_GlobalInvocationID.xy);
                  ivec2 dims         = imageSize(img_output); // fetch image dimensions
            
                  vec4 color = CreateTexture(vec2(pixel_coords.yx) / vec2(dims)); 

                  imageStore(img_output, pixel_coords, color);
            }";
            return compute_shader;
        }

        private string Code_HeightMap_Test1()
        {
            string compute_shader =
            @"layout(local_size_x = 1, local_size_y = 1) in;
            layout(IMAGE_FORMAT_0, binding = 1) writeonly uniform image2D img_output;
            
            vec4 CreateHeightmap(vec2 uv)
            {
                vec2 uv_inner = (uv-0.5) * 1.05;
                if (abs(uv_inner.x) > 0.5 || abs(uv_inner.y) > 0.5)
                    return vec4(vec3(0.0), 1.0);

                vec2 uv_tile = uv_inner;
                if (uv_tile.x < 0.0) uv_tile.x += 0.5;
                if (uv_tile.y < 0.0) uv_tile.y += 0.5;
                uv_tile *= 2.0;

                float h = 0.0;
                if (uv_inner.x < 0.0 && uv_inner.y < 0.0)
                {
                    vec2 uv_ramp = fract(uv_tile * 3.0);
                    uv_ramp = 1.0 - abs((uv_ramp - 0.5) * 2.0);
                    h = min(uv_ramp.x, uv_ramp.y);
                }
                else if (uv_inner.x < 0.0)
                {
                    vec2 uv_sin = uv_tile * 2.0 * 3.141593 * 3.0;
                    h = (0.5 - cos(uv_sin.x)*0.5) * (0.5 - cos(uv_sin.y) * 0.5);
                }
                else if (uv_inner.y < 0.0)
                {
                   vec2 uv_ramp = fract(uv_tile * 6.0);
                   uv_ramp = 1.0 - max(abs((uv_ramp - 0.5) * 2.0) - 0.5, 0.0) * 2.0;
                   h = min(uv_ramp.x, uv_ramp.y);
                }
                else
                {
                   vec2 uv_needle = fract(uv_tile * 6.0);
                   uv_needle = 0.5 - abs(uv_needle-0.5);
                   uv_needle = sin(uv_needle * 3.141593);
                   h = min(uv_needle.x, uv_needle.y);
                }

                if (int(trunc(uv_tile.x*3.0) + trunc(uv_tile.y*3.0)) % 2 == 1)
                    h = 1.0 - h;

                return vec4(vec3(h), 1.0);
            }

            void main() {
  
                  // get index in global work group i.e x,y position
                  ivec2 pixel_coords = ivec2(gl_GlobalInvocationID.xy);
                  ivec2 dims         = imageSize(img_output); // fetch image dimensions
            
                  vec4 color = CreateHeightmap(vec2(pixel_coords.yx) / vec2(dims)); 

                  imageStore(img_output, pixel_coords, color);
            }";
            return compute_shader;
        }

        private string Code_ConeStepMap()
        {
            string compute_shader =
            @"layout(local_size_x = 1, local_size_y = 1) in;            
            layout(IMAGE_FORMAT_0, binding = 1) writeonly uniform image2D cone_map_image;
            
            //#define SOURCE_TEXTURE

            #if defined( SOURCE_TEXTURE )

            // height map source texture
            layout(binding = 2) uniform sampler2D u_height_map;

            // read height from height map
            float get_height(in ivec2 coord)
            {
              return texelFetch(u_height_map, coord, 0).x;
            }

            #else

            // read cone map image
            layout(IMAGE_FORMAT_1, binding = 2) readonly uniform image2D height_map_image;

            // read height from image
            float get_height(in ivec2 coord)
            {
              return imageLoad(height_map_image, coord).x;
            }

            #endif

            const float max_cone_c = 1.0;

            void main()
            {  
              ivec2 pixel_coords = ivec2(gl_GlobalInvocationID.xy);  // get index in global work group i.e x,y position

              ivec2 map_dim  = imageSize(cone_map_image);
              int   cx       = map_dim.x;   
              int   cy       = map_dim.y;  
              int   x        = pixel_coords.x;   
              int   y        = pixel_coords.y;        
              float step_x   = 1.0 / float(cx);
              float step_y   = 1.0 / float(cy);
              float step     = max(step_x, step_y); 
  
              float h        = get_height(pixel_coords);  
              float c        = max_cone_c;
              float max_h    = 1.0 - h;
              float max_dist = min(max_cone_c * max_h, 1.0);

              for( float dist = step; dist <= max_dist && c > dist / max_h; dist += step )
              {
                int   d2       = int(round((dist*dist) / (step*step)));
                int   dy       = int(round(dist / step_y));
                float sample_h = 0;
                for( int dx = 0; sample_h < 1.0 && float(dx) / float(cx) <= dist; ++ dx )
                {
                  if ( (dx*dx + dy*dy) < d2 && dy < cy-1 )
                    dy ++;
                  do
                  {
                    int sx_n = ((cx + x - dx) % cx);
                    int sx_p = ((cx + x + dx) % cx);
                    int sy_n = ((cy + y - dy) % cy);
                    int sy_p = ((cy + y + dy) % cy);
            
                    sample_h = max( sample_h, get_height(ivec2(sx_p, sy_p)) );
                    sample_h = max( sample_h, get_height(ivec2(sx_p, sy_n)) );
                    sample_h = max( sample_h, get_height(ivec2(sx_n, sy_p)) );
                    sample_h = max( sample_h, get_height(ivec2(sx_n, sy_n)) );

                    dy --;
                  }
                  while ( dy > 0 && (dx*dx + dy*dy) >= d2 );
                }
                if ( sample_h > h )
                {
                  float d_h      = float(sample_h - h);
                  float sample_c = dist / d_h; 
                  c              = min(c, sample_c);
                }
              }
    
              vec4 cone_map = vec4(h, sqrt(c), 0.0, 0.0);
  
              imageStore(cone_map_image, pixel_coords, cone_map);
            }";
            return compute_shader;
        }
    }
}
