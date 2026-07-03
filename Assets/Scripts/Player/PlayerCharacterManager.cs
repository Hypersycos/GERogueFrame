using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using static Hypersycos.GERogueFrame.BasePCharacterSO;

namespace Hypersycos.GERogueFrame
{
    //[RequireComponent(typeof(NetworkAnimator))]
    public class PlayerCharacterManager : NetworkBehaviour
    {
        public int characterID;
        public BasePCharacterSO so;
        public NetworkAnimator netAnimator;
        public CharacterController controller;
        public Collider nonOwnerCollider;
        public Transform bar;

        private void Reset()
        {
            netAnimator = GetComponent<NetworkAnimator>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            List<BoundedStatInstance> defenses = new();
            so.Apply(GetComponent<PlayerState>(), ref defenses);

            GameObject cameraTarget = transform.Find("CameraTarget").gameObject;

            if (IsOwner)
            {
                nonOwnerCollider.enabled = false;
                controller.enabled = true;
                gameObject.GetComponent<PlayerMovementController>().enabled = true;
                gameObject.AddComponent<PlayerAnimatorScript>();
                Destroy(bar.gameObject);

                GameObject.FindWithTag("MainCamera").GetComponent<CameraManager>().SetMyCamera(GetComponent<PlayerState>());

                PersistentStateManager.Singleton.mapState.so.generator.ModifyPlayerOnOwner(gameObject);
            }
            else
            {
                bar.gameObject.SetActive(true);
                bar.GetComponentInChildren<StatBarScript>().SetStats(defenses);
            }

            PersistentStateManager.Singleton.mapState.so.generator.ModifyPlayer(gameObject);
        }
    }
}
