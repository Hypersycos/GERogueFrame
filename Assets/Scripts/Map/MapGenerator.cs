using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hypersycos.GERogueFrame
{
    public interface IMapGenerator
    {
        public Task Setup(int seed, int width, int height, GameObject parent);
    }

    public class MapGenerator : MonoBehaviour
    {
        public MapGeneratorSO so;

        Task generator;

        float time = 0;
        public void Start()
        {
            //generator = GenerateFromSeed(UnityEngine.Random.Range(0, 10000), 1000, 1000, so);
        }

        public async Task GenerateFromSeed(int seed, int width, int height, MapGeneratorSO generator)
        {
            GameObject prefab = Instantiate(generator.worldPrefab, transform);
            await generator.generator.Setup(seed, width, height, prefab);
        }

        public void Update()
        {
            time += Time.deltaTime;
            if (generator == null)
            {
                if (time > 3)
                {
                    time = 0;
                    Debug.Log("Starting generation!");
                    generator = GenerateFromSeed(UnityEngine.Random.Range(0, 10000), 1000, 1000, so);
                }
            }
            else if (generator.IsCompleted)
            {
                Debug.Log($"Took {time}s to generate.");
                time = -1000000;
                generator = null;
            }
        }
    }
}
