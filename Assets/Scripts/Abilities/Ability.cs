using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

namespace Hypersycos.GERogueFrame
{
    public interface IServerTickPayload
    {
        public int ServerTick { get; }
    }

    public abstract class Ability
    {
        public int priority { get; protected set; }
        public float endlag { get; protected set; }
        public float queueFor { get; protected set; }
        public bool chargeAtStart { get; protected set; }

        public Ability(int priority, bool chargeAtStart, float endlag, float queueFor)
        {
            this.priority = priority;
            this.chargeAtStart = chargeAtStart;
            this.endlag = endlag;
            this.queueFor = queueFor;
        }
        public abstract void Update(CharacterState myState);
        public abstract void FixedUpdate(CharacterState myState);
        public abstract bool CastingUpdate(Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState);
        public abstract bool CastingFixedUpdate(Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState);
        public abstract bool IsDirty { get; protected set; }
        public abstract bool HasOwnerSync { get; }
        public abstract AbilityPayload Sync();
        public abstract void SyncClient(AbilityPayload payload);
        public abstract void SyncOwner(AbilityPayload payload);
        public abstract bool CanCast(CharacterState myState);
        public abstract bool OwnerCast(Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState, out int chosenEffect, out AbilityPayload verifyData, out AbilityPayload abilityPayload);
        public abstract bool ServerCast(int desiredEffect, AbilityPayload verifyData, AbilityPayload abilityPayload, CharacterState myState, out int chosenEffect, out AbilityPayload payload);
        public abstract void ClientCast(int effectID, AbilityPayload payload, CharacterState myState);
        public abstract bool OwnerCastEnd(Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState, out AbilityPayload verifyData, out AbilityPayload abilityPayload);
        public abstract bool ServerCastEnd(AbilityPayload verifyData, AbilityPayload abilityPayload, CharacterState myState, out AbilityPayload payload);
        public abstract void ClientCastEnd(AbilityPayload payload, CharacterState myState);
    }
}
