using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hypersycos.RogueFrame
{
    public class StatBarScript : MonoBehaviour
    {
        private class FadeTimer
        {
            public float Amount;
            public float Timer = 0;

            public FadeTimer(float amount)
            {
                Amount = amount;
            }
        }
        private class Bar
        {
            public RectTransform Current;
            public RectTransform Fade;
            public RectTransform Background;
            public float FadeValue = 0;
            public BoundedStatInstance Stat;
            public List<FadeTimer> fadeTimers = new();

            public Bar(RectTransform current, RectTransform fade, RectTransform background, BoundedStatInstance stat)
            {
                Current = current;
                Fade = fade;
                Background = background;
                Stat = stat;
            }

            public void AddTimer(FadeTimer timer)
            {
                fadeTimers.Add(timer);
                FadeValue += timer.Amount;
            }

            public float Tick(float deltaTime)
            {
                float change = 0;
                List<FadeTimer> toRemove = new();
                foreach(FadeTimer timer in fadeTimers)
                {
                    timer.Timer += deltaTime;
                    if (timer.Timer > Stat.StatType.BarFadeStart)
                    {
                        float diff = Stat.MaxValue * Stat.StatType.BarFadeRate * deltaTime;
                        change += Mathf.Min(diff, timer.Amount);
                        timer.Amount -= diff;
                    }
                    if (timer.Amount <= 0)
                    {
                        toRemove.Add(timer);
                    }
                }
                if (FadeValue < change)
                {
                    change = FadeValue;
                    fadeTimers.Clear();
                }
                else
                {
                    foreach (FadeTimer timer in toRemove)
                    {
                        fadeTimers.Remove(timer);
                    }
                }
                FadeValue -= change;
                return change;
            }
        }

        [SerializeField] private List<BoundedStatInstance> StatInstances;
        [SerializeField] private RectTransform BarPrefab;
        [SerializeField] private RectTransform Backing;
        [SerializeField] private bool ShowText;
        [SerializeField] private TMP_Text Text;
        private List<Bar> bars = new();
        private List<RectTransform> Images = new();
        private List<RectTransform> Fades = new();
        private List<RectTransform> Backs = new();
        float total = 0;
        float nonExtendedTotal = 0;
        float width => Backing.rect.width;
        // Start is called before the first frame update
        void Start()
        {
            foreach(BoundedStatInstance statInstance in StatInstances)
            {
                AddStat(statInstance);
            }
            Redraw();
        }

        void RecolorBar(Bar bar)
        {
            SemiBoundedStatInstance rStat = ((DefenseStatInstance)bar.Stat).ReductionStat;
            Color barColor = bar.Stat.StatType.Color;
            float temp = rStat.Value - 50;
            float coefficient = temp / (temp + 200);
            if (coefficient > 0)
                barColor = (1 - coefficient) * barColor + rStat.StatType.Color * coefficient;
            else
                barColor = (coefficient * 2 + 1) * barColor + rStat.StatType.BackColor * -(coefficient * 2);
            bar.Current.GetComponent<Image>().color = barColor;
        }

        void CreateBar(BoundedStatInstance statInstance)
        {
            StatType stat = statInstance.StatType;
            RectTransform current = Instantiate(BarPrefab, Backing);
            current.name = stat.Name;
            Images.Add(current);
            current.GetComponent<Image>().color = stat.Color;

            RectTransform fade = Instantiate(BarPrefab, Backing);
            fade.name = stat.Name + " Fade";
            Fades.Add(fade);
            fade.GetComponent<Image>().color = stat.FadeColor;

            RectTransform background = Instantiate(BarPrefab, Backing);
            background.name = stat.Name + " Background";
            Backs.Add(background);
            background.GetComponent<Image>().color = stat.BackColor;
            background.transform.SetAsLastSibling();

            Bar bar = new Bar(current, fade, background, statInstance);

            bars.Add(bar);

            if (statInstance is DefenseStatInstance)
            {
                DefenseStatInstance dStat = (DefenseStatInstance)statInstance;
                if (dStat.ReductionStat != null && dStat.ReductionStat.StatType != null)
                {
                    dStat.ReductionStat.OnChange.AddListener((_, _) => RecolorBar(bar));
                    RecolorBar(bar);
                }
            }
        }

        void AddStat(BoundedStatInstance stat)
        {
            if (!StatInstances.Contains(stat))
                StatInstances.Add(stat);
            CreateBar(stat);
            if (stat is DefenseStatInstance)
            {
                DefenseStatInstance dStat = (DefenseStatInstance)stat;
                if (dStat.IsOverhealth)
                {
                    total += stat.Value;
                }
                else
                {
                    total += stat.MaxValue;
                    nonExtendedTotal += stat.MaxValue;
                }
            }
            else
            {
                total += stat.MaxValue;
                nonExtendedTotal += stat.MaxValue;
            }
            stat.OnMaxIncrease.AddListener(UpdateTotal);
            stat.OnMaxDecrease.AddListener(UpdateTotal);
            stat.OnDecrease.AddListener(UpdateOne);
            stat.OnEmpty.AddListener((stat, change, _) => UpdateOne(stat, change));
            stat.OnIncrease.AddListener(UpdateOne);
            stat.OnFill.AddListener((stat, change, _) => UpdateOne(stat, change));
        }

        public void AddStats(List<BoundedStatInstance> stats)
        {
            foreach (BoundedStatInstance statInstance in stats)
            {
                AddStat(statInstance);
            }
            Redraw();
        }

        public void RemoveStats(List<BoundedStatInstance> stats)
        {
            //TODO: Fix
            throw new System.Exception("Don't remove stats lol");
#pragma warning disable CS0162 // Unreachable code detected
            foreach (BoundedStatInstance statInstance in stats)
            {
                int index = StatInstances.IndexOf(statInstance);
                StatInstances.Remove(statInstance);
                Images.RemoveAt(index);
                total -= statInstance.MaxValue;
                statInstance.OnMaxIncrease.RemoveListener(UpdateTotal);
                statInstance.OnMaxDecrease.RemoveListener(UpdateTotal);
                statInstance.OnDecrease.RemoveListener(UpdateOne);
                statInstance.OnEmpty.RemoveListener((stat, change, _) => UpdateOne(stat, change));
                statInstance.OnIncrease.RemoveListener(UpdateOne);
                statInstance.OnFill.RemoveListener((stat, change, _) => UpdateOne(stat, change));
            }
#pragma warning restore CS0162 // Unreachable code detected
            Redraw();
        }

        void CalculateTotal()
        {
            total = 0;
            nonExtendedTotal = 0;
            foreach (BoundedStatInstance statInstance in StatInstances)
            {
                if (statInstance is DefenseStatInstance)
                {
                    DefenseStatInstance dStat = (DefenseStatInstance)statInstance;
                    if (dStat.IsOverhealth)
                    {
                        total += statInstance.Value;
                        int index = StatInstances.IndexOf(statInstance);
                        total += bars[index].FadeValue;
                    }
                    else
                    {
                        total += statInstance.MaxValue;
                        nonExtendedTotal += statInstance.MaxValue;
                    }
                }
                else
                {
                    total += statInstance.MaxValue;
                    nonExtendedTotal += statInstance.MaxValue;
                }
            }
        }

        void UpdateTotal(BoundedStatInstance stat, float change)
        {
            CalculateTotal();
            Redraw();
        }

        void SetBackground()
        {
            for (int i = 0; i < StatInstances.Count; i++)
            {
                BoundedStatInstance statInstance = StatInstances[i];
                float max = statInstance.MaxValue / total * width;
                float hidden = max;
                hidden -= Fades[i].sizeDelta.x;
                hidden -= Images[i].sizeDelta.x;
                Backs[i].sizeDelta = new Vector2(hidden, 0);
            }
        }

        void UpdateOne(BoundedStatInstance stat, float change)
        {
            if (change == 0)
            {
                return;
            }

            float newSize = stat.Value / total * width;
            float chase = change / total * width;
            int index = StatInstances.IndexOf(stat);
            Images[index].sizeDelta = new Vector2(newSize, 0);

            if (stat is DefenseStatInstance)
            {
                DefenseStatInstance dStat = (DefenseStatInstance)stat;
                if (dStat.IsOverhealth)
                {
                    if (change < 0)
                    {
                        bars[index].AddTimer(new FadeTimer(-change));
                    }
                    else
                    {
                        total += change;
                    }
                    Redraw();
                    return;
                }
            }

            if (change < 0)
            {
                Vector2 sizeDelta = Fades[index].sizeDelta;
                Fades[index].sizeDelta = new Vector2(sizeDelta.x - chase, sizeDelta.y);
                bars[index].AddTimer(new FadeTimer(-change));
            }
            else
            {
                if (chase <= Fades[index].sizeDelta.x)
                { //TODO: Verify this doesn't bug out
                    Vector2 sizeDelta = Fades[index].sizeDelta;
                    Fades[index].sizeDelta = new Vector2(sizeDelta.x - chase, sizeDelta.y);
                }
                else
                { //TODO: Why is Backs[index] being touched here?
                    Fades[index].sizeDelta = new Vector2(0, 0);
                    float diff = Fades[index].sizeDelta.x - chase;
                    Vector2 sizeDelta = Backs[index].sizeDelta;
                    sizeDelta.x += diff;
                    Backs[index].sizeDelta = sizeDelta;
                }
            }
        }

        void Redraw()
        {
            foreach(Bar bar in bars)
            {
                float newSize = bar.Stat.Value / total * width;
                Vector2 size = new Vector2(newSize, 0);
                bar.Current.sizeDelta = size;

                float fadeSize = bar.FadeValue / total * width;
                size = new Vector2(fadeSize, 0);
                bar.Fade.sizeDelta = size;

                float hiddenSize = bar.Stat.MaxValue / total * width - newSize - fadeSize;
                size = new Vector2(hiddenSize, 0);
                bar.Background.sizeDelta = size;
            }
        }

        public void Update()
        {
            float value = 0;
            bool needsRedraw = false;
            foreach (Bar bar in bars)
            { //tick all the fades in each bar
                float change = bar.Tick(Time.deltaTime);
                if (change > 0)
                {
                    float newSize = bar.Stat.Value / total * width;

                    float fadeSize = bar.FadeValue / total * width;
                    Vector2 size = new Vector2(fadeSize, 0);
                    bar.Fade.sizeDelta = size;

                    if (bar.Stat is DefenseStatInstance && ((DefenseStatInstance)bar.Stat).IsOverhealth)
                    { //overhealth changing => total changed => redraw required
                        total -= change;
                        needsRedraw = true;
                    }
                    else
                    {
                        float hiddenSize = bar.Stat.MaxValue / total * width - newSize - fadeSize;
                        size = new Vector2(hiddenSize, 0);
                        bar.Background.sizeDelta = size;
                    }
                }
                value += bar.Stat.Value;
            }
            if (needsRedraw)
                Redraw();
            if (ShowText)
            {
                Text.text = ((int)value).ToString() + " / " + ((int)nonExtendedTotal).ToString();
            }
            else
            {
                Text.text = "";
            }
        }
    }
}
