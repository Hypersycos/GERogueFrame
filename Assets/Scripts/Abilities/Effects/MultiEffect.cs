using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    class MultiEffect : ICastEffect
    {
        public List<ICastEffect> Effects;

        public MultiEffect()
        {
			Effects = new();
        }

        public MultiEffect(List<ICastEffect> effects)
        {
            Effects = effects;
        }

        public bool HasClientCast => Effects.Count > 0 && Effects.Aggregate(true, (acc, x) => acc && x.HasClientCast);

        public bool HasOwnerClientCast => Effects.Count > 0 && Effects.Aggregate(true, (acc, x) => acc && x.HasOwnerClientCast);

        public void ClientCast(AbilityPayload networkPayload)
        {
            var multi = networkPayload as MultiPayload;
            for (int i = 0; i < Effects.Count; i++)
            {
                Effects[i].ClientCast(multi?.Payloads[i]);
            } 
        }

        public ICastEffect Clone()
        {
            return new MultiEffect(Effects.Select(x => x.Clone()).ToList());
        }

        public AbilityPayload OwnerCast(ITargetPayload targetPayload, CharacterState myState)
        {
            List<AbilityPayload> payloads = new();
            bool hasNonNull = false;
            for (int i = 0; i < Effects.Count; i++)
            {
                AbilityPayload result = Effects[i].OwnerCast(targetPayload, myState);
                if (result != null)
                    hasNonNull = true;
                payloads.Add(result);
            }
            return hasNonNull ? new MultiPayload(payloads) : null;
        }

        public void OwnerClientCast(AbilityPayload networkPayload)
        {
            var multi = networkPayload as MultiPayload;
            for (int i = 0; i < Effects.Count; i++)
            {
                Effects[i].OwnerClientCast(multi?.Payloads[i]);
            }
        }

        public AbilityPayload ServerCast(ITargetPayload targetPayload, AbilityPayload networkPayload, CharacterState myState)
        {
            var multi = networkPayload as MultiPayload;
            List<AbilityPayload> payloads = new();
            bool hasNonNull = false;
            for (int i = 0; i < Effects.Count; i++)
            {
                AbilityPayload result = Effects[i].ServerCast(targetPayload, multi?.Payloads[i], myState);
                if (result != null)
                    hasNonNull = true;
                payloads.Add(result);
            }
            return hasNonNull ? new MultiPayload(payloads) : null;
        }
    }

    [PayloadId("MultiPayload", nameof(Deserialize))]
    public record MultiPayload : AbilityPayload
    {

		public List<AbilityPayload> Payloads = new List<AbilityPayload>();
        public MultiPayload(List<AbilityPayload> payloads)
        {
            Payloads = payloads;
        }

        public override void Serialize(FastBufferWriter writer)
        {
			writer.WriteValueSafe(Payloads.Count);
			foreach(AbilityPayload payload in Payloads)
			{
				writer.WriteValueSafe(payload);
			}
        }

        public new static AbilityPayload Deserialize(FastBufferReader reader)
        {
            reader.ReadValueSafe(out int count);
			List<AbilityPayload> payloads = new();
			for(int i = 0; i < count; i++)
			{
				payloads.Add(AbilityPayload.Deserialize(reader));
			}
            return new MultiPayload(payloads);
        }
    }
}
