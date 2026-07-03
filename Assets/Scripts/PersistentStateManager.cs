using Hypersycos.Utils;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Hypersycos.GERogueFrame
{
    public enum GameState : byte
    {
        Lobby,
        LoadingGame,
        Playing
    }

    public struct PlayerInfo : IEquatable<PlayerInfo>, INetworkSerializable
    {
        public int characterID;
        public NetworkObjectReference playerObj;

        public PlayerInfo(int characterID)
        {
            this.characterID = characterID;
            playerObj = new();
        }

        public bool Equals(PlayerInfo other)
        {
            return characterID == other.characterID;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref characterID);
            serializer.SerializeValue(ref playerObj);
        }
    }

    public class PersistentStateManager : NetworkBehaviour
    {
        [SerializeField] LoadingScreenManager loadingScreen;

        NetworkVariable<GameState> _gameState = new NetworkVariable<GameState>(GameState.Lobby);
        public GameState gameState => _gameState.Value;
        public MapState mapState = new();
        public bool isLatestMap = false;
        public UnityEvent MapUpdated;

        BetterNetworkList<KeyIndexPair<ulong>> playerIDs = new();
        BetterNetworkList<PlayerInfo> playerCharacters = new();
        public NetworkDict<ulong, PlayerInfo> playerCharacterMap;

        public UnityEvent AllPlayersLoaded;

        NetworkVariable<float> _difficulty = new();
        public float difficulty { get => _difficulty.Value; set => _difficulty.Value = value; }

        public SODatabase SODB => SODatabase.NetworkedDB;

        public int GetCharacterID(BasePCharacterSO so) => SODB.PlayerCharacterIDs[so.UUID];
        public BasePCharacterSO GetCharacterFromID(string id) => SODB.PlayerCharacters[SODB.PlayerCharacterIDs[id]];

        public static PersistentStateManager Singleton { get; private set; }

        protected override void OnSynchronize<T>(ref BufferSerializer<T> serializer)
        {
            base.OnSynchronize(ref serializer);
            if (serializer.IsWriter)
            {
                var writer = serializer.GetFastBufferWriter();
                SODatabase.Write(writer);
            }
            else
            {
                var reader = serializer.GetFastBufferReader();
                SODatabase.Read(reader);
            }
            serializer.SerializeValue(ref mapState);
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            Singleton = this;
        }

        protected override void OnNetworkPreSpawn(ref NetworkManager networkManager)
        {
            if (networkManager.IsServer)
            {
                SODatabase.Copy();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (NetworkManager.IsServer)
                _gameState.Value = GameState.Lobby;
            playerCharacterMap = new(playerIDs, playerCharacters, !NetworkManager.IsServer);
            DontDestroyOnLoad(this);

            NetworkManager.SceneManager.OnLoad += SceneChangeStarted;
            NetworkManager.SceneManager.OnLoadComplete += SceneChangeCompleted;

            _gameState.OnValueChanged += HandleGameStateValueChange;
        }

        private void HandleGameStateValueChange(GameState previousValue, GameState newValue)
        {
            if (previousValue == GameState.LoadingGame && newValue != GameState.LoadingGame)
                loadingScreen.Hide();
        }

        public override void OnNetworkDespawn()
        {
            NetworkManager.SceneManager.OnLoad -= SceneChangeStarted;
            NetworkManager.SceneManager.OnLoadComplete -= SceneChangeCompleted;
        }

        void SceneChangeStarted(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
        {
            if (clientId == NetworkManager.LocalClientId && sceneName == "GameScene")
                loadingScreen.ShowMapLoad();
        }

        private void SceneChangeCompleted(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            if (clientId == NetworkManager.LocalClientId && sceneName == "GameScene")
                loadingScreen.DisplayMapProgress();
        }

        public void SetPlayerCharacter(ulong id, int characterID)
        {
            playerCharacterMap.Add(id, new PlayerInfo(characterID));
        }

        public void StartGame()
        {
            if (gameState != GameState.Lobby)
                return;
            _gameState.Value = GameState.LoadingGame;
            difficulty = 1;

            StartPreRound();
        }

        public void StartPreRound()
        {
            MapState newMap = new() { width = 1000, height = 1000, seed = UnityEngine.Random.Range(0, 10000) };
            newMap.so = SODB.Maps.TakeRandom();
            mapState = newMap;
            isLatestMap = true;
            SyncMapRpc(newMap);

            AllPlayersLoaded.AddListener(StartRound);
            NetworkManager.SceneManager.OnLoadEventCompleted += OnGameSceneLoaded;
            NetworkManager.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void SyncMapRpc(MapState newMap)
        {
            mapState = newMap;
            loadingScreen.UpdateMapLoad();
            isLatestMap = true;
            MapUpdated?.Invoke();
        }

        public static void SingletonQuitToMenu() => Singleton?.QuitToMenu();

        public void QuitToMenu()
        {
            NetworkManager.Singleton.Shutdown();
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            Destroy(NetworkManager.Singleton.gameObject);
            DestroySingleton();
        }

        public static void DestroySingleton()
        {
            if (Singleton != null)
            {
                Destroy(Singleton.gameObject);
            }
        }

        private void StartRound()
        {
            AllPlayersLoaded.RemoveListener(StartRound);
            SpawnPlayers();
            GameObject.FindWithTag("Managers").GetComponent<EnemySpawnManager>().enabled = true;
        }

        private void SpawnPlayers()
        {
            AllPlayersLoaded.RemoveListener(SpawnPlayers);

            ControlsWrapper.Singleton.CloseMenu(default);

            Dictionary<ulong, NetworkObject> spawns = new();

            mapState.so.generator.GetSpawnPoint(playerCharacterMap.Count, out Vector3[] positions, out Quaternion[] rotations);

            int i = 0;

            foreach (var player in playerCharacterMap)
            {
                spawns.Add(player.Key, SpawnPlayerAt(player.Key, player.Value.characterID, positions[i], rotations[i]));
                i++;
            }

            foreach (var spawn in spawns)
            {
                var copy = playerCharacterMap[spawn.Key];
                copy.playerObj = spawn.Value;
                playerCharacterMap[spawn.Key] = copy;
            }

            _gameState.Value = GameState.Playing;
        }

        private NetworkObject SpawnPlayerAt(ulong playerID, int characterID, Vector3 pos, Quaternion rot)
        {
            NetworkObject PlayerPrefab = SODB.PlayerCharacters[characterID].NetworkPrefab;

            NetworkObject spawned = NetworkManager.Singleton.SpawnManager
                                    .InstantiateAndSpawn(PlayerPrefab, playerID, true, true,
                                                         position: pos, rotation: Quaternion.Inverse(rot));

            spawned.GetComponent<PlayerCharacterManager>().characterID = characterID;
            return spawned;
        }

        private void OnGameSceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted -= OnGameSceneLoaded;

            //TODO: still needed?
        }
    }
}
