using Hypersycos.GERogueFrame;
using Hypersycos.SaveSystem;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "ColorSetting", menuName = "SaveSystem/Values/Color", order = 0)]
    public class ColorSetting : NetworkableValue<Color>
    {
    }
}
