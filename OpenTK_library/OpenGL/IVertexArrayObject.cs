namespace OpenTK_library.OpenGL
{
    public struct TVertexFormat
    {
        public int buffer_id;
        public int attribute_index;
        public int tuple_size;
        public int elems_offset;
        public bool normalize;

        public TVertexFormat(int buffer_id, int attribute_index, int tuple_size, int elems_offset, bool normalize)
        {
            this.buffer_id = buffer_id;
            this.attribute_index = attribute_index;
            this.tuple_size = tuple_size;
            this.elems_offset = elems_offset;
            this.normalize = normalize;
        }
    }

    public interface IVertexArrayObject : IObject
    {
        void Bind();

        void AppendVertexBuffer<T_ATTRIBUTE>(int id, int elems_stride, T_ATTRIBUTE[] attributes) where T_ATTRIBUTE : struct;

        void Create<T_INDEX>(TVertexFormat[] formats, T_INDEX[] indices) where T_INDEX : struct;

        void Draw(int no_of_vertices = 0);
        void DrawInstanced(int no_of_instances, int no_of_vertices = 0);
    }
}
