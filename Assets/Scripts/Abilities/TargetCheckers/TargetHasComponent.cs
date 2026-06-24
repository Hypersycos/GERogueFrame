using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    class TargetHasComponent<T> : ISecondaryTargetChecker where T : Component
    {
        public ISecondaryTargetChecker Clone()
        {
            return this;
        }

        public bool HasValidTarget(ITargetPayload target, CharacterState myState, out ITargetPayload hit)
        {
            var objPayload = target as IGameObjectPayload;
            if (objPayload != null)
            {
                T test = objPayload.Target.GetComponent<T>();
                if (test != null)
                {
                    hit = target;
                    return true;
                }
            }
            hit = null;
            return false;
        }
    }

    class TargetHasComponent : ISecondaryTargetChecker
    {
        [OdinSerialize] Type type;
        public ISecondaryTargetChecker Clone()
        {
            return this;
        }

        public bool HasValidTarget(ITargetPayload target, CharacterState myState, out ITargetPayload hit)
        {
            if (target is IGameObjectPayload objPayload)
            {
                Component test = objPayload.Target.GetComponent(type);
                if (test != null)
                {
                    hit = target;
                    return true;
                }
            }
            hit = null;
            return false;
        }
    }
}
