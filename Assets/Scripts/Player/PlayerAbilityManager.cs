using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    class PlayerAbilityManager : NetworkBehaviour
    {
        public Ability weapon;
        public Ability weaponAlt;

        public Ability ability1;
        public Ability ability2;
        public Ability ability3;
        public Ability ability4;

        public Ability ultimate;

        PlayerState myState;
        Controls controls;
        GameObject playerCamera;

        Ability currentlyCasting = null;

        Dictionary<Ability, uint> abilityMap = new();
        Dictionary<uint, Ability> idMap = new();

        private void Awake()
        {
            myState = GetComponent<PlayerState>();
            playerCamera = GameObject.FindGameObjectWithTag("MainCamera");
            
            controls = new();

            controls.Player.Enable();
            controls.Player.Ability1.started += CastAbility1;
            controls.Player.Ability1.canceled += EndCastAbility1;
        }
        
        public void BuildMap()
        {
            List<Ability> abilities = new List<Ability>() { weapon, weaponAlt, ability1, ability2, ability3, ability4, ultimate };

            uint i = 0;
            foreach (Ability ability in abilities)
            {
                if (ability == null)
                    continue;
                abilityMap.Add(ability, i);
                idMap.Add(i, ability);
                i++;
            }
        }

        #region CastAbility
        private void CastAbility(Ability ability)
        {
            if (currentlyCasting == null)
                currentlyCasting = ability;

            Vector3 cameraForward = playerCamera.transform.forward;
            Vector3 cameraPos = playerCamera.transform.position;
            bool success = ability.OwnerCast(cameraForward, transform.position, cameraPos, myState, out AbilityPayload payload);
            if (success)
            {
                if (IsHost)
                {
                    success = ability.ServerCast(payload, cameraForward, transform.position, cameraPos, myState, out payload);
                    if (success)
                    {
                        if (payload != null)
                            AbilityCastPayloadRpc(abilityMap[ability], ability.currentID, NetworkManager.ServerTime, payload);
                        else
                            AbilityCastRpc(abilityMap[ability], ability.currentID, NetworkManager.ServerTime);
                    }
                }
                else
                {
                    if (payload != null)
                        CastAbilityPayloadRpc(abilityMap[ability], ability.currentID, cameraPos, cameraForward, NetworkManager.ServerTime, payload);
                    else
                        CastAbilityRpc(abilityMap[ability], ability.currentID, cameraPos, cameraForward, NetworkManager.ServerTime);
                }
            }
            else
                currentlyCasting = null;
        }

        [Rpc(SendTo.Server)]
        private void CastAbilityRpc(uint id, uint effectID, Vector3 cameraPos, Vector3 cameraForward, NetworkTime time)
        {
            Ability ability = idMap[id];
            if (currentlyCasting == null)
                currentlyCasting = ability;
            else
            {
                CastFailedRpc(id);
                return;
            }

            bool success = ability.ServerCast(null, cameraForward, transform.position, cameraPos, myState, out AbilityPayload payload);
            if (success)
            {
                if (payload != null)
                    AbilityCastPayloadRpc(abilityMap[ability], ability.currentID, time, payload);
                else
                    AbilityCastRpc(abilityMap[ability], ability.currentID, time);
            }
            else
            {
                currentlyCasting = null;
                CastFailedRpc(id);
            }
        }

        [Rpc(SendTo.Server)]
        private void CastAbilityPayloadRpc(uint id, uint effectID, Vector3 cameraPos, Vector3 cameraForward, NetworkTime time, AbilityNetworkPayload payloadIn)
        {
            Ability ability = idMap[id];
            if (currentlyCasting == null)
                currentlyCasting = ability;
            else
            {
                CastFailedRpc(id);
                return;
            }

            bool success = ability.ServerCast(payloadIn, cameraForward, transform.position, cameraPos, myState, out AbilityPayload payload);
            if (success)
            {
                if (payload != null)
                    AbilityCastPayloadRpc(abilityMap[ability], ability.currentID, time, payload);
                else
                    AbilityCastRpc(abilityMap[ability], ability.currentID, time);
            }
            else
            {
                currentlyCasting = null;
                CastFailedRpc(id);
            }
        }

        [Rpc(SendTo.Owner)]
        private void CastFailedRpc(uint id)
        {
            throw new NotImplementedException();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AbilityCastRpc(uint id, uint effectID, NetworkTime time)
        {
            Ability ability = idMap[id];
            if (IsOwner)
            {
                //TODO: actually handle this case
                if (ability.currentID != effectID)
                    throw new Exception("ohno");
            }
            else
            {
                ability.targets[(int)effectID].GetEffect().ClientCastStart(null);
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AbilityCastPayloadRpc(uint id, uint effectID, NetworkTime time, AbilityNetworkPayload payload)
        {
            Ability ability = idMap[id];
            if (IsOwner)
            {
                //TODO: actually handle this case
                if (ability.currentID != effectID)
                    throw new Exception("ohno");
            }
            else
            {
                ability.targets[(int)effectID].GetEffect().ClientCastStart(payload);
            }
        }
        #endregion

        #region EndCast
        private void EndCast(Ability ability)
        {
            if (currentlyCasting != ability)
                return;
            Vector3 cameraForward = playerCamera.transform.forward;
            Vector3 cameraPos = playerCamera.transform.position;
            bool success = ability.OwnerCastEnd(cameraForward, transform.position, cameraPos, myState, out AbilityPayload payload);
            if (success)
            {
                if (IsHost)
                {
                    success = ability.ServerCast(payload, cameraForward, transform.position, cameraPos, myState, out payload);
                    if (success)
                    {
                        if (payload != null)
                            EndAbilityCastPayloadRpc(abilityMap[ability], ability.currentID, NetworkManager.ServerTime, payload);
                        else
                            EndAbilityCastRpc(abilityMap[ability], ability.currentID, NetworkManager.ServerTime);
                    }
                }
                if (payload != null)
                    EndCastAbilityPayloadRpc(abilityMap[ability], ability.currentID, cameraPos, cameraForward, NetworkManager.ServerTime, payload);
                else
                    EndCastAbilityRpc(abilityMap[ability], ability.currentID, cameraPos, cameraForward, NetworkManager.ServerTime);
            }
            currentlyCasting = null;
        }
        [Rpc(SendTo.Server)]
        private void EndCastAbilityRpc(uint id, uint effectID, Vector3 cameraPos, Vector3 cameraForward, NetworkTime time)
        {
            Ability ability = idMap[id];
            if (currentlyCasting != ability)
            { 
                EndCastFailedRpc(id);
                return;
            }

            bool success = ability.ServerCastEnd(null, playerCamera.transform.forward, transform.position, playerCamera.transform.position, myState, out AbilityPayload payload);
            if (success)
            {
                if (payload != null)
                    EndAbilityCastPayloadRpc(abilityMap[ability], ability.currentID, time, payload);
                else
                    EndAbilityCastRpc(abilityMap[ability], ability.currentID, time);
            }
            else
                EndCastFailedRpc(id);
            currentlyCasting = null;
        }

        [Rpc(SendTo.Server)]
        private void EndCastAbilityPayloadRpc(uint id, uint effectID, Vector3 cameraPos, Vector3 cameraForward, NetworkTime time, AbilityNetworkPayload payloadIn)
        {
            Ability ability = idMap[id];
            if (currentlyCasting != ability)
            {
                EndCastFailedRpc(id);
                return;
            }

            bool success = ability.ServerCastEnd(payloadIn, playerCamera.transform.forward, transform.position, playerCamera.transform.position, myState, out AbilityPayload payload);
            if (success)
            {
                if (payload != null)
                    EndAbilityCastPayloadRpc(abilityMap[ability], ability.currentID, time, payload);
                else
                    EndAbilityCastRpc(abilityMap[ability], ability.currentID, time);
            }
            else
                EndCastFailedRpc(id);
            currentlyCasting = null;
        }

        [Rpc(SendTo.Owner)]
        private void EndCastFailedRpc(uint id)
        {
            throw new NotImplementedException();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void EndAbilityCastRpc(uint id, uint effectID, NetworkTime time)
        {
            Ability ability = idMap[id];
            if (IsOwner)
            {
                //TODO: actually handle this case
                if (ability.currentID != effectID)
                    throw new Exception("ohno");
            }
            else
            {
                ability.targets[(int)effectID].GetEffect().ClientCastStart(null);
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void EndAbilityCastPayloadRpc(uint id, uint effectID, NetworkTime time, AbilityNetworkPayload payload)
        {
            Ability ability = idMap[id];
            if (IsOwner)
            {
                //TODO: actually handle this case
                if (ability.currentID != effectID)
                    throw new Exception("ohno");
            }
            else
            {
                ability.targets[(int)effectID].GetEffect().ClientCastStart(payload);
            }
        }

        #endregion

        private void CastAbility1(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            CastAbility(ability1);
        }

        private void EndCastAbility1(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            EndCast(ability1);
        }
    }
}
