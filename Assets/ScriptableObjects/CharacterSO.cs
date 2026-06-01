using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New Character", menuName = "GERogueFrame/Character", order = 0)]
    public class CharacterSO : ScriptableObject, INetworkSerializable
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

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref UUID);
        }
    }
}
