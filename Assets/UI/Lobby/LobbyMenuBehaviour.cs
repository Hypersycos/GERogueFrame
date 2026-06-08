using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hypersycos.GERogueFrame
{
    public class LobbyMenuBehaviour : NetworkBehaviour
    {
        private struct LobbyPlayerState : IEquatable<LobbyPlayerState>, INetworkSerializable
        {
            public ulong id;
            public bool isReady;
            public uint characterID;

            public LobbyPlayerState(ulong id)
            {
                this.id = id;
                isReady = false;
                characterID = uint.MaxValue;
            }

            public override bool Equals(object obj)
            {
                return obj is LobbyPlayerState state &&
                       id == state.id &&
                       isReady == state.isReady &&
                       characterID == state.characterID;
            }

            public bool Equals(LobbyPlayerState other)
            {
                return id == other.id &&
                       isReady == other.isReady &&
                       characterID == other.characterID;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(id, isReady);
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref id);
                serializer.SerializeValue(ref isReady);
                serializer.SerializeValue(ref characterID);
            }
        }

        NetworkManager networkManager;
        [SerializeField] UIDocument document;
        [SerializeField] VisualTreeAsset playerIcon;
        [SerializeField] VisualTreeAsset characterSelectButton;
        [SerializeField] Transform spawnTransform;
        private void Reset()
        {
            document = GetComponent<UIDocument>();
        }

        BetterNetworkList<KeyIndexPair<ulong>> readyKeys = new();
        BetterNetworkList<LobbyPlayerState> readyValues = new();
        NetworkDict<ulong, LobbyPlayerState> readyData;
        int readyCount = 0;
        bool countdownCanStop = true;

        Button readyButton;
        ListView playerList;
        ScrollView characterList;
        Label countdown;
        Button backButton;

        GameObject myCharacterObj;
        Dictionary<LobbyPlayerState, GameObject> characterObjs;

        private void OnEnable()
        {
            VisualElement root = document.rootVisualElement;

            readyButton = root.Q<Button>("ReadyButton");
            playerList = root.Q<ListView>("PlayerList");
            characterList = root.Q<ScrollView>("CharacterList");
            countdown = root.Q<Label>("Countdown");
            backButton = root.Q<Button>("BackButton");

            readyButton.clicked += ReadyClicked;
            readyValues.OnListChanged += OnReadyDataChanged;
        }

        private void OnReadyDataChanged(NetworkListEvent<LobbyPlayerState> changeEvent)
        {
            if (changeEvent.Type == NetworkListEvent<LobbyPlayerState>.EventType.Value)
            {
                if (readyValues[changeEvent.Index].id == networkManager.LocalClientId)
                {
                    if (changeEvent.Value.isReady)
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
            }
        }

        private void ReadyClicked()
        {
            SetReadyServerRpc(!readyData[networkManager.LocalClientId].isReady);
        }

        private void RefreshList(NetworkListEvent<LobbyPlayerState> changeEvent)
        {
            //playerList.Clear();
            playerList.RefreshItems();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            networkManager = NetworkManager.Singleton;

            readyData = new(readyKeys, readyValues, !networkManager.IsServer);

            if (networkManager.IsServer)
            {
                networkManager.OnConnectionEvent += HandlePlayerConnection;
                foreach (ulong playerID in networkManager.ConnectedClientsIds)
                {
                    readyData.Add(playerID, new LobbyPlayerState(playerID));
                }
            }

            playerList.itemsSource = readyValues;
            playerList.makeItem = () => playerIcon.Instantiate();
            playerList.bindItem = (element, index) =>
            {
                LobbyPlayerState state = readyValues[index];
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
            readyValues.OnListChanged += RefreshList;
            playerList.RefreshItems();

            StartCoroutine(CreateCharacterIcons());
            backButton.clicked += PersistentStateManager.SingletonQuitToMenu;
        }

        public IEnumerator CreateCharacterIcons()
        {
            while (!PersistentStateManager.Singleton.IsSpawned)
            {
                yield return null;
            }
            characterList.Clear();
            foreach (BaseCharacterSO so in PersistentStateManager.Singleton.availableCharacters)
            {
                var template = characterSelectButton.Instantiate();
                Button btn = template.Q<Button>();
                btn.style.backgroundImage = so.Icon;
                //element.style.backgroundColor;

                btn.clicked += () => SelectCharacter(so, btn);
                characterList.Add(template);
            }
        }

        private void HandlePlayerConnection(NetworkManager manager, ConnectionEventData data)
        {
            if (manager.ShutdownInProgress)
                return;
            switch (data.EventType)
            {
                case ConnectionEvent.ClientConnected:
                    readyData.Add(data.ClientId, new LobbyPlayerState(data.ClientId));
                    StopCountdown();
                    break;
                case ConnectionEvent.PeerConnected:
                    break;
                case ConnectionEvent.ClientDisconnected:
                    if (readyData[data.ClientId].isReady)
                        readyCount--;
                    readyData.Remove(data.ClientId);
                    StopCountdown();
                    break;
                case ConnectionEvent.PeerDisconnected:
                    break;
                default:
                    break;
            }
        }

        private void SelectCharacter(BaseCharacterSO character, VisualElement visualElement)
        {
            if (myCharacterObj)
                Destroy(myCharacterObj);
            myCharacterObj = Instantiate(character.Model,
                                         spawnTransform.position + character.Model.transform.position,
                                         spawnTransform.rotation * character.Model.transform.rotation);
            SetCharacterRpc(character);
            readyButton.enabledSelf = true;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void SetCharacterRpc(BaseCharacterSO so, RpcParams rpcParams = default)
        {
            ulong senderID = rpcParams.Receive.SenderClientId;
            LobbyPlayerState state = readyData[senderID];
            state.characterID = PersistentStateManager.Singleton.GetCharacterID(so);
            readyData[senderID] = state;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void SetReadyServerRpc(bool ready, RpcParams rpcParams = default)
        {
            ulong senderID = rpcParams.Receive.SenderClientId;

            LobbyPlayerState state = readyData[senderID];
            bool oldReady = state.isReady;
            state.isReady = ready && state.characterID != uint.MaxValue;

            if (oldReady != state.isReady)
            {
                if (state.isReady)
                    readyCount++;
                else
                    readyCount--;
            }

            if (readyCount == readyData.Count)
            {
                StartCountdown();
            }
            else
            {
                StopCountdown();
            }

            readyData[senderID] = state;
            return;
        }

        void StartCountdown()
        {
            if (serverCoroutine != null)
                StopCoroutine(serverCoroutine);

            serverCoroutine = StartCoroutine(ServerCountdown());
            SetCountdownRpc(true);
        }

        void StopCountdown()
        {
            if (!countdownCanStop)
                return;

            if (serverCoroutine != null)
                StopCoroutine(serverCoroutine);
            serverCoroutine = null;
            SetCountdownRpc(false);
        }

        Coroutine serverCoroutine;

        IEnumerator ServerCountdown()
        {
            yield return new WaitForSecondsRealtime(5);
            countdownCanStop = false;

            foreach(var item in readyData)
            {
                PersistentStateManager.Singleton.SetPlayerCharacter(item.Key, item.Value.characterID);
            }
            PersistentStateManager.Singleton.StartGame();
        }

        Coroutine clientCoroutine;

        IEnumerator ClientCountdown()
        {
            for (int i = 5; i > 0; i--)
            {
                countdown.text = i.ToString();
                yield return new WaitForSecondsRealtime(1);
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        void SetCountdownRpc(bool doCountdown, RpcParams rpcParams = default)
        {
            if (doCountdown)
            {
                if (clientCoroutine != null)
                    StopCoroutine(clientCoroutine);
                clientCoroutine = StartCoroutine(ClientCountdown());
            }
            else
            {
                if (clientCoroutine != null)
                    StopCoroutine(clientCoroutine);
                clientCoroutine = null;
                countdown.text = "";
            }
        }

        public override void OnDestroy()
        {
            networkManager.OnConnectionEvent -= HandlePlayerConnection;
            base.OnDestroy();
        }
    }
}