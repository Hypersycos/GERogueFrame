using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Hypersycos.GERogueFrame
{
    public class LoadingScreenManager : MonoBehaviour
    {
        [SerializeField] PersistentStateManager stateManager;
        [SerializeField] PlayerLoadScript playerLoadPrefab;
        [SerializeField] RawImage mapBackground;
        [SerializeField] TextMeshProUGUI mapName;
        [SerializeField] RectTransform playerLoadHolder;

        MapGenerator mapGen;

        Dictionary<ulong, PlayerLoadScript> loadScripts = new();
        public void ShowMapLoad()
        {
            enabled = false;
            gameObject.SetActive(true);
            mapName.text = stateManager.mapState.so.ItemName;
            mapBackground.texture = stateManager.mapState.so.Image;
            mapBackground.GetComponent<AspectRatioFitter>().aspectRatio = (float)mapBackground.texture.width / mapBackground.texture.height;
            foreach (var player in stateManager.playerCharacterMap)
            {
                var playerImg = Instantiate(playerLoadPrefab, playerLoadHolder);
                playerImg.Setup(player.Key.ToString(), stateManager.SODB.PlayerCharacters[player.Value.characterID]);
                loadScripts.Add(player.Key, playerImg);
            }
        }

        public void DisplayMapProgress()
        {
            mapGen = FindAnyObjectByType<MapGenerator>();
            enabled = true;
        }

        void Update()
        {
            if (mapGen != null)
            {
                foreach (var progress in mapGen.mapBuildProgress)
                {
                    loadScripts[progress.Key].SetProgress(progress.Value);
                }
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            enabled = false;
        }
    }
}
