using System;
using System.Collections.Generic;
using UnityEngine;
public class DistrictGenerator : MonoBehaviour
{
    //[SerializeField, Range(0, 1)] private float importanceOfNeighbours = 0.4f;
    //[SerializeField, Range(0, 1)] private float importanceOfCityCenterDistance = 0.3f;

    //[SerializeField] private List<DistrictType> districtTypes = new List<DistrictType>();
    //[SerializeField] private List<District> generatedDistricts = new List<District>();
    //[SerializeField, Min(0)] private int numberOfDistricts;
    //private int minNumberOfDistricts;
    //private int maxNumberOfDistricts;

    //private IDictionary<int, District> districtsDictionary;
    //private static int counter = -1;
    //private static bool relationsAndIDsInitialized = false;



    // [SerializeField, Min(0)] private int rejectionSamples = 30;

    // private List<Vector3> candidatePoints;
    // private CityBoundaries cityBoundaries;


    //private Dictionary<int, Region> regions;

    [SerializeField] GameObject prepareBorders;
    private BorderPreparation prepareBordersScript;

    [SerializeField, Min(0)] private int segmentLength = 50;
    [SerializeField, Min(0)] private int roadWidth = 8;

    private Dictionary<int, List<List<Vector2Int>>> regionLots;

    //private void OnValidate()
    //{
    //    counter = -1;
    //    ValidateDistrictColors();
    //    if (!relationsAndIDsInitialized)
    //    {
    //        InitializeRelationsAndIDs();
    //    }
    //}

    public static void GenerateDistricts(DistrictsData districtsData, CityBoundariesData boundariesData, PoissonDiskData poissonDiskData)
    {
        CalculateMinAndMaxDistricts(districtsData);
        GenerateCandidatePositions(districtsData, boundariesData, poissonDiskData);
        DistrictSelection.SelectDistrictPositions(districtsData, boundariesData, poissonDiskData);
    }

    public static void InitializeDistricts(bool relationsAndIDsInitialized, List<DistrictType> districtTypes)
    {
        ValidateDistrictColors(districtTypes);
        if (!relationsAndIDsInitialized)
        {
            InitializeRelationsAndIDs(relationsAndIDsInitialized, districtTypes);
        }
    }

    private static void ValidateDistrictColors(List<DistrictType> districtTypes)
    {
        if (districtTypes.Count > 0)
        { 
            for (int i = 0; i < districtTypes.Count; i++)
            {
                var districtType = districtTypes[i];
                if (districtType.color == Color.black)
                {
                    districtType.color = AdjustBlackColor(districtType.color);
                    districtTypes[i] = districtType;
                }
            }
        }
    }

    private static Color AdjustBlackColor(Color color)
    {
        return new Color(color.r + 0.1f, color.g + 0.1f, color.b + 0.1f, color.a);
    }

    private static void CalculateMinAndMaxDistricts(DistrictsData districtsData)
    {
        int minNumberOfDistricts = 0;
        int maxNumberOfDistricts = 0;
        foreach (DistrictType districtType in districtsData.districtTypes)
        {
            minNumberOfDistricts += districtType.minNumberOfPlacements;
            maxNumberOfDistricts += districtType.maxNumberOfPlacements;
        }
        districtsData.minNumberOfDistricts = minNumberOfDistricts;
        districtsData.maxNumberOfDistricts = maxNumberOfDistricts;

    }

    private static void InitializeRelationsAndIDs(bool relationsAndIDsInitialized, List<DistrictType> districtTypes)
    {
        if (districtTypes.Count > 0)
        {
            for (int i = 0; i < districtTypes.Count; i++)
            {
                var districtType = districtTypes[i];
                districtType.id = i;

                if (districtType.relations.Count == 0)
                {
                    for (int j = 0; j < districtTypes.Count; j++)
                    {
                        if (i == j) { continue; }

                        var relatedDistrictType = districtTypes[j];
                        districtType.relations.Add(new DistrictRelation
                        {
                            districtTypeId = relatedDistrictType.id,
                            _name = relatedDistrictType.name,
                            attraction = 0,
                            repulsion = 0
                        });
                    }
                }
                districtTypes[i] = districtType;
            }
        }

        relationsAndIDsInitialized = true;
    }

