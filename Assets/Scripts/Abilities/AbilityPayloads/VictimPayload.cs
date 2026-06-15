using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;

namespace Hypersycos.GERogueFrame
{
    public interface IVictimPayload
    {
        public CharacterState Victim { get; }
    }

    public record VictimPayload : AbilityPayload, IVictimPayload
    {
        public override string id => "Victim";

        protected CharacterState _victim;

        public CharacterState Victim => _victim;

        public VictimPayload(CharacterState victim)
        {
            _victim = victim;
        }

        public override void Serialize(FastBufferWriter writer)
        {
            base.Serialize(writer);
            writer.WriteValueSafe(new NetworkBehaviourReference(_victim));
        }

        public new static AbilityPayload Deserialize(FastBufferReader reader)
        {
            reader.ReadValueSafe(out NetworkBehaviourReference victim);
            bool success = victim.TryGet(out CharacterState state);
            if (success)
                return new VictimPayload(state);
            return null;
        }
    }
}
