using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New Character", menuName = "GERogueFrame/EnemyCharacter", order = 0)]
    public class EnemySO : BaseCharacterSO
    {
        public List<Defense> defenses;
        public List<Resource> resources;
        public List<IAbilityData> abilities;
        public float spawnCost;
        public int navmeshID;

        public void Apply(EnemyState state)
        {
            foreach(Resource resource in resources)
            {
                BoundedStatInstance resInst = new(0, 0, resource.Max, resource.StatType);
                state.Resources.Add(resInst);
                ApplyResource(resInst, resource, 0);
            }

            foreach (Defense def in defenses)
            {
                DefenseStatInstance defInst = new();
                CreateDefense(ref defInst, def, def.StatType.Name.ToLower() == "overhealth");
                state.Defenses.Add(defInst);
            }

            state.ApplyDefensePool();
            state.StartSyncingValues(state.Defenses.ToList<ISyncStat>());
            state.StartSyncingValues(state.Resources.ToList<ISyncStat>());

            EnemyAbilityManager aManager = state.GetComponent<EnemyAbilityManager>();

            if (abilities != null)
            {
                foreach (IAbilityData ability in abilities)
                {
                    aManager.AddAbility(ability);
                }
            }
        }

#if false && UNITY_EDITOR
        [ContextMenu("Generate Network Prefab")]
        public void RegenerateNetworkPrefab()
        {
            GameObject copy = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(Model));

            copy.AddComponent<NetworkObject>();
            var manager = copy.AddComponent<EnemyCharacterManager>();
            manager.so = this;
            manager.netAnimator = copy.AddComponent<NetworkAnimator>();
            manager.netAnimator.AuthorityMode = NetworkAnimator.AuthorityModes.Owner;
            manager.netAnimator.Animator = copy.GetComponent<Animator>();
            manager.controller = copy.GetComponent<CharacterController>();
            manager.bar = PrefabUtility.LoadPrefabContents("Assets/Prefabs/EnemyBar.prefab").transform;
            manager.bar.SetParent(cameraTarget);
            manager.bar.localPosition = new Vector3(0, 0.5f, 0);
            manager.bar.gameObject.SetActive(false);
            NetworkTransform netTransform = copy.AddComponent<NetworkTransform>();
            netTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            netTransform.SyncRotAngleX = false;
            netTransform.SyncRotAngleZ = false;
            copy.AddComponent<PlayerState>().DamageTickPrefab = AssetDatabase.LoadAssetAtPath<TMPro.TMP_Text>("Assets/UI/DamageNumber.prefab");
            copy.AddComponent<PlayerAbilityManager>();

            GameObject camPos = new GameObject("CameraPos");
            NetworkTransform camTrans = camPos.AddComponent<NetworkTransform>();
            camTrans.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            camTrans.SyncScaleX = false;
            camTrans.SyncScaleY = false;
            camTrans.SyncScaleZ = false;
            camPos.transform.SetParent(copy.transform);

            GameObject basePrefab = PrefabUtility.SaveAsPrefabAsset(copy, $"Assets/NetworkPrefabs/{CharacterName}Net.prefab", out bool success);

            if (success)
            {
                NetworkPrefab = basePrefab.GetComponent<NetworkObject>();
                EditorUtility.SetDirty(this);
            }
            PrefabUtility.UnloadPrefabContents(copy);
            Debug.Log($"Generated Assets/NetworkPrefabs/{CharacterName}Net.prefab");
        }
#endif
    }
}
