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

        public void Setup(string uname, BaseCharacterSO character)
        {
            username.text = uname;
            characterImage.sprite = character.Icon;
        }
    }
}