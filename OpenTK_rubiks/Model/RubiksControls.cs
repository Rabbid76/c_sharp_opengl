using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using OpenTK_library.Controls;
using static OpenTK_library.Mathematics.Operations;

namespace OpenTK_rubiks.Model
{
    /// <summary>
    ///  general Rubik's data, types and conversions
    /// </summary>
    public static class RubiksGlobal
    {
        public enum TAxis { x = 0, y = 1, z = 2 };
        public enum TRow { low = 0, mid = 1, high = 2 };
        public enum TDirection { left=0, right=1 };

        public static int NoOfCubes {  get { return 27; } }

        static Vector3[] _axis_vectors = new Vector3[] { new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1) };
        public static Vector3 AxisVector(int index)
        {
            return _axis_vectors[index];
        }

        public static TAxis Axis(Vector3 rot_axis)
        {
            return Math.Abs(rot_axis.X) > 0.5f ? TAxis.x : Math.Abs(rot_axis.Y) > 0.5f ? TAxis.y : TAxis.z;
        }
       
        public static TDirection Direction(Vector3 rot_axis)
        {
            return rot_axis.X + rot_axis.Y + rot_axis.Z > 0.0f  ? TDirection.left : TDirection.right;
        }

        public static TRow Row(TAxis axis, int sub_cube_i)
        {
            switch (axis)
            {
                case TAxis.x: return (TRow)(sub_cube_i % 3);
                case TAxis.y: return (TRow)((sub_cube_i % 9) / 3);
                case TAxis.z: return (TRow)(sub_cube_i / 9);
            }
            return TRow.mid;
        }
    }

    /// <summary>
    /// Rubik's SSBO (Shader Storage Block Object) data
    /// </summary>
    public unsafe struct T_RUBIKS_DATA
    {
        public fixed float _model[16 * 27];
        public fixed Int32 _cube_hit[1];
        public fixed Int32 _side_hit[1];

        public T_RUBIKS_DATA SetModel(int cube_i, Matrix4 m)
        {
            for (int i = 0; i < 16; ++i)
                this._model[cube_i * 16 + i] = m[i / 4, i % 4];
            return this;
        }

        public Matrix4 GetModel(int cube_i)
        {
            int base_i = cube_i * 16;
            return new Matrix4(_model[base_i + 0], _model[base_i + 1], _model[base_i + 2], _model[base_i + 3],
                               _model[base_i + 4], _model[base_i + 5], _model[base_i + 6], _model[base_i + 7],
                               _model[base_i + 8], _model[base_i + 9], _model[base_i + 10], _model[base_i + 11],
                               _model[base_i + 12], _model[base_i + 13], _model[base_i + 14], _model[base_i + 15]);
        }

        public int cube_hit
        {
            get { return _cube_hit[0]; }
            set { _cube_hit[0] = value; }
        }

        public int side_hit
        {
            get { return _side_hit[0]; }
            set { _side_hit[0] = value; }
        }
    }

    /// <summary>
    /// Rubik's manipulation information
    /// </summary>
    public class ChangeOperation
    {
        /// <summary>rotation axis</summary>
        private RubiksGlobal.TAxis _axis = RubiksGlobal.TAxis.x;
        /// <summary>direction of rotation</summary>
        private RubiksGlobal.TDirection _direction = RubiksGlobal.TDirection.left;
        /// <summary>rotation row along the rotation axis</summary>
        private RubiksGlobal.TRow _row = RubiksGlobal.TRow.low;

        public RubiksGlobal.TAxis axis { get { return _axis; } }
        public RubiksGlobal.TDirection direction { get { return _direction; } }
        public RubiksGlobal.TRow row { get { return _row; } }

        public ChangeOperation(RubiksGlobal.TAxis axis, RubiksGlobal.TDirection direction, RubiksGlobal.TRow row)
        {
            _axis = axis;
            _direction = direction;
            _row = row;
        }

        public ChangeOperation(Vector3 rot_axis, int sub_cube_i)
        {
            _axis = RubiksGlobal.Axis(rot_axis);
            _direction = RubiksGlobal.Direction(rot_axis);
            _row = RubiksGlobal.Row(_axis, sub_cube_i);
        }
    }

    /// <summary>
    /// Rubik's manipulation and animation
    /// Representation of the positions and arrangement of the components (sub cubes), of the Rubik's cube.
    /// </summary>
    public class RubiksControls
    {
        /// <summary>time getter delegate (time in seconds)</summary>
        GetTime _get_time;
        /// <summary>final Rubik's cube data for rendering</summary>
        T_RUBIKS_DATA _data = new T_RUBIKS_DATA(); 
        /// <summary>distance between 2 sub cubes (unscaled)</summary>
        float _offset = 0.0f;
        /// <summary>scale of the sub cube</summary>
        float _scale = 1.0f;
        /// <summary>map the logical geometric position in the Rubik' cube to a corresponding sub cube</summary>
        int[] _cube_map = new int[RubiksGlobal.NoOfCubes];
        /// <summary>translation and scale of the sub cubes</summary>
        Matrix4[] _trans_scale = new Matrix4[RubiksGlobal.NoOfCubes];
        /// <summary>current rotation of the sub cubes</summary>
        Matrix4[] _current_pos = new Matrix4[RubiksGlobal.NoOfCubes];
        /// <summary>additional animation transformation</summary>
        Matrix4[] _animation = new Matrix4[RubiksGlobal.NoOfCubes];
        /// <summary>current time</summary>
        double _animation_time_s = 0.0; // time span for an animation
        /// <summary>queue of pending change operations</summary>
        List<ChangeOperation> _pending_queue = new List<ChangeOperation>();
        /// <summary> true: animation is active</summary>
        bool _animation_is_active = false;
        /// <summary>current time</summary>
        double _current_time = 0.0;
        /// <summary>start time of animation</summary>
        double _animation_start_time = 0.0;

        public float Offset { get { return _offset; } }
        public float Scale { get { return _scale; } }
        public bool AnimationActive { get { return _animation_is_active; } }
        public bool AnimationPending { get { return _animation_is_active || _pending_queue.Count() > 0; } }

        public double AnimationTime
        {
            get { return _animation_time_s; }
            set { _animation_time_s = value; }
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="get_time">time in seconds</param>
        /// <param name="offset">time in seconds</param>
        /// <param name="scale">cube scale</param>
                public RubiksControls(GetTime get_time, float offset, float scale)
        {
            _get_time = get_time;
            Init(offset, scale);
        }

        /// <summary>
        ///  Rubik's cube Shader Storage Block Buffer data for rendering 
        /// </summary>
        public ref T_RUBIKS_DATA Data { get { return ref _data; } }

        /// <summary>
        /// get the render time in seconds
        /// </summary>
        private double time { get { return this._get_time(); } }

        /// <summary>
        ///  Initializes the size attributes and matrices.  
        /// </summary>
        /// <param name="offset">unscaled distance between 2 sub cubes</param>
        /// <param name="scale">scale of a single sub cube</param>
        void InitGeometry(float offset, float scale)
        {
            _offset = offset;
            _scale = scale;

            // calculate initial positions of sub cubes
            for (int z = 0; z < 3; ++z)
            {
                for (int y = 0; y < 3; ++y)
                {
                    for (int x = 0; x < 3; ++x)
                    {
                        int i = z * 9 + y * 3 + x;
                        _cube_map[i] = i;
                        Matrix4 part_scale = Matrix4.CreateScale(_scale);
                        Matrix4 part_trans = Matrix4.CreateTranslation((float)(x - 1) * _offset, (float)(y - 1) * _offset, (float)(z - 1) * _offset);
                        _trans_scale[i] = part_trans * part_scale; // OpenTK `*`-operator is reversed
                    }
                }
            }
        }

        /// <summary>
        /// Calculate the final model matrices of the sub cubes. 
        /// </summary>
        void UpdateM44Cubes()
        {
            for (int i = 0; i < RubiksGlobal.NoOfCubes; ++i)
                _data.SetModel(i, _trans_scale[i] * _current_pos[i] * _animation[i]); // OpenTK `*`-operator is reversed
        }

        /// <summary>
        /// Initializes the data structures. 
        /// </summary>
        /// <param name="offset">unscaled distance between 2 sub cubes</param>
        /// <param name="scale">scale of a single sub cube</param>
        public void Init(float offset, float scale)
        {
            // initialize the size
            InitGeometry(offset, scale);

            // initialize animation and rotation matrices
            for (int i = 0; i < RubiksGlobal.NoOfCubes; ++i)
            {
                _current_pos[i] = Matrix4.Identity;
                _animation[i] = Matrix4.Identity;
            }

            // Update the final model matrices of the sub cubes
            UpdateM44Cubes();
        }

        /// <summary>
        /// Starts the rotation of a part of the Rubik's cube.
        /// </summary>
        /// <param name="op">specifies the change operation</param>
        public void Change(ChangeOperation op)
        {
            _pending_queue.Insert(0, op);
        }

        /// <summary>
        /// Shuffle the cube  
        /// </summary>
        /// <param name="steps">number of shuffles</param>
        public void Shuffle(int steps)
        {
            var rand = new Random();

            ChangeOperation[] shuffle_ops = new ChangeOperation[steps];
            for (int i = 0; i < steps; ++i)
            {
                RubiksGlobal.TAxis axis = RubiksGlobal.TAxis.x;
                RubiksGlobal.TRow row = RubiksGlobal.TRow.low;
                RubiksGlobal.TDirection direction = RubiksGlobal.TDirection.left;
                bool valid = false;
                do
                {
                    axis = (RubiksGlobal.TAxis)rand.Next(0, 2);
                    row = (RubiksGlobal.TRow)rand.Next(0, 2);
                    direction = (RubiksGlobal.TDirection)rand.Next(0, 1);

                    if (i == 0)
                        break;

                    // check if not inverse operation
                    valid = shuffle_ops[i - 1].axis != axis ||
                            shuffle_ops[i - 1].row != row ||
                            shuffle_ops[i - 1].direction == direction;

                    // check if not 3 equal operations in a row
                    if (valid && i > 1)
                    {
                        valid = shuffle_ops[i - 1].axis != axis ||
                                shuffle_ops[i - 1].row != row ||
                                shuffle_ops[i - 1].direction != direction ||
                                shuffle_ops[i - 2].axis != axis ||
                                shuffle_ops[i - 2].row != row ||
                                shuffle_ops[i - 2].direction != direction;
                    }
                }
                while (valid == false);

                shuffle_ops[i] = new ChangeOperation(axis, direction, row);
            }

            // add change operations to pending queue
            for (int i = 0; i < steps; ++i)
                Change(shuffle_ops[i]);
        }

        /// <summary>
        /// Get all cubes in specific row of an specific axis.
        /// </summary>
        /// <param name="axis">axis</param>
        /// <param name="row">row</param>
        /// <returns></returns>
        List<int> SubCubeIndices(RubiksGlobal.TAxis axis, RubiksGlobal.TRow row)
        {
            List<int> indices = new List<int>();

            // define the affected sub cubes
            int[] r_x = { 0, 2 };
            int[] r_y = { 0, 2 };
            int[] r_z = { 0, 2 };
            switch (axis)
            {
                case RubiksGlobal.TAxis.x: r_x[0] = r_x[1] = (int)row; break;
                case RubiksGlobal.TAxis.y: r_y[0] = r_y[1] = (int)row; break;
                case RubiksGlobal.TAxis.z: r_z[0] = r_z[1] = (int)row; break;
            }

            // collect indices of collected sub cubes 
            for (int z = r_z[0]; z <= r_z[1]; ++z)
            {
                for (int y = r_y[0]; y <= r_y[1]; ++y)
                {
                    for (int x = r_x[0]; x <= r_x[1]; ++x)
                    {
                        int i = z * 9 + y * 3 + x;
                        indices.Add(_cube_map[i]);
                    }
                }
            }

            return indices;
        }

        Matrix4[] _c_rot_mat =
        {
            new Matrix4( 1,  0,  0, 0,     0, 0, -1, 0,    0,  1, 0, 0,    0, 0, 0, 1 ),
            new Matrix4( 1,  0,  0, 0,     0, 0,  1, 0,    0, -1, 0, 0,    0, 0, 0, 1 ),
            new Matrix4( 0,  0,  1, 0,     0, 1,  0, 0,   -1,  0, 0, 0,    0, 0, 0, 1 ),
            new Matrix4( 0,  0, -1, 0,     0, 1,  0, 0,    1,  0, 0, 0,    0, 0, 0, 1 ),
            new Matrix4( 0, -1,  0, 0,     1, 0,  0, 0,    0,  0, 1, 0,    0, 0, 0, 1 ),
            new Matrix4( 0,  1,  0, 0,    -1, 0,  0, 0,    0,  0, 1, 0,    0, 0, 0, 1 )
         };
        /// <summary>
        /// Calculate the rotation of a part of the Rubik's cube.
        /// Compute the new positions of the sub cubes and calculate the model matrices.
        /// </summary>
        /// <param name="op">specifies the change operation</param>
        void Rotate(ChangeOperation op)
        {
            int axis_i = (int)op.axis;
            int row_i = (int)op.row;
            var cube_i = SubCubeIndices(op.axis, op.row);

            // update the position model matrix of the affected sub cubes 
            Matrix4 rot_mat = _c_rot_mat[axis_i * 2 + (op.direction == RubiksGlobal.TDirection.left ? 0 : 1)];
            foreach (var i in cube_i)
            {
                double angle = Radians(90.0) * (op.direction == RubiksGlobal.TDirection.left ? -1.0 : 1.0);
                rot_mat = CreateRotate((float)angle, RubiksGlobal.AxisVector(axis_i));
                _current_pos[i] = _current_pos[i] * rot_mat;  // OpenTK `*`-operator is reversed
            }

            // Recalculate the index map of the cubes
            int[,] indices = { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 2, 1 }, { 2, 2 }, { 1, 2 }, { 0, 2 }, { 0, 1 } };
            int[] current_map = (int[])_cube_map.Clone();

            for (int i_o = 0; i_o < 8; ++i_o)
            {
                int j_n = (op.direction == RubiksGlobal.TDirection.left ? i_o + 6 : i_o + 2) % 8;

                int[] ao = { 0, 0, 0 };
                int[] an = { 0, 0, 0 };

                ao[axis_i] = row_i;
                an[axis_i] = row_i;
                ao[(axis_i + 1) % 3] = indices[i_o, 0];
                an[(axis_i + 1) % 3] = indices[j_n, 0];
                ao[(axis_i + 2) % 3] = indices[i_o, 1];
                an[(axis_i + 2) % 3] = indices[j_n, 1];

                int ci_o = ao[0] + ao[1] * 3 + ao[2] * 9;
                int ci_n = an[0] + an[1] * 3 + an[2] * 9;

                _cube_map[ci_n] = current_map[ci_o];
            }

            // reset animation matrices
            for (int i = 0; i < RubiksGlobal.NoOfCubes; ++i)
                _animation[i] = Matrix4.Identity;
        }

        /// <summary>
        /// Update animation and pending changes.
        /// </summary>
        public void Update()
        {
            bool pending_changes = _pending_queue.Count() > 0;
            _animation_is_active = _animation_is_active && pending_changes;
            
            _current_time = time;

            if (pending_changes == false)
                return;
            ChangeOperation op = _pending_queue.Last();

            if (_animation_is_active)
            {
                double past_animation_time_s = _current_time - _animation_start_time;
                if (past_animation_time_s < _animation_time_s)
                {
                    // get change information
                    int axis_i = (int)op.axis;
                    int row_i = (int)op.row;
                    var cube_i = SubCubeIndices(op.axis, op.row);

                    // update the position model matrix of the affected sub cubes 
                    foreach (var i in cube_i)
                    {
                        double angle = Radians(90.0) * (op.direction == RubiksGlobal.TDirection.left ? -1.0 : 1.0);
                        angle *= past_animation_time_s / _animation_time_s;
                        _animation[i] = CreateRotate((float)angle, RubiksGlobal.AxisVector(axis_i));
                    }

                    // Update the final model matrices of the sub cubes
                    UpdateM44Cubes();

                    return;
                }
                _animation_is_active = false;
            }
            else if (pending_changes)
            {
                _animation_is_active = true;
                _animation_start_time = time;
                return;
            }

            _pending_queue.RemoveAt(_pending_queue.Count() - 1);
            Rotate(op);

            // Update the final model matrices of the sub cubes
            UpdateM44Cubes();
        }

        /// <summary>
        /// Reset hit information 
        /// </summary>
        public void ResetHit()
        {
            _data.cube_hit = -1;
            _data.side_hit = 0;
        }

        /// <summary>
        /// Intersect a plane on a side of the cube with a ray.  
        /// </summary>
        /// <param name="r0_ray">start point of ray</param>
        /// <param name="d_ray">direction of the ray</param>
        /// <param name="side_i">index of the side of the cube</param>
        /// <param name="dist">distance to the intersection</param>
        /// <param name="pt">intersection point</param>
        public bool IntersectSidePlane(Vector3 r0_ray, Vector3 d_ray, int side_i, out float dist, out Vector3 pt)
        {
            dist = 0.0f;
            pt = Vector3.Zero;

            // define the cube corner points and its faces

            Vector3[] cube_pts =
            {
                new Vector3(-1.0f, -1.0f, -1.0f), // 0 : left  front bottom
                new Vector3(1.0f, -1.0f, -1.0f), // 1 : right front bottom
                new Vector3(-1.0f, 1.0f, -1.0f), // 2 : left  back  bottom
                new Vector3(1.0f, 1.0f, -1.0f), // 3 : right back  bottom
                new Vector3(-1.0f, -1.0f, 1.0f), // 4 : left  front top
                new Vector3(1.0f, -1.0f, 1.0f), // 5 : right front top
                new Vector3(-1.0f, 1.0f, 1.0f), // 6 : left  back  top
                new Vector3(1.0f, 1.0f, 1.0f), // 7 : right back  top
            };

            int[,] cube_faces =
            {
                { 2, 0, 4, 6 }, // 0 : left
                { 1, 3, 7, 5 }, // 1 : right
                { 0, 1, 5, 4 }, // 2 : front
                { 2, 3, 7, 6 }, // 3 : back
                { 0, 1, 3, 2 }, // 4 : bottom
                { 4, 5, 7, 6 }  // 5 : top
            };

            float cube_sidelen_2 = (_offset + 1.0f) * _scale; // half side length of the entire cube

            // calculate the normal vector of the cube side
            Vector3 pa = cube_pts[cube_faces[side_i, 0]];
            Vector3 pb = cube_pts[cube_faces[side_i, 1]];
            Vector3 pc = cube_pts[cube_faces[side_i, 3]];

            // Note, the normalization of `d_ray` and `n_plane` is not necessary,
            // because: `norm(D) * dot(PR, norm(N)) / dot(norm(D), norm(N))` is equal `D * dot(PR, N) / dot(D, N)`

            Vector3 dir = d_ray; // d_ray.Normalized();
            Vector3 n_plane = Vector3.Cross(pb - pa, pc - pa); // Vector3.Normalize(Vector3.Cross(pb - pa, pc - pa));
            Vector3 p0_plane = pa * cube_sidelen_2;

            if (Math.Abs(Math.Abs(Vector3.Dot(dir, n_plane)) - 1.0f) < 0.0017f) // 0.0017 < sin(1°)
                return false;

            // calculate the distance to the intersection with the plane defined by the side of the cube
            dist = Vector3.Dot(pa - r0_ray, n_plane) / Vector3.Dot(dir, n_plane);

            // calculate the intersection point with the plane
            pt = r0_ray + dir * dist;

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r0_ray">start point of the ray</param>
        /// <param name="d_ray">direction vector of the ray</param>
        /// <param name="side_i">index of the intersected side </param>
        /// <param name="pt">intersection point</param>
        /// <returns></returns>
        public bool Intersect(Vector3 r0_ray, Vector3 d_ray, out int side_i, out Vector3 pt)
        {
            side_i = -1;
            pt = Vector3.Zero;

            // find the nearest intersection of a side of the cube and the ray 

            int isect_side = -1;
            float isect_dist = float.MaxValue;
            Vector3 isect_pt = Vector3.Zero;
            for (int i = 0; i < 6; ++i)
            {
                // calculate the intersection of the ray and a side of the cube map
                float dist;
                Vector3 x_pt;
                if (IntersectSidePlane(r0_ray, d_ray, i, out dist, out x_pt) == false)
                    continue;

                // check if the intersection point is closer than the previous one
                if (Math.Abs(dist) > isect_dist)
                    continue;

                // check if the intersection is on the side of the cube
                float cube_sidelen_2 = (_offset + 1.0f) * _scale; // half side length of the entire cube
                bool on_side = Math.Abs(x_pt.X) < cube_sidelen_2 + 0.001 &&
                               Math.Abs(x_pt.Y) < cube_sidelen_2 + 0.001 &&
                               Math.Abs(x_pt.Z) < cube_sidelen_2 + 0.001;
                if (on_side == false)
                    continue;

                isect_side = i;
                isect_dist = Math.Abs(dist);
                isect_pt = x_pt;
            }

            if (isect_side < 0)
                return false;

            side_i = isect_side;
            pt = isect_pt;
            return true;
        }

        /// <summary>
        /// Get the index of the intersected sub cube.  
        /// </summary>
        /// <param name="side_i">intersection side</param>
        /// <param name="pt">intersection point</param>
        /// <param name="cube_i">sub cube index</param>
        /// <param name="mapped_cube_i">mapped sub cube index</param>
        /// <returns></returns>
        public bool IntersectedSubCube(int side_i, Vector3 pt, out int cube_i, out int mapped_cube_i)
        {
            cube_i = -1;
            mapped_cube_i = -1;

            if (side_i < 0)
                return false;

            // get intersected sub cube
            float[] coords = { pt.X, pt.Y, pt.Z };
            List<int> i_coord = new List<int>();
            bool hit_is_on = true;
            foreach (float coord in coords)
            {
                int i = -1;
                if (Math.Abs(coord) <= 1.0f * _scale)
                    i = 1;
                else if (coord <= -(_offset - 1.0f) * _scale)
                    i = 0;
                else if (coord >= (_offset - 1.0f) * _scale)
                    i = 2;
                i_coord.Add(i);
                hit_is_on = hit_is_on && i >= 0;
            }

            if (hit_is_on)
            {
                int i = 9 * i_coord[2] + 3 * i_coord[1] + i_coord[0];
                cube_i = i;
                mapped_cube_i = _cube_map[i];
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get the side index of the intersected sub cube. 
        /// </summary>
        /// <param name="side_i">index of intersected side</param>
        /// <param name="cube_i">index of intersected sub cube</param>
        /// <param name="subcube_side_i">index of intersected side of intersected sub cube </param>
        /// <returns></returns>
        public bool IntersectedSubCubeSide(int side_i, int cube_i, out int subcube_side_i)
        {
            subcube_side_i = -1;

            if (side_i < 0 || cube_i < 0)
                return false;

            // get the side on the intersected sub cube
            Matrix4 cube_mat4 = _current_pos[cube_i];
            int axis_i = side_i / 2;
            float sign = (side_i % 2 != 0) ? 1.0f : -1.0f;
            Vector3 test_vec = new Vector3(cube_mat4[0, axis_i], cube_mat4[1, axis_i], cube_mat4[2, axis_i]) * sign;

            int isect_i_side = -1;
            if (test_vec.X < -0.5f)
                isect_i_side = 0;
            else if (test_vec.X > 0.5f)
                isect_i_side = 1;
            else if (test_vec.Y < -0.5f)
                isect_i_side = 2;
            else if (test_vec.Y > 0.5f)
                isect_i_side = 3;
            else if (test_vec.Z < -0.5f)
                isect_i_side = 4;
            else if (test_vec.Z > 0.5f)
                isect_i_side = 5;

            if (isect_i_side >= 0)
            {
                subcube_side_i = isect_i_side;
                return true;
            }
            return false;
        }
    }
}
