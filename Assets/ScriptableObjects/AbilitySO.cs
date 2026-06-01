using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public struct ResourceCost
    {
        public string Resource;
        public float Cost;
    }

    [CreateAssetMenu(fileName = "New Ability", menuName = "GERogueFrame/Ability", order = 0)]
    public class AbilitySO : ScriptableObject
    {
        public string AbilityName;
        public string AbilityDescription;
        public Texture2D AbilityIcon;
        public List<ResourceCost> AbilityResources;
        public float AbilityCooldown;
    }
}
