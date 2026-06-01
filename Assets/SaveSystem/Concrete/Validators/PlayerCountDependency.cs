using System;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "PlayerCountDependency", menuName = "SaveSystem/Validators/Player Count Dependency", order = 26)]
    class PlayerCountDependency : NumberDependenceSO<int>
    {
        public void CallListeners(int value) => CallListeners();
        public void OnLobbyStart()
        {
            NetworkManager.Singleton.OnConnectionEvent += CallListeners;
        }

        void CallListeners(NetworkManager manager, ConnectionEventData data)
        {
            if (manager.ConnectedClientsList.Count > 0)
                CallListeners();
        }

        protected override int Calculate() => NetworkManager.Singleton?.ConnectedClientsList.Count ?? 9999 * Multiply / Divide + Add;

        protected void OnEnable()
        {
            OnLobbyStart();
        }

        public override void Create()
        {
        }
    }
}
