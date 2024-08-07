using System.Collections.Generic;
using UnityEngine;

public class PoissonDiskSampling : MonoBehaviour
{
    /// <summary>
    /// Generates a list of points for district selection and placement using Poisson disk sampling.
    /// <para>
    /// Calculates the number of districts and their radius and sets up a grid.
    /// Also generates and validates points to ensure they are properly spaced and within the boundaries.
    /// </para>
    /// </summary>
    /// <param name="numberOfDistricts">Total number of districts to generate.</param>
    /// <param name="minNumberOfDistricts">Minimum number of districts allowed.</param>
    /// <param name="maxNumberOfDistricts">Maximum number of districts allowed.</param>
    /// <param name="cityRadius">Radius of the city area.</param>
    /// <param name="cityCenter">Center position of the city.</param>
    /// <param name="numSampleBeforeRejection">Number of samples to try before rejecting a spawn point.</param>
    /// <returns>A list of Vector3 points representing district locations.</returns>
    public static List<Vector3> GenerateDistrictPoints(int numberOfDistricts, int minNumberOfDistricts, int maxNumberOfDistricts, float cityRadius, Vector3 cityCenter, int numSampleBeforeRejection = 30)
    {
        int districts = ValidateNumberOfDistricts(numberOfDistricts, minNumberOfDistricts, maxNumberOfDistricts);
        float districtRadius = CalculateDistrictRadius(districts, cityRadius);
        float cellSize = districtRadius / Mathf.Sqrt(2);
        int gridSize = Mathf.CeilToInt((cityRadius * 2) / cellSize);
        int[,] grid = new int[gridSize, gridSize];
        List<Vector3> points = new List<Vector3>();
        List<Vector3> spawnPoints = new List<Vector3>() { cityCenter };

        while (spawnPoints.Count > 0 && points.Count < districts)
        {
            GeneratePoints(cityCenter, cityRadius, districtRadius, cellSize, points, spawnPoints, grid, numSampleBeforeRejection);
        }

        return points;
    }

    /// <summary>
    /// Generates candidate points around a spawn point and adds valid points to the list.
    /// <para>
    /// Picks a spawn point, samples multiple candidate points around it, and validates them.
    /// If a valid point is found, it's added to the list of points and spawn points.
    /// </para>
    /// </summary>
    /// <param name="cityCenter">Center position of the city.</param>
    /// <param name="cityRadius">Radius of the city area.</param>
    /// <param name="districtRadius">Radius of each district.</param>
    /// <param name="cellSize">Size of each grid cell.</param>
    /// <param name="points">List of valid points.</param>
    /// <param name="spawnPoints">List of points used as spawning locations for new candidates.</param>
    /// <param name="grid">Grid used for spatial indexing of points.</param>
    /// <param name="numSampleBeforeRejection">Number of candidate points to sample before rejecting.</param>
    private static void GeneratePoints(Vector3 cityCenter, float cityRadius, float districtRadius, float cellSize, List<Vector3> points, List<Vector3> spawnPoints, int[,] grid, int numSampleBeforeRejection)
    {
        int spawnIndex = Random.Range(0, spawnPoints.Count);
        Vector3 spawnCentre = spawnPoints[spawnIndex];
        bool candidateAccepted = false;

        for (int i = 0; i < numSampleBeforeRejection; i++)
        {
            Vector3 candidate = GenerateCandidate(spawnCentre, districtRadius);
            if (IsValid(candidate, cityCenter, cityRadius, cellSize, districtRadius, points, grid))
            {
                AddPoint(candidate, cityCenter, cityRadius, cellSize, points, spawnPoints, grid);
                candidateAccepted = true;
                break;
            }
        }

        if (!candidateAccepted)
        {
            spawnPoints.RemoveAt(spawnIndex);
        }
    }

    /// <summary>
    /// Generates a random candidate point around a given center.
    /// <para>
    /// Uses polar coordinates to create a point at a random angle and distance within a specified radius.
    /// </para>
    /// </summary>
    /// <param name="spawnCentre">Center point around which the candidate is generated.</param>
    /// <param name="districtRadius">Radius within which to generate the candidate point.</param>
    /// <returns>The generated candidate point.</returns>
    private static Vector3 GenerateCandidate(Vector3 spawnCentre, float districtRadius)
    {
        float angle = Random.value * Mathf.PI * 2;
        Vector3 dir = new Vector3(Mathf.Sin(angle), 1, Mathf.Cos(angle));
        float rand = Random.Range(districtRadius, 2 * districtRadius);
        return new Vector3(spawnCentre.x + dir.x * rand, 1, spawnCentre.z + dir.z * rand);
    }

