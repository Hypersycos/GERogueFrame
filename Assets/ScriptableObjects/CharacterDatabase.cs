using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    [CreateAssetMenu(fileName = "New CharacterDatabase", menuName = "GERogueFrame/CharacterDatabase", order = 0)]
    public class CharacterDatabase : ScriptableObject
    {
        public List<BaseCharacterSO> characterList;
    }
}
