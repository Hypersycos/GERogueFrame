using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    class TargetGetComponent<T> : ISecondaryTargetChecker where T : Component
    {
        public ISecondaryTargetChecker Clone()
        {
            return this;
        }

        public bool HasValidTarget(TargetPayload target, CharacterState myState, out TargetPayload hit)
        {
            var objPayload = target as IGameObjectPayload;
            if (objPayload != null)
            {
                T test = objPayload.Target.GetComponent<T>();
                if (test != null)
                {
                    hit = new ComponentPayload<T>(test);
                    return true;
                }
            }
            hit = null;
            return false;
        }
    }

    class TargetGetComponent : ISecondaryTargetChecker
    {
        [OdinSerialize] Type type;
        public ISecondaryTargetChecker Clone()
        {
            return this;
        }

        public bool HasValidTarget(TargetPayload target, CharacterState myState, out TargetPayload hit)
        {
            var objPayload = target as IGameObjectPayload;
            if (objPayload != null)
            {
                Component test = objPayload.Target.GetComponent(type);
                if (test != null)
                {
                    hit = new ComponentPayload(test);
                    return true;
                }
            }
            hit = null;
            return false;
        }
    }
}
