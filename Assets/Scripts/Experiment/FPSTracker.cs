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

            public int tooSlowBeforeCount;
            public int tooFastBeforeCount;
            public int totalBeforeCount;

            public float tooSlowBeforeTime;
            public float tooFastBeforeTime;

            public int tooSlowAfterCount;
            public int tooFastAfterCount;
            public int totalAfterCount;

            public float tooSlowAfterTime;
            public float tooFastAfterTime;
        }


        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] TextMeshProUGUI aboveFPS;
        [SerializeField] TextMeshProUGUI belowFPS;

        [SerializeField] TypedRegisteredValueSO<int> MinVRRSetting;
        [SerializeField] TypedRegisteredValueSO<int> MaxVRRSetting;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        float minFrameTime;
        float maxFrameTime;
        List<float> tooSlowBefore = new();
        List<float> tooFastBefore = new();
        List<float> tooSlowAfter = new();
        List<float> tooFastAfter = new();

        Dictionary<int, ExperimentTracker> runningExperiments = new();
        public List<ExperimentTracker> finishedExperiments = new();
        public List<SingleExperimentTracker> experiments = new();
        SingleExperimentTracker currentExperiment = new();

        float timer = 0;
        int frameCounterBefore = 0;
        int frameCounterAfter = 0;
        int maxFPS => MaxVRRSetting.Value;
        int minFPS => MinVRRSetting.Value;

        static readonly int[] targets = { 30, 40, 50, 60, 70, 80, 100, 120, 140, 170, 200, 240 };

        bool paused = false;
        bool running = true;
        int recordingType;

        void Awake()
        {
            QualitySettings.vSyncCount = 0;
            canvasGroup = GetComponent<CanvasGroup>();

            //StartExperiment(maxFPS, maxFPS, 15, 5, 0, false);
            foreach(int target in targets)
            {
                if (target < minFPS)
                    continue;

                if (target >= maxFPS || (float)target / maxFPS > 0.9f)
                {
                    break;
                }

                runningExperiments.Add(target, new() { test = target, minRange = target, maxRange = maxFPS, target = (maxFPS - target) / 2 + target});
            }

            ControlsWrapper.Singleton.MenuOpened += Pause;
            ControlsWrapper.Singleton.MenuClosed += Unpause;
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
            tooSlowBefore.Clear();
            tooFastBefore.Clear();
            tooSlowAfter.Clear();
            tooFastAfter.Clear();
            frameCounterBefore = 0;
            frameCounterAfter = 0;
            timer = 0;
            recordingType = 0;
        }

        public void LerpTarget(int targetRate, float duration)
        {
            float startRate = Application.targetFrameRate;

            IEnumerator coroutine()
            {
                float start = Time.time;
                float targetTime = Time.time + duration;
                recordingType = 1;

                while (Time.time < targetTime)
                {
                    float target = Mathf.Lerp(startRate, targetRate, ((Time.time - start) / duration));
                    SetTarget(Mathf.RoundToInt(target));
                    yield return new WaitForEndOfFrame();
                }
                recordingType = 2;
                SetTarget(Mathf.RoundToInt(targetRate));
            }

            StartCoroutine(coroutine());
        }

        // Update is called once per frame
        void Update()
        {
            if (Time.deltaTime < minFrameTime)
            {
                switch (recordingType)
                {
                    case 0:
                        tooFastBefore.Add(Time.deltaTime);
                        break;
                    case 1:
                        break;
                    case 2:
                        tooFastAfter.Add(Time.deltaTime);
                        break;
                }
            }
            else if (Time.deltaTime > maxFrameTime)
            {
                switch (recordingType)
                {
                    case 0:
                        tooSlowBefore.Add(Time.deltaTime);
                        break;
                    case 1:
                        break;
                    case 2:
                        tooSlowAfter.Add(Time.deltaTime);
                        break;
                }
            }
            timer += Time.deltaTime;
            switch (recordingType)
            {
                case 0:
                    frameCounterBefore++;
                    break;
                case 2:
                    frameCounterAfter++;
                    break;
            }
        }

        public void Pause()
        {
            paused = true;
            enabled = false;
            Time.timeScale = 0;
        }

        public void Unpause()
        {
            paused = false;
            enabled = running;
            Time.timeScale = running ? 1 : 0;
        }

        public void UpChange() => RecordChange(ChangeType.Up);
        public void DownChange() => RecordChange(ChangeType.Down);
        public void NoChange() => RecordChange(ChangeType.NoChange);
        public void Changed() => RecordChange(ChangeType.Changed);

        public enum ChangeType
        {
            NoChange = 0,
            Up = 1,
            Down = 2,
            Changed = 3
        }

        void RecordChange(ChangeType type)
        {
            Debug.Log(type);
            var test = runningExperiments[currentExperiment.start];

            switch (type)
            {
                case ChangeType.NoChange:
                    test.minRange = test.target + 1;
                    break;
                case ChangeType.Up:
                case ChangeType.Down:
                case ChangeType.Changed:
                    test.maxRange = test.target - 1;
                    break;
                default:
                    break;
            }

            if (currentExperiment.start != currentExperiment.end)
            {
                if (test.maxRange - test.minRange < 3)
                {
                    runningExperiments.Remove(currentExperiment.start);
                    test.target = Mathf.RoundToInt((test.minRange + test.maxRange) / 2);
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
            int numExperiments = 0;
            foreach (var experiment in runningExperiments.Values)
            {
                numExperiments += Mathf.CeilToInt(Mathf.Log(experiment.maxRange - experiment.minRange, 2)) - 1;
            }

            numExperiments += 2;
            numExperiments = Mathf.CeilToInt(numExperiments / 0.9f);

            GameObject.FindWithTag("Managers").GetComponent<ObjectiveManager>().roundEndTime.Value = Time.time + numExperiments * 15;
            DoNext(false);
        }

        void DisplayResults()
        {
            Time.timeScale = 0;
            GameObject.FindWithTag("EndExperimentUI").GetComponent<EndExperimentUI>().DisplayResults(this, minFPS, maxFPS);
        }

        void DoNext(bool fadeOut = true)
        {
            bool dummy = Random.Range(0f,100f) < 10f;
            if (runningExperiments.Count == 0)
            {
                DisplayResults();
                return;
            }
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
                const float fadeDuration = 0.25f;
                const float timeScaleDuration = 0.5f;

                SetTarget(startFPS);

                if (fadeOut)
                {
                    ControlsWrapper.Singleton.SetUIState(false);

                    startFade = Time.realtimeSinceStartup;
                    endFade = startFade + fadeDuration;
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;

                    while (Time.realtimeSinceStartup < endFade)
                    {
                        canvasGroup.alpha = 1 - (Time.realtimeSinceStartup - startFade) / fadeDuration;
                        yield return null;
                    }
                    canvasGroup.alpha = 0;


                    startFade = Time.realtimeSinceStartup;
                    endFade = startFade + timeScaleDuration;
                    while (Time.realtimeSinceStartup < endFade)
                    {
                        Time.timeScale = paused ? 0 : (Time.realtimeSinceStartup - startFade) / timeScaleDuration;
                        yield return null;
                    }
                    Time.timeScale = paused ? 0 : 1;
                }
                ResetStats(startFPS);
                running = true;
                enabled = !paused;

                if (endFPS != startFPS)
                {
                    yield return new WaitForSecondsRealtime(transitionPoint - transitionDuration / 2);

                    if (transitionDuration > 0)
                        LerpTarget(endFPS, transitionDuration);
                    else
                    {
                        recordingType = 2;
                        SetTarget(endFPS);
                    }

                    yield return new WaitForSecondsRealtime(duration - transitionPoint + transitionDuration / 2);
                }
                else
                {
                    yield return new WaitForSecondsRealtime(duration);
                }

                running = false;
                enabled = false;

                /*if (tooFast.Count > 0)
                    aboveFPS.text = $"{tooFast.Count * 100 / frameCounter}% of frames were above the expected frame rate";
                else*/
                    aboveFPS.text = "";

                /*if (tooSlow.Count > 0)
                    //belowFPS.text = $"{tooSlow.Count * 100 / frameCounter}% of frames were below the expected frame rate";
                else*/
                    belowFPS.text = "";

                currentExperiment.tooSlowBeforeCount = tooSlowBefore.Count;
                currentExperiment.tooFastBeforeCount = tooFastBefore.Count;
                currentExperiment.tooSlowBeforeTime = tooSlowBefore.Sum() * 1000;
                currentExperiment.tooFastBeforeTime = tooFastBefore.Sum() * 1000;
                currentExperiment.totalBeforeCount = frameCounterBefore;

                currentExperiment.tooSlowAfterCount = tooSlowAfter.Count;
                currentExperiment.tooFastAfterCount = tooFastAfter.Count;
                currentExperiment.tooSlowAfterTime = tooSlowAfter.Sum() * 1000;
                currentExperiment.tooFastAfterTime = tooFastAfter.Sum() * 1000;
                currentExperiment.totalAfterCount = frameCounterAfter;

                startFade = Time.realtimeSinceStartup;
                endFade = startFade + timeScaleDuration;
                while (Time.realtimeSinceStartup < endFade)
                {
                    Time.timeScale = 1 - (Time.realtimeSinceStartup - startFade) / timeScaleDuration;
                    yield return null;
                }
                Time.timeScale = 0;

                startFade = Time.realtimeSinceStartup;
                endFade = startFade + fadeDuration;

                ControlsWrapper.Singleton.SetUIState(true);
                while (Time.realtimeSinceStartup < endFade)
                {
                    canvasGroup.alpha = (Time.realtimeSinceStartup - startFade) / fadeDuration;
                    yield return null;
                }
                canvasGroup.alpha = 1;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;

                SetTarget(maxFPS);

/*                yield return new WaitForSecondsRealtime(0.5f);

                if (Random.Range(0, 20) < currentExperiment.end - currentExperiment.start)
                    Changed();
                else
                    NoChange();*/
            }

            StartCoroutine(coroutine());
        }
    }
}
