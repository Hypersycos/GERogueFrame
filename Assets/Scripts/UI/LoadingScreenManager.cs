using Hypersycos.Utils;
using System;
using System.Collections;
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
        [SerializeField] Image fadeTransition;
        [SerializeField] TextMeshProUGUI mapName;
        [SerializeField] RectTransform playerLoadHolder;
        [SerializeField] CanvasGroup group;

        MapGenerator mapGen;

        Dictionary<ulong, PlayerLoadScript> loadScripts = new();

        public void ShowMapLoad()
        {
            enabled = false;
            gameObject.SetActive(true);
            playerLoadHolder.DestroyAllChildren();
            loadScripts.Clear();
            foreach (var player in stateManager.playerCharacterMap)
            {
                var playerImg = Instantiate(playerLoadPrefab, playerLoadHolder);
                playerImg.Setup(player.Key.ToString(), stateManager.SODB.PlayerCharacters[player.Value.characterID]);
                loadScripts.Add(player.Key, playerImg);
            }

            if (PersistentStateManager.Singleton.rounds > 1)
            {
                IEnumerator inner()
                {
                    float start = Time.time;
                    float end = start + 2;
                    while (Time.time < end)
                    {
                        group.alpha = Mathf.Lerp(1, 0, (end - Time.time)/2);
                        yield return new WaitForEndOfFrame();
                    }
                }

                StartCoroutine(inner());
            }
        }

        public void UpdateMapLoad()
        {
            mapName.text = stateManager.mapState.so.ItemName;
            mapBackground.texture = stateManager.mapState.so.Image;
            mapBackground.GetComponent<AspectRatioFitter>().aspectRatio = (float)mapBackground.texture.width / mapBackground.texture.height;
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
            IEnumerator inner()
            {
                float start = Time.time;
                float end = start + 1;
                while (Time.time < end)
                {
                    group.alpha = Mathf.Lerp(0, 1, end - Time.time);
                    yield return new WaitForEndOfFrame();
                }
                gameObject.SetActive(false);
                enabled = false;
            }
            StartCoroutine(inner());
        }

        internal void BackToLobby()
        {
            IEnumerator inner()
            {
                float start = Time.time;
                float end = start + 2;
                while (Time.time < end)
                {
                    group.alpha = Mathf.Lerp(1, 0, end - Time.time);
                    yield return new WaitForEndOfFrame();
                }
            }

            mapName.text = "Lobby";
            mapBackground.texture = null;

            playerLoadHolder.DestroyAllChildren();
            loadScripts.Clear();

            gameObject.SetActive(true);
            StartCoroutine(inner());
        }
    }
}
