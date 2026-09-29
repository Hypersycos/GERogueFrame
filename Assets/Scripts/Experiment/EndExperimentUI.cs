using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hypersycos.GERogueFrame
{
    public class EndExperimentUI : MonoBehaviour, IPointerClickHandler
    {
        public TMP_InputField resultsInput;
        public TextMeshProUGUI displayText;
        public CanvasGroup group;
        public Transform resultsPanel;
        public GameObject resultPanel;
        string serialized = "";
        internal void DisplayResults(FPSTracker fPSTracker, int minFPS, int maxFPS)
        {
            PersistentAudioManager.PlayMusic(true);

            serialized = $"{minFPS} - {maxFPS}\n";

            bool moved = false;

            foreach (var experiment in fPSTracker.finishedExperiments.OrderBy((x) => x.test))
            {
                GameObject outer = Instantiate(resultPanel, resultsPanel);
                Transform panel = outer.transform.GetChild(0);
                panel.GetChild(0).GetComponent<TextMeshProUGUI>().text = experiment.test.ToString();
                panel.GetChild(1).GetComponent<RectTransform>().sizeDelta = new(80, (experiment.target - experiment.test) * 3f);
                if ((experiment.target - experiment.test) * 3f > 50)
                {
                    panel.GetChild(1).GetComponentInChildren<TextMeshProUGUI>().text = (experiment.target - experiment.test).ToString();
                }
                else
                {
                    panel.GetChild(1).GetComponentInChildren<TextMeshProUGUI>().text = "";
                }
                panel.GetChild(2).GetComponent<RectTransform>().anchoredPosition = new(0, (experiment.target - experiment.test) * -3f - 50);
                panel.GetChild(2).GetComponent<TextMeshProUGUI>().text = experiment.target.ToString();

                outer.GetComponent<LayoutElement>().minHeight = (experiment.target - experiment.test) * 3f + 100 + (moved ? 30 : 0);
                outer.GetComponent<LayoutElement>().preferredHeight = (experiment.target - experiment.test) * 3f + 100 + (moved ? 30 : 0);

                if (moved)
                    panel.GetComponent<RectTransform>().anchoredPosition = new(0, -30);

                moved = !moved;
                serialized += $"{experiment.test}: {experiment.target} / {experiment.minRange} - {experiment.maxRange}\n";
            }

            foreach (var experiment in fPSTracker.experiments)
            {
                serialized += "\n";
                if (experiment.start == experiment.end)
                    serialized += "DUMMY: ";
                serialized += $"{experiment.start} -> {experiment.end} @ {experiment.transitionPoint}: {experiment.decision}";

                string beforeIssues = "";
                if (experiment.tooSlowBeforeCount > 0)
                {
                    serialized += $" with {experiment.tooSlowBeforeCount} of {experiment.totalBeforeCount} ({experiment.tooSlowBeforeTime}ms) slow";
                    if (experiment.tooFastBeforeCount > 0)
                        serialized += $" and {experiment.tooFastBeforeCount} ({experiment.tooFastBeforeTime}ms) fast @ {experiment.start}";
                    else
                        serialized += $" @ {experiment.start}";
                    beforeIssues = " and";
                }
                else if (experiment.tooFastBeforeCount > 0)
                {
                    serialized += $" with {experiment.tooFastBeforeCount} of {experiment.totalBeforeCount} ({experiment.tooFastBeforeTime}ms) fast @ {experiment.start}";
                    beforeIssues = " and";
                }

                if (experiment.tooSlowAfterCount > 0)
                {
                    serialized += $" {beforeIssues} with {experiment.tooSlowAfterCount} of {experiment.totalAfterCount} ({experiment.tooSlowAfterTime}ms) slow";
                    if (experiment.tooFastAfterCount > 0)
                        serialized += $" and {experiment.tooFastAfterCount} ({experiment.tooFastAfterTime}ms) fast @ {experiment.end}";
                    else
                        serialized += $" @ {experiment.end}";
                }
                else if (experiment.tooFastAfterCount > 0)
                {
                    serialized += $" {beforeIssues} with {experiment.tooFastAfterCount} of {experiment.totalAfterCount} ({experiment.tooFastAfterTime}ms) fast @ {experiment.end}";
                }
            }
            resultsInput.text = serialized;

            using (StreamWriter writer = new StreamWriter(Path.Combine(Application.persistentDataPath, "result.txt")))
            {
                writer.Write(serialized);
            }

            group.alpha = 1;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var linkIndex = TMP_TextUtilities.FindIntersectingLink(displayText, eventData.position, null);

            if (linkIndex == -1)
            {
                return;
            }

            var linkId = displayText.textInfo.linkInfo[linkIndex].GetLinkID();

            if (linkId == "results")
            {
                GUIUtility.systemCopyBuffer = serialized;
            }
            else if (linkId == "questionnaire")
            {
                Application.OpenURL("https://warwick.co1.qualtrics.com/jfe/form/SV_cGTprHX639SOBQa");
            }
        }

        public void Quit() => Application.Quit();
    }
}
