using OpenTK.Mathematics; // Vector2, Vector3, Vector4, Matrix4
using OpenTK_library.Mathematics;
using OpenTK_library.Mesh;
using OpenTK_library.Scene;
using OpenTK_library.OpenGL;
using OpenTK_library.OpenGL.OpenGL4;

namespace OpenTK_library.MeshBuilder
{
    // TODO $$$ add mesh interface and generalized mesh model builder

    public class TrefoilKnotModel
        : OpenTK_library.Scene.Model
    {
        public static TrefoilKnotBuilder Create(IOpenGLObjectFactory objectFactory, int slices = 256, int stacks = 32, float ra = 0.6f, float rb = 0.2f, float rc = 0.4f, float rd = 0.175f, float[] c = null)
        {
            return new TrefoilKnotBuilder(objectFactory, slices, stacks, ra, rb, rc, rd, c);
        }

        public class TrefoilKnotBuilder
        {
            private TrefoilKnotModel _model = new TrefoilKnotModel();
            private IOpenGLObjectFactory openGLFactory = new OpenGLObjectFactory4(); // TODO

            public static implicit operator OpenTK_library.Scene.Model(TrefoilKnotBuilder builder)
            {
                return builder._model;
            }


            public TrefoilKnotBuilder(IOpenGLObjectFactory objectFactory, int slices, int stacks, float ra, float rb, float rc, float rd, float[] c)
            {
                (float[] attributes, uint[] indices) = new TrefoilKnot(slices, stacks, ra, rb, rc, rd, c).Create();
                int tuple_size = 12;

                ModelNode node = new ModelNode(objectFactory);
                _model._root_node = node;
                node.ModelMatrix = Matrix4.Identity;
                AABB mesh_box = new AABB();

                // extend bounding box by vertices
                for (int i = 0; i < attributes.Length; i += tuple_size)
                {
                    Vector3 vertex = new Vector3(attributes[i], attributes[i + 1], attributes[i + 2]);
                    mesh_box = mesh_box | vertex;
                    _model._scene_box = _model._scene_box | vertex;
                }

                // create Mesh
                OpenTK_library.Scene.Mesh mesh = new OpenTK_library.Scene.Mesh();
                mesh.TupleSize = (uint)tuple_size;
                mesh.Box = mesh_box;
                node.Add(mesh);

                mesh.VertexAttribute = (0, 3);
                mesh.NormalAttribute = (3, 3);
                mesh.AddTextureAttrib((6, 2));
                mesh.AddColorAttrib((8, 4));

                TVertexFormat[] format = {
                    new TVertexFormat(0, vertex_index, 3, 0, false),
                    new TVertexFormat(0, normal_index, 3, 3, false),
                    new TVertexFormat(0, texture0_index, 2, 6, false),
                    new TVertexFormat(0, 2, 4, 8, false),
                };
                
                // setup vertex arrays and index array
                var vao = openGLFactory.NewVertexArrayObject();
                vao.AppendVertexBuffer(0, (int)tuple_size, attributes);
                vao.Create(format, indices);

                mesh.FaceSize = 3;
                mesh.VertexArray = vao;
            }
        }
    }
}