    private static void GenerateCandidatePositions(DistrictsData districtsData, CityBoundariesData cityBoundaries, PoissonDiskData poissonDiskData)
    {
        List<Vector3> allPoints = PoissonDiskSampling.GenerateDistrictPoints(districtsData.numberOfDistricts, districtsData.minNumberOfDistricts, districtsData.maxNumberOfDistricts, cityBoundaries.outerBoundaryRadius, cityBoundaries.center, poissonDiskData.rejectionSamples);
        districtsData.numberOfDistricts = PoissonDiskSampling.ValidateNumberOfDistricts(districtsData.numberOfDistricts, districtsData.minNumberOfDistricts, districtsData.maxNumberOfDistricts, false);
        poissonDiskData.candidatePoints = allPoints;
    }

    //private void SelectDistrictPositions()
    //{
    //    if (candidatePoints == null || candidatePoints.Count == 0)
    //    {
    //        Debug.LogError("No candidate points available for district generation.");
    //        return;
    //    }

    //    generatedDistricts.Clear();
    //    districtsDictionary = new Dictionary<int, District>();

    //    List<Vector3> locations = new List<Vector3>(candidatePoints);
    //    List<Vector3> pointsToRemove = new List<Vector3>();

    //    List<DistrictType> minDistrictTypes = GetMinDistrictTypes();
    //    List<DistrictType> restDistrictTypes = GetRestDistrictTypes();

    //    foreach (Vector3 location in locations)
    //    {
    //        List<DistrictType> districtTypesToPlace = minDistrictTypes;

    //        if (minDistrictTypes.Count == 0)
    //        {
    //            districtTypesToPlace = restDistrictTypes;
    //        }
    //        DistrictType bestDistrictType = CalculateBestDistrictForLocation(location, districtTypesToPlace);

    //        if (minDistrictTypes.Count == 0)
    //        {
    //            restDistrictTypes.Remove(bestDistrictType);

    //        } else
    //        {
    //            minDistrictTypes.Remove(bestDistrictType);
    //        }

    //        District newDistrict = new District
    //        {
    //            name = bestDistrictType.name,
    //            position = location,
    //            type = bestDistrictType
    //        };
    //        generatedDistricts.Add(newDistrict);
    //        districtsDictionary.Add(GenerateUniqueID(), newDistrict);
    //        pointsToRemove.Add(location);
    //    }

    //    foreach (Vector3 point in pointsToRemove)
    //    {
    //        candidatePoints.Remove(point);
    //    }
    //}

    //DistrictType CalculateBestDistrictForLocation(Vector3 location, List<DistrictType> districtTypesToPlace)
    //{
    //    DistrictType bestDistrictType = districtTypesToPlace[0];
    //    float bestSuitability = float.MinValue;

    //    foreach (DistrictType type in districtTypes)
    //    {
    //        if (districtTypesToPlace.Contains(type))
    //        {
    //            float suitability = CalculateSuitability(type, location);
    //            if (suitability > bestSuitability)
    //            {
    //                bestSuitability = suitability;
    //                bestDistrictType = type;
    //            }
    //        }
    //    }

    //    return bestDistrictType;
    //}

    //private List<DistrictType> GetMinDistrictTypes()
    //{
    //    List<DistrictType> minDistrictTypes = new List<DistrictType>();

    //    foreach (DistrictType type in districtTypes)
    //    {
    //        int minPlacements = type.minNumberOfPlacements;
    //        for (int i = 0; i < minPlacements; i++)
    //        {
    //            minDistrictTypes.Add(type);
    //        }
    //    }

    //    return minDistrictTypes;
    //}

    //private List<DistrictType> GetRestDistrictTypes()
    //{
    //    List<DistrictType> restDistrictTypes = new List<DistrictType>();

    //    foreach (DistrictType type in districtTypes)
    //    {
    //        int restPlacements = type.maxNumberOfPlacements - type.minNumberOfPlacements;
    //        for (int i = 0; i < restPlacements; i++)
    //        {
    //            restDistrictTypes.Add(type);
    //        }
    //    }

