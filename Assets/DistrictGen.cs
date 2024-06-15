using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistrictGen : MonoBehaviour
{
    public float radius = 100;
    public Vector3 regionSize = new Vector3(1000, 1, 1000);
    public int rejectionSamples = 30;
    public float displayRadius = 10;

    private List<Vector3> points;
    private CityBoundaries cityBoundaries;

    private void OnValidate()
    {
        List<Vector3> allPoints = PoissonDiskSampling.GenerateCandiatePoints(radius, regionSize, rejectionSamples);
        cityBoundaries = gameObject.GetComponent<CityBoundaries>();
        points = cityBoundaries.CheckPointsPosition(allPoints);
    }

    private void OnDrawGizmos()
    {
        if (points != null)
        {
            foreach (Vector3 point in points)
            {
                Gizmos.DrawSphere(point, displayRadius);
            }
        }
    }
}
