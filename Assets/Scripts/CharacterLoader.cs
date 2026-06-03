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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Load()
        {
#if UNITY_EDITOR
            //CharacterDatabase db = AssetDatabase.LoadAssetAtPath<CharacterDatabase>("Assets/Resources/BaseCharacterDatabase.asset");
            characters.Clear();
            characterDict.Clear();
#endif
            CharacterDatabase db = Resources.Load<CharacterDatabase>("BaseCharacterDatabase");
            foreach (CharacterSO ch in db.characterList)
            {
                ch.UUID = ch.CharacterName;
                characters.Add(ch);
                characterDict.Add(ch.UUID, ch);
            }
        }
    }
}