    //    return restDistrictTypes;
    //}

    //float CalculateSuitability(DistrictType type, Vector3 location)
    //{
    //    float Sd = CalculateSuitabilityBasedOnNeighbors(type, location);
    //    float Sa = CalculateSuitabilityBasedOnPosition(type, location);

    //    float S = importanceOfNeighbours * Sd + importanceOfCityCenterDistance * Sa;
    //    return S;
    //}

    //float CalculateSuitabilityBasedOnNeighbors(DistrictType type, Vector3 location)
    //{
    //    float Sd = 0f;
    //    foreach (District placedDistrict in generatedDistricts)
    //    {
    //        float attraction = GetAttraction(type, placedDistrict.type);
    //        float repulsion = GetRepulsion(type, placedDistrict.type);
    //        float distance = ScaleDistance(Vector3.Distance(location, placedDistrict.position));

    //        Sd += (attraction - repulsion) / distance;
    //    }
    //    return Sd;
    //}

    //float CalculateSuitabilityBasedOnPosition(DistrictType type, Vector3 location)
    //{
    //    float distanceFromCenter = Vector3.Distance(location, cityBoundaries.transform.position);
    //    float scaledDistance = ScaleDistance(distanceFromCenter);

    //    //float Sa = 10 - CalculateAverage(scaledDistance, type.distanceFromCenter);
    //    float Sa = GetSuitability(scaledDistance, type.distanceFromCenter);

    //    return Sa;
    //}

    //float ScaleDistance(float value)
    //{
    //    float originalMax = cityBoundaries.outerBoundaryRadius;
    //    float normalizedVal = (value - 0) / (originalMax - 0);
    //    float scaledVal = normalizedVal * (10 - 0) + 0;
    //    return scaledVal;
    //}

    //float GetAttraction(DistrictType currentType, DistrictType otherType)
    //{
    //    foreach (DistrictRelation relation in currentType.relations)
    //    {
    //        if (relation.districtTypeId == otherType.id)
    //        {
    //            return relation.attraction;
    //        }
    //    }
    //    return 0f;
    //}
    //float GetRepulsion(DistrictType currentType, DistrictType otherType)
    //{
    //    foreach (DistrictRelation relation in currentType.relations)
    //    {
    //        if (relation.districtTypeId == otherType.id)
    //        {
    //            return relation.repulsion;
    //        }
    //    }
    //    return 0f;
    //}

    //float CalculateAverage(float a, float b)
    //{
    //    return (a + b) / 2.0f;
    //}

    //float GetSuitability(float calculatedValue, float specifiedValue)
    //{
    //    if (Mathf.Approximately(calculatedValue, specifiedValue)) // Mathf.Approximately, um Gleitkommazahlen zu vergleichen
    //    {
    //        return 10f;
    //    }
    //    else if (calculatedValue < specifiedValue)
    //    {
    //        return 5f;
    //    }
    //    else if (calculatedValue > specifiedValue)
    //    {
    //        return 0f;
    //    }
    //    else
    //    {
    //        return -5f;
    //    }
    //}

    //private int GenerateUniqueID()
    //{
    //    return ++counter;
    //}
}

[System.Serializable]
public struct DistrictRelation
{
    [NonSerialized] public int districtTypeId;

    [SerializeField, HideInInspector] public string _name;

    public string name
    {
        get { return _name; }
        private set { _name = value; }
    }

    [Range(0, 10)] public float attraction;
    [Range(0, 10)] public float repulsion;
}


[System.Serializable]
public struct DistrictType
{
    [NonSerialized] public int id;
    public string name;
    public Color color;
    [Range(0, 10)] public float distanceFromCenter;
    [Min(1)] public int minNumberOfPlacements;
    [Min(1)] public int maxNumberOfPlacements;
    public List<GameObject> buildingTypes;
    [Min(1)] public int minLotSizeSquared;
    public List<DistrictRelation> relations;
}

[System.Serializable]
public struct District
{
    public string name;
    public Vector3 position;
    public DistrictType type;
}
