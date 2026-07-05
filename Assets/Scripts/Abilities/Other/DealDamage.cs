using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public class DealDamage : MonoBehaviour
    {
        public float damageAmount;
        public IStatTypeTarget healthTypeTargets = StatTypeTarget.AllValid;
        public void Apply(CharacterState owner, CharacterState target)
        {
            target.ApplyDamageInstance(new(true, damageAmount, owner, healthTypeTargets));
        }

        public void Apply(NonNetworkedProjectileScript _, CharacterState target, CharacterState owner)
        {
            target.ApplyDamageInstance(new(true, damageAmount, owner, healthTypeTargets));
        }
    }
}
