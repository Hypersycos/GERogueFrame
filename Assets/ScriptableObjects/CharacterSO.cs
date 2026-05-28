using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New Character", menuName = "GERogueFrame/Character", order = 0)]
    public class CharacterSO : ScriptableObject
    {
        public uint UUID;
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

        public AbilitySO upgradeTree;
    }
}
