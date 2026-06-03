using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    public class RoundHandler : NetworkBehaviour
    {
        [SerializeField] private NetworkObject playerPrefab;
        private List<ulong> loadingClients = new List<ulong>();
        private int clientCount = 0;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    loadingClients.Add(client.ClientId);
                }
            }
            clientCount = loadingClients.Count;

            if (IsClient)
            {
                ClientIsReadyServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void ClientIsReadyServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (!loadingClients.Contains(serverRpcParams.Receive.SenderClientId)) { return; }

            SpawnPlayer(serverRpcParams.Receive.SenderClientId);
            loadingClients.Remove(serverRpcParams.Receive.SenderClientId);
        }

        private void SpawnPlayer(ulong clientId)
        {
            NetworkObject player = Instantiate(playerPrefab, new Vector3(0, 5, 0), Quaternion.identity);
            player.name = clientId.ToString();
            player.SpawnAsPlayerObject(clientId, true);
        }
    }
}
