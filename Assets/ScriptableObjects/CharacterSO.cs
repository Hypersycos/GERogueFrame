using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;
using UnityEngine.UIElements;
using TMPro;
using Sirenix.Serialization;
using Sirenix.OdinInspector;





#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New Character", menuName = "GERogueFrame/Character", order = 0)]
    public class BaseCharacterSO : SerializedScriptableObject, IEquatable<BaseCharacterSO>
    {
        [Serializable]
        public struct ResourceRegen
        {
            public float Value;
            public float Delay;
            public float ReducedRate;
        }

        [Serializable]
        public struct Resource
        {
            public float Max;
            public ResourceRegen FlatRegen;
            public ResourceRegen MaxRegen;
            public ResourceRegen CurrentRegen;
            public StatType StatType;
        }

        [Serializable]
        public struct Defense
        {
            public float Max;
            public bool HasResist;
            public float Resist;
            public ResourceRegen FlatRegen;
            public ResourceRegen MaxRegen;
            public ResourceRegen CurrentRegen;
            public StatType StatType;
            public StatType ResistStatType;
        }

        public string UUID;
        public string CharacterName;
        public string CharacterDescription;
        public Texture2D Icon;
        public GameObject Model;
        public NetworkObject NetworkPrefab;
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

        public void Reset()
        {
            Energy = new Resource { StatType = StatType.StatTypeMap["Energy"], Max = 100, MaxRegen = new ResourceRegen { Value = .25f, Delay = 0.2f } };
            Health = new Defense { StatType = StatType.StatTypeMap["Health"], ResistStatType = StatType.StatTypeMap["Armour"], Max = 1000, FlatRegen = new ResourceRegen { Value = 2, Delay = 4, ReducedRate = .25f } };
            Shields = new Defense { StatType = StatType.StatTypeMap["Shields"], Max = 0, MaxRegen = new ResourceRegen { Value = .25f, Delay = 3 } };
            Overhealth = new Defense { StatType = StatType.StatTypeMap["OverHealth"], Max = -1, FlatRegen = new ResourceRegen { Value = -5, Delay = 0 }, CurrentRegen = new ResourceRegen { Value = -0.2f, Delay = 0 } };
        }

        public bool Equals(BaseCharacterSO other)
        {
            return UUID == other.UUID;
        }

        protected void ApplyResource(BoundedStatInstance inst, ref Resource values, float tickRate)
        {
            if (values.FlatRegen.Value != 0)
                inst.AddModifier(new StatRegenerationModifier(StatModifier.StackType.Flat, null, values.FlatRegen.Value, null, tickRate, values.FlatRegen.Delay, values.FlatRegen.ReducedRate));
            if (values.MaxRegen.Value != 0)
                inst.AddModifier(new StatRegenerationModifier(StatModifier.StackType.Multiplicative, null, values.MaxRegen.Value, null, tickRate, values.MaxRegen.Delay, values.MaxRegen.ReducedRate));
            if (values.CurrentRegen.Value != 0)
                inst.AddModifier(new StatRegenerationModifier(StatModifier.StackType.MultiplicativeAdditive, null, values.CurrentRegen.Value, null, tickRate, values.CurrentRegen.Delay, values.CurrentRegen.ReducedRate));
        }

        protected void ApplyDefense(DefenseStatInstance inst, ref Defense values, float tickRate)
        {
            if (values.FlatRegen.Value != 0)
                inst.AddModifier(new StatRegenerationModifier(StatModifier.StackType.Flat, null, values.FlatRegen.Value, null, tickRate, values.FlatRegen.Delay, values.FlatRegen.ReducedRate));
            if (values.MaxRegen.Value != 0)
                inst.AddModifier(new StatRegenerationModifier(StatModifier.StackType.Multiplicative, null, values.MaxRegen.Value, null, tickRate, values.MaxRegen.Delay, values.MaxRegen.ReducedRate));
            if (values.CurrentRegen.Value != 0)
                inst.AddModifier(new StatRegenerationModifier(StatModifier.StackType.MultiplicativeAdditive, null, values.CurrentRegen.Value, null, tickRate, values.CurrentRegen.Delay, values.CurrentRegen.ReducedRate));
        }

        protected void CreateDefense(ref DefenseStatInstance inst, ref Defense def, bool isOverhealth)
        {
            if (def.Max < 0)
            {
                if (isOverhealth)
                    def.Max = float.MaxValue;
                else
                    def.Max = 0;
            }
            if (def.HasResist)
                inst = new(def.Max, new SemiBoundedStatInstance(def.Resist, 0, def.ResistStatType), def.StatType, isOverhealth);
            else
                inst = new(def.Max, null, def.StatType, isOverhealth);
            if (def.Max > 0)
            {
                ApplyDefense(inst, ref def, 0);
            }
        }

        public void Apply(PlayerState state, ref List<BoundedStatInstance> defenses)
        {
            state.Energy = new(Energy.Max, 0, Energy.Max, Energy.StatType);
            if (Energy.Max > 0)
            {
                ApplyResource(state.Energy, ref Energy, 0);
            }

            CreateDefense(ref state.Health, ref Health, false);
            CreateDefense(ref state.Shields, ref Shields, false);
            CreateDefense(ref state.OverHealth, ref Overhealth, true);
            state.OverHealth.AddValue(100);

            state.ApplyDefensePool(new List<DefenseStatInstance>() { state.Health, state.Shields, state.OverHealth });
            state.StartSyncingValues(new List<ISyncStat>() { state.Health, state.Shields, state.OverHealth, state.Energy });

            defenses = new List<BoundedStatInstance>() { state.Health, state.Shields, state.OverHealth };
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
            }

            PlayerAbilityManager aManager = state.GetComponent<PlayerAbilityManager>();

            aManager.weapon = Weapon?.CreateAbility();
            aManager.weaponAlt = WeaponAlt?.CreateAbility();

            aManager.ability1 = Ability1?.CreateAbility();
            aManager.ability2 = Ability2?.CreateAbility();
            aManager.ability3 = Ability3?.CreateAbility();
            aManager.ability4 = Ability4?.CreateAbility();

            aManager.ultimate = Ultimate?.CreateAbility();
        }

