using Hypersycos.SaveSystem;
using Hypersycos.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Hypersycos.GERogueFrame
{
    public class FPSTracker : MonoBehaviour
    {
        public class ExperimentTracker
        {
            public int test;
            public int target;
            public int minRange;
            public int maxRange;
        }

        public struct SingleExperimentTracker
        {
            public int start;
            public int end;
            public float transitionPoint;
            public ChangeType decision;

            public int tooSlowCount;
            public int tooFastCount;

            public float tooSlowTime;
            public float tooFastTime;
        }


        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] TextMeshProUGUI aboveFPS;
        [SerializeField] TextMeshProUGUI belowFPS;

        [SerializeField] TypedRegisteredValueSO<int> MinVRRSetting;
        [SerializeField] TypedRegisteredValueSO<int> MaxVRRSetting;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        float minFrameTime;
        float maxFrameTime;
        List<float> tooSlow = new();
        List<float> tooFast = new();

        Dictionary<int, ExperimentTracker> runningExperiments;
        List<ExperimentTracker> finishedExperiments;
        List<SingleExperimentTracker> experiments;
        SingleExperimentTracker currentExperiment;

        float timer = 0;
        int frameCounter = 0;
        int maxFPS => MaxVRRSetting.Value;
        int minFPS => MinVRRSetting.Value;

        static readonly int[] targets = { 30, 40, 50, 60, 70, 80, 100, 120, 140, 170, 200, 240 };

        void Awake()
        {
            QualitySettings.vSyncCount = 0;
            canvasGroup = GetComponent<CanvasGroup>();

            //StartExperiment(maxFPS, maxFPS, 15, 5, 0, false);
            foreach(int target in targets)
            {
                if (target < minFPS)
                    continue;

                if (target > maxFPS)
                    break;

                runningExperiments.Add(target, new() { test = target, minRange = target, maxRange = maxFPS, target = (maxFPS - target) / 2 + target});
            }
        }

        public void SetTarget(int targetRate)
        {
            Application.targetFrameRate = targetRate;

            float frameTime = 1f / targetRate;
            minFrameTime = frameTime * 0.9f;
            maxFrameTime = frameTime * 1.1f;
        }

        public void ResetStats(int targetRate)
        {
            SetTarget(targetRate);
            tooSlow.Clear();
            tooFast.Clear();
            frameCounter = 0;
            timer = 0;
        }

        public void LerpTarget(int targetRate, float duration)
        {
            float startRate = Application.targetFrameRate;

            IEnumerator coroutine()
            {
                float start = Time.time;
                float targetTime = Time.time + duration;

                while (Time.time < targetTime)
                {
                    float target = Mathf.Lerp(startRate, targetRate, ((Time.time - start) / duration));
                    SetTarget(Mathf.RoundToInt(target));
                    yield return new WaitForEndOfFrame();
                }
                SetTarget(Mathf.RoundToInt(targetRate));
            }

            StartCoroutine(coroutine());
        }

        // Update is called once per frame
        void Update()
        {
            if (Time.deltaTime < minFrameTime)
            {
                tooFast.Add(timer);
                //Debug.Log($"Fast: {Time.deltaTime} / {minFrameTime}");
            }
            else if (Time.deltaTime > maxFrameTime)
            {
                tooSlow.Add(timer);
                //Debug.Log($"Slow: {Time.deltaTime} / {maxFrameTime}");
            }
            timer += Time.deltaTime;
            frameCounter++;
        }

        public void UpChange() => RecordChange(ChangeType.Up);
        public void DownChange() => RecordChange(ChangeType.Down);
        public void NoChange() => RecordChange(ChangeType.None);

        public enum ChangeType
        {
            None = 0,
            Up = 1,
            Down = 2,
        }

        void RecordChange(ChangeType type)
        {
            var test = runningExperiments[currentExperiment.start];
            if (currentExperiment.start != currentExperiment.end)
            {
                switch (type)
                {
                    case ChangeType.None:
                        test.minRange = (test.minRange + test.maxRange) / 2;
                        break;
                    case ChangeType.Up:
                    case ChangeType.Down:
                        test.maxRange = (test.minRange + test.maxRange) / 2;
                        break;
                    default:
                        break;
                }

                if (test.maxRange - test.minRange < 3)
                {
                    runningExperiments.Remove(currentExperiment.start);
                    finishedExperiments.Add(test);
                }
                else
                {
                    test.target = (test.maxRange - test.minRange) / 2 + test.minRange;
                }
            }

            currentExperiment.decision = type;
            experiments.Add(currentExperiment);
            DoNext();
        }

        public void StartRun()
        {
            DoNext(false);
        }

        void DoNext(bool fadeOut = true)
        {
            bool dummy = Random.Range(0f,100f) < 5f;
            int start = runningExperiments.Keys.ToList().TakeRandomFromReadOnly();
            int end;
            if (dummy)
                end = start;
            else
                end = runningExperiments[start].target;
            StartExperiment(start, end, 15, Random.Range(5f, 10f), Random.Range(0.25f, 0.5f), fadeOut);
        }

        void StartExperiment(int startFPS, int endFPS, float duration, float transitionPoint, float transitionDuration, bool fadeOut = true)
        {
            Debug.Log($"Experiment: {duration}s of {startFPS}->{endFPS} @ {transitionPoint}s over {transitionDuration}s");
            currentExperiment = new() { start = startFPS, end = endFPS, transitionPoint = transitionPoint };
            IEnumerator coroutine()
            {
                float startFade, endFade;
                if (fadeOut)
                {
                    SetTarget(startFPS);
                    ControlsWrapper.Singleton.SetUIState(false);

                    startFade = Time.realtimeSinceStartup;
                    endFade = startFade + 1;
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;

                    while (Time.realtimeSinceStartup < endFade)
                    {
                        canvasGroup.alpha = 1 - (Time.realtimeSinceStartup - startFade);
                        yield return null;
                    }
                    canvasGroup.alpha = 0;


                    startFade = Time.realtimeSinceStartup;
                    endFade = startFade + 2;
                    while (Time.realtimeSinceStartup < endFade)
                    {
                        Time.timeScale = (Time.realtimeSinceStartup - startFade) / 2;
                        yield return null;
                    }
                    Time.timeScale = 1;
                }
                ResetStats(startFPS);
                enabled = true;

                if (endFPS != startFPS)
                {
                    yield return new WaitForSecondsRealtime(transitionPoint - transitionDuration / 2);

                    if (transitionDuration > 0)
                        LerpTarget(endFPS, transitionDuration);
                    else
                        SetTarget(endFPS);

                    yield return new WaitForSecondsRealtime(duration - transitionPoint + transitionDuration / 2);
                }
                else
                {
                    yield return new WaitForSecondsRealtime(duration);
                }

                enabled = false;

                /*if (tooFast.Count > 0)
                    aboveFPS.text = $"{tooFast.Count * 100 / frameCounter}% of frames were above the expected frame rate";
                else*/
                    aboveFPS.text = "";

                /*if (tooSlow.Count > 0)
                    //belowFPS.text = $"{tooSlow.Count * 100 / frameCounter}% of frames were below the expected frame rate";
                else*/
                    belowFPS.text = "";

                currentExperiment.tooSlowCount = tooSlow.Count;
                currentExperiment.tooFastCount = tooFast.Count;
                currentExperiment.tooSlowTime = tooSlow.Sum();
                currentExperiment.tooFastTime = tooFast.Sum();

                startFade = Time.realtimeSinceStartup;
                endFade = startFade + 2;
                while (Time.realtimeSinceStartup < endFade)
                {
                    Time.timeScale = 1 - (Time.realtimeSinceStartup - startFade) / 2;
                    yield return null;
                }
                Time.timeScale = 0;

                ControlsWrapper.Singleton.SetUIState(true);
                while (Time.realtimeSinceStartup < endFade)
                {
                    canvasGroup.alpha = (Time.realtimeSinceStartup - startFade);
                    yield return null;
                }
                canvasGroup.alpha = 1;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            StartCoroutine(coroutine());
        }
    }
}
