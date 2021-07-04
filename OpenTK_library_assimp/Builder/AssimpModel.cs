using System;
using System.IO;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Assimp;
using Assimp.Configs;
using OpenTK_library.Mathematics;
using OpenTK_library.Scene;
using OpenTK_library.OpenGL;
using OpenTK_library.OpenGL.OpenGL4;

namespace OpenTK_library_assimp.Builder
{
    /// <summary>
    /// [Open Asset Import Library](https://github.com/assimp)
    /// [assimp-net](https://github.com/assimp/assimp-net)
    /// [assimp-net/AssimpNet.Sample](https://github.com/assimp/assimp-net/blob/master/AssimpNet.Sample/SimpleOpenGLSample.cs)
    /// </summary>

    // TODO $$$ : Composite Builder
    // TODO $$$ : add model material buffer lists

    public class AssimpModel
        : OpenTK_library.Scene.Model
    {
        public static AssimpModelBuilder Create(IOpenGLObjectFactory openGLFactory, string filename)
        {
            return new AssimpModelBuilder(openGLFactory, filename);
        }

        public static AssimpModelBuilder Create(IOpenGLObjectFactory openGLFactory, Stream filestream)
        {
            return new AssimpModelBuilder(openGLFactory, filestream);
        }

        public class AssimpModelBuilder
        {
            private AssimpModel _model = new AssimpModel();
            private IOpenGLObjectFactory _openGLFactory;

            public static implicit operator OpenTK_library.Scene.Model(AssimpModelBuilder builder)
            {
                return builder._model;
            }

            private Scene _assimpmodel;

            PostProcessSteps flags = PostProcessPreset.TargetRealTimeMaximumQuality | PostProcessSteps.GenerateUVCoords;

            public AssimpModelBuilder(IOpenGLObjectFactory openGLFactory, string filename)
            {
                _openGLFactory = openGLFactory;

                AssimpContext importer = new AssimpContext();
                importer.SetConfig(new NormalSmoothingAngleConfig(66.0f));
                _assimpmodel = importer.ImportFile(filename, flags);

                Matrix4x4 identity = Matrix4x4.Identity;
                CreateBuffers(_assimpmodel.RootNode, null, ref identity);
            }

            public AssimpModelBuilder(IOpenGLObjectFactory openGLFactory, Stream filestream)
            {
                _openGLFactory = openGLFactory;

                AssimpContext importer = new AssimpContext();
                importer.SetConfig(new NormalSmoothingAngleConfig(66.0f));
                _assimpmodel = importer.ImportFileFromStream(filestream, flags);

                Matrix4x4 identity = Matrix4x4.Identity;
                CreateBuffers(_assimpmodel.RootNode, null, ref identity);
            }

            private void CreateBuffers(Node assimpnode, ModelNode parent_node, ref Matrix4x4 model_matrix)
            {
                // create new mesh
                ModelNode node = new ModelNode(_openGLFactory);
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

                        OpenTK_library.Scene.Mesh mesh = new OpenTK_library.Scene.Mesh();
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
                            mesh.AddColorAttrib((tuple_index, 4));
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
                        var vao = _openGLFactory.NewVertexArrayObject();
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
