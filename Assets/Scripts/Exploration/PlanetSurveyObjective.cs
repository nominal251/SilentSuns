using UnityEngine;

// Uses the scanner results the instruments already write on Planet.
public class PlanetSurveyObjective : Objective
{
    [SerializeField] private Planet planet;
    [SerializeField] private int planetNumber = 1;
    public override string Title => planet != null ? planet.name : "Planet survey";

    public void SetPlanet(Planet target, int number)
    {
        planet = target;
        planetNumber = number;
    }

    // The ship tracker calls this after checking pause and startup, not during the menu.
    public void CheckScans()
    {
        if (IsRunning && planet != null && planet.waveformComplete && planet.compComplete)
            Complete();
    }

    public override string GetProgressText()
    {
        if (planet == null || !planet.gameObject.activeInHierarchy)
            return "PLANET " + planetNumber + " NOT LOCATED";
        return planet.name + "- Signal Scan: [" + (planet.waveformComplete ? "X" : " ") +
            "] - Comp Scan: [" + (planet.compComplete ? "X" : " ") + "]";
    }

    protected override void OnObjectiveReset()
    {
        if (planet == null) return;
        planet.waveformComplete = false;
        planet.compComplete = false;
    }
}