#if UNITY_EDITOR
        [ContextMenu("Generate Network Prefab")]
        public void RegenerateNetworkPrefab()
        {
            GameObject copy = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(Model));
            Transform cameraTarget = copy.transform.Find("CameraTarget");

            copy.AddComponent<NetworkObject>();
            var manager = copy.AddComponent<PlayerCharacterManager>();
            manager.so = this;
            manager.netAnimator = copy.AddComponent<NetworkAnimator>();
            manager.netAnimator.AuthorityMode = NetworkAnimator.AuthorityModes.Owner;
            manager.netAnimator.Animator = copy.GetComponent<Animator>();
            manager.controller = copy.GetComponent<CharacterController>();
            manager.bar = PrefabUtility.LoadPrefabContents("Assets/Prefabs/FriendlyBar.prefab").transform;
            manager.bar.SetParent(cameraTarget);
            manager.bar.localPosition = new Vector3(0, 0.5f, 0);
            manager.bar.gameObject.SetActive(false);
            NetworkTransform netTransform = copy.AddComponent<NetworkTransform>();
            netTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            netTransform.SyncRotAngleX = false;
            netTransform.SyncRotAngleZ = false;
            copy.AddComponent<PlayerState>().DamageTickPrefab = AssetDatabase.LoadAssetAtPath<TMPro.TMP_Text>("Assets/UI/DamageNumber.prefab");
            copy.AddComponent<PlayerAbilityManager>();

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



    public static class SerializationExtensions
    {
        public static void ReadValueSafe(this FastBufferReader reader, out BaseCharacterSO so)
        {
            reader.ReadValueSafe(out string val);
            so = CharacterLoader.characterDict[val];
        }

        public static void WriteValueSafe(this FastBufferWriter writer, in BaseCharacterSO so)
        {
            writer.WriteValueSafe(so.UUID);
        }
    }
}
