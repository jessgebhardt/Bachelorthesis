using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

public class SecondaryRoadsGenerator : MonoBehaviour
{
    public static void GenerateSecondaryRoads(List<List<Vector2Int>> extractedRegions, List<List<Vector2Int>> regionsSegmentMarks)
    {
        Debug.Log(extractedRegions.Count);
        Debug.Log(regionsSegmentMarks.Count);

        List<Vector2Int[]> chosenSegments = ChooseStartpoints(regionsSegmentMarks);

        GenerateRoads();

        Debug.Log(chosenSegments.Count);

    }

    private static (Vector2Int, Vector2Int) FindMaxDistancePoints(List<Vector2Int> points)
    {
        if (points == null || points.Count < 2)
        {
            throw new System.ArgumentException("The list must contain at least two points.");
        }

        float maxDistance = 0f;
        Vector2Int maxPoint1 = points[0];
        Vector2Int maxPoint2 = points[1];

        for (int i = 0; i < points.Count - 1; i++)
        {
            for (int j = i + 1; j < points.Count; j++)
            {
                float distance = Vector2Int.Distance(points[i], points[j]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    maxPoint1 = points[i];
                    maxPoint2 = points[j];
                }
            }
        }

        return (maxPoint1, maxPoint2);
    }

    private static List<Vector2Int[]> ChooseStartpoints(List<List<Vector2Int>> regionsSegmentMarks)
    {
        List<Vector2Int[]> chosenSegments = new List<Vector2Int[]>();

        for (int i = 0; i < regionsSegmentMarks.Count; i++)
        {
            if (regionsSegmentMarks[(int)i].Count >= 2)
            {
                Vector2Int[] segments = new Vector2Int[2];
                (Vector2Int point1, Vector2Int point2) = FindMaxDistancePoints(regionsSegmentMarks[(int)i]);
                segments[0] = point1;
                segments[1] = point2;
                chosenSegments.Add(segments);
            }
            else
            {
                Vector2Int[] segments = new Vector2Int[1];
                segments[0] = regionsSegmentMarks[(int)i][0];
                chosenSegments.Add(segments);
            }
        }

        return chosenSegments;
    }

    private static void GenerateRoads()
    {

    }
}
