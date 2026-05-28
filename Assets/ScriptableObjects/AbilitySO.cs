using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New Ability", menuName = "GERogueFrame/Ability", order = 0)]
    public class AbilitySO : ScriptableObject
    {
        public string AbilityName;
        public string AbilityDescription;
        public Texture2D AbilityIcon;
        public string AbilityResource;
        public float AbilityCost;
        public float AbilityCooldown;
    }
}
