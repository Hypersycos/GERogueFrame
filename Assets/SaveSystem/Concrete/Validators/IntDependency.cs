using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "IntDependency", menuName = "SaveSystem/Validators/Int Dependency", order = 26)]
    class IntDependency : RegisteredNumberDependenceSO<int>
    {
        protected override int Calculate() => Dependence.Value * Multiply / Divide + Add;
    }
}
