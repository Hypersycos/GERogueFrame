using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.Utils
{
    public static class LayerMaskExtensions
    {
        public static bool IsInLayerMask(this LayerMask mask, GameObject obj) => (mask.value & (1 << obj.layer)) != 0;
        public static bool IsInLayerMask(this LayerMask mask, int layer) => (mask.value & (1 << layer)) != 0;
    }
}
