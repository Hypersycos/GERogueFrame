using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;

namespace Hypersycos.GERogueFrame
{
    class WrinklyMountains : IMapGenerator
    {
        const int octaves = 7;
        const float perlinScale = 0.01f / 16 / 2;
        static Vector2[] octaveOffsets;
        static List<Func<float, float, float>> generators = new() { gen2, gen1, gen1, gen1, gen1, gen1, gen2, noise };

        Tuple<float[], int, int> heightMapData;

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
        public static float GetValue(int x, int y)
        {
            float px = x * perlinScale;
            float py = y * perlinScale;
            float result = generators[0](px + octaveOffsets[0].x, py + octaveOffsets[0].y);

            for (int i = 1; i < octaves; i++)
            {
                float mask = Mathf.SmoothStep(1.0f, i > 4 ? 0.5f : 0.15f, result);

                float opx = px * (1 << i) + octaveOffsets[i].x;
                float opy = py * (1 << i) + octaveOffsets[i].y;
                float octave = generators[i](opx, opy) - .5f;
                result += octave * mask / (1 << i);

                if (i == 4 && result <= 0.3f)
                    result = Mathf.SmoothStep(result * result * result, result, result / 0.3f);
            }
            return result;
        }

        public async Task Setup(int seed, int width, int height, GameObject parent, IProgress<float> progress)
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

            width = Mathf.FloorToInt(width * 2);
            height = Mathf.FloorToInt(height * 2);

            //TODO: Replace with AddComponent
            var drawer = parent.GetComponent<TerrainDrawer>();
            drawer.scale = new Vector3(1f / 2, 100, 1f / 2);

            heightMapData = await drawer.SetSize(width, height);
            float[] heightMap = heightMapData.Item1;
            int hMapX = heightMapData.Item2;
            int hMapY = heightMapData.Item3;

            int progressCount = 0;

            void updateProgress()
            {
                Interlocked.Increment(ref progressCount);
                progress.Report(progressCount / (hMapY * 2f));
            }

            await Task.Run(() =>
            {
                Parallel.For(0, hMapY, (j) =>
                {
                    for (int i = 0; i < hMapX; ++i)
                    {
                        heightMap[i + hMapX * j] = GetValue(i, j);
                    }
                    updateProgress();
                });
            });

            Progress<float> buildProgress = new Progress<float>();
            buildProgress.ProgressChanged += (_, x) => progress.Report(x / 2 + .5f);
            await drawer.BuildMeshes(buildProgress);
        }

        public void GetSpawnPoint(int count, out Vector3[] positions, out Quaternion[] rotations)
        {
            float rotation = 360 / count;
            float distance = count * 15 / (2 * Mathf.PI);

            positions = new Vector3[count];
            rotations = new Quaternion[count];

            for (int i = 0; i < count; i++)
            {
                rotations[i] = Quaternion.AngleAxis(rotation * i, Vector3.up);
                positions[i] = rotations[i] * (Vector3.forward * distance);// + Vector3.up * 2;
                Vector3 heightmapPos = positions[i] * 2 + new Vector3(heightMapData.Item2, heightMapData.Item3) / 2;

                int minX = Mathf.FloorToInt(heightmapPos.x);
                int minY = Mathf.FloorToInt(heightmapPos.z);

                int index1 = heightMapData.Item2 * minY + minX;
                int index2 = heightMapData.Item2 * (minY + 1) + minX;
                float maxHeight = Mathf.Max(heightMapData.Item1[index1], heightMapData.Item1[index1 + 1], heightMapData.Item1[index2], heightMapData.Item1[index2 + 1]);
                positions[i].y = maxHeight * 100 + 2;
            }
        }
    }
}
