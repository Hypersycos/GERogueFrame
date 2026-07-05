using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;
using UnityEngine.UIElements;
using TMPro;
using Sirenix.Serialization;
using Sirenix.OdinInspector;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New Character", menuName = "GERogueFrame/PlayerCharacter", order = 0)]
    public class BasePCharacterSO : BaseCharacterSO, IEquatable<BasePCharacterSO>
    {
        public List<Canvas> UI;

        public float Speed;

        [OdinSerialize] public IAbilityData Weapon;
        [OdinSerialize] public IAbilityData WeaponAlt;

        [OdinSerialize] public IAbilityData Ability1;
        [OdinSerialize] public IAbilityData Ability2;
        [OdinSerialize] public IAbilityData Ability3;
        [OdinSerialize] public IAbilityData Ability4;
        [OdinSerialize] public IAbilityData Ultimate;

        public UpgradeTreeSO UpgradeTree;

        public Resource Energy;
        public Defense Health;
        public Defense Shields;
        public Defense Overhealth;

#if UNITY_EDITOR
        public void Reset()
        {
            Energy = new Resource { StatType = StatType.StatTypeMap["Energy"], Max = 100, MaxRegen = new ResourceRegen { Value = .25f, Delay = 0.2f } };
            Health = new Defense { StatType = StatType.StatTypeMap["Health"], ResistStatType = StatType.StatTypeMap["Armour"], Max = 1000, FlatRegen = new ResourceRegen { Value = 2, Delay = 4, ReducedRate = .25f } };
            Shields = new Defense { StatType = StatType.StatTypeMap["Shields"], Max = 0, MaxRegen = new ResourceRegen { Value = .25f, Delay = 3 } };
            Overhealth = new Defense { StatType = StatType.StatTypeMap["OverHealth"], Max = -1, FlatRegen = new ResourceRegen { Value = -5, Delay = 0 }, CurrentRegen = new ResourceRegen { Value = -0.2f, Delay = 0 } };
        }
#endif

        public bool Equals(BasePCharacterSO other)
        {
            return UUID == other.UUID;
        }

        public void Apply(PlayerState state, ref List<BoundedStatInstance> defenses)
        {
            state.Energy = new(Energy.Max, 0, Energy.Max, Energy.StatType);
            if (Energy.Max > 0)
            {
                ApplyResource(state.Energy, Energy, 0);
            }

            CreateDefense(ref state.Health, Health, false);
            CreateDefense(ref state.Shields, Shields, false);
            CreateDefense(ref state.OverHealth, Overhealth, true);
            state.OverHealth.AddValue(100);

            state.ApplyDefensePool(new List<DefenseStatInstance>() { state.Health, state.Shields, state.OverHealth });
            state.StartSyncingValues(new List<ISyncStat>() { state.Health, state.Shields, state.OverHealth, state.Energy });

            defenses = new List<BoundedStatInstance>() { state.Health, state.Shields, state.OverHealth };

            PlayerAbilityManager aManager = state.GetComponent<PlayerAbilityManager>();

            aManager.weapon = Weapon?.CreateAbility();
            aManager.weaponAlt = WeaponAlt?.CreateAbility();

            aManager.ability1 = Ability1?.CreateAbility();
            aManager.ability2 = Ability2?.CreateAbility();
            aManager.ability3 = Ability3?.CreateAbility();
            aManager.ability4 = Ability4?.CreateAbility();

            aManager.ultimate = Ultimate?.CreateAbility();

            if (state.IsOwner)
            {
                GameObject UICanvas = GameObject.FindWithTag("PlayerUI");
                Transform HealthBar = UICanvas.transform.Find("HealthBar");
                HealthBar.gameObject.SetActive(true);
                HealthBar.GetComponentInChildren<StatBarScript>().SetStats(defenses);
                if (Energy.Max > 0)
                {
                    Transform EnergyBar = UICanvas.transform.Find("EnergyBar");
                    EnergyBar.gameObject.SetActive(true);
                    EnergyBar.GetComponentInChildren<StatBarScript>().SetStats(new List<BoundedStatInstance>() { state.Energy });
                }

                Transform AbilityHolder = UICanvas.transform.Find("Abilities");
                AbilityIcon[] icons = new AbilityIcon[4];
                icons[0] = Ability1?.CreateIcon(AbilityHolder);
                icons[1] = Ability2?.CreateIcon(AbilityHolder);
                icons[2] = Ability3?.CreateIcon(AbilityHolder);
                icons[3] = Ability4?.CreateIcon(AbilityHolder);

                icons[0].SetAbility(aManager.ability1, Ability1, state);
                icons[1].SetAbility(aManager.ability2, Ability2, state);
                icons[2].SetAbility(aManager.ability3, Ability3, state);
                icons[3].SetAbility(aManager.ability4, Ability4, state);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Generate Network Prefab")]
        public void RegenerateNetworkPrefab()
        {
            GameObject copy = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(Model));
            Transform cameraTarget = cameraTarget = copy.GetComponentsInChildren<Transform>()
                                                        .FirstOrDefault(c => c.gameObject.name == "CameraTarget");

            copy.AddComponent<NetworkObject>();
            var manager = copy.AddComponent<PlayerCharacterManager>();
            manager.so = this;
            manager.netAnimator = copy.AddComponent<NetworkAnimator>();
            manager.netAnimator.AuthorityMode = NetworkAnimator.AuthorityModes.Owner;
            manager.netAnimator.Animator = copy.GetComponent<Animator>();
            manager.controller = copy.GetComponent<CharacterController>();
            if (manager.controller == null)
            {
                manager.controller = copy.AddComponent<CharacterController>();
                manager.controller.enabled = false;
            }
            if (copy.GetComponents<Collider>().Length < 2)
            {
                manager.nonOwnerCollider = copy.AddComponent<CapsuleCollider>();
            }
            else
            {
                manager.nonOwnerCollider = copy.GetComponents<Collider>().Where((x) => x is not CharacterController).First();
            }
            manager.bar = PrefabUtility.LoadPrefabContents("Assets/Prefabs/FriendlyBar.prefab").transform;
            manager.bar.SetParent(cameraTarget);
            manager.bar.localPosition = new Vector3(0, 0.5f, 0);
            manager.bar.gameObject.SetActive(false);
            NetworkTransform netTransform = copy.AddComponent<NetworkTransform>();
            netTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            netTransform.SyncRotAngleX = false;
            netTransform.SyncRotAngleZ = false;
            var state = copy.AddComponent<PlayerState>();
            state.DamageTickPrefab = AssetDatabase.LoadAssetAtPath<TMPro.TMP_Text>("Assets/UI/DamageNumber.prefab");
            state.cameraTarget = cameraTarget;
            state.projectileSource = copy.GetComponentsInChildren<Transform>()
                                         .FirstOrDefault(c => c.gameObject.name == "ProjectileSource");
            copy.AddComponent<PlayerAbilityManager>();
            var movement = copy.AddComponent<PlayerMovementController>();
            movement.enabled = false;
            movement.movementSpeed = Speed;

            GameObject camPos = new GameObject("CameraPos");
            NetworkTransform camTrans = camPos.AddComponent<NetworkTransform>();
            camTrans.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            camTrans.SyncScaleX = false;
            camTrans.SyncScaleY = false;
            camTrans.SyncScaleZ = false;
            camPos.transform.SetParent(copy.transform);

            foreach (Transform trans in copy.GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.layer = 6;
            }

            GameObject basePrefab = PrefabUtility.SaveAsPrefabAsset(copy, $"Assets/NetworkPrefabs/{ItemName}Net.prefab", out bool success);

            if (success)
            {
                NetworkPrefab = basePrefab.GetComponent<NetworkObject>();
                EditorUtility.SetDirty(this);
            }
            PrefabUtility.UnloadPrefabContents(copy);
            Debug.Log($"Generated Assets/NetworkPrefabs/{ItemName}Net.prefab");
        }
#endif
    }
}
