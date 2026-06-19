using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using static Hypersycos.GERogueFrame.BaseCharacterSO;

namespace Hypersycos.GERogueFrame
{
    //[RequireComponent(typeof(NetworkAnimator))]
    public class PlayerCharacterManager : NetworkBehaviour
    {
        public uint characterID;
        public BaseCharacterSO so;
        public NetworkAnimator netAnimator;
        public CharacterController controller;
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
                controller.enabled = true;
                gameObject.AddComponent<PlayerMovementController>().movementSpeed = so.Speed;
                gameObject.AddComponent<PlayerAnimatorScript>();
                Destroy(bar.gameObject);

                var cam = transform.Find("CameraPos").gameObject.AddComponent<PlayerCameraManager>();
                cam.SetTarget(transform.Find("CameraTarget").gameObject);

                GameObject.FindWithTag("MainCamera").GetComponent<CameraManager>().SetCam(cam.transform);
            }
            else
            {
                bar.gameObject.SetActive(true);
                bar.GetComponentInChildren<StatBarScript>().SetStats(defenses);
            }
        }
    }
}
