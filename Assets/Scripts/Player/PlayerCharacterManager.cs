using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

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
            so.Apply(GetComponent<PlayerState>());

            if (IsOwner)
            {
                GameObject.FindWithTag("MainCamera").GetComponent<CameraManager>().SetTarget(myModel.transform.Find("CameraTarget").gameObject);
                GetComponent<CharacterController>().enabled = true;
                gameObject.AddComponent<PlayerMovementController>();
            }
            else
            {

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