    /// <summary>
    /// Adds a valid candidate point to the list of points and updates the grid.
    /// <para>
    /// Also adds the candidate point to the list of spawn points for further sampling.
    /// </para>
    /// </summary>
    /// <param name="candidate">The valid candidate point to add.</param>
    /// <param name="cityCenter">Center position of the city.</param>
    /// <param name="cityRadius">Radius of the city area.</param>
    /// <param name="cellSize">Size of each grid cell for spatial indexing.</param>
    /// <param name="points">List of valid points where districts are placed.</param>
    /// <param name="spawnPoints">List of points used as spawning locations for new candidates.</param>
    /// <param name="grid">Grid used for spatial indexing of points.</param>
    private static void AddPoint(Vector3 candidate, Vector3 cityCenter, float cityRadius, float cellSize, List<Vector3> points, List<Vector3> spawnPoints, int[,] grid)
    {
        points.Add(candidate);
        spawnPoints.Add(candidate);
        int gridX = Mathf.FloorToInt((candidate.x - (cityCenter.x - cityRadius)) / cellSize);
        int gridZ = Mathf.FloorToInt((candidate.z - (cityCenter.z - cityRadius)) / cellSize);
        grid[gridX, gridZ] = points.Count;
    }

    /// <summary>
    /// Validates whether a candidate point is suitable based on distance constraints and grid cell checks.
    /// <para>
    /// Ensures the candidate point is within the city bounds and sufficiently far from existing points.
    /// </para>
    /// </summary>
    /// <param name="candidate">The candidate point to validate.</param>
    /// <param name="cityCenter">Center position of the city.</param>
    /// <param name="cityRadius">Radius of the city area.</param>
    /// <param name="cellSize">Size of each grid cell for spatial indexing.</param>
    /// <param name="radius">Minimum distance required between points.</param>
    /// <param name="points">List of valid points where districts are placed.</param>
    /// <param name="grid">Grid used for spatial indexing of points.</param>
    /// <returns>True if the candidate point is valid, false otherwise.</returns>
    private static bool IsValid(Vector3 candidate, Vector3 cityCenter, float cityRadius, float cellSize, float radius, List<Vector3> points, int[,] grid)
    {
        if (Vector3.Distance(candidate, cityCenter) < cityRadius)
        {
            int cellX = (int)(candidate.x / cellSize);
            int cellZ = (int)(candidate.z / cellSize);
            int searchStartX = Mathf.Max(0, cellX - 2);
            int searchEndX = Mathf.Min(cellX + 2, grid.GetLength(0) - 1);
            int searchStartZ = Mathf.Max(0, cellZ - 2);
            int searchEndZ = Mathf.Min(cellZ + 2, grid.GetLength(1) - 1);

            for (int x = searchStartX; x <= searchEndX; x++)
            {
                for (int z = searchStartZ; z <= searchEndZ; z++)
                {
                    int pointIndex = grid[x, z] - 1;
                    if (pointIndex != -1 && (candidate - points[pointIndex]).sqrMagnitude < radius * radius)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Calculates the radius of each district based on the total number of districts and city area.
    /// <para>
    /// Computes the radius required to evenly distribute districts over the city area.
    /// </para>
    /// </summary>
    /// <param name="numberOfDistricts">Total number of districts to generate.</param>
    /// <param name="cityRadius">Radius of the city area.</param>
    /// <returns>The calculated radius for each district.</returns>
    private static float CalculateDistrictRadius(int numberOfDistricts, float cityRadius)
    {
        float cityArea = Mathf.PI * cityRadius * cityRadius;
        float areaPerDistrict = cityArea / numberOfDistricts * 2;
        return Mathf.Sqrt(areaPerDistrict / Mathf.PI);
    }

    /// <summary>
    /// Validates and adjusts the number of districts to ensure it falls within the specified range.
    /// <para>
    /// Logs warnings if the number of districts is outside the provided minimum or maximum bounds and adjusts it accordingly.
    /// </para>
    /// </summary>
    /// <param name="setNumber">The number of districts to validate.</param>
    /// <param name="minNumber">The minimum number of districts allowed.</param>
    /// <param name="maxNumber">The maximum number of districts allowed.</param>
    /// <returns>The validated number of districts.</returns>
    public static int ValidateNumberOfDistricts(int setNumber, int minNumber, int maxNumber)
    {
        if (setNumber < minNumber)
        {
            Debug.LogWarning("numberOfDistricts was too low and has been set to the minimum number of provided districts");
            return minNumber;
        }
        else if (setNumber > maxNumber)
        {
            Debug.LogWarning("numberOfDistricts was too high and has been set to the maximum number of provided districts");
            return maxNumber;
        }
        return setNumber;
    }
}