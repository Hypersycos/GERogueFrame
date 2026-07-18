using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Hypersycos.Utils;

namespace Hypersycos.GERogueFrame
{
    class EnemySpawnManager : MonoBehaviour
    {
        public float credits;
        public float creditsPerSecond;

        public float ambientCredits;
        public float ambientCreditsPerSecond;

        public float difficultyMult => PersistentStateManager.Singleton.difficulty;

        public uint targetEnemyCount;
        //public float maxEnemyCount;

        public uint targetAmbientCount;
        public float maxAmbientCount;

        float enemyCostSum;
        float meanEnemyCost;

        List<EnemySO> enemies;

        HashSet<CharacterState> spawnedEnemies = new();
        uint spawnedEnemyCount = 0;
        uint ambientSpawnCount = 0;

        private void Start()
        {
            enemyCostSum = 0;
            enemies = SODatabase.NetworkedDB.Enemies.OrderBy((x) => x.spawnCost).ToList();
            foreach (EnemySO so in enemies)
            {
                enemyCostSum += so.spawnCost;
            }
            meanEnemyCost = enemyCostSum / enemies.Count;
        }

        private void FixedUpdate()
        {
            credits += Time.fixedDeltaTime * creditsPerSecond;
            ambientCredits += Time.fixedDeltaTime * ambientCreditsPerSecond;

            if (ShouldStartSpawn())
            {
                int spawnCount = 0;
                var chosenPlayer = NetworkManager.Singleton.ConnectedClientsList.TakeRandomFromReadOnly()
                                                 .PlayerObject.GetComponent<CharacterState>();
                while (ShouldContinueSpawning(spawnCount++))
                {
                    SpawnEnemy(chosenPlayer.CentrePos, 15, GetSpawnableEnemy(credits), 5, ref credits);
                }
            }

            if (ShouldStartAmbientSpawn())
            {
                int spawnCount = 0;
                var chosenPlayer = NetworkManager.Singleton.ConnectedClientsList.TakeRandomFromReadOnly()
                                                 .PlayerObject.GetComponent<CharacterState>();
                while (ShouldContinueAmbientSpawning(spawnCount++))
                {
                    SpawnAmbientEnemy(GetSpawnableEnemy(ambientCredits), chosenPlayer.CentrePos);
                }
            }
        }
        public EnemySO GetSpawnableEnemy(float credits)
        {
            int lb = 0;
            int ub = enemies.Count - 1;

            while (lb <= ub)
            {
                int mid = lb + (ub - lb) / 2;

                if (enemies[mid].spawnCost <= credits)
                {
                    lb = mid + 1;
                }
                else
                {
                    ub = mid - 1;
                }
            }

            return enemies[UnityEngine.Random.Range(0, lb)];
        }


        private void SpawnEnemy(Vector3 position, Quaternion rotation, EnemySO enemy, bool isAmbient)
        {
            var spawned = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(enemy.NetworkPrefab, destroyWithScene: true, position: position, rotation: rotation);
            var spawnedState = spawned.GetComponent<CharacterState>();
            spawnedEnemies.Add(spawnedState);
            spawnedState.OnKilled.AddListener((x, _) => spawnedEnemies.Remove(x));
            if (isAmbient)
                spawnedState.OnKilled.AddListener((x, _) => ambientSpawnCount--);
            else
                spawnedState.OnKilled.AddListener((x, _) => spawnedEnemyCount--);
        }

