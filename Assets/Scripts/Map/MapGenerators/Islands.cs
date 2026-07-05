using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Audio;

namespace Hypersycos.GERogueFrame
{
    class Islands : IMapGenerator
    {
        const int baseOctaves = 2;
        const int topOctaves = 3;
        const int botOctaves = 3;

        [SerializeField] float resolutionScalar = 1f;
        [SerializeField] float generationScalar = 1f;
        [SerializeField] float heightScalar = 80;
        const float _perlinScale = 0.01f / 2;

        float perlinScale;
        float resolution;
        float heightScale;

        const float bigIslandCutoff = 0.48f;
        const float smallIslandCutoff = 0.55f;

        Vector2[] octaveOffsets;
        static List<Func<float, float, float>> baseGenerators = new() { noise, gen1 };
        static List<Func<float, float, float>> topGenerators = new() { gen2, gen1, gen2 };
        static List<Func<float, float, float>> botGenerators = new() { noise, gen1, gen2 };

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

            if (result < bigIslandCutoff)
            {
                float mult = Mathf.SmoothStep(1, 0, result / bigIslandCutoff * 2 - 1);
                float result2 = baseGenerators[0](px * 8 + octaveOffsets[baseOctaves].x, py * 8 + octaveOffsets[baseOctaves].y);
                f = 8;
                result2 -= 0.5f;
                result2 *= mult;
                result2 += 0.5f;
                if (result2 >= smallIslandCutoff)
                {
                    result = result2 - smallIslandCutoff + bigIslandCutoff;
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
                if (f == 1 && result > 0.5f)
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

        public async Task Setup(int seed, int width, int height, GameObject parent, IProgress<float> progress)
        {
            octaveOffsets = new Vector2[Mathf.Max(botOctaves, topOctaves) + baseOctaves];

            resolution = resolutionScalar;
            perlinScale = _perlinScale / (generationScalar * resolutionScalar);
            heightScale = heightScalar * 80;

            //TODO: Replace with platform-agnostic random
            System.Random rand = new System.Random(seed);

            for (int o = 0; o < Mathf.Max(botOctaves, topOctaves) + baseOctaves; o++)
            {
                float offsetX = rand.Next(-100, 100);
                float offsetY = rand.Next(-100, 100);
                octaveOffsets[o] = new Vector2(offsetX, offsetY);
            }

            width = Mathf.FloorToInt(width * resolution);
            height = Mathf.FloorToInt(height * resolution);

            //TODO: Replace with AddComponent
            var topDrawer = parent.GetComponent<TerrainDrawer>();
            var botDrawer = parent.transform.GetChild(0).GetComponent<TerrainDrawer>();

            topDrawer.scale = new Vector3(1 / resolution, heightScale, 1 / resolution);
            botDrawer.scale = new Vector3(1f, -1, 1f);

            heightMapData = await topDrawer.SetSize(width, height, 200, 200);
            float[] heightMap = heightMapData.Item1;
            int hMapWidth = heightMapData.Item2;
            int hMapHeight = heightMapData.Item3;

            Tuple<float[], int, int> sizes2 = await botDrawer.SetSize(width, height, 200, 200);
            float[] botHeightMap = sizes2.Item1;

            int progressCount = 0;

            void updateProgress()
            {
                Interlocked.Increment(ref progressCount);
                progress.Report((progressCount * 200) / (hMapWidth * 8f));
            }

            await Task.Run(() =>
            {
                Parallel.For(0, hMapHeight, (j) =>
                {
                    for (int i = 0; i < hMapWidth; ++i)
                    {
                        heightMap[i + j * hMapWidth] = GetValueTop(i, j);
                    }
                    if (j % 200 == 99)
                        updateProgress();
                });

                Parallel.For(0, hMapHeight, (j) =>
                {
                    for (int i = 0; i < hMapWidth; ++i)
                    {
                        botHeightMap[i + j * hMapWidth] = GetValueBot(i, j);
                    }
                    if (j % 200 == 99)
                        updateProgress();
                });
            });

            Progress<float> buildProgress = new Progress<float>();
            EventHandler<float> update = (_, x) => progress.Report(x * 3f / 8 + .25f);
            buildProgress.ProgressChanged += update;
            await topDrawer.BuildMeshes(buildProgress, 0.5f);

            buildProgress.ProgressChanged -= update;
            update = (_, x) => progress.Report(x * 3f / 8 + 5f / 8);
            buildProgress.ProgressChanged += update;
            await botDrawer.BuildMeshes(buildProgress, 0.5f);
        }

        public void GetSpawnPoint(int count, out Vector3[] positions, out Quaternion[] rotations)
        {
            float rotation = 360 / count;
            float distance = count * 15 / (2 * Mathf.PI);

            positions = new Vector3[count];
            rotations = new Quaternion[count];

            bool succeeded = false;

            while (!succeeded)
            {
                Vector3 basePos = new Vector3(heightmapXToX(UnityEngine.Random.Range(1, heightMapData.Item2 - 1)),
                                              0,
                                              heightmapZToZ(UnityEngine.Random.Range(1, heightMapData.Item3 - 1))) * 0.4f;
                succeeded = true;

                for (int i = 0; i < count; i++)
                {
                    rotations[i] = Quaternion.AngleAxis(rotation * i, Vector3.up);
                    positions[i] = basePos + rotations[i] * (Vector3.forward * distance);

                    if (GetHeightLerp(positions[i].x, positions[i].z, out float y) && y > 0.51 * heightScale)
                    {
                        positions[i].y = y + 1;
                    }
                    else
                    {
                        succeeded = false;
                        break;
                    }
                }
            }
        }

        void IMapGenerator.ModifyPlayerOnOwner(GameObject player)
        {
            var mod = player.AddComponent<LowGravityAboveNothing>();
            mod.lowGravity = 0.1f;
            mod.mask = 1;
        }

        public float GetArea()
        {
            return heightMapData.Item2 * heightMapData.Item3 / (resolution * resolution * 2);
        }

        public void GetObjectiveLocations(List<ObjectiveSO> objectives, out Vector3[] positions, out Quaternion[] rotations)
        {
            positions = new Vector3[objectives.Count];
            rotations = new Quaternion[objectives.Count];
            float aspect = (heightMapData.Item2 - 2) / (float)(heightMapData.Item3 - 2);
            int cols = Mathf.RoundToInt(Mathf.Sqrt(objectives.Count * aspect));
            int rows = Mathf.CeilToInt(objectives.Count / (float)cols);

            float dx = (heightMapData.Item2 - 2) / cols;
            float dy = (heightMapData.Item3 - 2) / rows;

            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    if (i * rows + j >= objectives.Count)
                        break;

                    bool success = false;

                    int count = 0;
                    while (!success)
                    {
                        float x = i * dx + .5f * dx;
                        float z = j * dy + .5f * dy;

                        x += UnityEngine.Random.Range(-.25f, .25f) * dx * (count + 4f) / 4;
                        z += UnityEngine.Random.Range(-.25f, .25f) * dy * (count + 4f) / 4;

                        x = Mathf.Clamp(x, heightMapData.Item2 * 0.05f, heightMapData.Item2 * 0.95f);
                        z = Mathf.Clamp(z, heightMapData.Item3 * 0.05f, heightMapData.Item3 * 0.95f);

                        x = heightmapXToX(x);
                        z = heightmapZToZ(z);

                        if (GetHeightLerp(x, z, out float y) && y > 0.52 * heightScale)
                        {
                            positions[i * rows + j] = new Vector3(x, y, z);
                            rotations[i * rows + j] = Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), Vector3.up);
                            success = true;
                        }
                        else
                        {
                            count++;
                        }
                    }
                }
            }
        }

        float xToHeightmapX(float x) => x * resolution + (heightMapData.Item2 - 2) / 2 + 1;
        float zToHeightmapZ(float z) => z * resolution + (heightMapData.Item3 - 2) / 2 + 1;
        float heightmapXToX(float x) => (x - 1 - ((heightMapData.Item2 - 2) / 2)) / resolution;
        float heightmapZToZ(float z) => (z - 1 - ((heightMapData.Item3 - 2) / 2)) / resolution;

        public bool GetHeightMax(float x, float z, out float y)
        {
            int ix = Mathf.FloorToInt(xToHeightmapX(x));
            int iz = Mathf.FloorToInt(zToHeightmapZ(z));
            Func<int, int, float> height = (X, Y) => heightMapData.Item1[X + Y * heightMapData.Item2];
            y = Mathf.Min(height(ix, iz), height(ix + 1, iz), height(ix, iz + 1), height(ix + 1, iz + 1));
            if (y < 0.5)
                return false;
            y = Mathf.Max(height(ix, iz), height(ix + 1, iz), height(ix, iz + 1), height(ix + 1, iz + 1)) * heightScale;
            return true;
        }

        public bool GetHeightMin(float x, float z, out float y)
        {
            int ix = Mathf.FloorToInt(xToHeightmapX(x));
            int iz = Mathf.FloorToInt(zToHeightmapZ(z));
            Func<int, int, float> height = (X, Y) => heightMapData.Item1[X + Y * heightMapData.Item2];
            y = Mathf.Min(height(ix, iz), height(ix + 1, iz), height(ix, iz + 1), height(ix + 1, iz + 1)) * heightScale;
            if (y < 0.5)
                return false;
            return true;
        }

        public bool GetHeightLerp(float x, float z, out float y)
        {
            float t1 = xToHeightmapX(x);
            float t2 = zToHeightmapZ(z);

            int ix = Mathf.FloorToInt(t1);
            int iz = Mathf.FloorToInt(t2);

            Func<int, int, float> height = (X, Y) => heightMapData.Item1[X + Y * heightMapData.Item2];

            t1 -= ix;
            t2 -= iz;

            y = height(ix, iz) * t1 * t2 + height(ix + 1, iz) * (1 - t1) * t2
                + height(ix, iz + 1) * t1 * (1 - t2) + height(ix + 1, iz + 1) * (1 - t1) * (1 - t2);
            if (y < 0.5) return false;
            y *= heightScale;
            return true;
        }
    }
}
