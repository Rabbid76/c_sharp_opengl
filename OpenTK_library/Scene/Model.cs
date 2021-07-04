using System;
using System.Collections.Generic;

using OpenTK.Mathematics; // Vector2, Vector3, Vector4, Matrix4

using OpenTK_library.Mathematics;
using OpenTK_library.OpenGL;
using OpenTK_library.Type;

namespace OpenTK_library.Scene
{
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
        protected IVertexArrayObject _vao;

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
            if (_textureattrib == null)
                _textureattrib = new List<(uint, uint)>();
            _textureattrib.Add(attribute);
            return this;
        }

        public Mesh AddColorAttrib((uint, uint) attribute)
        {
            if (_colorattrib == null)
                _colorattrib = new List<(uint, uint)>();
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

        public IVertexArrayObject VertexArray
        {
            get => _vao;
            set => _vao = value;
        }
    }

    public class ModelNode
        : OpenTK_library.OpenGL.Object
    {
        private readonly IOpenGLObjectFactory _openGLFactory;
        protected Matrix4 _model;
        protected List<Mesh> _meshs = new List<Mesh>();
        protected List<ModelNode> _children = new List<ModelNode>();
        protected IStorageBuffer _model_ssbo;
        protected bool _model_ssbo_needs_update = true;

        protected override void DisposeObjects()
        {
            if (_model_ssbo != null)
                _model_ssbo.Dispose();
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

        public IStorageBuffer ModelSSBO
        {
            get
            {
                if (_model_ssbo == null)
                {
                    _model_ssbo = _openGLFactory.NewStorageBuffer();
                    TMat44 model = new TMat44(_model);
                    _model_ssbo.Create(ref model);
                }
                return _model_ssbo;
            }
        }

        public bool ModelSSBONeedsupdate
        {
            get => _model_ssbo_needs_update;
            set => _model_ssbo_needs_update = value;
        }

        public ModelNode(IOpenGLObjectFactory openGLFactory)
        {
            _openGLFactory = openGLFactory;
        }

        public void UpdateModel(Matrix4 mode_matrix)
        {
            TMat44 model = new TMat44(mode_matrix);
            if (_model_ssbo == null)
            {
                _model_ssbo = _openGLFactory.NewStorageBuffer();
                _model_ssbo.Create(ref model);
            }
            else
            {
                _model_ssbo.Update(ref model);
            }
            ModelSSBONeedsupdate = false;
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
    }
}