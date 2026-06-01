using Hypersycos.SaveSystem;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "StringSetting", menuName = "SaveSystem/Values/String", order = 0)]
    public class StringSetting : NetworkableValue<string>
    {
    }
}
