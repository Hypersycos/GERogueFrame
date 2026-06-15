using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Hypersycos.Utils;

namespace Hypersycos.GERogueFrame
{
    class PlayerAbilityManager : NetworkBehaviour
    {
        protected Ability _weapon;
        public Ability weapon { get => _weapon; set { _weapon = value; AssignAbility(value, controls.Player.Fire, 0); } }
        protected Ability _weaponAlt;
        public Ability weaponAlt { get => _weapon; set { _weaponAlt = value; AssignAbility(value, controls.Player.Altfire, 1); } }

        public Ability ability1 { get => _weapon; set { _ability1 = value; AssignAbility(value, controls.Player.Ability1, 2); } }
        protected Ability _ability1;
        public Ability ability2 { get => _weapon; set { _ability2 = value; AssignAbility(value, controls.Player.Ability2, 3); } }
        protected Ability _ability2;
        public Ability ability3 { get => _weapon; set { _ability3 = value; AssignAbility(value, controls.Player.Ability3, 4); } }
        protected Ability _ability3;
        public Ability ability4 { get => _weapon; set { _ability4 = value; AssignAbility(value, controls.Player.Ability4, 5); } }
        protected Ability _ability4;

        protected Ability _ultimate;
        public Ability ultimate { get => _weapon; set { _ultimate = value; AssignAbility(value, controls.Player.Ultimate, 6); } }

        PlayerState myState;
        Controls controls;
        GameObject playerCamera;

        Ability currentlyCasting = null;

        TwoWayDictionary<Ability, uint> abilityMap = new();

        Dictionary<InputAction, Tuple<Action<InputAction.CallbackContext>, Action<InputAction.CallbackContext>>> actionMap = new();

        private void Awake()
        {
            myState = GetComponent<PlayerState>();
            playerCamera = GameObject.FindGameObjectWithTag("MainCamera");
            
            controls = new();
            controls.Player.Enable();
        }

        private void AssignAbility(Ability ability, InputAction action, uint id)
        {
            if (actionMap.TryGetValue(action, out var actionPair))
            {
                action.started -= actionPair.Item1;
                action.canceled -= actionPair.Item2;
                abilityMap.Remove(id);
            }
            if (ability != null)
            {
                actionMap[action] = new((_) => CastAbility(ability), (_) => EndCast(ability));
                action.started += actionMap[action].Item1;
                action.canceled += actionMap[action].Item2;
                abilityMap.Add(ability, id);
            }
        }

        private void Update()
        {
            if (currentlyCasting != null)
            {
                if (IsOwner)
                    currentlyCasting.currentEffect.OwnerCastUpdate();
                if (IsServer)
                    currentlyCasting.currentEffect.ServerCastUpdate();
                if (IsClient)
                    currentlyCasting.currentEffect.ClientCastUpdate();
            }

            foreach (Ability ability in abilityMap.Keys)
            {
                ability.Update(myState);
            }
        }

        private void FixedUpdate()
        {
            if (currentlyCasting != null)
            {
                if (IsOwner)
                    currentlyCasting.currentEffect.OwnerCastFixedUpdate();
                if (IsServer)
                    currentlyCasting.currentEffect.ServerCastFixedUpdate();
                if (IsClient)
                    currentlyCasting.currentEffect.ClientCastFixedUpdate();
            }

            foreach (Ability ability in abilityMap.Keys)
            {
                ability.FixedUpdate(myState);
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
                            AbilityCastPayloadRpc(abilityMap[ability], ability.currentID, NetworkManager.ServerTime.TickWithPartial, payload);
                        else
                            AbilityCastRpc(abilityMap[ability], ability.currentID, NetworkManager.ServerTime.TickWithPartial);
                    }
                }
                else
                {
                    if (payload != null)
                        CastAbilityPayloadRpc(abilityMap[ability], ability.currentID, cameraPos, cameraForward, NetworkManager.ServerTime.TickWithPartial, payload);
                    else
                        CastAbilityRpc(abilityMap[ability], ability.currentID, cameraPos, cameraForward, NetworkManager.ServerTime.TickWithPartial);
                }
            }
            else
                currentlyCasting = null;
        }

        [Rpc(SendTo.Server)]
        private void CastAbilityRpc(uint id, uint effectID, Vector3 cameraPos, Vector3 cameraForward, double time)
        {
            Ability ability = abilityMap[id];
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
        private void CastAbilityPayloadRpc(uint id, uint effectID, Vector3 cameraPos, Vector3 cameraForward, double time, AbilityNetworkPayload payloadIn)
        {
            Ability ability = abilityMap[id];
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
        private void AbilityCastRpc(uint id, uint effectID, double time)
        {
            Ability ability = abilityMap[id];
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
        private void AbilityCastPayloadRpc(uint id, uint effectID, double time, AbilityNetworkPayload payload)
        {
            Ability ability = abilityMap[id];
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
                    success = ability.ServerCastEnd(payload, cameraForward, transform.position, cameraPos, myState, out payload);
                    if (success)
                    {
                        if (payload != null)
                            EndAbilityCastPayloadRpc(abilityMap[ability], ability.currentID, NetworkManager.ServerTime.TickWithPartial, payload);
                        else
                            EndAbilityCastRpc(abilityMap[ability], ability.currentID, NetworkManager.ServerTime.TickWithPartial);
                    }
                }
                if (payload != null)
                    EndCastAbilityPayloadRpc(abilityMap[ability], ability.currentID, cameraPos, cameraForward, NetworkManager.ServerTime.TickWithPartial, payload);
                else
                    EndCastAbilityRpc(abilityMap[ability], ability.currentID, cameraPos, cameraForward, NetworkManager.ServerTime.TickWithPartial);
            }
            currentlyCasting = null;
        }
        [Rpc(SendTo.Server)]
        private void EndCastAbilityRpc(uint id, uint effectID, Vector3 cameraPos, Vector3 cameraForward, double time)
        {
            Ability ability = abilityMap[id];
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
        private void EndCastAbilityPayloadRpc(uint id, uint effectID, Vector3 cameraPos, Vector3 cameraForward, double time, AbilityNetworkPayload payloadIn)
        {
            Ability ability = abilityMap[id];
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
        private void EndAbilityCastRpc(uint id, uint effectID, double time)
        {
            Ability ability = abilityMap[id];
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
        private void EndAbilityCastPayloadRpc(uint id, uint effectID, double time, AbilityNetworkPayload payload)
        {
            Ability ability = abilityMap[id];
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
    }
}
