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

        [SerializeField] float resolutionScalar = 1;
        [SerializeField] float heightScalar = 1;
        [SerializeField] float generationScalar = 1;

        const float _perlinScale = 0.01f / 16;

        float perlinScale;
        float resolution;
        float heightScale;

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
        public float GetValue(int x, int y)
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
            resolution = resolutionScalar;
            perlinScale = _perlinScale / (generationScalar * resolutionScalar);
            heightScale = heightScalar * 80;

            octaveOffsets = new Vector2[octaves];

            //TODO: Replace with platform-agnostic random
            System.Random rand = new System.Random(seed);

            for (int o = 0; o < octaves; o++)
            {
                float offsetX = rand.Next(-100, 100);
                float offsetY = rand.Next(-100, 100);
                octaveOffsets[o] = new Vector2(offsetX, offsetY);
            }

            width = Mathf.FloorToInt(width * resolution);
            height = Mathf.FloorToInt(height * resolution);

            //TODO: Replace with AddComponent
            var drawer = parent.GetComponent<TerrainDrawer>();
            drawer.scale = new Vector3(1f / resolution, heightScale, 1f / resolution);

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
                positions[i] = rotations[i] * (Vector3.forward * distance);
                GetHeightLerp(positions[i].x, positions[i].z, out float y);
                positions[i].y = y + 1;
            }
        }

        public float GetArea()
        {
            return heightMapData.Item2 * heightMapData.Item3 / (resolution * resolution);
        }

        public void GetObjectiveLocations(List<ObjectiveSO> objectives, out Vector3[] positions, out Quaternion[] rotations)
        {
            positions = new Vector3[objectives.Count];
            rotations = new Quaternion[objectives.Count];
            float aspect = heightMapData.Item2 / (float)heightMapData.Item3;
            int cols = Mathf.CeilToInt(Mathf.Sqrt(objectives.Count * aspect));
            int rows = Mathf.CeilToInt(objectives.Count / (float)cols);

            float dx = (heightMapData.Item2 - 2) / cols;
            float dy = (heightMapData.Item3 - 2) / rows;

            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    if (i * rows + j >= objectives.Count)
                        break;

                    float x = i * dx + .5f * dx;
                    float z = j * dy + .5f * dy;

                    x += UnityEngine.Random.Range(-.25f, .25f) * dx;
                    z += UnityEngine.Random.Range(-.25f, .25f) * dy;

                    x = heightmapXToX(x);
                    z = heightmapZToZ(z);

                    GetHeightLerp(x, z, out float y);

                    positions[i * rows + j] = new(x, y, z);
                    rotations[i * rows + j] = Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), Vector3.up);
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
            y = Mathf.Max(height(ix, iz), height(ix + 1, iz), height(ix, iz + 1), height(ix + 1, iz + 1)) * heightScale;
            return true;
        }

        public bool GetHeightMin(float x, float z, out float y)
        {
            int ix = Mathf.FloorToInt(xToHeightmapX(x));
            int iz = Mathf.FloorToInt(zToHeightmapZ(z));
            Func<int, int, float> height = (X, Y) => heightMapData.Item1[X + Y * heightMapData.Item2];
            y = Mathf.Min(height(ix, iz), height(ix + 1, iz), height(ix, iz + 1), height(ix + 1, iz + 1)) * heightScale;
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
            y *= heightScale;
            return true;
        }
    }
}
