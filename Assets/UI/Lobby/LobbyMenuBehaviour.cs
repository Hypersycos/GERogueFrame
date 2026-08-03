using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine;
using Unity.VisualScripting;
using Hypersycos.Utils;
using System.Linq;
//using UnityEngine.UIElements;

namespace Hypersycos.GERogueFrame
{
    public class LobbyMenuBehaviour : NetworkBehaviour
    {

        NetworkManager networkManager;

        [SerializeField] Button readyButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Scrollbar scrollbar;

        Dictionary<ulong, GameObject> playerIcons = new();

        [SerializeField] Transform descriptionHolder;
        [SerializeField] GameObject descriptionPrefab;

        public override void OnNetworkSpawn()
        {
            //Set character
            SelectCharacter();
            scrollbar.onValueChanged.AddListener(CheckForScroll);
            //Add scrollbar hook
            readyButton.onClick.AddListener(StartGame);
            settingsButton.onClick.AddListener(OpenSettings);
        }

        private void OpenSettings()
        {
            ControlsWrapper.Singleton.OpenMenu(default);
        }

        private void StartGame()
        {
            foreach (ulong id in NetworkManager.ConnectedClientsIds)
            {
                PersistentStateManager.Singleton.SetPlayerCharacter(id, 0);
            }
            PersistentStateManager.Singleton.StartGame();
        }

        private void CheckForScroll(float arg0)
        {
            if (arg0 >= 0.99)
                readyButton.interactable = true;
        }

        private void SelectCharacter()
        {
            BasePCharacterSO character = SODatabase.LocalDB.PlayerCharacters[0];

            descriptionHolder.transform.DestroyAllChildren();

            IAbilityData[] datas = new IAbilityData[6] { character.Weapon, character.WeaponAlt, character.Ability1, character.Ability2, character.Ability3, character.Ability4 };
            string[] typeNames = new string[6] { "Primary Fire", "Alternative Fire", "Ability 1", "Ability 2", "Ability 3", "Ability 4" };

            for (int i = 0; i < datas.Length; i++)
            {
                var data = datas[i];

                if (data == null)
                    continue;

                BaseAbilityData baseData = null;
                if (data is BaseAbilityData bd)
                    baseData = bd;
                else if (data is AbilitySO so)
                    baseData = so.As<BaseAbilityData>();

                if (baseData != null)
                {
                    var inst = Instantiate(descriptionPrefab, descriptionHolder);
                    inst.GetComponentInChildren<Image>().sprite = baseData.AbilityIcon;
                    inst.GetComponentInChildren<TextMeshProUGUI>().text = $"<b>{baseData.AbilityName} ({typeNames[i]}): </b>{baseData.AbilityDescription}";
                }
            }

            readyButton.interactable = true;
        }
    }
}