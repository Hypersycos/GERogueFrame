using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hypersycos.GERogueFrame
{
    public class LinkHandler : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            var linkIndex = TMP_TextUtilities.FindIntersectingLink(GetComponent<TMP_Text>(), eventData.position, null);

            if (linkIndex == -1)
            {
                return;
            }

            var linkId = GetComponent<TMP_Text>().textInfo.linkInfo[linkIndex].GetLinkID();
            string linkString;

            switch (linkId)
            {
                case "VRRLink":
                    linkString = "https://livewarwickac-my.sharepoint.com/:w:/g/personal/u5753244_live_warwick_ac_uk/IQAdDjFOriHxTJ0XvwHpJKJ9Aen2e9cxQ3J7G1qChb1Xc2s?e=XIhYpp";
                    break;
                case "SurveyLink":
                    linkString = "https://warwick.co1.qualtrics.com/jfe/form/SV_cGTprHX639SOBQa";
                    break;
                case "PILLink":
                    linkString = "https://livewarwickac-my.sharepoint.com/:w:/g/personal/u5753244_live_warwick_ac_uk/IQDO5q0eMDcZTrEaumWTFqUHAYENvYIZRxMH3d0lHFLc9-c?e=cByo7G";
                    break;
                default:
                    linkString = "";
                    break;
            }

            if (linkString != "")
            {
                Application.OpenURL(linkString);
            }
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
