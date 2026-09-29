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
using Hypersycos.SaveSystem;
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

        [SerializeField] TypedRegisteredValueSO<int> MinVRRSetting;
        [SerializeField] Slider MinVRRSlider;
        [SerializeField] TMP_InputField MinTextValue;
        [SerializeField] TypedRegisteredValueSO<int> MaxVRRSetting;
        [SerializeField] Slider MaxVRRSlider;
        [SerializeField] TMP_InputField MaxTextValue;

        private double lastRefreshRate;

        public override void OnNetworkSpawn()
        {
            //Set character
            SelectCharacter();
            scrollbar.onValueChanged.AddListener(CheckForScroll);
            //Add scrollbar hook
            readyButton.onClick.AddListener(StartGame);
            settingsButton.onClick.AddListener(OpenSettings);

            lastRefreshRate = Screen.currentResolution.refreshRateRatio.value;
            UpdateSliderParams();
        }

        private void Update()
        {
            if (IsSpawned)
            {
                double currentRate = Screen.currentResolution.refreshRateRatio.value;

                // Compare using a tiny epsilon to handle minor floating point differences
                if (Math.Abs(currentRate - lastRefreshRate) > 0.01)
                {
                    lastRefreshRate = currentRate;
                    UpdateSliderParams();
                }
            }
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
            if (arg0 <= 0.01 && MinVRRSetting.Value != 0)
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
                    string formattedDesc = baseData.AbilityDescription.Replace("{{S|", "<b><i>");
                    formattedDesc = formattedDesc.Replace("}}", "</i></b>");
                    inst.GetComponentInChildren<TextMeshProUGUI>().text = $"<b>{baseData.AbilityName} ({typeNames[i]}): </b>{formattedDesc}";
                }
            }
        }

        public void UpdateSliderParams()
        {
            MinVRRSlider.maxValue = (float)Screen.currentResolution.refreshRateRatio.value;
            MaxVRRSlider.maxValue = (float)Screen.currentResolution.refreshRateRatio.value;

            MaxVRRSlider.value = (float)Screen.currentResolution.refreshRateRatio.value;
            UpdateMaximum((float)Screen.currentResolution.refreshRateRatio.value);

            MinVRRSlider.value = 0;
            UpdateMinimum(0);

            readyButton.interactable = false;
        }

        public void UpdateMinimum(int iValue)
        {
            MinTextValue.SetTextWithoutNotify(iValue.ToString());
            MinVRRSlider.SetValueWithoutNotify(iValue);
            MinVRRSetting.Value = iValue;
            if (iValue != 0)
                readyButton.interactable = true;
        }

        public void UpdateMinimum(float value) => UpdateMinimum(Mathf.RoundToInt(value));
        public void UpdateMinimum(string value)
        {
            if (int.TryParse(value, out int iValue) && iValue >= 0 && iValue <= MinVRRSlider.maxValue)
                UpdateMinimum(int.Parse(value));
            else
                MinTextValue.SetTextWithoutNotify(Mathf.RoundToInt(MinVRRSlider.value).ToString());
        }

        public void UpdateMaximum(int iValue)
        {
            MaxTextValue.SetTextWithoutNotify(iValue.ToString());
            MaxVRRSlider.SetValueWithoutNotify(iValue);
            MaxVRRSetting.Value = iValue;
        }

        public void UpdateMaximum(float value) => UpdateMaximum(Mathf.RoundToInt(value));
        public void UpdateMaximum(string value)
        {
            if (int.TryParse(value, out int iValue) && iValue >= 0 && iValue <= MaxVRRSlider.maxValue)
                UpdateMaximum(int.Parse(value));
            else
                MaxTextValue.SetTextWithoutNotify(Mathf.RoundToInt(MaxVRRSlider.value).ToString());
        }

        public void Quit() => Application.Quit();
    }
}