using TMPro;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public class ProgressBar : MonoBehaviour
    {
        [SerializeField] RectTransform bar;
        [SerializeField] TextMeshProUGUI text;
        RectTransform barParent;
        float vPadding = 2;
        float hPadding = 1;
        private void Awake()
        {
            barParent = bar.parent.GetComponent<RectTransform>();
            bar.anchoredPosition = new(hPadding, 0);
        }
        public void SetProgress(float progress, string v1, string v2)
        {
            bar.sizeDelta = (barParent.sizeDelta - new Vector2(hPadding * 2, vPadding * 2)) * new Vector2(progress, 1);
            this.text.text = $"{v1} / {v2}";
        }

        public void SetProgress(float progress, int amt, int max)
        {
            SetProgress(progress, amt.ToString(), max.ToString());
        }
    }
}
