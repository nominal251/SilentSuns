using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyDefinitions[] spawnableEnemies;
    [SerializeField] private Transform[] randomSpawnPoints;

    public List<EnemyDefinitions> enemiesToSpawn = new List<EnemyDefinitions>();
    [SerializeField] private int totalPlanetBudget;
    [SerializeField] private int totalBudgetUsed;

    private void Start()
    {
        FillSpawnList();
    }

    private void FillSpawnList() //fills up list of enemiesToSpawn
    {
        RandomizeEnemyInBudget();
    }

    private void RandomizeEnemyInBudget()
    {
        int randEnemy = Random.Range(0, spawnableEnemies.Length);

        if ((spawnableEnemies[randEnemy].cost + totalBudgetUsed) <= totalPlanetBudget) //if cost + used budget is less than total budget, enemy is good to add to spawn list
        {
            enemiesToSpawn.Add(spawnableEnemies[randEnemy]);
            totalBudgetUsed += spawnableEnemies[randEnemy].cost;
            RandomizeEnemyInBudget();
        }

        else
        {
            CheckIfAnyCanSpawn();
        }

        return;
    }

    private void CheckIfAnyCanSpawn()
    {
        List<int> canSpawnList = new List<int>();
        for (int i = 0; i < spawnableEnemies.Length; i++)
        {
            if ((spawnableEnemies[i].cost + totalBudgetUsed) <= totalPlanetBudget)
            {
                canSpawnList.Add(i);
                totalBudgetUsed += spawnableEnemies[i].cost;
            }
        }
        if (canSpawnList.Count == 1)
        {
            enemiesToSpawn.Add(spawnableEnemies[canSpawnList[0]]);
            SpawnEnemies();
            return;
        }

        if (canSpawnList.Count > 1)
        {
            int tmp = Random.Range(0, canSpawnList.Count);
            enemiesToSpawn.Add(spawnableEnemies[canSpawnList[tmp]]);
            totalBudgetUsed += spawnableEnemies[tmp].cost;
            CheckIfAnyCanSpawn();
        }

        else
        {
            SpawnEnemies();
            return;
        }
    }

    private void SpawnEnemies()
    {
        int totalToSpawn = enemiesToSpawn.Count;

        for (int i = 0; i < totalToSpawn; i++)
        {
            int tmpSpawn = Random.Range(0, randomSpawnPoints.Length);

            Transform spawnTrans = randomSpawnPoints[tmpSpawn];

            Instantiate(enemiesToSpawn[i].prefab, spawnTrans.position, spawnTrans.rotation);
        }
    }
}
