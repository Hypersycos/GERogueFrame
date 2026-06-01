using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.Utils
{
    public static class TransformExtensions
    {
        public static void DestroyAllChildren(this Transform t)
        {
            if (t.childCount == 0)
                return;

            for (int i = t.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
            }
        }
    }
}
