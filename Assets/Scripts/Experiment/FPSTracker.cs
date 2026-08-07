using Hypersycos.SaveSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Hypersycos.GERogueFrame
{
    public class FPSTracker : MonoBehaviour
    {
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

        float timer = 0;
        int frameCounter = 0;
        int maxFPS => MaxVRRSetting.Value;
        int minFPS => MinVRRSetting.Value;

        void Awake()
        {
            QualitySettings.vSyncCount = 0;
            canvasGroup = GetComponent<CanvasGroup>();

            //StartExperiment(maxFPS, maxFPS, 15, 5, 0, false);
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

        enum ChangeType
        {
            None = 0,
            Up = 1,
            Down = 2
        }

        void RecordChange(ChangeType type)
        {
            switch (type)
            {
                case ChangeType.None:
                    break;
                case ChangeType.Up:
                    break;
                case ChangeType.Down:
                    break;
                default:
                    break;
            }
            DoNext();
        }

        public void DoNext(bool fadeOut = true)
        {
            StartExperiment(Random.Range(minFPS, maxFPS+1), Random.Range(minFPS, maxFPS + 1), 20, Random.Range(5f, 15f), Random.Range(0f, 2f), fadeOut);
        }

        void StartExperiment(int startFPS, int endFPS, float duration, float transitionPoint, float transitionDuration, bool fadeOut = true)
        {
            Debug.Log($"Experiment: {duration}s of {startFPS}->{endFPS} @ {transitionPoint}s over {transitionDuration}s");
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

                if (tooFast.Count > 0)
                    aboveFPS.text = $"{tooFast.Count * 100 / frameCounter}% of frames were above the expected frame rate";
                else
                    aboveFPS.text = "";

                if (tooSlow.Count > 0)
                    belowFPS.text = $"{tooSlow.Count * 100 / frameCounter}% of frames were below the expected frame rate";
                else
                    belowFPS.text = "";

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
