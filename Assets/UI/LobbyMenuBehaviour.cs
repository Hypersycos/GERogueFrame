using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hypersycos.GERogueFrame
{
    public struct PlayerState : IEquatable<PlayerState>, INetworkSerializable
    {
        public ulong id;
        public bool isReady;
        public uint character;

        public PlayerState(ulong id)
        {
            this.id = id;
            isReady = false;
            character = 0;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerState state &&
                   id == state.id &&
                   isReady == state.isReady &&
                   character == state.character;
        }

        public bool Equals(PlayerState other)
        {
            return id == other.id &&
                   isReady == other.isReady &&
                   character == other.character;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(id, isReady);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue<ulong>(ref id);
            serializer.SerializeValue<bool>(ref isReady);
            serializer.SerializeValue<uint>(ref character);
        }
    }

    public class LobbyMenuBehaviour : NetworkBehaviour
    {
        NetworkManager networkManager;
        [SerializeField] UIDocument document;
        [SerializeField] VisualTreeAsset playerIcon;
        private void Reset()
        {
            document = GetComponent<UIDocument>();
        }

        NetworkVariable<List<PlayerState>> readyData = new();

        Button readyButton;
        ListView playerList;
        ListView characterList;

        bool myReady = false;
        uint myCharacter = 0;

        private void OnEnable()
        {
            VisualElement root = document.rootVisualElement;

            readyButton = root.Q<Button>("ReadyButton");
            playerList = root.Q<ListView>("PlayerList");
            characterList = root.Q<ListView>("CharacterList");

            readyButton.clicked += ReadyClicked;
        }
        private void ReadyClicked()
        {
            myReady = !myReady;
            SetReadyServerRpc(myReady);
            if (myReady)
            {
                readyButton.RemoveFromClassList("notReadyButton");
                readyButton.AddToClassList("readyButton");
                readyButton.text = "Ready";
            }
            else
            {
                readyButton.AddToClassList("notReadyButton");
                readyButton.RemoveFromClassList("readyButton");
                readyButton.text = "Not Ready";
            }
        }

        private void RefreshList(List<PlayerState> previousValue, List<PlayerState> newValue)
        {
            playerList.Clear();
            playerList.RefreshItems();
        }

        public override void OnNetworkSpawn()
        {
            networkManager = NetworkManager.Singleton;

            if (networkManager.IsHost)
            {
                readyData.Value = new();
                networkManager.OnClientConnectedCallback += CreatePlayerState;
                networkManager.OnClientDisconnectCallback += DeletePlayerState;
                foreach (ulong playerID in networkManager.ConnectedClientsIds)
                {
                    readyData.Value.Add(new PlayerState(playerID));
                }
                readyData.CheckDirtyState();
            }

            playerList.itemsSource = readyData.Value;
            playerList.makeItem = () => playerIcon.Instantiate();
            playerList.bindItem = (element, index) =>
            {
                PlayerState state = readyData.Value[index];
                VisualElement readyIcon = element.Q<VisualElement>("ReadyStatus");
                if (state.isReady)
                {
                    readyIcon.AddToClassList("playerReady");
                    readyIcon.RemoveFromClassList("playerNotReady");
                }
                else
                {
                    readyIcon.RemoveFromClassList("playerReady");
                    readyIcon.AddToClassList("playerNotReady");
                }
            };
            readyData.OnValueChanged += RefreshList;
            playerList.RefreshItems();
        }

        private void DeletePlayerState(ulong obj)
        {
            PlayerState? toRemove = null;
            foreach (PlayerState state in readyData.Value)
            {
                if (state.id == obj)
                {
                    toRemove = state;
                    break;
                }
            }
            if (toRemove != null)
            {
                readyData.Value.Remove(toRemove.Value);
                readyData.CheckDirtyState();
            }
        }

        private void CreatePlayerState(ulong obj)
        {
            readyData.Value.Add(new PlayerState(obj));
            readyData.CheckDirtyState();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void SetReadyServerRpc(bool ready, RpcParams rpcParams = default)
        {
            ulong senderID = rpcParams.Receive.SenderClientId;

            for (int i = 0; i < readyData.Value.Count; i++)
            {
                if (readyData.Value[i].id == senderID)
                {
                    var state = readyData.Value[i];
                    readyData.Value.RemoveAt(i);
                    state.isReady = ready;
                    readyData.Value.Insert(i, state);
                    readyData.CheckDirtyState(true);
                    break;
                }
            }
        }
    }
}