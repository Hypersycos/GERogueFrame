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

        private void Awake()
        {
            myState = GetComponent<PlayerState>();
            playerCamera = GameObject.FindGameObjectWithTag("MainCamera");

            controls = new();

            controls.Player.Enable();
            controls.Player.Ability1.started += CastAbility1;
        }

        private void CastAbility1(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            ability1.Cast(playerCamera.transform.forward, transform.position, playerCamera.transform.position, myState);
        }
    }
}
