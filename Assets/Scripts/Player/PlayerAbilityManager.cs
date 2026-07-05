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
        public Ability weaponAlt { get => _weaponAlt; set { _weaponAlt = value; AssignAbility(value, controls.Player.Altfire, 1); } }

        public Ability ability1 { get => _ability1; set { _ability1 = value; AssignAbility(value, controls.Player.Ability1, 2); } }
        protected Ability _ability1;
        public Ability ability2 { get => _ability2; set { _ability2 = value; AssignAbility(value, controls.Player.Ability2, 3); } }
        protected Ability _ability2;
        public Ability ability3 { get => _ability3; set { _ability3 = value; AssignAbility(value, controls.Player.Ability3, 4); } }
        protected Ability _ability3;
        public Ability ability4 { get => _ability4; set { _ability4 = value; AssignAbility(value, controls.Player.Ability4, 5); } }
        protected Ability _ability4;

        protected Ability _ultimate;
        public Ability ultimate { get => _ultimate; set { _ultimate = value; AssignAbility(value, controls.Player.Ultimate, 6); } }

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
                actionMap[action] = new((_) => CastAbilityWrapper(ability, false), (_) => EndCastAbilityWrapper(ability));
                action.started += actionMap[action].Item1;
                action.canceled += actionMap[action].Item2;
                abilityMap.Add(ability, id);
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            foreach(var map in actionMap)
            {
                map.Key.started -= map.Value.Item1;
                map.Key.canceled -= map.Value.Item2;
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
                try
                {
                    currentlyCasting.CastingFixedUpdate(cameraForward, transform.position, cameraPos, myState);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Exception occured while running CastingFixedUpdate for currentCast {cameraForward}, {transform?.position}, {cameraPos}, {myState}");
                    Debug.LogException(e);
                }
            }

            foreach (Ability ability in abilityMap.Keys)
            {
                try
                {
                    ability.FixedUpdate(myState);
                    if (ability.IsDirty)
                    {
                        SyncAbilityRpc(abilityMap[ability], ability.Sync());
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Exception occured while running FixedUpdate for currentCast");
                    Debug.LogException(e);
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
        private void CastAbilityWrapper(Ability ability, bool isEnd)
        {
            try
            {
                CastAbility(ability, isEnd);
            }
            catch (Exception e)
            {
                currentlyCasting = null;
                Debug.LogError($"Exception occured casting ability");
                Debug.LogException(e);
            }
        }
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

        private void ServerCastAbilityWrapper(uint id, int effectID, double time, AbilityPayload verifyData, AbilityPayload abilityPayload)
        {
            try
            {
                ServerCastAbility(id, effectID, time, verifyData, abilityPayload);
            }
            catch (Exception e)
            {
                currentlyCasting = null;
                Debug.LogError($"Exception occured casting ability");
                Debug.LogException(e);
                CastFailedRpc(id);
            }
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
            ServerCastAbilityWrapper(id, effectID, time, null, null);
        }

        [Rpc(SendTo.Server)]
        private void CastAbilityRpc(uint id, int effectID, double time, AbilityNetworkPayload verifyData, AbilityNetworkPayload abilityPayload)
        {
            ServerCastAbilityWrapper(id, effectID, time, verifyData, abilityPayload);
        }

        [Rpc(SendTo.Server)]
        private void CastAbilityPayloadRpc(uint id, int effectID, double time, AbilityNetworkPayload abilityPayload)
        {
            ServerCastAbilityWrapper(id, effectID, time, null, abilityPayload);
        }

        [Rpc(SendTo.Server)]
        private void CastAbilityVerifyRpc(uint id, int effectID, double time, AbilityNetworkPayload verifyData)
        {
            ServerCastAbilityWrapper(id, effectID, time, verifyData, null);
        }

        [Rpc(SendTo.Owner)]
        private void CastFailedRpc(uint id)
        {
            currentlyCasting = null;
            throw new NotImplementedException();
        }

        private void AbilityCastWrapper(uint id, int effectID, double time, AbilityPayload payload)
        {
            try
            {
                AbilityCast(id, effectID, time, payload);
            }
            catch (Exception e)
            {
                currentlyCasting = null;
                Debug.LogError($"Exception occured casting ability");
                Debug.LogException(e);
            }
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
            AbilityCastWrapper(id, effectID, time, null);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AbilityCastPayloadRpc(uint id, int effectID, double time, AbilityNetworkPayload payload)
        {
            AbilityCastWrapper(id, effectID, time, payload);
        }
        #endregion

        #region EndCast
        private void EndCastAbilityWrapper(Ability ability)
        {
            try
            {
                EndCastAbility(ability);
            }
            catch (Exception e)
            {
                currentlyCasting = null;
                Debug.LogError($"Exception occured end-casting ability");
                Debug.LogException(e);
                EndCastAbilityRpc(NetworkManager.ServerTime.TickWithPartial);
            }
        }
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

        private void ServerEndCastAbilityWrapper(double time, AbilityPayload verifyData, AbilityPayload abilityPayload)
        {
            try
            {
                ServerEndCastAbility(time, verifyData, abilityPayload);
            }
            catch (Exception e)
            {
                EndCastFailedRpc(abilityMap[currentlyCasting]);
                currentlyCasting = null;
                Debug.LogError($"Exception occured end-casting ability");
                Debug.LogException(e);
            }
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
            ServerEndCastAbilityWrapper(time, null, null);
        }

        [Rpc(SendTo.Server)]
        private void EndCastAbilityRpc(double time, AbilityNetworkPayload verifyData, AbilityNetworkPayload abilityPayload)
        {
            ServerEndCastAbilityWrapper(time, verifyData, abilityPayload);
        }

        [Rpc(SendTo.Server)]
        private void EndCastAbilityPayloadRpc(double time, AbilityNetworkPayload abilityPayload)
        {
            ServerEndCastAbilityWrapper(time, null, abilityPayload);
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

        private void AbilityCastEndWrapper(double time, AbilityPayload payload)
        {
            try
            {
                AbilityCastEnd(time, payload);
            }
            catch (Exception e)
            {
                currentlyCasting = null;
                Debug.LogError($"Exception occured end-casting ability");
                Debug.LogException(e);
            }
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
            AbilityCastEndWrapper(time, null);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AbilityCastEndPayloadRpc(double time, AbilityNetworkPayload payload)
        {
            AbilityCastEndWrapper(time, payload);
        }
        #endregion
    }
}
