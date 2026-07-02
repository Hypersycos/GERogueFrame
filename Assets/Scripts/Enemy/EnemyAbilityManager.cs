using Hypersycos.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hypersycos.GERogueFrame
{
    class EnemyAbilityManager : MonoBehaviour
    {
        CharacterState myState;

        Ability currentlyCasting = null;

        Dictionary<string, Ability> abilityMap = new();
        TwoWayDictionary<Ability, uint> abilities = new();

        public void AddAbility(IAbilityData abilityData)
        {
            Ability ability = abilityData.CreateAbility();
            abilities.Add(ability, (uint)abilities.Count);
            abilityMap.Add(abilityData.Name, ability);
        }

        public Ability GetAbility(uint id)
        {
            return abilities[id];
        }

        public Ability GetAbility(string name)
        {
            return abilityMap[name];
        }

        private void Awake()
        {
            myState = GetComponent<CharacterState>();
        }

        private void Update()
        {
            foreach (Ability ability in abilities.Keys)
            {
                ability.Update(myState);
            }
        }

        private void FixedUpdate()
        {
            foreach (Ability ability in abilities.Keys)
            {
                ability.FixedUpdate(myState);
                if (ability.IsDirty)
                {
                    SyncAbilityRpc(abilities[ability], ability.Sync());
                }
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SyncAbilityRpc(uint id, AbilityNetworkPayload payload)
        {
            Ability ability = abilities[id];
            ability.SyncClient(payload);
        }

        #region CastAbility
        public void CastAbility(Ability ability, Vector3 cameraForward, Vector3 cameraPos)
        {
            CastAbility(ability, false, cameraForward, cameraPos);
        }
        private void CastAbility(Ability ability, bool isEnd, Vector3 cameraForward, Vector3 cameraPos)
        {
            if (!ability.chargeAtStart && !isEnd)
                return;

            if (currentlyCasting == null)
                currentlyCasting = ability;
            else if (!isEnd || currentlyCasting != ability)
                return;

            if (!ability.chargeAtStart && !isEnd)
            {
                if (!ability.CanCast(myState))
                    currentlyCasting = null;
                return;
            }

            bool success = ability.OwnerCast(cameraForward, transform.position, cameraPos, myState, out int chosenEffect, out AbilityPayload verifyData, out AbilityPayload abilityPayload);
            if (success)
            {
                ServerCastAbility(abilities[ability], chosenEffect, NetworkManager.Singleton.ServerTime.TickWithPartial, verifyData, abilityPayload);
            }

            if (!success || isEnd)
                currentlyCasting = null;
        }

        private void ServerCastAbility(uint id, int effectID, double time, AbilityPayload verifyData, AbilityPayload abilityPayload)
        {
            Ability ability = abilities[id];
            if (currentlyCasting == null)
                currentlyCasting = ability;
            else if (currentlyCasting != ability)
            {
                //CastFailedRpc(id);
                return;
            }

            bool success = ability.ServerCast(effectID, verifyData, abilityPayload, myState, out int chosenEffect, out AbilityPayload payload);
            if (success)
            {
                if (payload != null)
                    AbilityCastPayloadRpc(abilities[ability], chosenEffect, time, payload);
                else
                    AbilityCastRpc(abilities[ability], chosenEffect, time);

                if (!ability.chargeAtStart)
                    currentlyCasting = null;
            }
            else
            {
                currentlyCasting = null;
                //CastFailedRpc(id);
            }
        }

        private void AbilityCast(uint id, int effectID, double time, AbilityPayload payload)
        {
            Ability ability = abilities[id];
            if (ability.chargeAtStart)
                currentlyCasting = ability;
            ability.ClientCast(effectID, payload, myState);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AbilityCastRpc(uint id, int effectID, double time)
        {
            AbilityCast(id, effectID, time, null);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AbilityCastPayloadRpc(uint id, int effectID, double time, AbilityNetworkPayload payload)
        {
            AbilityCast(id, effectID, time, payload);
        }
        #endregion

        #region EndCast
        public void EndCastAbility(Ability ability, Vector3 cameraForward, Vector3 cameraPos)
        {
            if (!ability.chargeAtStart && currentlyCasting == null)
            {
                CastAbility(ability, true, cameraForward, cameraPos);
                return;
            }

            if (currentlyCasting != ability)
                return;

            bool success = ability.OwnerCastEnd(cameraForward, transform.position, cameraPos, myState, out AbilityPayload verifyData, out AbilityPayload abilityPayload);
            if (success)
            {
                ServerEndCastAbility(NetworkManager.Singleton.ServerTime.TickWithPartial, verifyData, abilityPayload);
            }
            else
                currentlyCasting = null;
        }

        private void ServerEndCastAbility(double time, AbilityPayload verifyData, AbilityPayload abilityPayload)
        {
            Ability ability = currentlyCasting;

            bool success = ability.ServerCastEnd(verifyData, abilityPayload, myState, out AbilityPayload payload);
            if (success)
            {
                if (payload != null)
                    AbilityCastEndPayloadRpc(time, payload);
                else
                    AbilityCastEndRpc(time);
            }
            else
            {
                //EndCastFailedRpc(abilities[ability]);
            }

            currentlyCasting = null;
        }

        private void AbilityCastEnd(double time, AbilityPayload payload)
        {
            var ability = currentlyCasting;
            ability.ClientCastEnd(payload, myState);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AbilityCastEndRpc(double time)
        {
            AbilityCastEnd(time, null);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AbilityCastEndPayloadRpc(double time, AbilityNetworkPayload payload)
        {
            AbilityCastEnd(time, payload);
        }
        #endregion
    }
}
