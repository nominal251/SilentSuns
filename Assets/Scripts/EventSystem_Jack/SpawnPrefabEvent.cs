using UnityEngine;

[CreateAssetMenu(menuName = "Events/Spawn Prefab")]
public class SpawnPrefabEvent : SurfaceEvent
{
    public GameObject prefab;
    public Vector3 spawnPosition;

    public override void Execute(SurfaceEventManager manager)
    {
        Instantiate(prefab, spawnPosition, Quaternion.identity);
    }
}