using System;
using System.Collections.Generic;
using UnityEngine;
using static VoronoiDiagram;

public class DistrictGenerator : MonoBehaviour
{
    [SerializeField, Range(0, 1)] private float importanceOfNeighbours = 0.4f;
    [SerializeField, Range(0, 1)] private float importanceOfCityCenterDistance = 0.3f;

    [SerializeField] private DistrictType[] districtTypes;
    [SerializeField] private List<District> generatedDistricts = new List<District>();
    [SerializeField, Min(0)] private int numberOfDistricts;
    private int minNumberOfDistricts;
    private int maxNumberOfDistricts;

    private IDictionary<int, District> districtsDictionary;
    private static int counter = -1;

    [SerializeField, Min(0)] private int rejectionSamples = 30;

    private List<Vector3> candidatePoints;
    private CityBoundaries cityBoundaries;

    [SerializeField] private GameObject voronoiDiagram;
    [SerializeField, Min(0)] private int distictCellDistortion;
    private VoronoiDiagram voronoiScript;
    private Texture2D voronoiTexture;
    private Dictionary<int, Region> regions;

    [SerializeField] GameObject prepareBorders;
    private BorderPreparation prepareBordersScript;

    [SerializeField, Min(0)] private int segmentLength = 50;
    [SerializeField, Min(0)] private int roadWidth = 7;

    private Dictionary<int, List<List<Vector2Int>>> regionLots;

    private void OnValidate()
    {
        counter = -1;
        cityBoundaries = gameObject.GetComponent<CityBoundaries>();
        CalculateMinAndMaxDistricts();
        GenerateCandidatePositions();
        SelectDistrictPositions();
        voronoiScript = voronoiDiagram.GetComponent<VoronoiDiagram>();
        (voronoiTexture, regions) = voronoiScript.GenerateVoronoiDiagram(districtsDictionary, distictCellDistortion, new Vector2Int((int)cityBoundaries.transform.position.x, (int)cityBoundaries.transform.position.z), cityBoundaries.outerBoundaryRadius); // Why 100??? and why did i have to rotate the plane?? so many questions
        voronoiTexture.Apply();
    }

    public void GenerateRoads() 
    {
        prepareBordersScript = prepareBorders.GetComponent<BorderPreparation>();
        voronoiTexture = prepareBordersScript.GenerateRoads(voronoiTexture, cityBoundaries.outerBoundaryRadius, cityBoundaries.transform.position, segmentLength);
        voronoiTexture.Apply();
        ApplyTexture();
    }

    public void RemoveRoads()
    {
        RoadGenerator.RemoveOldRoadsAndRoadSystems();
    }

    public void GenerateLotsAndBuildings()
    {
        regionLots = LotGenerator.GenerateLots(voronoiTexture, regions, districtsDictionary, roadWidth);
        BuildingGenerator.GenerateBuildings(regionLots, districtsDictionary);
    }

    public void RemoveBuildings()
    {
        BuildingGenerator.RemoveOldBuildings();
    }

    private void CalculateMinAndMaxDistricts()
    {
        minNumberOfDistricts = 0;
        maxNumberOfDistricts = 0;
        foreach (DistrictType districtType in districtTypes)
        {
            minNumberOfDistricts += districtType.minNumberOfPlacements;
            maxNumberOfDistricts += districtType.maxNumberOfPlacements;
        }
    }

    private void GenerateCandidatePositions()
    {
        List<Vector3> allPoints = PoissonDiskSampling.GenerateDistrictPoints(numberOfDistricts, minNumberOfDistricts, maxNumberOfDistricts, cityBoundaries.outerBoundaryRadius, cityBoundaries.transform.position, rejectionSamples);
        numberOfDistricts = PoissonDiskSampling.ValidateNumberOfDistricts(numberOfDistricts, minNumberOfDistricts, maxNumberOfDistricts, false);
        candidatePoints = allPoints;
    }

    private void SelectDistrictPositions()
    {
        if (candidatePoints == null || candidatePoints.Count == 0)
        {
            Debug.LogError("No candidate points available for district generation.");
            return;
        }

        generatedDistricts.Clear();
        districtsDictionary = new Dictionary<int, District>();

        List<Vector3> locations = new List<Vector3>(candidatePoints);
        List<Vector3> pointsToRemove = new List<Vector3>();

        foreach (Vector3 location in locations)
        {
            DistrictType bestDistrictType = CalculateBestDistrictForLocation(location);
            District newDistrict = new District
            {
                name = bestDistrictType.name,
                position = location,
                type = bestDistrictType
            };
            generatedDistricts.Add(newDistrict);
            districtsDictionary.Add(GenerateUniqueID(), newDistrict);
            pointsToRemove.Add(location);
        }

        foreach (Vector3 point in pointsToRemove)
        {
            candidatePoints.Remove(point);
        }
    }

