using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.Utils
{
    public static class InlineVector3Scale
    {
        public static Vector3 InlineScale(this Vector3 vec1, Vector3 vec2)
        {
            vec1.Scale(vec2);
            return vec1;
        }
    }
}
