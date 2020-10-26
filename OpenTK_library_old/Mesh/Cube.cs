using System;
using System.Collections.Generic;
using OpenTK; // Vector3

namespace OpenTK_library.Mesh
{
    // TODO generic

    public class Cube
    {
        public Cube()
        { }

        public (float[] attribtes, uint[] indices) Create()
        {
            List<float> attributes = new List<float>();
            List<uint> indices = new List<uint>();

            float[] v = { -1, -1, 1, 1, -1, 1, 1, 1, 1, -1, 1, 1, -1, -1, -1, 1, -1, -1, 1, 1, -1, -1, 1, -1 };
            float[] c = { 1.0f, 0.0f, 0.0f, 1.0f, 0.5f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f };
            float[] n = { 0, 0, 1, 1, 0, 0, 0, 0, -1, -1, 0, 0, 0, 1, 0, 0, -1, 0 };
            float[] t = { 0, 0, 1, 0, 1, 1, 0, 1 };
            int[] ec = { 0, 1, 2, 3, 1, 5, 6, 2, 5, 4, 7, 6, 4, 0, 3, 7, 3, 2, 6, 7, 1, 0, 4, 5 };
            int[] es = { 0, 1, 2, 0, 2, 3 };
            for (int si = 0; si < 6; ++si)
            {
                for (int vi = 0; vi < 6; ++vi)
                {
                    int ci = es[vi];
                    int i = si * 4 + ci;
                    attributes.AddRange(new float[] { v[ec[i] * 3], v[ec[i] * 3 + 1], v[ec[i] * 3 + 2] });
                    attributes.AddRange(new float[] { n[si * 3], n[si * 3 + 1], n[si * 3 + 2] });
                    attributes.AddRange(new float[] { t[es[vi] * 2], t[es[vi] * 2 + 1] });
                    attributes.AddRange(new float[] { c[si * 3], c[si * 3 + 1], c[si * 3 + 2], 1 });
                }
            }

            return (attribtes: attributes.ToArray(), indices: indices.ToArray());
        }
    }
}