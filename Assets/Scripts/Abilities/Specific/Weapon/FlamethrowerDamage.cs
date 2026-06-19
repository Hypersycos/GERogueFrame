using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    class FlamethrowerDamage : MonoBehaviour
    {
        public DamageInstance damageInst;
        public DotStatusInstance dot;
        HashSet<CharacterState> debounce = new();

        public int ticks;
        public Vector3 movePerTick;
        public float scalePerTick;
        float falloffPerTick;
        float dotFalloffPerTick;
        public LayerMask terrainHitMask;
        Vector3 origin;
        SphereCollider myCollider;

        private void Apply(Collider other)
        {
            var state = other.GetComponent<CharacterState>();
            if (state == null || damageInst.owner.Team == state.Team || debounce.Contains(state))
                return;

            debounce.Add(state);

            Vector3 closestPoint = other.ClosestPoint(transform.position);

            if (Physics.Raycast(origin, closestPoint - origin, 1, terrainHitMask, QueryTriggerInteraction.Ignore) ||
                Physics.Raycast(transform.position, (closestPoint - transform.position).normalized, 1, terrainHitMask, QueryTriggerInteraction.Ignore))
                return;

            if (damageInst != null)
                state.ApplyDamageInstance(new(damageInst));
            if (dot != null)
                state.AddStatus(dot.CloneInstance());
        }

        private void OnTriggerEnter(Collider other)
        {
            Apply(other);
        }

        private void FixedUpdate()
        {
            if (ticks-- == 0)
            {
                Destroy(this.gameObject);
                return;
            }

            myCollider.radius += scalePerTick;
            transform.position += movePerTick;
            damageInst.Amount += falloffPerTick;
            dot.Amount += dotFalloffPerTick;
        }

        public void Setup(float damage, float range, float angle, float speed, DotStatusInstance dot, CharacterState owner, float falloff)
        {
            ticks = (int)(range / speed / Time.fixedDeltaTime);
            falloffPerTick = damage * (falloff - 1) / ticks;
            float distPerTick = speed * Time.fixedDeltaTime;
            movePerTick = distPerTick * (transform.rotation * Vector3.forward);
            scalePerTick = Mathf.Sin(angle) * distPerTick / 2;
            this.dot = dot;

            dotFalloffPerTick = this.dot.Amount * (falloff - 1) / ticks;

            myCollider = GetComponent<SphereCollider>();
            origin = transform.position;

            damageInst = new DamageInstance(true, damage, owner, StatTypeTarget.AllValid);

            myCollider.enabled = true;
            transform.position += movePerTick;
            myCollider.radius = scalePerTick;
        }
    }
}
