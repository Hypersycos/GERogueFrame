using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    class WrinklyMountains : IMapGenerator
    {
        int octaves = 7;
        const float perlinScale = 0.01f / 16 / 2;
        Vector2[] octaveOffsets;
        static List<Func<Vector2, float>> generators = new() { gen2, gen1, gen1, gen1, gen1, gen1, gen2, noise };

        private static float noise(Vector2 v) => Mathf.PerlinNoise(v.x, v.y);

        private static float gen1(Vector2 v)
        {
            Vector2 q = new Vector2(noise(v), noise(v + new Vector2(5.2f, 1.3f)));

            return noise(v + 4 * q);
        }

        private static float gen2(Vector2 v)
        {
            Vector2 q = new Vector2(noise(v), noise(v + new Vector2(5.2f, 1.3f)));
            Vector2 r = new Vector2(noise(v + 4 * q + new Vector2(1.7f, 9.2f)),
                                    noise(v + 4 * q + new Vector2(8.3f, 2.8f)));

            return noise(v + 4 * r);
        }
        public float GetValue(int x, int y)
        {
            Vector2 p = new Vector2(x, y) * perlinScale;
            float result = generators[0](p + octaveOffsets[0]);

            for (int i = 1; i < octaves; i++)
            {
                float mask = Mathf.SmoothStep(1.0f, i > 4 ? 0.5f : 0.15f, result);

                Vector2 op = p * (1 << i) + octaveOffsets[i];
                float octave = generators[i](op) - .5f;
                result += octave * mask / (1 << i);

                if (i == 4 && result <= 0.3f)
                    result = Mathf.SmoothStep(result * result * result, result, result / 0.3f);
            }
            return result;
        }

        public void Setup(int seed, out float resolution, out float heightScale)
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

            resolution = 2;
            heightScale = 100;
        }
    }
}