    DistrictType CalculateBestDistrictForLocation(Vector3 location)
    {
        DistrictType bestDistrictType = districtTypes[0];
        float bestSuitability = float.MinValue;

        foreach (DistrictType type in districtTypes)
        {
            float suitability = CalculateSuitability(type, location);
            if (suitability > bestSuitability)
            {
                bestSuitability = suitability;
                bestDistrictType = type;
            }
        }

        return bestDistrictType;
    }

    float CalculateSuitability(DistrictType type, Vector3 location)
    {
        float Sd = CalculateSuitabilityBasedOnNeighbors(type, location);
        float Sa = CalculateSuitabilityBasedOnArea(type, location);

        float S = importanceOfNeighbours * Sd + importanceOfCityCenterDistance * Sa;
        return S;
    }

    float CalculateSuitabilityBasedOnNeighbors(DistrictType type, Vector3 location)
    {
        float Sd = 0f;
        foreach (District placedDistrict in generatedDistricts)
        {
            float attraction = GetAttraction(type, placedDistrict.type);
            float repulsion = GetRepulsion(type, placedDistrict.type);
            float distance = Vector3.Distance(location, placedDistrict.position);

            Sd += (attraction - repulsion) / distance;
        }
        return Sd;
    }

    float CalculateSuitabilityBasedOnArea(DistrictType type, Vector3 location)
    {
        float distanceFromCenter = Vector3.Distance(location, cityBoundaries.transform.position);
        float scaledDistance = ScaleDistance(distanceFromCenter);
        float Sa = GetSuitability(scaledDistance, type.distanceFromCenter);
        return Sa;
    }

    float ScaleDistance(float value)
    {
        float originalMax = cityBoundaries.outerBoundaryRadius;
        float normalizedVal = (value - 0) / (originalMax - 0);
        float scaledVal = normalizedVal * (10 - 0) + 0;
        return scaledVal;
    }

    float GetAttraction(DistrictType districtType, DistrictType neighborType)
    {
        //var attractionDict = districtType.attractionValues;
        //if (attractionDict.ContainsKey(neighborType.name))
        //{
        //    return attractionDict[neighborType.name];
        //}
        //return 0; // Standardwert, wenn kein spezifischer Koeffizient definiert ist
        return UnityEngine.Random.Range(0, 10);
    }

    float GetRepulsion(DistrictType districtType, DistrictType neighborType)
    {
        //var repulsionDict = districtType.repulsionValues;
        //if (repulsionDict.ContainsKey(neighborType.name))
        //{
        //    return repulsionDict[neighborType.name];
        //}
        //return 0; // Standardwert, wenn kein spezifischer Koeffizient definiert ist
        return UnityEngine.Random.Range(0, 10);
    }

    float GetSuitability(float calculatedValue, float specifiedValue)
    {
        if (Mathf.Approximately(calculatedValue, specifiedValue)) // Mathf.Approximately, um Gleitkommazahlen zu vergleichen
        {
            return 10f;
        }
        else if (calculatedValue < specifiedValue)
        {
            return 5f;
        }
        else if (calculatedValue > specifiedValue)
        {
            return 0f;
        }
        else
        {
            return -5f;
        }
    }

    private int GenerateUniqueID()
    {
        return ++counter;
    }

    private void ApplyTexture()
    {
        
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial.mainTexture = voronoiTexture;
        }
    }
}

[System.Serializable]
public struct DistrictType
{
    public string name;
    public Color color;
    [Range(0, 10)] public float distanceFromCenter;
    [Range(0, 10)] public float distanceToPrimaryStreets;
    [Min(1)] public int minNumberOfPlacements;
    [Min(1)] public int maxNumberOfPlacements;
    // public Dictionary<string, float> attractionValues;
    // public Dictionary<string, float> repulsionValues;
    public List<GameObject> buildingTypes;
    [Min(1)] public int minLotSizeSquared;
}

[System.Serializable]
public struct District
{
    public string name;
    public Vector3 position;
    public DistrictType type;
}

//[System.Serializable]
//public struct AttractionRepulsion
//{
//    public string districtName;
//    public float value;
//}
