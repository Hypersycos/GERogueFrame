using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hypersycos.GERogueFrame
{
    public class TestDamageScript : MonoBehaviour
    {
        Controls controls;
        EnemyState enemyState;
        PlayerState pState;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            controls = new();
            enemyState = GetComponent<EnemyState>();

            //controls.Player.Fire.started += DoDamage;
            //controls.Player.Altfire.started += DoDamageRandom;
            controls.Player.Enable();
        }

        private void DoDamage(InputAction.CallbackContext context)
        {
            if (pState == null)
            {
                pState = NetworkManager.Singleton.ConnectedClientsList[0].PlayerObject.GetComponent<PlayerState>();
            }
            enemyState.ApplyDamageInstance(new DamageInstance(true, 20, pState, StatTypeTarget.AllValid));
        }

        private void DoDamageRandom(InputAction.CallbackContext context)
        {
            PlayerState state = NetworkManager.Singleton.ConnectedClientsList[UnityEngine.Random.Range(0, NetworkManager.Singleton.ConnectedClientsList.Count)].PlayerObject.GetComponent<PlayerState>();
            enemyState.ApplyDamageInstance(new DamageInstance(true, 20, state, StatTypeTarget.AllValid));
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
