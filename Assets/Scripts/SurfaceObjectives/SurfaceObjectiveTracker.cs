using UnityEngine;

public class SurfaceObjectiveTracker : MonoBehaviour
{
    private int objsToComplete;
    private int objsCurrentlyComplete = 0;

    public Lander lander;

    void Start()
    {
        objsToComplete = GameObject.FindGameObjectsWithTag("SurfaceObjective").Length;
        lander = FindAnyObjectByType<Lander>();
    }

    void Update()
    {
        if (objsCurrentlyComplete >= objsToComplete)
        {
            lander.objectivesComplete = true;
        }
    }

    public void CompleteObj()
    {
        objsCurrentlyComplete++;
    }
}