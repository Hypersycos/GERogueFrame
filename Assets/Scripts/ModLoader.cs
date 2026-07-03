using Hypersycos.SaveSystem;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    static class ModLoader
    {
        static List<NetworkPrefab> extraPrefabs = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Load()
        {
            extraPrefabs.Clear();
            Load(Resources.Load<CharacterDatabase>("BaseCharacterDatabase"), "base.");
            Load(Resources.Load<MapDatabase>("BaseMapDatabase"), "base.");
            Load(Resources.Load<EnemyDatabase>("BaseEnemyDatabase"), "base.");
            Load(Resources.Load<NonNetworkedProjectileDatabase>("BaseNonNetworkedProjectileDatabase"), "base.");
            Load(Resources.Load<NetworkedProjectileDatabase>("BaseNetworkedProjectileDatabase"), "base.");

            extraPrefabs.AddRange(Resources.Load<NetworkPrefabsList>("BaseNetworkPrefabList").PrefabList);

            LoadMods();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void RegisterPrefabs()
        {
            foreach (var item in SODatabase.LocalDB.Enemies)
            {
                NetworkPrefab prefab = new()
                {
                    Prefab = item.NetworkPrefab.gameObject,
                    Override = NetworkPrefabOverride.None,
                };
                NetworkManager.Singleton.NetworkConfig.Prefabs.Add(prefab);
            }

            foreach (var item in SODatabase.LocalDB.PlayerCharacters)
            {
                NetworkPrefab prefab = new()
                {
                    Prefab = item.NetworkPrefab.gameObject,
                    Override = NetworkPrefabOverride.None,
                };
                NetworkManager.Singleton.NetworkConfig.Prefabs.Add(prefab);
            }

            foreach (var item in SODatabase.LocalDB.Objectives)
            {
                NetworkPrefab prefab = new()
                {
                    Prefab = item.objective.gameObject,
                    Override = NetworkPrefabOverride.None,
                };
                NetworkManager.Singleton.NetworkConfig.Prefabs.Add(prefab);
            }

            foreach (var item in SODatabase.LocalDB.NetworkedProjectiles)
            {
                NetworkPrefab prefab = new()
                {
                    Prefab = item.projectileObj.gameObject,
                    Override = NetworkPrefabOverride.None,
                };
                NetworkManager.Singleton.NetworkConfig.Prefabs.Add(prefab);
            }

            foreach (var prefab in extraPrefabs)
            {
                NetworkManager.Singleton.NetworkConfig.Prefabs.Add(prefab);
            }
        }

        public static void LoadMods()
        {

        }

        public static void Load<T>(ModDatabase<T> db, string prefix) where T : ModDatabaseItem
        {
            SODatabase.LocalDB.Load(db.List, prefix);
        }
    }
}
