using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hypersycos.GERogueFrame
{
    public interface IMapGenerator
    {
        public void Setup(int seed, out float resolution, out float heightScale);
        public float GetValue(int x, int y);
    }

    [RequireComponent(typeof(TerrainDrawer))]
    public class MapGenerator : MonoBehaviour
    {
        public void Start()
        {
            GenerateFromSeed(UnityEngine.Random.Range(0, 10000), 1000, 1000, new LowPolyFields());
        }

        public void GenerateFromSeed(int seed, int width, int height, IMapGenerator generator)
        {
            generator.Setup(seed, out float resolution, out float heightScale);

            width = Mathf.FloorToInt(width * resolution);
            height = Mathf.FloorToInt(height * resolution);

            var drawer = GetComponent<TerrainDrawer>();
            drawer.scale = new Vector3(1 / resolution, heightScale, 1 / resolution);
            drawer.SetSize(width, height, out float[,] heightMap);

            for (int j = 0; j < heightMap.GetLength(1); ++j)
            {
                for (int i = 0; i < heightMap.GetLength(0); ++i)
                {
                    heightMap[i, j] = generator.GetValue(i, j);
                }
            }
            drawer.BuildMeshes();
        }
    }
}
