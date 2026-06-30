using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Hypersycos.GERogueFrame
{
    public class LoadingScreenManager : MonoBehaviour
    {
        [SerializeField] PersistentStateManager stateManager;
        [SerializeField] PlayerLoadScript playerLoadPrefab;
        [SerializeField] RectTransform playerLoadHolder;

        MapGenerator mapGen;
        public void ShowMapLoad()
        {
            gameObject.SetActive(true);
            foreach (var player in stateManager.playerCharacterMap)
            {
                var playerImg = Instantiate(playerLoadPrefab, playerLoadHolder);
                playerImg.Setup(player.Key.ToString(), stateManager.availableCharacters[(int)player.Value.characterID]);
            }
        }

        public void DisplayMapProgress()
        {
            mapGen = FindAnyObjectByType<MapGenerator>();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
