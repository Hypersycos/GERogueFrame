using Hypersycos.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TextCore.Text;
using System.Linq;
using Sirenix.OdinInspector;

namespace Hypersycos.GERogueFrame
{
    //[CreateAssetMenu(fileName = "New SODatabase", menuName = "GERogueFrame/SODatabase", order = 0)]
    public class SODatabase
    {
        public static SODatabase LocalDB;
        public static SODatabase NetworkedDB;

        public List<BasePCharacterSO> PlayerCharacters = new();
        public List<MapGeneratorSO> Maps = new();
        public List<EnemySO> Enemies = new();
        public List<NonNetworkedProjectile> NonNetworkedProjectiles = new();
        public List<NetworkedProjectile> NetworkedProjectiles = new();
        public List<ObjectiveSO> Objectives = new();

        public TwoWayDictionary<string, int> PlayerCharacterIDs = new();
        public TwoWayDictionary<string, int> MapIDs = new();
        public TwoWayDictionary<string, int> EnemyIDs = new();
        public TwoWayDictionary<string, int> NonNetworkedProjectileIDs = new();
        public TwoWayDictionary<string, int> NetworkedProjectileIDs = new();
        public TwoWayDictionary<string, int> ObjectiveIDs = new();

        public static List<string> missingIDs = new();

        public void Load<T>(List<T> list, string prefix) where T: ModDatabaseItem
        {
            switch(list)
            {
                case List<BasePCharacterSO> pList:
                    Load(pList, prefix);
                    break;
                case List<MapGeneratorSO> mList:
                    Load(mList, prefix);
                    break;
                case List<EnemySO> eList:
                    Load(eList, prefix);
                    break;
                case List<NonNetworkedProjectile> nnpList:
                    Load(nnpList, prefix);
                    break;
                case List<NetworkedProjectile> npList:
                    Load(npList, prefix);
                    break;
                case List<ObjectiveSO> oList:
                    Load(oList, prefix);
                    break;
            }
        }

        public void Load(List<BasePCharacterSO> list, string prefix)
        {
            foreach(var character in list)
            {
                PlayerCharacterIDs.Add(prefix + character.ItemName, PlayerCharacters.Count);
                PlayerCharacters.Add(character);
                character.UUID = prefix + character.ItemName;
            }
        }
        public void Load(List<MapGeneratorSO> list, string prefix)
        {
            foreach (var item in list)
            {
                MapIDs.Add(prefix + item.ItemName, Maps.Count);
                Maps.Add(item);
                item.UUID = prefix + item.ItemName;
            }
        }
        public void Load(List<EnemySO> list, string prefix)
        {
            foreach (var item in list)
            {
                EnemyIDs.Add(prefix + item.ItemName, Enemies.Count);
                Enemies.Add(item);
                item.UUID = prefix + item.ItemName;
            }
        }
        public void Load(List<NonNetworkedProjectile> list, string prefix)
        {
            foreach (var item in list)
            {
                NonNetworkedProjectileIDs.Add(prefix + item.ItemName, NonNetworkedProjectiles.Count);
                NonNetworkedProjectiles.Add(item);
                item.UUID = prefix + item.ItemName;
            }
        }
        public void Load(List<NetworkedProjectile> list, string prefix)
        {
            foreach (var item in list)
            {
                NetworkedProjectileIDs.Add(prefix + item.ItemName, NetworkedProjectiles.Count);
                NetworkedProjectiles.Add(item);
                item.UUID = prefix + item.ItemName;
            }
        }
        public void Load(List<ObjectiveSO> list, string prefix)
        {
            foreach (var item in list)
            {
                ObjectiveIDs.Add(prefix + item.ItemName, Objectives.Count);
                Objectives.Add(item);
                item.UUID = prefix + item.ItemName;
            }
        }

