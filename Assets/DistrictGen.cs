using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistrictGen : MonoBehaviour
{
    [SerializeField] private DistrictType[] districtTypes;
    [SerializeField] private District[] generatedDistricts;
    [SerializeField] private int numberOfDistricts;

    [SerializeField] private float districtRadius = 100;
    [SerializeField] private Vector3 regionSize = new Vector3(1000, 1, 1000);
    [SerializeField] private int rejectionSamples = 30;
    [SerializeField] private float displayRadius = 10;

    private List<Vector3> candiadatePoints;
    private CityBoundaries cityBoundaries;

    private void OnValidate()
    {
        cityBoundaries = gameObject.GetComponent<CityBoundaries>();
        List<Vector3> allPoints = PoissonDiskSampling.GenerateCandiatePoints(districtRadius, regionSize, rejectionSamples);

        candiadatePoints = cityBoundaries.CheckPointsPosition(allPoints);
    }

    private void OnDrawGizmos()
    {
        if (candiadatePoints != null)
        {
            foreach (Vector3 point in candiadatePoints)
            {
                Gizmos.DrawSphere(point, displayRadius);
            }
        }
    }
}

[System.Serializable]
struct DistrictType
{
    public string name;
    public Color color;
    public float area;
    public float distanceToPrimaryStreets;
    public float realativePosition;
}

[System.Serializable]
struct District
{
    public string name;
    public Color color;
    public Vector3 position;
}
