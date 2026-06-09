using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using static Hypersycos.GERogueFrame.BaseCharacterSO;

namespace Hypersycos.GERogueFrame
{
    //[RequireComponent(typeof(NetworkAnimator))]
    public class PlayerCharacterManager : NetworkBehaviour
    {
        public NetworkVariable<uint> characterID = new(uint.MaxValue);
        [SerializeField] NetworkAnimator netAnimator;
        GameObject myModel;

        private void Reset()
        {
            netAnimator = GetComponent<NetworkAnimator>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            characterID.OnValueChanged += SpawnCharacterModel;
            SpawnCharacterModel();
        }

        private void SpawnCharacterModel(uint previousValue, uint newValue)
        {
            SpawnCharacterModel();
        }

        public void SpawnCharacterModel()
        {
            if (characterID.Value >= PersistentStateManager.Singleton.availableCharacters.Count || characterID.Value < 0)
                return;

            if (myModel != null)
            {
                Destroy(myModel);
            }

            BaseCharacterSO so = PersistentStateManager.Singleton.availableCharacters[(int)characterID.Value];
            GameObject model = so.Model;

            myModel = Instantiate(model, transform);
            List<BoundedStatInstance> defenses = new();
            so.Apply(GetComponent<PlayerState>(), ref defenses);

            GameObject cameraTarget = myModel.transform.Find("CameraTarget").gameObject;

            if (IsOwner)
            {
                GameObject.FindWithTag("MainCamera").GetComponent<CameraManager>().SetTarget(cameraTarget);
                GetComponent<CharacterController>().enabled = true;
                gameObject.AddComponent<PlayerMovementController>();
            }
            else
            {
                Transform bar = transform.GetChild(0);
                bar.SetParent(cameraTarget.transform);
                bar.transform.localPosition = new Vector3(0, 0.5f, 0);
                bar.gameObject.SetActive(true);
                bar.GetComponentInChildren<StatBarScript>().SetStats(defenses);
            }

            return;

            Animator animator = myModel.GetComponent<Animator>();
            if (animator != null)
                netAnimator.Animator = animator;
            else
                netAnimator.Animator = null;
            netAnimator.enabled = netAnimator.Animator != null;
        }
    }
}