        private void Clear()
        {
            PlayerCharacters.Clear();
            Maps.Clear();
            Enemies.Clear();
            NonNetworkedProjectiles.Clear();
            NetworkedProjectiles.Clear();
            Objectives.Clear();

            PlayerCharacterIDs.Clear();
            MapIDs.Clear();
            EnemyIDs.Clear();
            NonNetworkedProjectileIDs.Clear();
            NetworkedProjectileIDs.Clear();
            ObjectiveIDs.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void SetSingletons()
        {
            LocalDB = new();
            NetworkedDB = new();
        }

        internal static void Write(FastBufferWriter writer)
        {
            void Write(TwoWayDictionary<string, int> idMap)
            {
                writer.WriteValueSafe(idMap.Count);
                foreach(var idPair in idMap.OrderBy((x) => x.Value))
                {
                    writer.WriteValueSafe(idPair.Key);
                }
            }

            Write(NetworkedDB.PlayerCharacterIDs);
            Write(NetworkedDB.MapIDs);
            Write(NetworkedDB.EnemyIDs);
            Write(NetworkedDB.NonNetworkedProjectileIDs);
            Write(NetworkedDB.NetworkedProjectileIDs);
            Write(NetworkedDB.ObjectiveIDs);
        }

        internal static void Read(FastBufferReader reader)
        {
            void Read<T>(List<T> source, List<T> dest, TwoWayDictionary<string, int> sourceIdMap, TwoWayDictionary<string, int> destIdMap)
            {
                reader.ReadValueSafe(out int count);
                for (int i = 0; i < count; i++)
                {
                    reader.ReadValueSafe(out string id);
                    if (sourceIdMap.TryGetValue(id, out int index))
                    {
                        dest.Add(source[sourceIdMap[id]]);
                        destIdMap.Add(id, i);
                    }
                    else
                    {
                        Debug.LogError($"Local map does not contain UUID {id}");
                        missingIDs.Add(id);
                    }
                }
            }

            NetworkedDB.Clear();
            missingIDs.Clear();

            Read(LocalDB.PlayerCharacters, NetworkedDB.PlayerCharacters, LocalDB.PlayerCharacterIDs, NetworkedDB.PlayerCharacterIDs);
            Read(LocalDB.Maps, NetworkedDB.Maps, LocalDB.MapIDs, NetworkedDB.MapIDs);
            Read(LocalDB.Enemies, NetworkedDB.Enemies, LocalDB.EnemyIDs, NetworkedDB.EnemyIDs);
            Read(LocalDB.NonNetworkedProjectiles, NetworkedDB.NonNetworkedProjectiles, LocalDB.NonNetworkedProjectileIDs, NetworkedDB.NonNetworkedProjectileIDs);
            Read(LocalDB.NetworkedProjectiles, NetworkedDB.NetworkedProjectiles, LocalDB.NetworkedProjectileIDs, NetworkedDB.NetworkedProjectileIDs);
            Read(LocalDB.Objectives, NetworkedDB.Objectives, LocalDB.ObjectiveIDs, NetworkedDB.ObjectiveIDs);
        }

        internal static void Copy()
        {
            void Copy<T>(List<T> source, List<T> dest, TwoWayDictionary<string, int> sourceIdMap, TwoWayDictionary<string, int> destIdMap)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    dest.Add(source[i]);
                    destIdMap.Add(sourceIdMap[i], i);
                }
            }

            NetworkedDB.Clear();

            Copy(LocalDB.PlayerCharacters, NetworkedDB.PlayerCharacters, LocalDB.PlayerCharacterIDs, NetworkedDB.PlayerCharacterIDs);
            Copy(LocalDB.Maps, NetworkedDB.Maps, LocalDB.MapIDs, NetworkedDB.MapIDs);
            Copy(LocalDB.Enemies, NetworkedDB.Enemies, LocalDB.EnemyIDs, NetworkedDB.EnemyIDs);
            Copy(LocalDB.NonNetworkedProjectiles, NetworkedDB.NonNetworkedProjectiles, LocalDB.NonNetworkedProjectileIDs, NetworkedDB.NonNetworkedProjectileIDs);
            Copy(LocalDB.NetworkedProjectiles, NetworkedDB.NetworkedProjectiles, LocalDB.NetworkedProjectileIDs, NetworkedDB.NetworkedProjectileIDs);
            Copy(LocalDB.Objectives, NetworkedDB.Objectives, LocalDB.ObjectiveIDs, NetworkedDB.ObjectiveIDs);
        }
    }

    public class ModDatabase<T> : ScriptableObject where T : ModDatabaseItem
    {
        public List<T> List;
    }

    public class ModDatabaseItem : SerializedScriptableObject
    {
        public string UUID;
        public string ItemName;
    }
}
