using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace Hypersycos.GERogueFrame
{
    class HitscanChecker : ITargetChecker
    {
        [OdinSerialize] ICastEffect CastEffect;
        [SerializeField] float MaxRange;
        [SerializeField] LayerMask HitLayerMask;
        [SerializeField] LayerMask TargetLayerMask;
        [SerializeField] QueryTriggerInteraction TriggerInteraction;

        public HitscanChecker(ICastEffect castEffect, float maxRange, LayerMask hitLayerMask, LayerMask targetLayerMask, QueryTriggerInteraction triggerInteraction)
        {
            CastEffect = castEffect;
            MaxRange = maxRange;
            HitLayerMask = hitLayerMask;
            TargetLayerMask = targetLayerMask;
            TriggerInteraction = triggerInteraction;
        }

        public ITargetChecker Clone()
        {
            return new HitscanChecker(CastEffect.Clone(), MaxRange, HitLayerMask, TargetLayerMask, TriggerInteraction);
        }

        public ICastEffect GetEffect()
        {
            return CastEffect;
        }

        public bool HasValidTarget(Vector3 direction, Vector3 position, Vector3 camPosition, CharacterState myState, out TargetPayload hit, out ICastEffect castEffect)
        {
            bool didHit = Physics.Raycast(camPosition, direction, out RaycastHit info, MaxRange, HitLayerMask, TriggerInteraction);
            if (didHit)
            {
                if ((TargetLayerMask & (1 << info.collider.gameObject.layer)) != 0)
                {
                    Debug.DrawLine(camPosition, info.point, Color.green, 10);
                    castEffect = CastEffect;
                    hit = new HitscanPayload(info);
                    return true;
                }
                else
                {
                    Debug.DrawLine(camPosition, info.point, Color.yellow, 10);
                    castEffect = null;
                    hit = null;
                    return false;
                }
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

    public record HitscanPayload : TargetPayload, IComponentPayload<Collider>, IVec3Payload, IGameObjectPayload
    {
        public RaycastHit hit;

        public HitscanPayload(RaycastHit hit)
        {
            this.hit = hit;
        }

        public Vector3 Target => hit.point;

        GameObject IGameObjectPayload.Target => hit.collider.gameObject;

        Collider IComponentPayload<Collider>.Component => hit.collider;
    }
}
