using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine;
using Unity.VisualScripting;
using Hypersycos.Utils;
using System.Linq;
//using UnityEngine.UIElements;

namespace Hypersycos.GERogueFrame
{
    public class LobbyMenuBehaviour : NetworkBehaviour
    {
        private struct LobbyPlayerState : IEquatable<LobbyPlayerState>, INetworkSerializable
        {
            public ulong id;
            public bool isReady;
            public int characterID;

            public LobbyPlayerState(ulong id)
            {
                this.id = id;
                isReady = false;
                characterID = int.MinValue;
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
        [SerializeField] Transform spawnTransform;

        [SerializeField] Button readyButton;
        [SerializeField] TextMeshProUGUI readyText;
        [SerializeField] Color readyColour;
        [SerializeField] Color notReadyColour;

        [SerializeField] Transform playerHolder;
        [SerializeField] GameObject playerPrefab;
        [SerializeField] Sprite readyCheck;
        [SerializeField] Sprite notReadyCheck;

        Dictionary<ulong, GameObject> playerIcons = new();

        [SerializeField] Transform characterHolder;
        [SerializeField] GameObject characterPrefab;

        [SerializeField] TextMeshProUGUI characterNameText;
        [SerializeField] Transform descriptionHolder;
        [SerializeField] GameObject descriptionPrefab;

        [SerializeField] Button backButton;
        [SerializeField] TextMeshProUGUI countdown;

        BetterNetworkList<KeyIndexPair<ulong>> readyKeys = new();
        BetterNetworkList<LobbyPlayerState> readyValues = new();
        NetworkDict<ulong, LobbyPlayerState> readyData;
        int readyCount = 0;
        bool countdownCanStop = true;

        GameObject myCharacterObj;
        Dictionary<LobbyPlayerState, GameObject> characterObjs;

        private void OnEnable()
        {
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
                        readyButton.image.color = readyColour;
                        readyText.text = "Ready";
                    }
                    else
                    {
                        readyButton.image.color = notReadyColour;
                        readyText.text = "Not Ready";
                    }
                }
            }
        }

        public void ReadyClicked()
        {
            SetReadyServerRpc(!readyData[networkManager.LocalClientId].isReady);
        }

        private void RefreshList(NetworkDict.EventType type, ulong key, LobbyPlayerState newValue)
        {
            switch (type)
            {
                case NetworkDict.EventType.Add:
                    var inst = Instantiate(playerPrefab, playerHolder);
                    playerIcons.Add(key, inst);
                    goto case NetworkDict.EventType.Change;
                case NetworkDict.EventType.Change:
                    if (newValue.isReady)
                    {
                        playerIcons[key].transform.GetChild(2).GetComponent<Image>().color = readyColour;
                        playerIcons[key].transform.GetChild(2).GetChild(0).GetComponent<Image>().sprite = readyCheck;
                    }
                    else
                    {
                        playerIcons[key].transform.GetChild(2).GetComponent<Image>().color = notReadyColour;
                        playerIcons[key].transform.GetChild(2).GetChild(0).GetComponent<Image>().sprite = notReadyCheck;
                    }
                    if (newValue.characterID >= 0)
                    {
                        playerIcons[key].transform.GetChild(1).GetComponent<Image>().sprite = SODatabase.NetworkedDB.PlayerCharacters[newValue.characterID].Icon;
                        playerIcons[key].transform.GetChild(1).GetComponent<Image>().enabled = true;
                    }
                    else
                        playerIcons[key].transform.GetChild(1).GetComponent<Image>().enabled = false;
                    break;
                case NetworkDict.EventType.Remove:
                    Destroy(playerIcons[key]);
                    playerIcons.Remove(key);
                    break;
                case NetworkDict.EventType.Clear:
                    foreach(GameObject obj in playerIcons.Values)
                    {
                        Destroy(obj);
                    }
                    playerIcons.Clear();
                    break;
                default:
                    break;
            }
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

            foreach(var pair in readyData)
            {
                RefreshList(NetworkDict.EventType.Add, pair.Key, pair.Value);
            }

            readyData.OnDictionaryChanged += RefreshList;

            StartCoroutine(CreateCharacterIcons());
        }

        public IEnumerator CreateCharacterIcons()
        {
            while (!PersistentStateManager.Singleton.IsSpawned)
            {
                yield return null;
            }
            foreach (BasePCharacterSO so in SODatabase.NetworkedDB.PlayerCharacters)
            {
                var template = Instantiate(characterPrefab, characterHolder);
                template.GetComponent<Image>().color = so.Color;
                template.transform.GetChild(0).GetComponent<Image>().sprite = so.Icon;

                var btn = template.GetComponent<Button>();

                btn.onClick.AddListener(() => SelectCharacter(so, btn));
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

        private void SelectCharacter(BasePCharacterSO character, Button button)
        {
            if (myCharacterObj)
                Destroy(myCharacterObj);
            myCharacterObj = Instantiate(character.Model,
                                         spawnTransform.position + character.Model.transform.position,
                                         spawnTransform.rotation * character.Model.transform.rotation);
            SetCharacterRpc(SODatabase.NetworkedDB.PlayerCharacterIDs[character.UUID]);

            characterNameText.text = character.ItemName;

            descriptionHolder.transform.DestroyAllChildren();

            IAbilityData[] datas = new IAbilityData[6] { character.Weapon, character.WeaponAlt, character.Ability1, character.Ability2, character.Ability3, character.Ability4 };
            string[] typeNames = new string[6] { "Primary Fire", "Alternative Fire", "Ability 1", "Ability 2", "Ability 3", "Ability 4" };

            for (int i = 0; i < datas.Length; i++)
            {
                var data = datas[i];

                if (data == null)
                    continue;

                BaseAbilityData baseData = null;
                if (data is BaseAbilityData bd)
                    baseData = bd;
                else if (data is AbilitySO so)
                    baseData = so.As<BaseAbilityData>();

                if (baseData != null)
                {
                    var inst = Instantiate(descriptionPrefab, descriptionHolder);
                    inst.GetComponentInChildren<Image>().sprite = baseData.AbilityIcon;
                    inst.GetComponentInChildren<TextMeshProUGUI>().text = $"<b>{baseData.AbilityName} ({typeNames[i]}): </b>{baseData.AbilityDescription}";
                }
            }

            readyButton.interactable = true;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void SetCharacterRpc(int id, RpcParams rpcParams = default)
        {
            if (id < 0 || id >= SODatabase.NetworkedDB.PlayerCharacters.Count)
                return;

            ulong senderID = rpcParams.Receive.SenderClientId;
            LobbyPlayerState state = readyData[senderID];
            state.characterID = id;
            readyData[senderID] = state;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void SetReadyServerRpc(bool ready, RpcParams rpcParams = default)
        {
            ulong senderID = rpcParams.Receive.SenderClientId;

            LobbyPlayerState state = readyData[senderID];

            ready = ready && state.characterID >= 0;

            bool oldReady = state.isReady;
            state.isReady = ready && state.characterID != int.MinValue;

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