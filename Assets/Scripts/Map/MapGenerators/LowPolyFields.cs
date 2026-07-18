/*using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    class LowPolyFields : IMapGenerator
    {
        int octaves = 4;
        const float perlinScale = 1.2f;
        Vector2[] octaveOffsets;

        private static float noise(Vector2 v) => Mathf.PerlinNoise(v.x, v.y);
        public float GetValue(int x, int y)
        {
            Vector2 p = new Vector2(x, y) * perlinScale;
            float result = noise(p + octaveOffsets[0]);

            for (int i = 1; i < octaves; i++)
            {
                float mask = Mathf.SmoothStep(1.0f, 0.15f, result);

                Vector2 op = p * (1 << i) + octaveOffsets[i];
                float octave = noise(op) - .5f;
                result += octave * mask / (1 << i);
            }
            return result;
        }

        public void Setup(int seed, int width, int height, GameObject parent)
        {
            octaveOffsets = new Vector2[octaves];

            //TODO: Replace with platform-agnostic random
            System.Random rand = new System.Random(seed);

            for (int o = 0; o < octaves; o++)
            {
                float offsetX = rand.Next(-100, 100);
                float offsetY = rand.Next(-100, 100);
                octaveOffsets[o] = new Vector2(offsetX, offsetY);
            }
        }
    }
}
*/