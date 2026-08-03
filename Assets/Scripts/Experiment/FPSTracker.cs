using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public class FPSTracker : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        float minFrameTime;
        float maxFrameTime;
        List<float> tooSlow;
        List<float> tooFast;

        float timer = 0;
        int frameCounter = 0;

        void Start()
        {
            QualitySettings.vSyncCount = 0;
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

        public void LerpTarget(int targetRate, float deltaTime)
        {
            float startRate = Application.targetFrameRate;

            IEnumerator coroutine()
            {
                float start = Time.time;
                float targetTime = Time.time + deltaTime;

                while (Time.time < targetTime)
                {
                    float target = Mathf.Lerp(startRate, targetRate, (Time.time - start / deltaTime));
                    SetTarget(Mathf.RoundToInt(target));
                    yield return new WaitForEndOfFrame();
                }
                Application.targetFrameRate = targetRate;
            }

            StartCoroutine(coroutine());
        }

        // Update is called once per frame
        void Update()
        {
            if (Time.deltaTime < minFrameTime)
            {
                tooFast.Add(timer);
            }
            else if (Time.deltaTime > maxFrameTime)
            {
                tooSlow.Add(timer);
            }
            timer += Time.deltaTime;
            frameCounter++;
        }
    }
}
