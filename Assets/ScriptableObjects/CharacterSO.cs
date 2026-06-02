using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New Character", menuName = "GERogueFrame/Character", order = 0)]
    public class CharacterSO : ScriptableObject, IEquatable<CharacterSO>
    {
        public string UUID;
        public string CharacterName;
        public string CharacterDescription;
        public Texture2D Icon;
        public GameObject Model;

        public int MaxHealth;
        public int MaxShields;
        public float SpeedMult;

        public AbilitySO ability1;
        public AbilitySO ability2;
        public AbilitySO ability3;
        public AbilitySO ability4;
        public AbilitySO ultimate;

        public UpgradeTreeSO upgradeTree;

        public bool Equals(CharacterSO other)
        {
            return UUID == other.UUID;
        }
    }

    public static class SerializationExtensions
    {
        public static void ReadValueSafe(this FastBufferReader reader, out CharacterSO so)
        {
            reader.ReadValueSafe(out string val);
            so = CharacterLoader.characterDict[val];
        }

        public static void WriteValueSafe(this FastBufferWriter writer, in CharacterSO so)
        {
            writer.WriteValueSafe(so.UUID);
        }
    }
}
