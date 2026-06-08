using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
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
        public uint characterID;

        public PlayerInfo(uint characterID)
        {
            this.characterID = characterID;
        }

        public bool Equals(PlayerInfo other)
        {
            return characterID == other.characterID;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref characterID);
        }
    }

    public class PersistentStateManager : NetworkBehaviour
    {

        readonly Dictionary<string, uint> characterMap = new();
        public readonly List<BaseCharacterSO> availableCharacters = new();

        NetworkVariable<GameState> _gameState = new NetworkVariable<GameState>(GameState.Lobby);
        public GameState gameState => _gameState.Value;

        BetterNetworkList<KeyIndexPair<ulong>> playerIDs = new();
        BetterNetworkList<PlayerInfo> playerCharacters = new();
        NetworkDict<ulong, PlayerInfo> playerCharacterMap;

        [SerializeField] NetworkObject PlayerPrefab;

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

        private void OnGameSceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted -= OnGameSceneLoaded;
            _gameState.Value = GameState.Playing;

            float rotation = 360 / playerCharacterMap.Count;
            float distance = playerCharacterMap.Count * 15 / (2 * Mathf.PI);
            int i = 0;

            foreach (var player in playerCharacterMap)
            {
                Quaternion rot = Quaternion.AngleAxis(rotation * i++, Vector3.up);
                Vector3 pos = rot * (Vector3.forward * distance) + Vector3.up * 2;

                NetworkObject spawned = NetworkManager.Singleton.SpawnManager
                                        .InstantiateAndSpawn(PlayerPrefab, player.Key, true, true,
                                                             position: pos, rotation: Quaternion.Inverse(rot));

                spawned.GetComponent<PlayerCharacterManager>().characterID.Value = playerCharacterMap[player.Key].characterID;
            }
        }
    }
}
