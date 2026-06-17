using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    class Detonate : ICastEffect
    {
        [PayloadId("Detonate", nameof(Deserialize))]
        private record DetonatePayload : AbilityPayload
        {
            public List<CharacterState> victims;

            public DetonatePayload(ListTarget<CharacterState> payload)
            {
                victims = payload.List as List<CharacterState>;
            }

            public DetonatePayload(List<CharacterState> victims)
            {
                this.victims = victims;
            }

            public override void Serialize(FastBufferWriter writer)
            {
                writer.WriteValueSafe(victims.Count);
                foreach(CharacterState victim in victims)
                {
                    writer.WriteValueSafe(new NetworkBehaviourReference(victim));
                }
            }

            public new static AbilityPayload Deserialize(FastBufferReader reader)
            {
                List<CharacterState> victims = new();
                reader.ReadValueSafe(out int count);
                for (int i = 0; i < count; i++)
                {
                    reader.ReadValueSafe(out NetworkBehaviourReference reference);
                    if (reference.TryGet(out CharacterState victim))
                        victims.Add(victim);
                }

                return new DetonatePayload(victims);
            }
        }

        public float ExplosionRange;
        public LayerMask EnemyLayerMask;
        [SerializeField] AreaOfEffectVisual detonateVisual;

        public ICastEffect Clone()
        {
            throw new NotImplementedException();
        }

        AbilityPayload ICastEffect.OwnerCastEnd(TargetPayload target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            var payloadIn = target as ListTarget<CharacterState>;
            return new DetonatePayload(payloadIn);
        }

        AbilityPayload ICastEffect.ServerCastEnd(AbilityPayload payload, TargetPayload target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            List<CharacterState> affectedStates = new();
            foreach(CharacterState state in (payload as DetonatePayload).victims)
            {
                IList<StatusInstance> HeatInstances = state.GetStatusInstances(HeatStatusInstance.Heat);
                if (HeatInstances == null) continue;

                float total = 0;
                for (int i = HeatInstances.Count - 1; i >= 0 ; i--)
                {
                    HeatStatusInstance inst = (HeatStatusInstance)HeatInstances[i];
                    if (inst.OneTimeEffects.Contains("Detonate"))
                    { //Prevents infinite loops if this damage instance is used to create a heat status effect
                        continue;
                    }
                    int ticks = (int)inst.duration + 1;
                    total += ticks * inst.Amount;
                    state.RemoveStatus(inst);
                }
                if (total == 0)
                {
                    continue;
                }
                foreach (Collider coll in Physics.OverlapSphere(state.transform.position, ExplosionRange, EnemyLayerMask))
                {
                    CharacterState victimState = coll.gameObject.GetComponent<CharacterState>();
                    if (victimState != null && victimState != state && victimState.Team != myState.Team)
                    {
                        DamageInstance inst = new DamageInstance(true, total, myState, StatTypeTarget.AllValid);
                        inst.OneTimeEffects.Add("Accelerant");
                        inst.OneTimeEffects.Add("FirePatch");
                        victimState.ApplyDamageInstance(inst);
                    }
                }
                affectedStates.Add(state);
            }

            return new DetonatePayload(affectedStates);
        }

        void ICastEffect.ClientCastEnd(AbilityPayload payload)
        {
            var det = payload as DetonatePayload;
            foreach(CharacterState state in det.victims)
            {
                if (state != null)
                {
                    AreaOfEffectVisual visual = GameObject.Instantiate(detonateVisual, state.transform.position, Quaternion.identity);
                }
            }
        }
    }

    class DetonateFilter : ISecondaryTargetChecker
    {
        public ISecondaryTargetChecker Clone()
        {
            return this;
        }

        public bool HasValidTarget(TargetPayload target, CharacterState myState, out TargetPayload hit)
        {
            var payload = target as AreaPayload;

            Collider[] colliders = payload.colliders;
            List<CharacterState> targets = new();

            foreach(Collider coll in colliders)
            {
                CharacterState state = coll.GetComponent<CharacterState>();
                if (state && state.Team != myState.Team && state.GetStatusCount(HeatStatusInstance.Heat) > 0)
                {
                    targets.Add(state);
                }
            }
            if (targets.Count > 0)
            {
                hit = new ListTarget<CharacterState>(targets);
                return true;
            }
            hit = null;
            return false;
        }
    }
}
