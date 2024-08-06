using System.Collections.Generic;
using UnityEngine;

public class PoissonDiskSampling : MonoBehaviour
{
    public static List<Vector3> GenerateDistrictPoints(int numberOfDistricts, int minNumberOfDistricts, int maxNumberOfDistricts, float cityRadius, Vector3 cityCenter, int numSampleBeforeRejection = 30)
    {
        int districts = ValidateNumberOfDistricts(numberOfDistricts, minNumberOfDistricts, maxNumberOfDistricts, true);

        float districtRadius = CalculateDistrictRadius(districts, cityRadius);

        float cellSize = districtRadius / Mathf.Sqrt(2);
        int gridSize = Mathf.CeilToInt((cityRadius * 2) / cellSize);
        int[,] grid = new int[gridSize, gridSize];
        List<Vector3> points = new List<Vector3>();
        List<Vector3> spawnPoints = new List<Vector3>() { cityCenter };

        while (spawnPoints.Count > 0 && points.Count < districts)
        {
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            Vector3 spawnCentre = spawnPoints[spawnIndex];
            bool candidateAccepted = false;

            for (int i = 0; i < numSampleBeforeRejection; i++)
            {
                float angle = Random.value * Mathf.PI * 2;
                Vector3 dir = new Vector3(Mathf.Sin(angle), 1, Mathf.Cos(angle));
                float rand = Random.Range(districtRadius, 2 * districtRadius);
                Vector3 candidate = new Vector3(spawnCentre.x + dir.x * rand, 1, spawnCentre.z + dir.z * rand);

                if (IsValid(candidate, cityCenter, cityRadius, cellSize, districtRadius, points, grid) &&
                    Vector3.Distance(candidate, cityCenter) <= cityRadius)
                {
                    points.Add(candidate);
                    spawnPoints.Add(candidate);
                    int gridX = Mathf.FloorToInt((candidate.x - (cityCenter.x - cityRadius)) / cellSize);
                    int gridZ = Mathf.FloorToInt((candidate.z - (cityCenter.z - cityRadius)) / cellSize);
                    grid[gridX, gridZ] = points.Count;
                    candidateAccepted = true;
                    break;
                }
            }
            if (!candidateAccepted)
            {
                spawnPoints.RemoveAt(spawnIndex);
            }
        }

        return points;
    }

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
                    if (pointIndex != -1)
                    {
                        float sqrDst = (candidate - points[pointIndex]).sqrMagnitude;
                        if (sqrDst < radius * radius)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        return false;
    }

    private static float CalculateDistrictRadius(int numberOfDistricts, float cityRadius)
    {
        float cityArea = Mathf.PI * cityRadius * cityRadius;
        float areaPerDistrict = cityArea / numberOfDistricts * 2;
        float adjustedDistrictRadius = Mathf.Sqrt(areaPerDistrict / Mathf.PI);
        return adjustedDistrictRadius;
    }

    public static int ValidateNumberOfDistricts(int setNumber, int minNumber, int maxNumber, bool warning)
    {
        int districts = setNumber;
        if (setNumber < minNumber)
        {
            districts = minNumber;
            if (warning) Debug.LogWarning("numberOfDistricts was too low and has been set to the minimum number of provided districts");
        }
        else if (setNumber > maxNumber)
        {
            districts = maxNumber;
            if (warning) Debug.LogWarning("numberOfDistricts was too high and has been set to the maximum number of provided districts");
        }
        return districts;
    }
}