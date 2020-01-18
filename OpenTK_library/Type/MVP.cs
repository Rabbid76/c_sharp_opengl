using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK; // Vector2, Vector3, Vector4, Matrix4

namespace OpenTK_library.Type
{
    public unsafe struct TMat44
    {
        public fixed float _matrix[16];

        public TMat44(Matrix4 matrix)
        {
            this.matrix = matrix;
        }

        public Matrix4 matrix
        {
            get
            {
                return new Matrix4(_matrix[0], _matrix[1], _matrix[2], _matrix[3],
                                   _matrix[4], _matrix[5], _matrix[6], _matrix[7],
                                   _matrix[8], _matrix[9], _matrix[10], _matrix[11],
                                   _matrix[12], _matrix[13], _matrix[14], _matrix[15]);
            }

            set
            {
                for (int i = 0; i < 16; ++i)
                    this._matrix[i] = value[i / 4, i % 4];
            }
        }
    }

    public unsafe struct TVP
    {
        public fixed float _projection[16];
        public fixed float _view[16];
        
        public TVP(Matrix4 view, Matrix4 projetion)
        {
            this.view = view;
            this.projetion = projetion;
        }

        public Matrix4 view
        {
            get
            {
                return new Matrix4(_view[0], _view[1], _view[2], _view[3],
                                   _view[4], _view[5], _view[6], _view[7],
                                   _view[8], _view[9], _view[10], _view[11],
                                   _view[12], _view[13], _view[14], _view[15]);
            }

            set
            {
                for (int i = 0; i < 16; ++i)
                    this._view[i] = value[i / 4, i % 4];
            }
        }

        public Matrix4 projetion
        {
            get
            {
                return new Matrix4(_projection[0], _projection[1], _projection[2], _projection[3],
                                   _projection[4], _projection[5], _projection[6], _projection[7],
                                   _projection[8], _projection[9], _projection[10], _projection[11],
                                   _projection[12], _projection[13], _projection[14], _projection[15]);
            }

            set
            {
                for (int i = 0; i < 16; ++i)
                    this._projection[i] = value[i / 4, i % 4];
            }
        }
    }

    public unsafe struct TMVP
    {
        public fixed float _projection[16];
        public fixed float _view[16];
        public fixed float _model[16];

        public TMVP(Matrix4 model, Matrix4 view, Matrix4 projetion)
        {
            this.model = model;
            this.view = view;
            this.projetion = projetion;
        }

        public Matrix4 model
        {
            get
            {
                return new Matrix4(_model[0], _model[1], _model[2], _model[3],
                                   _model[4], _model[5], _model[6], _model[7],
                                   _model[8], _model[9], _model[10], _model[11],
                                   _model[12], _model[13], _model[14], _model[15]);
            }

            set
            {
                for (int i = 0; i < 16; ++i)
                    this._model[i] = value[i / 4, i % 4];
            }
        }

        public Matrix4 view
        {
            get
            {
                return new Matrix4(_view[0], _view[1], _view[2], _view[3],
                                   _view[4], _view[5], _view[6], _view[7],
                                   _view[8], _view[9], _view[10], _view[11],
                                   _view[12], _view[13], _view[14], _view[15]);
            }

            set
            {
                for (int i = 0; i < 16; ++i)
                    this._view[i] = value[i / 4, i % 4];
            }
        }

        public Matrix4 projetion
        {
            get
            {
                return new Matrix4(_projection[0], _projection[1], _projection[2], _projection[3],
                                   _projection[4], _projection[5], _projection[6], _projection[7],
                                   _projection[8], _projection[9], _projection[10], _projection[11],
                                   _projection[12], _projection[13], _projection[14], _projection[15]);
            }

            set
            {
                for (int i = 0; i < 16; ++i)
                    this._projection[i] = value[i / 4, i % 4];
            }
        }
    }
}
