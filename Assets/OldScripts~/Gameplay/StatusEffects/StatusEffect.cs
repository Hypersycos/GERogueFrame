using System;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    [CreateAssetMenu(fileName = "New Status Effect", menuName = "Combat/Status Effect")]
    public class StatusEffect : ScriptableObject
    {
        public enum StackMethod
        {
            Additive, //Combine timers
            StackingRefresh, //Reset all timers
            SingleRefresh, //Pick biggest number
            Instance, //Individual instances
            SingleInstance, //Pick the biggest instance
            SingleInstancePerSource //Pick the biggest instance per source
        }

        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public string Description { get; private set; }
        [field: SerializeField] public StackMethod StackType { get; private set; }
        [field: SerializeField] public float DefaultDuration { get; private set; }
    }
}