using Hypersycos.Utils;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using static UnityEditor.FilePathAttribute;

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
        public uint characterID;
        public NetworkObjectReference playerObj;

        public PlayerInfo(uint characterID)
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
        [SerializeField] GameObject loadingScreen;

        readonly Dictionary<string, uint> characterMap = new();
        public readonly List<BaseCharacterSO> availableCharacters = new();

        NetworkVariable<GameState> _gameState = new NetworkVariable<GameState>(GameState.Lobby);
        public GameState gameState => _gameState.Value;

        NetworkVariable<MapState> _mapState = new NetworkVariable<MapState>();
        public MapState mapState => _mapState.Value;

        BetterNetworkList<KeyIndexPair<ulong>> playerIDs = new();
        BetterNetworkList<PlayerInfo> playerCharacters = new();
        NetworkDict<ulong, PlayerInfo> playerCharacterMap;

        public UnityEvent AllPlayersLoaded;

        public uint GetCharacterID(BaseCharacterSO so)
        {
            return characterMap[so.UUID];
        }

        public BaseCharacterSO GetCharacterFromID(string id) => CharacterLoader.characterDict[id];

        public static PersistentStateManager Singleton { get; private set; }

        protected override void OnSynchronize<T>(ref BufferSerializer<T> serializer)
        {
            base.OnSynchronize(ref serializer);
            if (serializer.IsWriter)
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(characterMap.Count);
                foreach(var kvp in characterMap)
                {
                    writer.WriteValueSafe(kvp.Key);
                    writer.WriteValueSafe(kvp.Value);
                }
            }
            else
            {
                characterMap.Clear();
                var reader = serializer.GetFastBufferReader();
                int count;
                reader.ReadValueSafe(out count);
                for (int i = 0; i < count; i++)
                {
                    string UUID;
                    uint gameID;
                    reader.ReadValueSafe(out UUID);
                    reader.ReadValueSafe(out gameID);
                    characterMap.Add(UUID, gameID);
                    availableCharacters.Add(CharacterLoader.characterDict[UUID]);
                }
            }
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
                characterMap.Clear();
                uint count = 0;
                foreach (BaseCharacterSO so in CharacterLoader.characters)
                {
                    characterMap.Add(so.UUID, count++);
                    availableCharacters.Add(so);
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (NetworkManager.IsServer)
                _gameState.Value = GameState.Lobby;
            playerCharacterMap = new(playerIDs, playerCharacters, NetworkManager.IsServer);
            DontDestroyOnLoad(this);

            NetworkManager.SceneManager.OnLoad += ShowLoadingScreen;

            _gameState.OnValueChanged += HandleGameStateValueChange;
        }

        private void HandleGameStateValueChange(GameState previousValue, GameState newValue)
        {
            if (previousValue == GameState.LoadingGame && newValue != GameState.LoadingGame)
                HideLoadingScreen();
        }

        public override void OnNetworkDespawn()
        {
            NetworkManager.SceneManager.OnLoad -= ShowLoadingScreen;
        }

        void ShowLoadingScreen(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
        {
            if (clientId == NetworkManager.LocalClientId)
                ShowLoadingScreen();
        }

        void ShowLoadingScreen()
        {
            loadingScreen.SetActive(true);
        }

        void HideLoadingScreen()
        {
            loadingScreen.SetActive(false);
        }

        public void SetPlayerCharacter(ulong id, uint characterID)
        {
            playerCharacterMap.Add(id, new PlayerInfo(characterID));
        }

        public void StartGame()
        {
            if (gameState != GameState.Lobby)
                return;
            _gameState.Value = GameState.LoadingGame;

            StartPreRound();
        }

        public void StartPreRound()
        {
            MapState newMap = new() { width = 1000, height = 1000, seed = UnityEngine.Random.Range(0, 10000) };
            newMap.so = MapDatabase.singleton.maps.TakeRandom();
            _mapState.Value = newMap;

            AllPlayersLoaded.AddListener(SpawnPlayers);
            NetworkManager.SceneManager.OnLoadEventCompleted += OnGameSceneLoaded;
            NetworkManager.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
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

        private NetworkObject SpawnPlayerAt(ulong playerID, uint characterID, Vector3 pos, Quaternion rot)
        {
            NetworkObject PlayerPrefab = availableCharacters[(int)characterID].NetworkPrefab;

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
