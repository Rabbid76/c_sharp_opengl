using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace OpenTK_library.OpenGL.OpenGL4
{
    internal class VertexArrayObject4SeparateFormat : Object, IVertexArrayObject
    {
        // TODO
        // - T_DATA has to be `float` or `double`
        // - T_INDEX has to be `ubyte`, `ushort` or `uint`

        SortedDictionary<int, int> _vbos = new SortedDictionary<int, int>();
        SortedDictionary<int, int> _vbos_stride = new SortedDictionary<int, int>();
        int _ibo = 0;
        int _no_of_indices = 0;
        int _index_size = 0;
        int _vao = 0;
        int _elem_size = 0;

        public int Object { get { return this._vao; } }

        public VertexArrayObject4SeparateFormat()
        { }

        protected override void DisposeObjects()
        {
            List<int> vbos = new List<int>(this._vbos.Values);

            GL.DeleteBuffers(vbos.Count, vbos.ToArray());
            GL.DeleteBuffer(this._ibo);
            GL.DeleteVertexArray(this._vao);
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

        /// Draw Mesh Instanced
        /// [Vertex Rendering - Instancing](https://www.khronos.org/opengl/wiki/Vertex_Rendering#Instancing)
        public void DrawInstanced(int no_of_instances, int no_of_vertices = 0)
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
                GL.DrawElementsInstanced(PrimitiveType.Triangles, this._no_of_indices, t_elem, IntPtr.Zero, no_of_instances);
            }
            else if (no_of_vertices > 0)
            {
                GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, no_of_vertices, no_of_instances);
            }
        }

        //! Create new OpenGL vertex buffer object
        public void AppendVertexBuffer<T_ATTRIBUTE>(int id, int elems_stride, T_ATTRIBUTE[] attributes) where T_ATTRIBUTE : struct
        {
            int data_size = attributes.Length * Marshal.SizeOf(default(T_ATTRIBUTE));

            GL.CreateBuffers(1, out int vbo);
            //BufferStorageFlags storage = BufferStorageFlags.DynamicStorageBit | BufferStorageFlags.MapWriteBit | BufferStorageFlags.MapPersistentBit;
            GL.NamedBufferStorage<T_ATTRIBUTE>(vbo, data_size, attributes, BufferStorageFlags.None);
            this._vbos[id] = vbo;
            this._vbos_stride[id] = elems_stride;
            this._elem_size = Marshal.SizeOf(default(T_ATTRIBUTE));
        }

        //! Create Vertex Array Object
        public void Create<T_INDEX>(TVertexFormat[] formats, T_INDEX[] indices) where T_INDEX : struct
        {
            this._index_size = Marshal.SizeOf(default(T_INDEX));
            this._no_of_indices = indices.Length;

            VertexAttribType v_type = this._elem_size == 8 ? VertexAttribType.Double : VertexAttribType.Float;

            GL.CreateVertexArrays(1, out this._vao);

            foreach (var vbo in this._vbos)
            {
                GL.VertexArrayVertexBuffer(this._vao, vbo.Key, vbo.Value, IntPtr.Zero, this._vbos_stride[vbo.Key] * this._elem_size);
            }

            foreach (var f in formats)
            {
                GL.VertexArrayAttribFormat(this._vao, f.attribute_index, f.tuple_size, v_type, f.normalize, f.elems_offset * this._elem_size);
                GL.VertexArrayAttribBinding(this._vao, f.attribute_index, f.buffer_id);
                GL.EnableVertexArrayAttrib(this._vao, f.attribute_index);
            }

            if (this._no_of_indices > 0)
            {
                GL.CreateBuffers(1, out this._ibo);
                GL.NamedBufferStorage<T_INDEX>(this._ibo, this._no_of_indices * this._index_size, indices, BufferStorageFlags.None);

                GL.VertexArrayElementBuffer(this._vao, this._ibo);
            }

            GL.BindVertexArray(0);
        }
    }
}
