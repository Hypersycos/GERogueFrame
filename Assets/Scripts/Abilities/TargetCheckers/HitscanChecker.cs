using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hypersycos.GERogueFrame
{
    class HitscanChecker : ITargetChecker
    {
        [OdinSerialize] ICastEffect CastEffect;
        [SerializeField] float MaxRange;
        [SerializeField] LayerMask LayerMask;
        [SerializeField] QueryTriggerInteraction TriggerInteraction;

        public HitscanChecker(ICastEffect castEffect, float maxRange, LayerMask layerMask, QueryTriggerInteraction triggerInteraction)
        {
            CastEffect = castEffect;
            MaxRange = maxRange;
            LayerMask = layerMask;
            TriggerInteraction = triggerInteraction;
        }

        public ITargetChecker Clone()
        {
            return new HitscanChecker(CastEffect.Clone(), MaxRange, LayerMask, TriggerInteraction);
        }

        public ICastEffect GetEffect()
        {
            return CastEffect;
        }

        public bool HasValidTarget(Vector3 direction, Vector3 position, Vector3 camPosition, CharacterState myState, out object hit, out ICastEffect castEffect)
        {
            bool didHit = Physics.Raycast(camPosition, direction, out RaycastHit info, MaxRange, LayerMask, TriggerInteraction);
            if (didHit)
            {
                Debug.DrawLine(camPosition, info.point, Color.green, 10);
                castEffect = CastEffect;
                hit = info.collider;
                return true;
            }
            else
            {
                Debug.DrawLine(camPosition, camPosition + direction * MaxRange, Color.red, 10);
                castEffect = null;
                hit = null;
                return false;
            }
        }
    }
}
