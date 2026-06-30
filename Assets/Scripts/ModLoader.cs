using Hypersycos.SaveSystem;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    static class ModLoader
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Load()
        {
            Load(Resources.Load<CharacterDatabase>("BaseCharacterDatabase"), "base.");
            Load(Resources.Load<MapDatabase>("BaseMapDatabase"), "base.");
            Load(Resources.Load<EnemyDatabase>("BaseEnemyDatabase"), "base.");
            Load(Resources.Load<NonNetworkedProjectileDatabase>("BaseNonNetworkedProjectileDatabase"), "base.");
            Load(Resources.Load<NetworkedProjectileDatabase>("BaseNetworkedProjectileDatabase"), "base.");

            LoadMods();
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