        private bool GetNavmeshPosition(Vector3 position, EnemySO enemy, float maxDist, float maxHDist, float maxVDist, out Vector3 result)
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, maxDist, enemy.NetworkPrefab.GetComponent<NavMeshAgent>().areaMask))
            {
                float xDist = hit.position.x - position.x;
                float yDist = hit.position.y - position.y;
                float zDist = hit.position.z - position.z;

                if (xDist * xDist * zDist * zDist < maxHDist && Mathf.Abs(yDist) < maxVDist)
                {
                    result = hit.position;
                    return true;
                }
            }
            result = Vector3.zero;
            return false;
        }

        private bool SpawnAmbientEnemy(EnemySO enemy, Vector3 centre)
        {
            bool success = false;
            Vector3 resultPos = Vector3.zero;

            int count = 0;

            while (!success && count++ < 20)
            {
                Vector3 spawnPoint = UnityEngine.Random.insideUnitSphere * 100 + centre;

                success = GetNavmeshPosition(spawnPoint, enemy, 15, 5, 10, out resultPos);

                foreach (var player in NetworkManager.Singleton.ConnectedClientsList)
                {
                    if ((player.PlayerObject.GetComponent<CharacterState>().CentrePos - spawnPoint).sqrMagnitude < 15 * 15)
                    {
                        success = false;
                        break;
                    }
                }
            }

            if (!success)
                return false;

            SpawnEnemy(resultPos, Quaternion.AngleAxis(UnityEngine.Random.Range(0,360), Vector3.up), enemy, true);
            ambientSpawnCount++;
            ambientCredits -= enemy.spawnCost;
            return true;
        }

        private bool ShouldContinueAmbientSpawning(int loopCount)
        {
            if (ambientCredits < enemies[0].spawnCost)
                return false;

            float spawnPressure = 1;
            if (targetAmbientCount < ambientSpawnCount)
                spawnPressure = (maxAmbientCount - ambientSpawnCount) / (float)(maxAmbientCount - targetAmbientCount);
            else if (ambientSpawnCount < targetAmbientCount)
                spawnPressure = 1 + (targetAmbientCount - ambientSpawnCount) / (float)(targetAmbientCount);

            float spawnProbability = spawnPressure * credits / (meanEnemyCost * 2 * difficultyMult * loopCount / 5f);
            return UnityEngine.Random.Range(0f, 1f) < spawnProbability;
        }

        private bool ShouldStartAmbientSpawn()
        {
            if (ambientCredits < enemies[0].spawnCost)
                return false;

            float spawnPressure = 1;
            if (targetAmbientCount < ambientSpawnCount)
                spawnPressure = (maxAmbientCount - ambientSpawnCount) / (float)(maxAmbientCount - targetAmbientCount);
            else if (ambientSpawnCount < targetAmbientCount)
                spawnPressure = 1 + (targetAmbientCount - ambientSpawnCount) / (float)(targetAmbientCount);

            float spawnProbability = spawnPressure * ambientCredits / (meanEnemyCost * 3 * 2 * difficultyMult) * Time.fixedDeltaTime;
            return UnityEngine.Random.Range(0f, 1f) < spawnProbability;
        }

        public void SpawnCredits(Vector3 centre, float radius, float minimumDistance, float credits)
        {
            int spawnCount = 0;
            while (credits > enemies[0].spawnCost && spawnCount++ < 1000)
            {
                SpawnEnemy(centre, radius, GetSpawnableEnemy(credits), minimumDistance, ref credits);
            }
            if (spawnCount >= 1000)
                Debug.LogError($"Failed to spawn {credits} enemies in 1000 attempts");

            this.credits += credits;
        }

        private bool SpawnEnemy(Vector3 centre, float radius, EnemySO enemy, float minimumDistance, ref float credits)
        {
            bool success = false;
            Vector3 resultPos = Vector3.zero;

            int count = 0;

            while (!success && count++ < 20)
            {
                Vector3 spawnPoint = UnityEngine.Random.insideUnitSphere * radius + centre;

                success = GetNavmeshPosition(spawnPoint, enemy, 15, 5, 10, out resultPos);

                foreach(var player in NetworkManager.Singleton.ConnectedClientsList)
                {
                    if ((player.PlayerObject.GetComponent<CharacterState>().CentrePos - spawnPoint).sqrMagnitude < minimumDistance * minimumDistance)
                    {
                        success = false;
                        break;
                    }
                }
            }

            if (!success)
                return false;

            SpawnEnemy(resultPos, Quaternion.FromToRotation(Vector3.forward, centre - resultPos), enemy, false);
            spawnedEnemyCount++;
            credits -= enemy.spawnCost;
            return true;
        }

        private bool ShouldContinueSpawning(int loopCount)
        {
            if (credits < enemies[0].spawnCost)
                return false;

            float spawnPressure = 1;
            if (targetEnemyCount < spawnedEnemyCount)
                spawnPressure = targetEnemyCount / spawnedEnemyCount;// (maxEnemyCount - spawnedEnemyCount) / (maxEnemyCount - targetEnemyCount);
            else if (spawnedEnemyCount < targetEnemyCount)
                spawnPressure = 1 + (targetEnemyCount - spawnedEnemyCount) / (targetEnemyCount);

            float spawnProbability = spawnPressure * credits / (meanEnemyCost * 2 * difficultyMult * loopCount / 5f);
            return UnityEngine.Random.Range(0f, 1f) < spawnProbability;
        }

        bool ShouldStartSpawn()
        {
            if (credits < enemies[0].spawnCost)
                return false;

            float spawnPressure = 1;
            if (targetEnemyCount < spawnedEnemyCount)
                spawnPressure = targetEnemyCount / spawnedEnemyCount;// (maxEnemyCount - spawnedEnemyCount) / (float)(maxEnemyCount - targetEnemyCount);
            else if (spawnedEnemyCount < targetEnemyCount)
                spawnPressure = 1 + (targetEnemyCount - spawnedEnemyCount) / (float)(targetEnemyCount);

            float spawnProbability = spawnPressure * credits / (meanEnemyCost * 3 * 2 * difficultyMult) * Time.fixedDeltaTime;
            return UnityEngine.Random.Range(0f, 1f) < spawnProbability;
        }
    }
}
