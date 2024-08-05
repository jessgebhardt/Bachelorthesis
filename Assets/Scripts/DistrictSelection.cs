using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

public class DistrictSelection : MonoBehaviour
{
    public static void SelectDistrictPositions(DistrictsData districtsData, CityBoundariesData boundariesData, PoissonDiskData poissonDiskData)
    {
        if (poissonDiskData.candidatePoints == null || poissonDiskData.candidatePoints.Count == 0)
        {
            Debug.LogError("No candidate points available for district generation.");
            return;
        }

        districtsData.generatedDistricts.Clear();
        districtsData.districtsDictionary = new Dictionary<int, District>();

        List<Vector3> locations = new List<Vector3>(poissonDiskData.candidatePoints);
        List<Vector3> pointsToRemove = new List<Vector3>();

        List<DistrictType> minDistrictTypes = GetMinDistrictTypes(districtsData.districtTypes);
        List<DistrictType> restDistrictTypes = GetRestDistrictTypes(districtsData.districtTypes);

        foreach (Vector3 location in locations)
        {
            List<DistrictType> districtTypesToPlace = minDistrictTypes;

            if (minDistrictTypes.Count == 0)
            {
                districtTypesToPlace = restDistrictTypes;
            }
            DistrictType bestDistrictType = CalculateBestDistrictForLocation(location, districtTypesToPlace, districtsData, boundariesData);

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
            districtsData.generatedDistricts.Add(newDistrict);
            districtsData.districtsDictionary.Add(GenerateUniqueID(districtsData.counter), newDistrict);
            pointsToRemove.Add(location);
        }

        foreach (Vector3 point in pointsToRemove)
        {
            poissonDiskData.candidatePoints.Remove(point);
        }
    }

    private static DistrictType CalculateBestDistrictForLocation(Vector3 location, List<DistrictType> districtTypesToPlace, DistrictsData districtsData, CityBoundariesData boundariesData)
    {
        DistrictType bestDistrictType = districtTypesToPlace[0];
        float bestSuitability = float.MinValue;

        foreach (DistrictType type in districtsData.districtTypes)
        {
            if (districtTypesToPlace.Contains(type))
            {
                float suitability = CalculateSuitability(type, location, districtsData, boundariesData);
                if (suitability > bestSuitability)
                {
                    bestSuitability = suitability;
                    bestDistrictType = type;
                }
            }
        }

        return bestDistrictType;
    }

    private static List<DistrictType> GetMinDistrictTypes(List<DistrictType> districtTypes)
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

    private static List<DistrictType> GetRestDistrictTypes(List<DistrictType> districtTypes)
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

    private static float CalculateSuitability(DistrictType type, Vector3 location, DistrictsData districtsData, CityBoundariesData boundariesData)
    {
        float Sd = CalculateSuitabilityBasedOnNeighbors(type, location, districtsData.generatedDistricts, boundariesData.outerBoundaryRadius);
        float Sa = CalculateSuitabilityBasedOnPosition(type, location, districtsData.generatedDistricts, boundariesData.center, boundariesData.outerBoundaryRadius);

        float S = districtsData.importanceOfNeighbours * Sd + districtsData.importanceOfCityCenterDistance * Sa;
        return S;
    }

    private static float CalculateSuitabilityBasedOnNeighbors(DistrictType type, Vector3 location, List<District> generatedDistricts, float outerBoundaryRadius)
    {
        float Sd = 0f;
        foreach (District placedDistrict in generatedDistricts)
        {
            float attraction = GetAttraction(type, placedDistrict.type);
            float repulsion = GetRepulsion(type, placedDistrict.type);
            float distance = ScaleDistance(Vector3.Distance(location, placedDistrict.position), outerBoundaryRadius);

            Sd += (attraction - repulsion) / distance;
        }
        return Sd;
    }

    private static float CalculateSuitabilityBasedOnPosition(DistrictType type, Vector3 location, List<District> generatedDistricts, Vector3 center, float outerBoundaryRadius)
    {
        float distanceFromCenter = Vector3.Distance(location, center);
        float scaledDistance = ScaleDistance(distanceFromCenter, outerBoundaryRadius);

        //float Sa = 10 - CalculateAverage(scaledDistance, type.distanceFromCenter);
        float Sa = GetSuitability(scaledDistance, type.distanceFromCenter);

        return Sa;
    }

    private static float ScaleDistance(float value, float outerBoundaryRadius)
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

    private static int GenerateUniqueID(int counter)
    {
        return ++counter;
    }
}
