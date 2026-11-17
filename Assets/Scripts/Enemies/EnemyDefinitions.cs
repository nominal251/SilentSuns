using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDefinitions", menuName = "Enemy Definitions/Scriptable Object")]
public class EnemyDefinitions : ScriptableObject
{
    public GameObject prefab;
    public int cost = 0;
    public int maxCount;
}