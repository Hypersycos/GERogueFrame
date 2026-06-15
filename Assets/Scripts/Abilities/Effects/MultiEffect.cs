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

        void ICastEffect.ClientCastEnd(AbilityPayload payload)
		{
			foreach(ICastEffect effect in Effects)
			{
				effect.ClientCastEnd(payload);
			}
		}

        void ICastEffect.ClientCastFixedUpdate()
		{
			foreach(ICastEffect effect in Effects)
			{
				effect.ClientCastFixedUpdate();
			}
		}

        void ICastEffect.ClientCastStart(AbilityPayload payload)
		{
			foreach(ICastEffect effect in Effects)
			{
				effect.ClientCastStart(payload);
			}
		}

        void ICastEffect.ClientCastUpdate()
		{
			foreach(ICastEffect effect in Effects)
			{
				effect.ClientCastUpdate();
			}
		}

        void ICastEffect.ClientNetworkUpdate(AbilityPayload payload)
		{
			foreach(ICastEffect effect in Effects)
			{
				effect.ClientNetworkUpdate(payload);
			}
		}

        ICastEffect ICastEffect.Clone()
		{
			List<ICastEffect> effects = Effects.Select((x) => x.Clone()).ToList();
			return new MultiEffect(effects);
		}

        AbilityPayload ICastEffect.OwnerCastEnd(TargetPayload target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
		{
			return new MultiPayload(Effects.Select((x) => x.OwnerCastEnd(target, position, cameraPosition, direction, myState)).ToList());
		}

        void ICastEffect.OwnerCastFixedUpdate()
		{
			foreach(ICastEffect effect in Effects)
			{
				effect.OwnerCastFixedUpdate();
			}
		}

        AbilityPayload ICastEffect.OwnerCastStart(TargetPayload target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
		{
            return new MultiPayload(Effects.Select((x) => x.OwnerCastStart(target, position, cameraPosition, direction, myState)).ToList());
		}

        void ICastEffect.OwnerCastUpdate()
		{
			foreach(ICastEffect effect in Effects)
			{
				effect.OwnerCastUpdate();
			}
		}

        AbilityPayload ICastEffect.ServerCastEnd(AbilityPayload payload, TargetPayload target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
		{
            return new MultiPayload(Effects.Select((x) => x.ServerCastEnd(payload, target, position, cameraPosition, direction, myState)).ToList());
		}

        void ICastEffect.ServerCastFixedUpdate()
		{
			foreach(ICastEffect effect in Effects)
			{
				effect.ServerCastFixedUpdate();
			}
		}

        AbilityPayload ICastEffect.ServerCastStart(AbilityPayload payload, TargetPayload target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
		{
            return new MultiPayload(Effects.Select((x) => x.ServerCastStart(payload, target, position, cameraPosition, direction, myState)).ToList());
		}

        void ICastEffect.ServerCastUpdate()
		{
			foreach(ICastEffect effect in Effects)
			{
				effect.ServerCastUpdate();
			}
		}

        void ICastEffect.ServerNetworkUpdate(AbilityPayload payload)
		{
			foreach(ICastEffect effect in Effects)
			{
				effect.ServerNetworkUpdate(payload);
			}
		}
    }

    public record MultiPayload : AbilityPayload
    {
		public override string id => "MultiPayload";

		public List<AbilityPayload> Payloads = new List<AbilityPayload>();
        public MultiPayload(List<AbilityPayload> payloads)
        {
            Payloads = payloads;
        }

        public override void Serialize(FastBufferWriter writer)
        {
            base.Serialize(writer);
			writer.WriteValueSafe(Payloads.Count);
			foreach(AbilityPayload payload in Payloads)
			{
				payload.Serialize(writer);
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
