using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hypersycos.GERogueFrame
{
    public class ListEffect : ICastEffect
    {
        [ShowInInspector, OdinSerialize] ICastEffect effect;

        public ListEffect(ICastEffect effect)
        {
            this.effect = effect;
        }

        public bool HasClientCast => effect.HasClientCast;

        public bool HasOwnerClientCast => effect.HasOwnerClientCast;

        public void ClientCast(AbilityPayload networkPayload)
        {
            foreach (AbilityPayload payload in (networkPayload as MultiPayload).Payloads)
            {
                effect.ClientCast(payload);
            }
        }

        public ICastEffect Clone()
        {
            return new ListEffect(effect.Clone());
        }

        public AbilityPayload OwnerCast(ITargetPayload targetPayload, CharacterState myState)
        {
            foreach(ITargetPayload payload in (targetPayload as IListTarget<ITargetPayload>).List)
            {
                effect.OwnerCast(payload, myState);
            }
            return null;
        }

        public void OwnerClientCast(AbilityPayload networkPayload)
        {
            foreach (AbilityPayload payload in (networkPayload as MultiPayload).Payloads)
            {
                effect.OwnerClientCast(payload);
            }
        }

        public AbilityPayload ServerCast(ITargetPayload targetPayload, AbilityPayload networkPayload, CharacterState myState)
        {
            List<AbilityPayload> payloads = new();
            foreach (ITargetPayload payload in (targetPayload as IListTarget<ITargetPayload>).List)
            {
                var result = effect.ServerCast(payload, null, myState);
                if (result != null)
                    payloads.Add(result);
            }
            return payloads.Count == 0 ? null : new MultiPayload(payloads);
        }
    }
}
