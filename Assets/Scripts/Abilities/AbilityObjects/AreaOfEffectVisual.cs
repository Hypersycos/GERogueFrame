using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public class AreaOfEffectVisual : MonoBehaviour
    { //Expands from startR to endR over rTime
      //with alpha startAlpha to endAlpha over aTime
        public float startR;
        public float endR;
        public float rTime;
        public float startAlpha;
        public float endAlpha;
        public float aTime;

        private float timer = 0;
        private float maxTime = 1;

        private void Start()
        {
            maxTime = Mathf.Max(rTime, aTime);
        }

        private void FixedUpdate()
        {
            timer += Time.fixedDeltaTime;
            if (timer > maxTime)
                Destroy(gameObject);
            float scale = Mathf.Lerp(startR, endR, timer / rTime);
            transform.localScale = new Vector3(scale, scale, scale);
            float alpha = Mathf.Lerp(startAlpha, endAlpha, timer / aTime);

            Material material = GetComponent<Renderer>().material;
            Color c = material.color;
            c.a = alpha;
            material.color = c;
        }
    }
}
