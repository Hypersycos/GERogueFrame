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
        ControlsWrapper controlWrapper;
        Controls controls => controlWrapper.controls;
        GameObject playerCamera;

        Ability currentlyCasting = null;

        TwoWayDictionary<Ability, uint> abilityMap = new();

        Dictionary<InputAction, Tuple<Action<InputAction.CallbackContext>, Action<InputAction.CallbackContext>>> actionMap = new();

        private void Awake()
        {
            myState = GetComponent<PlayerState>();
            if (IsOwner)
                playerCamera = GameObject.FindGameObjectWithTag("MainCamera");
            else
                playerCamera = transform.Find("CameraPos").gameObject;

            controlWrapper = ControlsWrapper.Singleton;
        }

        private void AssignAbility(Ability ability, InputAction action, uint id)
        {
            if (!IsOwner)
            {
                abilityMap.Remove(id);
                if (ability != null)
                    abilityMap.Add(ability, id);
                return;
            }

            if (actionMap.TryGetValue(action, out var actionPair))
            {
                action.started -= actionPair.Item1;
                action.canceled -= actionPair.Item2;
                abilityMap.Remove(id);
            }
            if (ability != null)
            {
                actionMap[action] = new((_) => CastAbility(ability, false), (_) => EndCastAbility(ability));
                action.started += actionMap[action].Item1;
                action.canceled += actionMap[action].Item2;
                abilityMap.Add(ability, id);
            }
        }

        private void Update()
        {
            if (currentlyCasting != null)
            {
                Vector3 cameraForward = playerCamera.transform.forward;
                Vector3 cameraPos = playerCamera.transform.position;
                currentlyCasting.CastingUpdate(cameraForward, transform.position, cameraPos, myState);
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
                Vector3 cameraForward = playerCamera.transform.forward;
                Vector3 cameraPos = playerCamera.transform.position;
                currentlyCasting.CastingFixedUpdate(cameraForward, transform.position, cameraPos, myState);
            }

            foreach (Ability ability in abilityMap.Keys)
            {
                ability.FixedUpdate(myState);
                if (ability.IsDirty)
                {
                    SyncAbilityRpc(abilityMap[ability], ability.Sync());
                }
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SyncAbilityRpc(uint id, AbilityNetworkPayload payload)
        {
            Ability ability = abilityMap[id];
            ability.SyncClient(payload);
            if (IsOwner && ability.HasOwnerSync)
                ability.SyncOwner(payload);
        }

        #region CastAbility
        private void CastAbility(Ability ability, bool isEnd)
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

            Vector3 cameraForward = playerCamera.transform.forward;
            Vector3 cameraPos = playerCamera.transform.position;
            bool success = ability.OwnerCast(cameraForward, transform.position, cameraPos, myState, out int chosenEffect, out AbilityPayload verifyData, out AbilityPayload abilityPayload);
            if (success)
            {
                if (IsHost)
                {
                    ServerCastAbility(abilityMap[ability], chosenEffect, NetworkManager.ServerTime.TickWithPartial, verifyData, abilityPayload);
                }
                else
                {
                    if (abilityPayload != null)
                        if (verifyData != null)
                            CastAbilityRpc(abilityMap[ability], chosenEffect, NetworkManager.ServerTime.TickWithPartial, verifyData, abilityPayload);
                        else
                            CastAbilityPayloadRpc(abilityMap[ability], chosenEffect, NetworkManager.ServerTime.TickWithPartial, abilityPayload);
                    else
                        if (verifyData != null)
                            CastAbilityVerifyRpc(abilityMap[ability], chosenEffect, NetworkManager.ServerTime.TickWithPartial, verifyData);
                        else
                            CastAbilityRpc(abilityMap[ability], chosenEffect, NetworkManager.ServerTime.TickWithPartial);
                }
            }
            
            if (!success || isEnd)
                currentlyCasting = null;
        }

        private void ServerCastAbility(uint id, int effectID, double time, AbilityPayload verifyData, AbilityPayload abilityPayload)
        {
            Ability ability = abilityMap[id];
            if (currentlyCasting == null)
                currentlyCasting = ability;
            else if (!IsHost || currentlyCasting != ability)
            {
                CastFailedRpc(id);
                return;
            }

            bool success = ability.ServerCast(effectID, verifyData, abilityPayload, myState, out int chosenEffect, out AbilityPayload payload);
            if (success)
            {
                if (payload != null)
                    AbilityCastPayloadRpc(abilityMap[ability], chosenEffect, time, payload);
                else
                    AbilityCastRpc(abilityMap[ability], chosenEffect, time);

                if (!ability.chargeAtStart)
                    currentlyCasting = null;
            }
            else
            {
                currentlyCasting = null;
                CastFailedRpc(id);
            }
        }

        [Rpc(SendTo.Server)]
        private void CastAbilityRpc(uint id, int effectID, double time)
        {
            ServerCastAbility(id, effectID, time, null, null);
        }

        [Rpc(SendTo.Server)]
        private void CastAbilityRpc(uint id, int effectID, double time, AbilityNetworkPayload verifyData, AbilityNetworkPayload abilityPayload)
        {
            ServerCastAbility(id, effectID, time, verifyData, abilityPayload);
        }

        [Rpc(SendTo.Server)]
        private void CastAbilityPayloadRpc(uint id, int effectID, double time, AbilityNetworkPayload abilityPayload)
        {
            ServerCastAbility(id, effectID, time, null, abilityPayload);
        }

        [Rpc(SendTo.Server)]
        private void CastAbilityVerifyRpc(uint id, int effectID, double time, AbilityNetworkPayload verifyData)
        {
            ServerCastAbility(id, effectID, time, verifyData, null);
        }

        [Rpc(SendTo.Owner)]
        private void CastFailedRpc(uint id)
        {
            currentlyCasting = null;
            throw new NotImplementedException();
        }

        private void AbilityCast(uint id, int effectID, double time, AbilityPayload payload)
        {
            Ability ability = abilityMap[id];
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
        private void EndCastAbility(Ability ability)
        {
            if (!ability.chargeAtStart && currentlyCasting == null)
            {
                CastAbility(ability, true);
                return;
            }

            if (currentlyCasting != ability)
                return;

            Vector3 cameraForward = playerCamera.transform.forward;
            Vector3 cameraPos = playerCamera.transform.position;
            bool success = ability.OwnerCastEnd(cameraForward, transform.position, cameraPos, myState, out AbilityPayload verifyData, out AbilityPayload abilityPayload);
            if (success)
            {
                if (IsHost)
                {
                    ServerEndCastAbility(NetworkManager.ServerTime.TickWithPartial, verifyData, abilityPayload);
                }
                else
                {
                    if (abilityPayload != null)
                        if (verifyData != null)
                            EndCastAbilityRpc(NetworkManager.ServerTime.TickWithPartial, verifyData, abilityPayload);
                        else
                            EndCastAbilityPayloadRpc(NetworkManager.ServerTime.TickWithPartial, abilityPayload);
                    else
                        if (verifyData != null)
                            EndCastAbilityVerifyRpc(NetworkManager.ServerTime.TickWithPartial, verifyData);
                        else
                            EndCastAbilityRpc(NetworkManager.ServerTime.TickWithPartial);
                }
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
                EndCastFailedRpc(abilityMap[ability]);
            }

            if (!IsHost)
                currentlyCasting = null;
        }

        [Rpc(SendTo.Server)]
        private void EndCastAbilityRpc(double time)
        {
            ServerEndCastAbility(time, null, null);
        }

        [Rpc(SendTo.Server)]
        private void EndCastAbilityRpc(double time, AbilityNetworkPayload verifyData, AbilityNetworkPayload abilityPayload)
        {
            ServerEndCastAbility(time, verifyData, abilityPayload);
        }

        [Rpc(SendTo.Server)]
        private void EndCastAbilityPayloadRpc(double time, AbilityNetworkPayload abilityPayload)
        {
            ServerEndCastAbility(time, null, abilityPayload);
        }

        [Rpc(SendTo.Server)]
        private void EndCastAbilityVerifyRpc(double time, AbilityNetworkPayload verifyData)
        {
            ServerEndCastAbility(time, verifyData, null);
        }

        [Rpc(SendTo.Owner)]
        private void EndCastFailedRpc(uint id)
        {
            currentlyCasting = null;
            throw new NotImplementedException();
        }

        private void AbilityCastEnd(double time, AbilityPayload payload)
        {
            var ability = currentlyCasting;
            ability.ClientCastEnd(payload, myState);
            currentlyCasting = null;
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
