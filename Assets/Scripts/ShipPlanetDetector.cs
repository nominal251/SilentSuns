using UnityEngine;

public class ShipPlanetDetector : MonoBehaviour
{
    public PlanetManager planetManager;

    void Update()
    {
        Planet detectedPlanet = null;

        foreach (Planet planet in planetManager.planets)
        {
            Vector2 shipPosition = transform.position;
            Vector2 planetPosition = planet.transform.position;

            float distanceSquared = (shipPosition - planetPosition).sqrMagnitude;
            float radiusSquared = planet.Radius * planet.Radius;

            if (distanceSquared <= radiusSquared)
            {
                detectedPlanet = planet;
                break;
            }
        }

        if (detectedPlanet != planetManager.CurrentPlanet)
        {
            if (detectedPlanet != null)
            {
                planetManager.SetCurrentPlanet(detectedPlanet);
            }
            else
            {
                planetManager.ClearCurrentPlanet();
            }
        }
    }
}