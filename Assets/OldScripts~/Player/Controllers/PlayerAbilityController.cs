using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hypersycos.RogueFrame
{
    public class PlayerAbilityController : NetworkBehaviour
    {
        //TODO: Replace latency hiding logic
        private Controls ControlAsset;
        [SerializeField] private new Transform camera;
        [SerializeField] private Transform cameraRoot;
        private bool castOnSelect = true;
        private ushort currentAbility = 0;
        private ushort lastCastAbility = 0;
        private Quaternion? lastCastRotation = null;
        private Vector3? lastCastOffset = null;
        private double clientCastLockout = 0;
        private double serverCastLockout = 0;
        [SerializeField] private List<Ability> abilities = new();
        [SerializeField] private PlayerState playerState;
        private float castSpeed = 1; //TODO: replace with generic stat
        public override void OnNetworkSpawn()
        {
            if (IsLocalPlayer)
            {
                ControlAsset = GetComponent<PlayerMovementController>().ControlAsset;
                ControlAsset.Player.Ability1.started += SetAbility;
                ControlAsset.Player.Ability2.started += SetAbility;
                ControlAsset.Player.Ability3.started += SetAbility;
                ControlAsset.Player.Ability4.started += SetAbility;
                ControlAsset.Player.Ultimate.started += CastUltimate;
                ControlAsset.Player.ChangeAbility.started += NextAbility;
                ControlAsset.Player.UseAbility.started += CastAbility;
                camera = GameObject.FindGameObjectWithTag("MainCamera").transform;
            }
            cameraRoot = transform.GetChild(0);
        }

        public override void OnNetworkDespawn()
        {
            if (IsLocalPlayer)
            {
                ControlAsset.Player.Ability1.started -= SetAbility;
                ControlAsset.Player.Ability2.started -= SetAbility;
                ControlAsset.Player.Ability3.started -= SetAbility;
                ControlAsset.Player.Ability4.started -= SetAbility;
                ControlAsset.Player.Ultimate.started -= CastUltimate;
                ControlAsset.Player.ChangeAbility.started -= NextAbility;
                ControlAsset.Player.UseAbility.started -= CastAbility;
            }
        }

        private void CastAbility(InputAction.CallbackContext obj)
        { //If not still in casting animation
            if (clientCastLockout >= 0 && clientCastLockout < NetworkManager.ServerTime.Time)
            { //Lock casting
                clientCastLockout = -1;
                CastServerRpc(currentAbility, camera.rotation, camera.position - cameraRoot.position);
                Ability selected = abilities[currentAbility];
                //store current ability for delayed casts
                lastCastAbility = currentAbility;
                if (selected.CastTime > 0)
                {
                    StartCoroutine(ClientDelayedCast(selected.CastTime));
                }
            }
        }

        IEnumerator ClientDelayedCast(double waitTime)
        {
            while (waitTime > 0f)
            {
                waitTime -= Time.fixedDeltaTime * castSpeed;
                yield return new WaitForFixedUpdate();
            }
            //Send player's camera rotation to server when delayed cast should happen
            DelayedCastServerRpc(camera.rotation);
        }

        [ClientRpc] 
        private void CastResultClientRpc(double lockout, double castDelay)
        {
            clientCastLockout = lockout;
        }

        [ServerRpc]
        private void CastServerRpc(ushort abilityIndex, Quaternion lookDirection, Vector3 cameraOffset)
        {
            if (serverCastLockout >= 0 && serverCastLockout < NetworkManager.ServerTime.Time)
            {
                serverCastLockout = -1;
            }
            else if (serverCastLockout == -1)
            {
                return;
            }
            else
            { //Server thinks still locked in casting animation, sends to client
                CastResultClientRpc(serverCastLockout, -1);
                return;
            }

            //Reset stored values
            lastCastOffset = null;
            lastCastRotation = null;

            Ability ability = abilities[abilityIndex];
            //Attempt to charge player for ability
            if (ability.CastCost(playerState))
            {
                Vector3 cameraPosition = cameraRoot.position + cameraOffset;
                //create immediate effects
                ability.CastEffect(cameraPosition, lookDirection, playerState);
                //set lockout to animation time and share with client
                serverCastLockout = NetworkManager.ServerTime.Time + ability.AnimationTime;
                CastResultClientRpc(serverCastLockout, ability.CastTime);
                //if ability has a delayed effect then start delayed cast
                if (ability.CastTime > 0)
                {
                    StartCoroutine(ServerDelayedCast(NetworkManager.ServerTime.Time + ability.CastTime, lookDirection, cameraOffset));
                }
                lastCastAbility = abilityIndex;
            }
            else
            {
                //Inform client cast failed & reset cast lockout
                CastResultClientRpc(0, -1);
                serverCastLockout = 0;
            }
        }

        [ServerRpc]
        private void DelayedCastServerRpc(Quaternion lookDirection)
        { //send delayed camera angle to server
            lastCastRotation = lookDirection;
        }

        IEnumerator ServerDelayedCast(double expectedCastTime, Quaternion oldLookDirection, Vector3 oldOffset)
        {
            while (NetworkManager.ServerTime.Time < expectedCastTime)
            {
                if (castSpeed != 1)
                { //account for cast speed per frame. Allows variable cast speed
                    //and minimum computation in the usual case (1)
                    expectedCastTime += (1 - castSpeed) * Time.fixedDeltaTime;
                }
                yield return new WaitForFixedUpdate();
            }
            //wait up to 250ms if haven't received new rotation
            float timeout = 0.25f;
            while (lastCastRotation == null && timeout > 0 )
            {
                timeout -= Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            Ability delayedAbility = abilities[lastCastAbility];
            //Use original cast state if new values not arrived
            Quaternion lookDirection = lastCastRotation ?? oldLookDirection;
            Vector3 cameraOffset = lastCastOffset ?? oldOffset;
            Vector3 cameraPosition = cameraRoot.position + cameraOffset;
            delayedAbility.DelayedCastEffect(cameraPosition, lookDirection, playerState);
        }

        private void NextAbility(InputAction.CallbackContext obj)
        {
            currentAbility += 1;
            if (currentAbility > 3)
            {
                currentAbility = 0;
            }
        }

        private void CastUltimate(InputAction.CallbackContext obj)
        {
            CastServerRpc(4, camera.rotation, camera.position - cameraRoot.position);
        }

        private void SetAbility(InputAction.CallbackContext obj)
        {
            switch(obj.action.name)
            {
                case "Ability 1":
                    currentAbility = 0;
                    break;
                case "Ability 2":
                    currentAbility = 1;
                    break;
                case "Ability 3":
                    currentAbility = 2;
                    break;
                case "Ability 4":
                    currentAbility = 3;
                    break;
            }
            if (castOnSelect)
            {
                CastAbility(obj);
            }
        }
    }
}