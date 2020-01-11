﻿using System;
using System.Collections.Generic;
using OpenTK; // Vector2, Vector3, Vector4, Matrix4
using OpenTK.Graphics.OpenGL4; // GL
using OpenTK_compute_conestepmap.ViewModel;
using OpenTK_library;
using OpenTK_library.Type;
using OpenTK_library.Mesh;
using OpenTK_library.Controls;
using OpenTK_library.Generator;
using OpenTK_library.OpenGL;

namespace OpenTK_compute_conestepmap.Model
{
    public class ComputeModel
        : IDisposable
    {
        internal unsafe struct TLightSource
        {
            public fixed float _light_dir[4];
            public float _ambient;
            public float _diffuse;
            public float _specular;
            public float _shininess;

            public TLightSource(Vector4 light_dir, float ambient, float diffuse, float specular, float shininess)
            {
                this._ambient = ambient;
                this._diffuse = diffuse;
                this._specular = specular;
                this._shininess = shininess;
                this.lightDir = light_dir;
            }

            public Vector4 lightDir
            {
                get
                {
                    return new Vector4(this._light_dir[0], this._light_dir[1], this._light_dir[2], this._light_dir[3]);
                }
                set
                {
                    float[] data = new float[] { value.X, value.Y, value.Z, value.W };
                    for (int i = 0; i < 4; ++i)
                        this._light_dir[i] = data[i];
                }
            }
        }

        private bool _disposed = false;
        private int _cx = 0;
        private int _cy = 0;
        private OpenTK_library.OpenGL.Version _version = new OpenTK_library.OpenGL.Version();
        private Extensions _extensions = new Extensions();
        private DebugCallback _debug_callback = new DebugCallback();

        private List<TextureGenerator> _generators;
        private List<Framebuffer> _fbos;
        private int _image_cx = 512; //1024;
        private int _image_cy = 512; //1024;
        private int _frame = 0;
        double _period = 0;

        public ComputeModel()
        { }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                foreach (var fbo in _fbos)
                    fbo.Dispose();
                _fbos.Clear();
                foreach (var generrator in _generators)
                    generrator.Dispose();
                _generators.Clear();
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void MouseDown(Vector2 wnd_pos, bool left)
        {
            // ...
        }

        public void MouseUp(Vector2 wnd_pos, bool left)
        {
            // ...
        }

        public void MouseMove(Vector2 wnd_pos)
        {
            // ...
        }

        public void Setup(int cx, int cy)
        {
            this._cx = cx;
            this._cy = cy;

            // Version strings
            _version.Retrieve();

            // Get OpenGL extensions
            _extensions.Retrieve();

            // Debug callback
            _debug_callback.Init();

            // create Vertex Array Object, Array Buffer Object and Element Array Buffer Object

            // [...]

            // Create shader program

            // [...]

            // framebuffers

            _fbos = new List<Framebuffer>();
            _fbos.Add(new Framebuffer());
            _fbos[0].Create(_image_cx, _image_cy, Framebuffer.Kind.texture, Framebuffer.Format.RGBA_F32, true, false);
            _fbos[0].Clear();
            _fbos.Add(new Framebuffer());
            _fbos[1].Create(_image_cx, _image_cy, Framebuffer.Kind.texture, Framebuffer.Format.RGBA_F32, true, false);
            _fbos[1].Clear();
            _fbos.Add(new Framebuffer());
            _fbos[2].Create(_image_cx, _image_cy, Framebuffer.Kind.texture, Framebuffer.Format.RGBA_F32, true, false);
            _fbos[2].Clear();

            // create generators
            this._generators = new List<TextureGenerator>();
            this._generators.Add(new TextureGenerator(TextureGenerator.TType.texture_test1, new Texture[] { _fbos[0].Textures[0] }));
            this._generators.Add(new TextureGenerator(TextureGenerator.TType.heightmap_test1, new Texture[] { _fbos[1].Textures[0] }));
            this._generators.Add(new TextureGenerator(TextureGenerator.TType.cone_step_map, new Texture[] { _fbos[2].Textures[0] }, new Texture[] { _fbos[1].Textures[0] }));

            foreach (var generator in this._generators)
                generator.GenetateProgram();

            // states

            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
        }

        public void Draw(int cx, int cy, double app_t)
        {
            this._period = app_t;

            bool resized = this._cx != cx || this._cy != cy;
            if (resized)
            {
                this._cx = cx;
                this._cy = cy;
                GL.Viewport(0, 0, this._cx, this._cy);
            }

            if (_frame < 3)
                this._generators[_frame].Generate();
            this._frame++;

            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Be aware, this won't work if the target framebuffer is a multisampling framebuffer
            List<Vector2> pts = new List<Vector2>();
            int side_len = 0;
            if (this._cx >= this._cy * 1.5)
            {
                side_len = Math.Min(this._cy, this._cx / 3);
                int offset_x = (this._cx - side_len * 3) / 2;
                int offset_y = (this._cy - side_len) / 2;
                pts.Add(new Vector2(offset_x, offset_y));
                pts.Add(new Vector2(offset_x + side_len, offset_y));
                pts.Add(new Vector2(offset_x + side_len * 2, offset_y));
            }
            else if (this._cy >= this._cx * 1.5)
            {
                side_len = Math.Min(this._cx, this._cy / 3);
                int offset_x = (this._cx - side_len) / 2;
                int offset_y = (this._cy - side_len * 3) / 2;
                pts.Add(new Vector2(offset_x, offset_y));
                pts.Add(new Vector2(offset_x, offset_y + side_len));
                pts.Add(new Vector2(offset_x, offset_y + side_len * 2));
            }
            else if (this._cx > this._cy)
            {
                side_len = this._cy / 2;
                pts.Add(new Vector2((this._cx - side_len) / 2, 0));
                pts.Add(new Vector2((this._cx - 2 * side_len) / 2, side_len));
                pts.Add(new Vector2((this._cx - 2 * side_len) / 2 + side_len, side_len));
            }
            else
            {
                side_len = this._cx / 2;
                pts.Add(new Vector2(side_len / 2, (this._cy - 2 * side_len) / 2));
                pts.Add(new Vector2(0, (this._cy - 2 * side_len) / 2 + side_len));
                pts.Add(new Vector2(side_len, (this._cy - 2 * side_len) / 2 + side_len));
            }

            for (int i = 0; i < 3; ++ i)
                _fbos[i].Blit(null, (int)pts[i].X, (int)pts[i].Y, side_len, side_len, false);
        }
    }
}
