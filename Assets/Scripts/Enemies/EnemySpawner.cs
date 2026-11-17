using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Array")]
    [SerializeField] private EnemyDefinitions[] spawnableEnemies;

    [Header("Enemy Random Spawn Array")]
    [SerializeField] private Transform[] randomSpawnPoints;

    private List<EnemyDefinitions> enemiesToSpawn = new List<EnemyDefinitions>(); //list of the enemies that we want to spawn

    [Header("Planet Variables")]
    [SerializeField] private int totalPlanetBudget;
    private int totalBudgetUsed;

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

    private void CheckIfAnyCanSpawn() //this is checking if any other enemies can spawn
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
        if (canSpawnList.Count == 1) //if only one enemy can spawn, add it to the list and spawn all enemies, because we know none else fit in the budget
        {
            enemiesToSpawn.Add(spawnableEnemies[canSpawnList[0]]);
            SpawnEnemies();
            return;
        }

        if (canSpawnList.Count > 1) //if more than one enemy fits in budget still, add one at random and check to see if any more can spawn
        {
            int tmp = Random.Range(0, canSpawnList.Count);
            enemiesToSpawn.Add(spawnableEnemies[canSpawnList[tmp]]);
            totalBudgetUsed += spawnableEnemies[tmp].cost;
            CheckIfAnyCanSpawn();
        }

        else //if zero can fit in budget, spawn enemies
        {
            SpawnEnemies();
            return;
        }
    }

    private void SpawnEnemies()
    {
        int totalToSpawn = enemiesToSpawn.Count;

        for (int i = 0; i < totalToSpawn; i++) //loops through what can spawn and spawns them all at random points
        {
            int tmpSpawn = Random.Range(0, randomSpawnPoints.Length);

            Transform spawnTrans = randomSpawnPoints[tmpSpawn];

            Instantiate(enemiesToSpawn[i].prefab, spawnTrans.position, spawnTrans.rotation);
        }
    }
}
