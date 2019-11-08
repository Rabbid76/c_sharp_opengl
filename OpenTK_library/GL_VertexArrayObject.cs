using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4; // GL, ShaderType

// TDOD
// - separate Vertex Buffer Object
// - multiple configurations (multiple manged objects?)

namespace OpenTK_library
{
    public struct GL_TVertexFormat
    {
        public int buffer_id;
        public int attribute_index;
        public int tuple_size;
        public int elems_offset;
        public bool normalize;

        public GL_TVertexFormat(int buffer_id, int attribute_index, int tuple_size, int elems_offset, bool normalize)
        {
            this.buffer_id = buffer_id;
            this.attribute_index = attribute_index;
            this.tuple_size = tuple_size;
            this.elems_offset = elems_offset;
            this.normalize = normalize;
        }
    }

    public class GL_VertexArrayObject<T_ATTRIBUTE, T_INDEX> 
        : IDisposable
        where T_ATTRIBUTE : struct where T_INDEX : struct
    {
        // TODO
        // - T_DATA has to be `float` or `double`
        // - T_INDEX has to be `ubyte`, `ushort` or `uint`

        private bool _disposed = false;
        private bool _vertex_specification_4 = true;

        SortedDictionary<int, int> _vbos = new SortedDictionary<int, int>();
        SortedDictionary<int, int> _vbos_stride = new SortedDictionary<int, int>();
        int _ibo = 0;
        int _no_of_indices = 0;
        int _index_size = 0;
        int _vao = 0;

        public GL_VertexArrayObject()
        { }

        ~GL_VertexArrayObject()
        {
            List<int> vbos = new List<int>(this._vbos.Values);

            GL.DeleteBuffers(vbos.Count, vbos.ToArray());
            GL.DeleteBuffer(this._ibo);
            GL.DeleteVertexArray(this._vao);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                List<int> vbos = new List<int>(this._vbos.Values);
                
                GL.DeleteBuffers(vbos.Count, vbos.ToArray());
                GL.DeleteBuffer(this._ibo);
                GL.DeleteVertexArray(this._vao);

                this._vbos.Clear();
                this._ibo = 0;

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        //! Bind Vertex Array Object
        public void Bind()
        {
            GL.BindVertexArray(this._vao);
        }

        // Draw Mesh
        public void Draw(int no_of_vertices = 0)
        {
            // TODO $$$ primitive type

            GL.BindVertexArray(this._vao);

            if (this._no_of_indices > 0)
            {
                DrawElementsType t_elem = DrawElementsType.UnsignedInt;
                if (this._index_size == 2)
                    t_elem = DrawElementsType.UnsignedShort;
                else if (this._index_size == 1)
                    t_elem = DrawElementsType.UnsignedByte;
                GL.DrawElements(BeginMode.Triangles, this._no_of_indices, t_elem, 0);
            }
            else if (no_of_vertices > 0)
            {
                GL.DrawArrays(PrimitiveType.Triangles, 0, no_of_vertices);
            }
        }

        //! Create new OpenGL vertex buffer object
        public void AppendVertexBuffer(int id, int elems_stride, T_ATTRIBUTE[] attributes)
        {
            int data_size = attributes.Length * Marshal.SizeOf(default(T_ATTRIBUTE));
            
            int vbo = 0;
            if (this._vertex_specification_4)
            {
                GL.CreateBuffers(1, out vbo);
                //BufferStorageFlags storage = BufferStorageFlags.DynamicStorageBit | BufferStorageFlags.MapWriteBit | BufferStorageFlags.MapPersistentBit;
                GL.NamedBufferStorage<T_ATTRIBUTE>(vbo, data_size, attributes, BufferStorageFlags.None);
            }
            else
            {
                vbo = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
                GL.BufferData<T_ATTRIBUTE>(BufferTarget.ArrayBuffer, data_size, attributes, BufferUsageHint.StaticDraw);
            }
            this._vbos[id] = vbo;
            this._vbos_stride[id] = elems_stride;
        }

        //! Create Vertex Array Object
        public void Create(GL_TVertexFormat[] formats, T_INDEX[] indices)
        {
            this._index_size = Marshal.SizeOf(default(T_INDEX));
            this._no_of_indices = indices.Length;
            int elem_size = Marshal.SizeOf(default(T_ATTRIBUTE));

            if (this._vertex_specification_4)
            {
                VertexAttribType v_type = elem_size == 8 ? VertexAttribType.Double : VertexAttribType.Float;

                GL.CreateVertexArrays(1, out this._vao);

                foreach (var vbo in this._vbos)
                {
                    GL.VertexArrayVertexBuffer(this._vao, vbo.Key, vbo.Value, IntPtr.Zero, this._vbos_stride[vbo.Key] * elem_size);
                }

                foreach (var f in formats)
                {
                    GL.VertexArrayAttribFormat(this._vao, f.attribute_index, f.tuple_size, v_type, f.normalize, f.elems_offset * elem_size);
                    GL.VertexArrayAttribBinding(this._vao, f.attribute_index, f.buffer_id);
                    GL.EnableVertexArrayAttrib(this._vao, f.attribute_index);
                }

                if (this._no_of_indices > 0)
                {
                    GL.CreateBuffers(1, out this._ibo);
                    GL.NamedBufferStorage<T_INDEX>(this._ibo, this._no_of_indices * this._index_size, indices, BufferStorageFlags.None);

                    GL.BindVertexArray(this._vao);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, this._ibo);
                }
            }
            else
            {
                VertexAttribPointerType v_type = elem_size == 8 ? VertexAttribPointerType.Double : VertexAttribPointerType.Float;

                this._vao = GL.GenVertexArray();
                GL.BindVertexArray(this._vao);

                foreach(var f in formats)
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, this._vbos[f.buffer_id]);
                    GL.VertexAttribPointer(f.attribute_index, f.tuple_size, v_type, f.normalize, this._vbos_stride[f.buffer_id] * elem_size, f.elems_offset * elem_size);
                    GL.EnableVertexAttribArray(f.attribute_index);
                }
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

                if (this._no_of_indices > 0)
                {
                    this._ibo = GL.GenBuffer();
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, this._ibo);
                    GL.BufferData<T_INDEX>(BufferTarget.ElementArrayBuffer, this._no_of_indices * this._index_size, indices, BufferUsageHint.StaticDraw);
                }
            }

            GL.BindVertexArray(0);
        }
    }
}
