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

    /// <summary>
    /// Selects and places districts based on candidate points and district data.
    /// <para>
    /// Initializes district data, calculates the best district type for each candidate point,
    /// places the districts, and removes used candidate points.
    /// </para>
    /// </summary>
    /// <param name="poissonData">Data containing candidate points for district placement.</param>
    /// <param name="districtData">Data related to district types, their importance, and generated districts.</param>
    /// <param name="boundariesData">Data containing city center and outer boundary radius.</param>
    public static void SelectDistrictPositions(PoissonData poissonData, DistrictData districtData, BoundariesData boundariesData)
    {
        if (poissonData.candidatePoints == null || poissonData.candidatePoints.Count == 0)
        {
            Debug.LogError("No candidate points available for district generation.");
            return;
        }

        InitializeDistrictData(districtData, boundariesData);

        List<Vector3> locations = new List<Vector3>(poissonData.candidatePoints);
        List<Vector3> pointsToRemove = new List<Vector3>();

        List<DistrictType> minDistrictTypes = GetMinDistrictTypes();
        List<DistrictType> restDistrictTypes = GetRestDistrictTypes();

        foreach (Vector3 location in locations)
        {
            List<DistrictType> districtTypesToPlace = minDistrictTypes.Count > 0 ? minDistrictTypes : restDistrictTypes;
            DistrictType bestDistrictType = CalculateBestDistrictForLocation(location, districtTypesToPlace);

            if (minDistrictTypes.Count > 0)
                minDistrictTypes.Remove(bestDistrictType);
            else
                restDistrictTypes.Remove(bestDistrictType);

            AddDistrict(districtData, location, bestDistrictType);
            pointsToRemove.Add(location);
        }

        RemoveUsedPoints(poissonData, pointsToRemove);
    }

    /// <summary>
    /// Initializes the district data and boundary parameters.
    /// <para>
    /// Sets up the district types, importance values, generated districts, and boundary details.
    /// </para>
    /// </summary>
    /// <param name="districtData">Data related to district types and generated districts.</param>
    /// <param name="boundariesData">Data containing city center and outer boundary radius.</param>
    private static void InitializeDistrictData(DistrictData districtData, BoundariesData boundariesData)
    {
        counter = -1;

        districtTypes = districtData.districtTypes;
        importanceOfNeighbours = districtData.importanceOfNeighbours;
        importanceOfCityCenterDistance = districtData.importanceOfCityCenterDistance;
        generatedDistricts = districtData.generatedDistricts;
        center = boundariesData.center;
        outerBoundaryRadius = boundariesData.outerBoundaryRadius;

        districtData.generatedDistricts.Clear();
        districtData.districtsDictionary = new Dictionary<int, District>();
    }

    /// <summary>
    /// Creates a new district and adds it to the list of generated districts and the dictionary of districts.
    /// <para>
    /// Assigns a unique ID to the district and adds it to the data structures.
    /// </para>
    /// </summary>
    /// <param name="districtData">Data related to district types and generated districts.</param>
    /// <param name="location">Position of the new district.</param>
    /// <param name="bestDistrictType">Type of the district to be created.</param>
    private static void AddDistrict(DistrictData districtData, Vector3 location, DistrictType bestDistrictType)
    {
        District newDistrict = new District
        {
            name = bestDistrictType.name,
            position = location,
            type = bestDistrictType
        };
        districtData.generatedDistricts.Add(newDistrict);
        districtData.districtsDictionary.Add(GenerateUniqueID(), newDistrict);
    }

    /// <summary>
    /// Removes the specified points from the list of candidate points.
    /// <para>
    /// Ensures that points used for district placement are no longer available for future placement.
    /// </para>
    /// </summary>
    /// <param name="poissonData">Data containing candidate points.</param>
    /// <param name="pointsToRemove">List of points to be removed.</param>
    private static void RemoveUsedPoints(PoissonData poissonData, List<Vector3> pointsToRemove)
    {
        foreach (Vector3 point in pointsToRemove)
        {
            poissonData.candidatePoints.Remove(point);
        }
    }

    /// <summary>
    /// Calculates the most suitable district type for a given location based on suitability criteria.
    /// <para>
    /// Evaluates each district type and selects the one with the highest suitability for the location.
    /// </para>
    /// </summary>
    /// <param name="location">Location where the district will be placed.</param>
    /// <param name="districtTypesToPlace">List of district types available for placement.</param>
    /// <returns>The most suitable district type for the location.</returns>
    private static DistrictType CalculateBestDistrictForLocation(Vector3 location, List<DistrictType> districtTypesToPlace)
    {
        DistrictType bestDistrictType = districtTypesToPlace[0];
        float bestSuitability = float.MinValue;

        foreach (DistrictType type in districtTypesToPlace)
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

    /// <summary>
    /// Gets the list of district types that must be placed at least the minimum number of times.
    /// <para>
    /// Creates a list where each district type appears according to its minimum number of placements.
    /// </para>
    /// </summary>
    /// <returns>A list of district types with minimum placement requirements.</returns>
    private static List<DistrictType> GetMinDistrictTypes()
    {
        List<DistrictType> minDistrictTypes = new List<DistrictType>();

        foreach (DistrictType type in districtTypes)
        {
            for (int i = 0; i < type.minNumberOfPlacements; i++)
            {
                minDistrictTypes.Add(type);
            }
        }

        return minDistrictTypes;
    }

    /// <summary>
    /// Gets the list of remaining district types that can be placed beyond the minimum requirement.
    /// <para>
    /// Creates a list where each district type appears according to its remaining maximum placements.
    /// </para>
    /// </summary>
    /// <returns>A list of district types with remaining placement opportunities.</returns>
    private static List<DistrictType> GetRestDistrictTypes()
    {
        List<DistrictType> restDistrictTypes = new List<DistrictType>();

        foreach (DistrictType type in districtTypes)
        {
            for (int i = 0; i < type.maxNumberOfPlacements - type.minNumberOfPlacements; i++)
            {
                restDistrictTypes.Add(type);
            }
        }

        return restDistrictTypes;
    }

    /// <summary>
    /// Calculates the suitability of a district type for a location based on various criteria.
    /// <para>
    /// Considers both the importance of neighboring districts and distance from the city center.
    /// </para>
    /// </summary>
    /// <param name="type">District type to evaluate.</param>
    /// <param name="location">Location where the district would be placed.</param>
    /// <returns>The calculated suitability score for the district type and location.</returns>
    private static float CalculateSuitability(DistrictType type, Vector3 location)
    {
        float Sd = CalculateSuitabilityBasedOnNeighbors(type, location);
        float Sa = CalculateSuitabilityBasedOnPosition(type, location);

        return importanceOfNeighbours * Sd + importanceOfCityCenterDistance * Sa;
    }

    /// <summary>
    /// Calculates the suitability of a district type based on its neighboring districts.
    /// <para>
    /// Evaluates the attraction and repulsion between the candidate district and existing districts.
    /// </para>
    /// </summary>
    /// <param name="type">District type to evaluate.</param>
    /// <param name="location">Location where the district would be placed.</param>
    /// <returns>The suitability score based on neighboring districts.</returns>
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

    /// <summary>
    /// Calculates the suitability of a district type based on its distance from the city center.
    /// <para>
    /// Evaluates how well the district type matches the desired distance from the city center.
    /// </para>
    /// </summary>
    /// <param name="type">District type to evaluate.</param>
    /// <param name="location">Location where the district would be placed.</param>
    /// <returns>The suitability score based on distance from the city center.</returns>
    private static float CalculateSuitabilityBasedOnPosition(DistrictType type, Vector3 location)
    {
        float distanceFromCenter = Vector3.Distance(location, center);
        float scaledDistance = ScaleDistance(distanceFromCenter);

        return GetSuitability(scaledDistance, type.distanceFromCenter);
    }

    /// <summary>
    /// Scales a distance value based on the outer boundary radius.
    /// <para>
    /// Converts the distance into a normalized value for suitability calculations.
    /// </para>
    /// </summary>
    /// <param name="value">Distance value to scale.</param>
    /// <returns>The scaled distance value.</returns>
    private static float ScaleDistance(float value)
    {
        float normalizedVal = value / outerBoundaryRadius;
        return normalizedVal * 10f;
    }

    /// <summary>
    /// Retrieves the attraction value between two district types based on their relationship.
    /// <para>
    /// Searches the relations of the current district type to find the attraction value for the given other district type.
    /// </para>
    /// </summary>
    /// <param name="currentType">The current district type.</param>
    /// <param name="otherType">The other district type to compare with.</param>
    /// <returns>The attraction value between the two district types.</returns>
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

    /// <summary>
    /// Retrieves the repulsion value between two district types based on their relationship.
    /// <para>
    /// Searches the relations of the current district type to find the repulsion value for the given other district type.
    /// </para>
    /// </summary>
    /// <param name="currentType">The current district type.</param>
    /// <param name="otherType">The other district type to compare with.</param>
    /// <returns>The repulsion value between the two district types.</returns>
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

    /// <summary>
    /// Determines the suitability score based on the calculated value and specified value.
    /// <para>
    /// Compares the calculated value with the specified value to determine suitability.
    /// </para>
    /// </summary>
    /// <param name="calculatedValue">Calculated suitability value.</param>
    /// <param name="specifiedValue">Desired suitability value.</param>
    /// <returns>The suitability score based on the comparison.</returns>
    private static float GetSuitability(float calculatedValue, float specifiedValue)
    {
        if (Mathf.Approximately(calculatedValue, specifiedValue))
            return 10f;
        else if (calculatedValue < specifiedValue)
            return 5f;
        else
            return 0f;
    }

    /// <summary>
    /// Generates a unique ID for a new district.
    /// <para>
    /// Increments the counter and returns its value as the unique ID.
    /// </para>
    /// </summary>
    /// <returns>A unique ID for the new district.</returns>
    private static int GenerateUniqueID()
    {
        return ++counter;
    }
}
