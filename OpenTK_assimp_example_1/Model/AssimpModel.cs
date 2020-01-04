using System;
using System.IO;
using System.Collections.Generic;

using OpenTK; // Vector2, Vector3, Vector4, Matrix4

using Assimp;
using Assimp.Configs;

using OpenTK_library.OpenGL;
using OpenTK_library.Mathematics;

namespace OpenTK_assimp_example_1.Model
{
    /// <summary>
    /// [Open Asset Import Library](https://github.com/assimp)
    /// [assimp-net](https://github.com/assimp/assimp-net)
    /// [assimp-net/AssimpNet.Sample](https://github.com/assimp/assimp-net/blob/master/AssimpNet.Sample/SimpleOpenGLSample.cs)
    /// </summary>

    // TODO $$$ : Composite Builder
    // TODO $$$ : add model material buffer lists

    public class Mesh
        : OpenTK_library.OpenGL.Object
    {
        protected AABB _box;

        protected (uint, uint) _vertexattrib = (0, 0);
        protected (uint, uint) _normalattrib = (0, 0);
        protected (uint, uint) _binormalattrib = (0, 0);
        protected (uint, uint) _tangentattrib = (0, 0);
        protected List<(uint, uint)> _textureattrib;
        protected List<(uint, uint)> _colorattrib;
        protected uint _tuple_size = 0;
        protected uint _face_size = 0;
        protected VertexArrayObject<float, uint> _vao;

        protected override void DisposeObjects()
        {
            _vao.Dispose();
        }

        public AABB Box
        {
            get => _box;
            set => _box = value;
        }

        public (uint, uint) VertexAttribute
        {
            get => _vertexattrib;
            set => _vertexattrib = value;
        }

        public (uint, uint) NormalAttribute
        {
            get => _normalattrib;
            set => _normalattrib = value;
        }

        public (uint, uint) BinormalAttribute
        {
            get => _binormalattrib;
            set => _binormalattrib = value;
        }

        public (uint, uint) TangentAttribute
        {
            get => _tangentattrib;
            set => _tangentattrib = value;
        }

        public List<(uint, uint)> TextureAttribute { get => _textureattrib; }

        public List<(uint, uint)> ColorAttribute { get => _colorattrib; }

        public Mesh AddTextureAttrib((uint, uint) attribute)
        {
            _textureattrib.Add(attribute);
            return this;
        }

        public Mesh AddColorAttrib((uint, uint) attribute)
        {
            _colorattrib.Add(attribute);
            return this;
        }

        public uint TupleSize
        {
            get => _tuple_size;
            set => _tuple_size = value;
        }

        public uint FaceSize
        {
            get => _face_size;
            set => _face_size = value;
        }

        public VertexArrayObject<float, uint> VertexArray
        {
            get => _vao;
            set => _vao = value;
        }
    }

    public class ModelNode
        : OpenTK_library.OpenGL.Object
    {
        protected Matrix4 _model;
        protected List<Mesh> _meshs = new List<Mesh>();
        protected List<ModelNode> _children = new List<ModelNode>();

        protected override void DisposeObjects()
        {
            foreach (var mesh in _meshs)
                mesh.Dispose();
            _meshs.Clear();
            foreach (var child in _children)
                child.Dispose();
            _children.Clear();
        }

        public Matrix4 ModelMatrix
        {
            get => _model;
            set => _model = value;
        }

        public List<Mesh> Meshs { get => _meshs; }
        public List<ModelNode> Children { get => _children; }

        public ModelNode Add(Mesh mesh)
        {
            _meshs.Add(mesh);
            return this;
        }

        public ModelNode AddChild(ModelNode child)
        {
            _children.Add(child);
            return this;
        }
    }

