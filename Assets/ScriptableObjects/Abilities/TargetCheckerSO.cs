using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public abstract class TargetCheckerSO : ScriptableObject, ITargetChecker
    {
        [SerializeField] private ICastEffect effect;

        [SerializeField] private int _priority;
        public int priority { get => _priority; }

        public ICastEffect castEffect;
        public virtual ITargetChecker Clone()
        {
            TargetCheckerSO clone = Instantiate(this);
            clone.effect = clone.effect.Clone();
            return clone;
        }

        public abstract ICastEffect HasValidTarget(Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState);

        public ICastEffect GetEffect()
        {
            return effect;
        }
    }
}
