using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace Hypersycos.GERogueFrame
{
    class Islands : IMapGenerator
    {
        const int baseOctaves = 2;
        const int topOctaves = 3;
        const int botOctaves = 3;
        const float perlinScale = 0.01f / 2;
        Vector2[] octaveOffsets;
        static List<Func<float, float, float>> baseGenerators = new() { noise, gen1 };
        static List<Func<float, float, float>> topGenerators = new() { gen2, gen1, gen2 };
        static List<Func<float, float, float>> botGenerators = new() { noise, gen1, gen2 };

        private static float noise(float x, float y) => Mathf.PerlinNoise(x, y);

        private static float gen1(float x, float y)
        {
            float qx = noise(x, y);
            float qy = noise(x + 5.2f, y + 1.3f);

            return noise(x + 4 * qx, y + 4 * qy);
        }

        private static float gen2(float x, float y)
        {
            float qx = noise(x, y);
            float qy = noise(x + 5.2f, y + 1.3f);

            float rx = noise(x + 4 * qx + 1.7f, y + 4 * qy + 9.2f);
            float ry = noise(x + 4 * qx + 8.3f, y + 4 * qy + 2.8f);

            return noise(x + 4 * rx, y + 4 * ry);
        }

        public float GetValueBase(int x, int y, Func<float, float> octaveMask, out int f)
        {
            float px = x * perlinScale;
            float py = y * perlinScale;
            float result = baseGenerators[0](px + octaveOffsets[0].x, py + octaveOffsets[0].y);

            for (int i = 1; i < baseOctaves; i++)
            {
                float opx = px * (1 << i) + octaveOffsets[i].x;
                float opy = py * (1 << i) + octaveOffsets[i].y;
                float octave = baseGenerators[i](opx, opy) - 0.5f;
                result += octave * octaveMask(result) / (2 << i);
            }

            f = 1;

            if (result < 0.45f)
            {
                float mult = Mathf.SmoothStep(1, 0, result / 0.45f * 2 - 1);
                float result2 = baseGenerators[0](px * 8 + octaveOffsets[baseOctaves].x, py * 8 + octaveOffsets[baseOctaves].y);
                f = 8;
                result2 -= 0.5f;
                result2 *= mult;
                result2 += 0.5f;
                if (result2 >= 0.55f)
                {
                    result = result2 - 0.1f;
                }
            }
            return result;
        }

        public float GetValueTop(int x, int y)
        {
            float result = GetValueBase(x, y, x => Mathf.SmoothStep(1, 0, x * 4 - 2.2f), out int f);

            if (result <= 0.52f)
            {
                if (f > 1)
                {
                    result -= 0.5f;
                    result *= 0.2f;
                    result += 0.5f;
                }
                return result;
            }

            int offset = 1 << baseOctaves;
            float px = x * perlinScale * f;
            float py = y * perlinScale * f;

            for (int i = 0; i < topOctaves; i++)
            {
                float mask1 = Mathf.SmoothStep(0, 1, result * 2 - 1);
                //float mask2 = Mathf.SmoothStep(1, 0, result * 4 - 3);
                float mask = mask1 / 2;

                float opx = px * (1 << i) + octaveOffsets[i].x;
                float opy = py * (1 << i) + octaveOffsets[i].y;
                float octave = topGenerators[i](opx, opy);
                result += octave * mask / offset;
                offset <<= 1;
            }

            if (f > 1)
            {
                result -= 0.5f;
                result *= 0.2f;
                result += 0.5f;
            }

            return result;
        }

        public float GetValueBot(int x, int y)
        {
            float result = GetValueBase(x, y, x => Mathf.SmoothStep(1, 0, x * 8 - 5), out int f);

            if (result <= 0.52f)
            {
                if (f == 1)
                {
                    result -= 0.5f;
                    result *= 2;
                    result += 0.5f;
                }
                return result;
            }

            int offset = 1 << baseOctaves - 1;
            float px = x * perlinScale * f;
            float py = y * perlinScale * f;

            for (int i = 0; i < botOctaves; i++)
            {
                float mask = Mathf.SmoothStep(0f, 1, result * 4 - 2);

                float opx = px * (1 << i) + octaveOffsets[i].x;
                float opy = py * (1 << i) + octaveOffsets[i].y;
                float octave = botGenerators[i](opx, opy) / 2;
                result += octave * mask / offset;
                offset <<= 1;
            }

            if (f > 1)
            {
                result -= 0.5f;
                result *= 0.3f;
                result += 0.5f;
            }

            return result;
        }

        public async Task Setup(int seed, int width, int height, GameObject parent)
        {
            octaveOffsets = new Vector2[Mathf.Max(botOctaves, topOctaves) + baseOctaves];

            //TODO: Replace with platform-agnostic random
            System.Random rand = new System.Random(seed);

            for (int o = 0; o < Mathf.Max(botOctaves, topOctaves) + baseOctaves; o++)
            {
                float offsetX = rand.Next(-100, 100);
                float offsetY = rand.Next(-100, 100);
                octaveOffsets[o] = new Vector2(offsetX, offsetY);
            }

            width = Mathf.FloorToInt(width);
            height = Mathf.FloorToInt(height);

            //TODO: Replace with AddComponent
            var topDrawer = parent.GetComponent<TerrainDrawer>();
            var botDrawer = parent.transform.GetChild(0).GetComponent<TerrainDrawer>();

            topDrawer.scale = new Vector3(1f, 80, 1f);
            botDrawer.scale = new Vector3(1, -1, 1);

            topDrawer.SetSize(width, height, out float[] heightMap, out int hMapWidth, out int hMapHeight);
            botDrawer.SetSize(width, height, out float[] botHeightMap, out _, out _);

            await Task.Run(() =>
            {
                Parallel.For(0, hMapHeight, (j) =>
                {
                    for (int i = 0; i < hMapWidth; ++i)
                    {
                        heightMap[i + j * hMapWidth] = GetValueTop(i, j);
                    }
                });

                Parallel.For(0, hMapHeight, (j) =>
                {
                    for (int i = 0; i < hMapWidth; ++i)
                    {
                        botHeightMap[i + j * hMapWidth] = GetValueBot(i, j);
                    }
                });
            });

            await topDrawer.BuildMeshes(0.45f, 1);
            await botDrawer.BuildMeshes(0.45f, 1);
        }
    }
}
