using Hypersycos.SaveSystem;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public static class CharacterLoader
    {
        public static List<CharacterSO> characters = new();
        public static Dictionary<string, CharacterSO> characterDict = new();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Load()
        {
#if UNITY_EDITOR
            CharacterDatabase db = AssetDatabase.LoadAssetAtPath<CharacterDatabase>("Assets/Resources/BaseCharacterDatabase.asset");
            characters.Clear();
            characterDict.Clear();
#else
            CharacterDatabase db = Resources.Load<CharacterDatabase>("BaseCharacterDatabase");
#endif
            foreach (CharacterSO ch in db.characterList)
            {
                characters.Add(ch);
                characterDict.Add(ch.name, ch);
                ch.UUID = ch.CharacterName;
            }
        }
    }
}
