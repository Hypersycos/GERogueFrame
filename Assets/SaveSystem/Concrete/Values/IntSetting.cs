using Hypersycos.SaveSystem;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "IntSetting", menuName = "SaveSystem/Values/Int", order = 0)]
    public class IntSetting : NetworkableValue<int>
    {
    }
}