    public class Model
        : OpenTK_library.OpenGL.Object
    {
        public static readonly int vertex_index = 0;
        public static readonly int normal_index = 1;
        public static readonly int texture0_index = 2;
        public static readonly int color0_index = 3;
        public static readonly int binormal_index = 4;
        public static readonly int tangent_index = 5;
        public static readonly int textureN_index = 10;
        public static readonly int colorN_index = 20;

        protected AABB _scene_box = new AABB();
        protected ModelNode _root_node;

        protected override void DisposeObjects()
        {
            _root_node.Dispose();
        }

        public AABB SceneBox { get => _scene_box; }

        public ModelNode Root { get => _root_node; }

        public static AssimpModelBuilder Create(Stream filestream)
        {
            return new AssimpModelBuilder(filestream);
        }

        public class AssimpModelBuilder
        {
            private Model _model = new Model();

            public static implicit operator Model(AssimpModelBuilder builder)
            {
                return builder._model;
            }

            private Scene _assimpmodel;
            
            public AssimpModelBuilder(Stream filestream)
            {
                AssimpContext importer = new AssimpContext();
                importer.SetConfig(new NormalSmoothingAngleConfig(66.0f));
                _assimpmodel = importer.ImportFileFromStream(filestream, PostProcessSteps.GenerateNormals);

                Matrix4x4 identity = Matrix4x4.Identity;
                CreateBuffers(_assimpmodel.RootNode, null, ref identity);
            }

            private void CreateBuffers(Node assimpnode, ModelNode parent_node, ref Matrix4x4 model_matrix)
            {
                // create new mesh
                ModelNode node = new ModelNode();
                if (parent_node != null)
                    parent_node.AddChild(node);
                else
                    _model._root_node = node;

                // set model matrix
                Matrix4x4 mm = assimpnode.Transform;
                node.ModelMatrix = new Matrix4(
                    mm.A1, mm.A2, mm.A3, mm.A4,
                    mm.B1, mm.B2, mm.B3, mm.B4,
                    mm.C1, mm.C2, mm.C3, mm.C4,
                    mm.D1, mm.D2, mm.D3, mm.D4);

                // combined model matrix
                Matrix4x4 prev_model = model_matrix;
                Matrix4x4 new_transform = assimpnode.Transform;
                model_matrix = new_transform * model_matrix; // ? has this to be reversed link in OpneTK?

                if (assimpnode.HasMeshes)
                {
                    foreach (int index in assimpnode.MeshIndices)
                    {
                        Assimp.Mesh assimpmesh = _assimpmodel.Meshes[index];
                        AABB mesh_box = new AABB();

                        // extend bounding box by vertices
                        for (int i = 0; i < assimpmesh.VertexCount; i++)
                        {
                            Vector3D tmp = assimpmesh.Vertices[i];
                            mesh_box = mesh_box | new Vector3(tmp.X, tmp.Y, tmp.Z);

                            tmp = model_matrix * tmp;
                            _model._scene_box = _model._scene_box | new Vector3(tmp.X, tmp.Y, tmp.Z);
                        }

                        // create Mesh
                        uint tuple_index = 0;
                        List<TVertexFormat> formalist = new List<TVertexFormat>();

                        Mesh mesh = new Mesh();
                        mesh.Box = mesh_box;
                        node.Add(mesh);

                        // specify vertices
                        mesh.VertexAttribute = (tuple_index, 3);
                        formalist.Add(new TVertexFormat(0, vertex_index, 3, (int)tuple_index, false));
                        tuple_index += 3;

                        // specify normals
                        if (assimpmesh.HasNormals)
                        {
                            mesh.NormalAttribute = (tuple_index, 3);
                            formalist.Add(new TVertexFormat(0, normal_index, 3, (int)tuple_index, false));
                            tuple_index += 3;
                        }

                        // specify bi-normals and tangents
                        if (assimpmesh.HasTangentBasis)
                        {
                            mesh.BinormalAttribute = (tuple_index, 3);
                            formalist.Add(new TVertexFormat(0, binormal_index, 3, (int)tuple_index, false));
                            tuple_index += 3;

                            mesh.TangentAttribute = (tuple_index, 3);
                            formalist.Add(new TVertexFormat(0, tangent_index, 3, (int)tuple_index, false));
                            tuple_index += 3;
                        }

                        // specify texture channels
                        for (int textur_channel = 0; assimpmesh.HasTextureCoords(textur_channel); ++textur_channel)
                        {
                            mesh.AddTextureAttrib((tuple_index, 3));
                            int attr_i = textur_channel == 0 ? texture0_index : (textureN_index + textur_channel - 1);
                            formalist.Add(new TVertexFormat(0, attr_i, 3, (int)tuple_index, false));
                            tuple_index += 3;
                        }

                        // specify color channels
                        for (int color_channel = 0; assimpmesh.HasVertexColors(color_channel); ++color_channel)
                        {
                            mesh.AddTextureAttrib((tuple_index, 3));
                            int attr_i = color_channel == 0 ? color0_index : (colorN_index + color_channel - 1);
                            formalist.Add(new TVertexFormat(0, attr_i, 4, (int)tuple_index, false));
                            tuple_index += 4;
                        }

                        // TODO $$$ bones
                        if (assimpmesh.HasBones)
                        {
                            // [...]
                            Console.WriteLine("bones not yet implemented");
                        }

                        // set tuple size
                        mesh.TupleSize = tuple_index;

                        // setup index buffer
                        List<float> attributes = new List<float>();
                        List<uint> indices = new List<uint>();
                        uint elem_index = 0;
                        foreach (Face face in assimpmesh.Faces)
                        {
                            if (face.IndexCount < 3)
                                continue; // lines?

                            for (uint i = 2; i < (uint)face.IndexCount; i++)
                            {
                                indices.Add(elem_index);
                                indices.Add(elem_index + 1);
                                indices.Add(elem_index + i);
                            }
                            elem_index += (uint)face.IndexCount;

                            for (int i = 0; i < face.IndexCount; i++)
                            {
                                int ei = face.Indices[i];

                                // add vertex attribute
                                var vertex = assimpmesh.Vertices[ei];
                                attributes.Add(vertex.X);
                                attributes.Add(vertex.Y);
                                attributes.Add(vertex.Z);

                                // add normals
                                if (assimpmesh.HasNormals)
                                {
                                    var normal = assimpmesh.Normals[ei];
                                    attributes.Add(normal.X);
                                    attributes.Add(normal.Y);
                                    attributes.Add(normal.Z);
                                }

                                // add bi-normals and tangents 
                                if (assimpmesh.HasTangentBasis)
                                {
                                    var binormal = assimpmesh.BiTangents[ei];
                                    attributes.Add(binormal.X);
                                    attributes.Add(binormal.Y);
                                    attributes.Add(binormal.Z);

                                    var tangent = assimpmesh.Tangents[ei];
                                    attributes.Add(tangent.X);
                                    attributes.Add(tangent.Y);
                                    attributes.Add(tangent.Z);
                                }

                                // add texture coordinates
                                for (int textur_channel = 0; assimpmesh.HasTextureCoords(textur_channel); ++textur_channel)
                                {
                                    var uvw = assimpmesh.TextureCoordinateChannels[textur_channel][ei];
                                    attributes.Add(uvw.X);
                                    attributes.Add(uvw.Y);
                                    attributes.Add(uvw.Z);
                                }

                                // add color attributes
                                for (int color_channel = 0; assimpmesh.HasVertexColors(color_channel); ++color_channel)
                                {
                                    var vertColor = assimpmesh.VertexColorChannels[color_channel][ei];
                                    attributes.Add(vertColor.R);
                                    attributes.Add(vertColor.G);
                                    attributes.Add(vertColor.B);
                                    attributes.Add(vertColor.A);
                                }
                            }
                        }

                        // setup vertex arrays and index array
                        TVertexFormat[] format = formalist.ToArray();
                        var vao = new VertexArrayObject<float, uint>();
                        vao.AppendVertexBuffer(0, (int)tuple_index, attributes.ToArray());
                        vao.Create(format, indices.ToArray());

                        mesh.FaceSize = 3;
                        mesh.VertexArray = vao;
                    }
                }

                for (int i = 0; i < assimpnode.ChildCount; i++)
                {
                    CreateBuffers(assimpnode.Children[i], node, ref model_matrix);
                }
                model_matrix = prev_model;
            }
        }
    }
}
