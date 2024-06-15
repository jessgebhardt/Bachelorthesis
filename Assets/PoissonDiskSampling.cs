using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoissonDiskSampling : MonoBehaviour
{
    public static List<Vector3> GenerateCandiatePoints(float radius, Vector3 sampleRegionSize, int numSampleBeforeRejection = 30)
    {
        float cellSize = radius/Mathf.Sqrt(2);

        int[,] grid = new int[Mathf.CeilToInt(sampleRegionSize.x / cellSize), Mathf.CeilToInt(sampleRegionSize.z / cellSize)];
        List<Vector3> points = new List<Vector3>();
        List<Vector3> spawnPoints = new List<Vector3>();

        spawnPoints.Add(new Vector3(sampleRegionSize.x/2, 1, sampleRegionSize.z / 2));
        while (spawnPoints.Count > 0)
        {
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            Vector3 spawnCentre = spawnPoints[spawnIndex];
            bool candidateAccepted = false;

            for (int i = 0; i < numSampleBeforeRejection; i++)
            {
                float angle = Random.value * Mathf.PI*2;
                Vector3 dir = new Vector3(Mathf.Sin(angle), 1, Mathf.Cos(angle));
                // Vector3 candidate = spawnCentre + dir * Random.Range(radius, 2 * radius);
                // Debug.Log(candidate);
                float rand = Random.Range(radius, 2 * radius);
                Vector3 candidate = new Vector3(spawnCentre.x + dir.x * rand, 1 , spawnCentre.z + dir.z * rand);
                if (IsValid(candidate, sampleRegionSize, cellSize, radius, points, grid))
                {
                    points.Add(candidate);
                    spawnPoints.Add(candidate);
                    grid[(int)(candidate.x / cellSize), (int)(candidate.z / cellSize)] = points.Count;
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

    static bool IsValid(Vector3 candidate, Vector3 sampleRegionSize, float cellSize,float radius, List<Vector3> points, int[,] grid)
    {
        if (candidate.x >= 0 && candidate.x < sampleRegionSize.x && candidate.z >= 0 && candidate.z < sampleRegionSize.z)
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
                        if (sqrDst < radius*radius)
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
}
