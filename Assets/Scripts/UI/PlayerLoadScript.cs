using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypersycos.GERogueFrame
{
    internal class PlayerLoadScript : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI username;
        [SerializeField] Image characterImage;
        [SerializeField] Image loadingProgress;

        public void Setup(string uname, BasePCharacterSO character)
        {
            //username.text = uname;
            characterImage.sprite = character.Icon;
            loadingProgress.color = character.Color;
        }

        internal void SetProgress(float value)
        {
            loadingProgress.transform.localScale = new(value, 1, 1);
        }
    }
}