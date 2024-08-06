using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistrictSelector : MonoBehaviour
{
    private static int counter = -1;
    private static List<DistrictType> districtTypes;
    private static float importanceOfNeighbours;
    private static float importanceOfCityCenterDistance;
    private static List<District> generatedDistricts;
    private static Vector3 center;
    private static float outerBoundaryRadius;

    public static void SelectDistrictPositions(PoissonData poissonData, DistrictData districtData, BoundariesData boundariesData)
    {
        if (poissonData.candidatePoints == null || poissonData.candidatePoints.Count == 0)
        {
            Debug.LogError("No candidate points available for district generation.");
            return;
        }
        counter = -1;

        districtTypes = districtData.districtTypes;
        importanceOfNeighbours = districtData.importanceOfNeighbours;
        importanceOfCityCenterDistance = districtData.importanceOfCityCenterDistance;
        generatedDistricts = districtData.generatedDistricts;
        center = boundariesData.center;
        outerBoundaryRadius = boundariesData.outerBoundaryRadius;


        districtData.generatedDistricts.Clear();
        districtData.districtsDictionary = new Dictionary<int, District>();

        List<Vector3> locations = new List<Vector3>(poissonData.candidatePoints);
        List<Vector3> pointsToRemove = new List<Vector3>();

        List<DistrictType> minDistrictTypes = GetMinDistrictTypes();
        List<DistrictType> restDistrictTypes = GetRestDistrictTypes();

        foreach (Vector3 location in locations)
        {
            List<DistrictType> districtTypesToPlace = minDistrictTypes;

            if (minDistrictTypes.Count == 0)
            {
                districtTypesToPlace = restDistrictTypes;
            }
            DistrictType bestDistrictType = CalculateBestDistrictForLocation(location, districtTypesToPlace);

            if (minDistrictTypes.Count == 0)
            {
                restDistrictTypes.Remove(bestDistrictType);

            }
            else
            {
                minDistrictTypes.Remove(bestDistrictType);
            }

            District newDistrict = new District
            {
                name = bestDistrictType.name,
                position = location,
                type = bestDistrictType
            };
            districtData.generatedDistricts.Add(newDistrict);
            districtData.districtsDictionary.Add(GenerateUniqueID(), newDistrict);
            pointsToRemove.Add(location);
        }

        foreach (Vector3 point in pointsToRemove)
        {
            poissonData.candidatePoints.Remove(point);
        }
    }

    private static DistrictType CalculateBestDistrictForLocation(Vector3 location, List<DistrictType> districtTypesToPlace)
    {
        DistrictType bestDistrictType = districtTypesToPlace[0];
        float bestSuitability = float.MinValue;

        foreach (DistrictType type in districtTypes)
        {
            if (districtTypesToPlace.Contains(type))
            {
                float suitability = CalculateSuitability(type, location);
                if (suitability > bestSuitability)
                {
                    bestSuitability = suitability;
                    bestDistrictType = type;
                }
            }
        }

        return bestDistrictType;
    }

    private static List<DistrictType> GetMinDistrictTypes()
    {
        List<DistrictType> minDistrictTypes = new List<DistrictType>();

        foreach (DistrictType type in districtTypes)
        {
            int minPlacements = type.minNumberOfPlacements;
            for (int i = 0; i < minPlacements; i++)
            {
                minDistrictTypes.Add(type);
            }
        }

        return minDistrictTypes;
    }

    private static List<DistrictType> GetRestDistrictTypes()
    {
        List<DistrictType> restDistrictTypes = new List<DistrictType>();

        foreach (DistrictType type in districtTypes)
        {
            int restPlacements = type.maxNumberOfPlacements - type.minNumberOfPlacements;
            for (int i = 0; i < restPlacements; i++)
            {
                restDistrictTypes.Add(type);
            }
        }

        return restDistrictTypes;
    }

    private static float CalculateSuitability(DistrictType type, Vector3 location)
    {
        float Sd = CalculateSuitabilityBasedOnNeighbors(type, location);
        float Sa = CalculateSuitabilityBasedOnPosition(type, location);

        float S = importanceOfNeighbours * Sd + importanceOfCityCenterDistance * Sa;
        return S;
    }

    private static float CalculateSuitabilityBasedOnNeighbors(DistrictType type, Vector3 location)
    {
        float Sd = 0f;
        foreach (District placedDistrict in generatedDistricts)
        {
            float attraction = GetAttraction(type, placedDistrict.type);
            float repulsion = GetRepulsion(type, placedDistrict.type);
            float distance = ScaleDistance(Vector3.Distance(location, placedDistrict.position));

            Sd += (attraction - repulsion) / distance;
        }
        return Sd;
    }

    private static float CalculateSuitabilityBasedOnPosition(DistrictType type, Vector3 location)
    {
        float distanceFromCenter = Vector3.Distance(location, center);
        float scaledDistance = ScaleDistance(distanceFromCenter);

        //float Sa = 10 - CalculateAverage(scaledDistance, type.distanceFromCenter);
        float Sa = GetSuitability(scaledDistance, type.distanceFromCenter);

        return Sa;
    }

    private static float ScaleDistance(float value)
    {
        float originalMax = outerBoundaryRadius;
        float normalizedVal = (value - 0) / (originalMax - 0);
        float scaledVal = normalizedVal * (10 - 0) + 0;
        return scaledVal;
    }

    private static float GetAttraction(DistrictType currentType, DistrictType otherType)
    {
        foreach (DistrictRelation relation in currentType.relations)
        {
            if (relation.districtTypeId == otherType.id)
            {
                return relation.attraction;
            }
        }
        return 0f;
    }
    private static float GetRepulsion(DistrictType currentType, DistrictType otherType)
    {
        foreach (DistrictRelation relation in currentType.relations)
        {
            if (relation.districtTypeId == otherType.id)
            {
                return relation.repulsion;
            }
        }
        return 0f;
    }

    private static float CalculateAverage(float a, float b)
    {
        return (a + b) / 2.0f;
    }

    private static float GetSuitability(float calculatedValue, float specifiedValue)
    {
        if (Mathf.Approximately(calculatedValue, specifiedValue))
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

    private static int GenerateUniqueID()
    {
        return ++counter;
    }
}
