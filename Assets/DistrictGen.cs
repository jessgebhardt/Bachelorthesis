using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DistrictGen : MonoBehaviour
{
    [SerializeField, Range(0, 1)] private float importanceOfNeighbours = 0.4f;
    [SerializeField, Range(0, 1)] private float importanceOfCityCenterDistance = 0.3f;
    [SerializeField, Range(0, 1)] private float importanceOfPrimaryStreetDistance = 0.2f;

    [SerializeField] private DistrictType[] districtTypes;
    [SerializeField] private List<District> generatedDistricts = new List<District>();
    [SerializeField, Min(0)] private int numberOfDistricts;
    private int minNumberOfDistricts;
    private int maxNumberOfDistricts;
    // private List<Vector3> coreDistrictLocations; // generate Core and Outer seperately?? -> damit man die Bezirke einfacher verteilen kan aufgrund von der min und max anzahl, don't forget!!!
    // private List<Vector3> outerDistrictLocations;

    [SerializeField] private Vector3 sampleRegionSize = new Vector3(1000, 1, 1000); // Muss nicht vom User eingestellt werden, später noch ändern
    [SerializeField] private int rejectionSamples = 30;
    [SerializeField] private float displayRadius = 10;

    private List<Vector3> candidatePoints;
    private CityBoundaries cityBoundaries;

    // SerializeField] private Material primaryRoadMaterial;
    [SerializeField] private GameObject voronoiDiagram;
    [SerializeField] private int distictCellDistortion;
    private VoronoiDiagram voronoiScript;

    private void OnValidate()
    {
        cityBoundaries = gameObject.GetComponent<CityBoundaries>();
        CalculateMinAndMaxDistricts();
        GenerateCandidatePositions();
        SelectDistrictPositions();
        voronoiScript = voronoiDiagram.GetComponent<VoronoiDiagram>();
        voronoiScript.GenerateVoronoiDiagram(generatedDistricts, (int)sampleRegionSize.x+100, distictCellDistortion); // Why 100??? and why did i have to rotate the plane?? so many questions
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
        List<Vector3> allPoints = PoissonDiskSampling.GenerateDistrictPoints(sampleRegionSize, numberOfDistricts, minNumberOfDistricts, maxNumberOfDistricts, cityBoundaries.outerBoundaryRadius, cityBoundaries.transform.position, rejectionSamples);
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

        // Maybe generate Points for core and outer seperately?
        List<Vector3> coreDistrictLocations = cityBoundaries.CheckWithinBoundaries(candidatePoints, "inner");
        foreach (Vector3 coreLocation in coreDistrictLocations)
        {
            DistrictType bestDistrictType = CalculateBestDistrictForLocation(coreLocation, "inner");
            District newDistrict = new District
            {
                name = bestDistrictType.name,
                position = coreLocation,
                type = bestDistrictType
            };
            generatedDistricts.Add(newDistrict);
            candidatePoints.Remove(coreLocation);
        }


        List<Vector3> outerDistrictLocations = cityBoundaries.CheckWithinBoundaries(candidatePoints, "outer");

        foreach (Vector3 outerLocation in outerDistrictLocations)
        {
            DistrictType bestDistrictType = CalculateBestDistrictForLocation(outerLocation, "outer");
            District newDistrict = new District
            {
                name = bestDistrictType.name,
                position = outerLocation,
                type = bestDistrictType
            };
            generatedDistricts.Add(newDistrict);
            candidatePoints.Remove(outerLocation);
        }
    //    List<KeyValuePair<Vector3, float>> evaluatedPoints = new List<KeyValuePair<Vector3, float>>();
    //    // int districtsToGenerate = Mathf.Min(numberOfDistricts, candidatePoints.Count);
    //    foreach (var point in outerDistricts)
    //    {
    //        float score = EvaluatePoint(point);
    //        // ...
    //        evaluatedPoints.Add(new KeyValuePair<Vector3, float>(point, score));
    //    }
    //    evaluatedPoints.Sort((x, y) => y.Value.CompareTo(x.Value));

    //    List<Vector3> selectedPoints = new List<Vector3>();
    //    for (int i = 0; i < numberOfDistricts && i < evaluatedPoints.Count; i++)
    //    {
    //        selectedPoints.Add(evaluatedPoints[i].Key);
    //    }

    //    for (int i = 0; i < selectedPoints.Count; i++)
    //    {
    //        // change following
    //        DistrictType[] outerDistrictTypes = districtTypes.Where(d => d.outerBoundaries).ToArray();
    //        int randDistrict = Random.Range(0, outerDistrictTypes.Length);
    //        DistrictType type = outerDistrictTypes[randDistrict];
    //        District newDistrict = new District
    //        {
    //            name = type.name,
    //            color = type.color,
    //            position = selectedPoints[i]
    //        };
    //        generatedDistricts.Add(newDistrict);
    //    }
    //    candidatePoints.Clear();
    }

    //private float EvaluatePoint(Vector3 point)
    //{
    //    // change following
    //    float score = 0;
    //    score = Random.Range(0.0f, 1.0f);
    //    return score;
    //}

    //private List<Vector3> SelectBestPoints(List<Vector3> points)
    //{
    //    // change following
    //    List<Vector3> bestPoints = new List<Vector3>();
    //    for (int i = 0; i < numberOfDistricts && i < points.Count; i++)
    //    {
    //        bestPoints.Add(points[i]);
    //    }
    //    return bestPoints;
    //}

    DistrictType CalculateBestDistrictForLocation(Vector3 location, string boundaryType)
    {
        DistrictType[] selectedDistrictTypes = districtTypes;
        if (boundaryType == "inner")
        {
            selectedDistrictTypes = districtTypes.Where(d => d.innerBoundaries).ToArray();
        }
        else if (boundaryType == "outer")
        {
            selectedDistrictTypes = districtTypes.Where(d => d.outerBoundaries).ToArray();
        }

        DistrictType bestDistrictType = selectedDistrictTypes[0];
        float bestSuitability = float.MinValue;

        foreach (DistrictType type in selectedDistrictTypes)
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
        float Sh = CalculateSuitabilityBasedOnPrimaryStreets(type, location);

        float S = importanceOfNeighbours * Sd + importanceOfCityCenterDistance * Sa + importanceOfPrimaryStreetDistance * Sh;
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
        float originalMax = Mathf.Sqrt(Mathf.Pow(sampleRegionSize.x / 2, 2) + Mathf.Pow(sampleRegionSize.z / 2, 2));
        float normalizedVal = (value - 0) / (originalMax - 0);
        float scaledVal = normalizedVal * (10 - 0) + 0;
        return scaledVal;
    }

    float CalculateSuitabilityBasedOnPrimaryStreets(DistrictType type, Vector3 location)
    {
        float closestDistance = Random.Range(0, 10);
        //float closestDistance = float.MaxValue;
        //foreach (var primaryStreet in primaryStreets)
        //{
        //    float distance = Vector3.Distance(location, primaryStreet);
        //    float scaledDistance = ScaleDistance(distance);
        //    if (scaledDistance < closestDistance)
        //    {
        //        closestDistance = scaledDistance;
        //    }
        //}
        float Sh = GetSuitability(closestDistance, type.distanceToPrimaryStreets);
        return Sh;
    }

    float GetAttraction(DistrictType districtType, DistrictType neighborType)
    {
        //var attractionDict = districtType.attractionValues;
        //if (attractionDict.ContainsKey(neighborType.name))
        //{
        //    return attractionDict[neighborType.name];
        //}
        //return 0; // Standardwert, wenn kein spezifischer Koeffizient definiert ist
        return Random.Range(0, 10);
    }

    float GetRepulsion(DistrictType districtType, DistrictType neighborType)
    {
        //var repulsionDict = districtType.repulsionValues;
        //if (repulsionDict.ContainsKey(neighborType.name))
        //{
        //    return repulsionDict[neighborType.name];
        //}
        //return 0; // Standardwert, wenn kein spezifischer Koeffizient definiert ist
        return Random.Range(0, 10);
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


    private void OnDrawGizmos()
    {
        if (candidatePoints != null)
        {
            Gizmos.color = Color.white;
            foreach (Vector3 point in candidatePoints)
            {
                Gizmos.DrawSphere(point, displayRadius);
            }
        }
        Debug.Log(generatedDistricts.Count);
        if (generatedDistricts != null)
        {
            Gizmos.color = Color.white;
            foreach (District district in generatedDistricts)
            {
                Gizmos.color = district.type.color;
                Gizmos.DrawSphere(district.position, displayRadius);
            }
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
    [Min(1)] public int minNumberOfPlacements; // Nächster Schritt, miteinbeziehen
    [Min(1)] public int maxNumberOfPlacements; // Nächster Schritt, miteinbeziehen
    public bool innerBoundaries;
    public bool outerBoundaries;
    // public Dictionary<string, float> attractionValues;
    // public Dictionary<string, float> repulsionValues;
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
