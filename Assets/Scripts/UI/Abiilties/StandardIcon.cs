using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypersycos.GERogueFrame
{
    class StandardIcon : AbilityIcon
    {
        Ability myAbility;
        PlayerState myState;

        [SerializeField] RectTransform cooldownTransform;
        [SerializeField] TextMeshProUGUI cooldownText;
        [SerializeField] TextMeshProUGUI manaText;
        [SerializeField] Image cantCast;
        [SerializeField] Image background;
        [SerializeField] Image icon;
        public override void SetAbility(Ability ability, IAbilityData idata, PlayerState state)
        {
            BaseAbilityData baseData = idata as BaseAbilityData;
            if (baseData == null)
                baseData = (idata as AbilitySO).As<BaseAbilityData>();

            myAbility = ability;
            myState = state;
            icon.sprite = baseData?.AbilityIcon;
            background.color = state.GetComponent<PlayerCharacterManager>().so.Color;
            
            if (idata is StandardAbilityData sData && sData.EnergyCost > 0)
            {
                manaText.text = sData.EnergyCost.ToString();
            }
            else
            {
                manaText.text = "";
            }
        }

        private void Update()
        {
            if (myAbility.CanCast(myState))
            {
                cantCast.enabled = false;
                cooldownText.enabled = false;
                cooldownTransform.localScale = Vector2.zero;
            }
            else
            {
                cantCast.enabled = true;
                if (myAbility is ICooldownAbility cooldownAbility && cooldownAbility.CurrentCooldown > 0)
                {
                    cooldownTransform.localScale = new(1, cooldownAbility.CurrentCooldownPercent);
                    cooldownText.enabled = true;
                    cooldownText.text = Mathf.CeilToInt(cooldownAbility.CurrentCooldown).ToString();
                }
            }
        }
    }
}
