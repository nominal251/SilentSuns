using System.Collections.Generic;
using UnityEngine;

public class SurfaceEventManager : MonoBehaviour
{
    [SerializeField] private List<SurfaceEvent> onLoadEvents;
    [SerializeField] private List<SurfaceEvent> periodicEvents;
    [SerializeField] private float rollInterval = 1f;

    private float timer;

    void Start()
    {
        Roll(onLoadEvents); // Roll for On Load category events on scene load
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= rollInterval) // Roll for Periodic category events every second
        {
            timer -= rollInterval;
            Roll(periodicEvents);
        }
    }

    void Roll(List<SurfaceEvent> events)
    {
        foreach (SurfaceEvent e in events)
        {
            if (Random.value <= e.chance)
                e.Execute(this);
        }
    }
}