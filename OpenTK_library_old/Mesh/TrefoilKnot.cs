using System;
using System.Collections.Generic;
using OpenTK; // Vector3

namespace OpenTK_library.Mesh
{
    // TODO generic

    public class TrefoilKnot
    {
        private int _slices = 32;
        private int _stacks = 256;
        private float _ra = 0.6f;
        private float _rb = 0.2f;
        private float _rc = 0.4f;
        private float _rd = 0.175f;
        private float[] _c = new float[] { 0.9f, 0.5f, 0.1f, 0.0f };

        public TrefoilKnot(int slices = 256, int stacks = 32, float ra = 0.6f, float rb = 0.2f, float rc = 0.4f, float rd = 0.175f, float[] c = null)
        {
            this._slices = slices;
            this._stacks = stacks;
            this._ra = ra;
            this._rb = rb;
            this._rc = rc;
            this._rd = rd;
            this._c = c ?? this._c;
        }

        public ( float[] attribtes, uint[] indices ) Create()
        {
            List<float> attributes = new List<float>();
            List<uint> indices = new List<uint>();

            float E = 0.01f;
            float ds = 1.0f / this._slices;
            float dt = 1.0f / this._stacks;

            uint vertexCount = 0;
            for (float s = 0; s < 1 + ds / 2; s += ds)
            {
                for (float t = 0; t < 1 + dt / 2; t += dt)
                {
                    Vector3 p = this.Compute(s, t);
                    Vector3 u = this.Compute(s + E, t);
                    u = Vector3.Subtract(u, p);
                    Vector3 v = this.Compute(s, t + E);
                    v = Vector3.Subtract(v, p);
                    Vector3 nv = Vector3.Cross(u, v).Normalized();

                    attributes.AddRange(new float[] { p.X, p.Y, p.Z });
                    attributes.AddRange(new float[] { nv.X, nv.Y, nv.Z });
                    attributes.AddRange(new float[] { s*18.0f, t});
                    attributes.AddRange(this._c);
                    vertexCount++;
                }
            }

            uint n = 0;
            for (uint i = 0; i < (uint)this._slices; ++i)
            {
                for (uint j = 0; j < (uint)this._stacks; ++j)
                {
                    indices.AddRange(new uint[] { n + j, (n + j + (uint)this._stacks + 1) % vertexCount, n + j + 1 });
                    indices.AddRange(new uint[] { (n + j + (uint)this._stacks + 1) % vertexCount, (n + j + 1 + (uint)this._stacks + 1) % vertexCount, (n + j + 1) % vertexCount });
                }
                n += (uint)this._stacks + 1;
            }

            return ( attribtes: attributes.ToArray(), indices: indices.ToArray() );
        }

        private Vector3 Compute(float s, float t)
        {
            float TwoPi = (float)Math.PI * 2;
            float a = this._ra;
            float b = this._rb;
            float c = this._rc;
            float d = this._rd;
            float u = (1 - s) * 2 * TwoPi;
            float v = t * TwoPi;
            float r = a + b * (float)Math.Cos(1.5 * u);
            float x = r * (float)Math.Cos(u);
            float y = r * (float)Math.Sin(u);
            float z = c * (float)Math.Sin(1.5 * u);
            Vector3 dv = new Vector3( 
               -1.5f * b * (float)Math.Sin(1.5f * u) * (float)Math.Cos(u) - (a + b * (float)Math.Cos(1.5f * u)) * (float)Math.Sin(u),
               -1.5f * b * (float)Math.Sin(1.5f * u) * (float)Math.Sin(u) + (a + b * (float)Math.Cos(1.5f * u)) * (float)Math.Cos(u),
                1.5f * c * (float)Math.Cos(1.5f * u)
            );
            Vector3 q = dv.Normalized();
            Vector3 qvn = Vector3.Normalize( new Vector3(q.Y, -q.X, 0) );
            Vector3 ww = Vector3.Cross(q, qvn);
            Vector3 range = new Vector3(
                x + d * (qvn[0] * (float)Math.Cos(v) + ww[0] * (float)Math.Sin(v)),
                y + d * (qvn[1] * (float)Math.Cos(v) + ww[1] * (float)Math.Sin(v)),
                z + d * ww[2] * (float)Math.Sin(v)
            );
            return range;
        }
    }
}

/*
 class MeshTrefoilKnot extends MeshObject {
     compute( s, t ) {
        
        let dv = [
            -1.5 * b * Math.sin( 1.5 * u ) * Math.cos( u ) - ( a + b * Math.cos( 1.5 * u ) ) * Math.sin( u ),
            -1.5 * b * Math.sin( 1.5 * u ) * Math.sin( u ) + ( a + b * Math.cos( 1.5 * u ) ) * Math.cos( u ),
            1.5 * c * Math.cos( 1.5 * u )
        ];
        let q = Vec3.normalize( dv );
        let qvn = Vec3.normalize( [ q[1], -q[0], 0 ] );
        let ww = Vec3.cross( q, qvn );
        let range = [
            x + d * ( qvn[0] * Math.cos( v ) + ww[0] * Math.sin( v ) ),
            y + d * ( qvn[1] * Math.cos( v ) + ww[1] * Math.sin( v ) ),
            z + d * ww[2] * Math.sin( v )
        ]    
        return range;
    }
};
*/
