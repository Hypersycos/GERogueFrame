using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "FloatDependency", menuName = "SaveSystem/Validators/Float Dependency", order = 26)]
    class FloatDependency : RegisteredNumberDependenceSO<float>
    {
        protected override float Calculate() => Dependence.Value * Multiply / Divide + Add;
    }
}
