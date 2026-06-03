using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    [CreateAssetMenu(fileName = "New Stat Type", menuName = "Character/Stat Type")]
    public class StatType : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public string Description { get; private set; }
        [field: SerializeField] public Color Color { get; private set; }
        //backing colour for bar
        [field: SerializeField] public Color BackColor { get; private set; }
        //colour for "damaged" bar
        [field: SerializeField] public Color FadeColor { get; private set; }
        //time delay before "damaged" bar starts fading
        [field: SerializeField] public float BarFadeStart { get; private set; }
        //the rate at which the bar fades. 1 => 1s for an instance which is 100% of the stat's maximum
        [field: SerializeField] public float BarFadeRate { get; private set; }
        [field: SerializeField] public float DefaultStartValue { get; private set; }
        [field: SerializeField] public bool DefaultStartIsPercentage { get; private set; }
    }
}
