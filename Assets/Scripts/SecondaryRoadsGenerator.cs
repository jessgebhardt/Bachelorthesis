using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SecondaryRoadsGenerator : MonoBehaviour
{
    private static int width;
    public static Texture2D GenerateSecondaryRoads(Texture2D voronoiTexture, int roadWidth)
    {
        List<List<Vector2Int>> extractedRegions = DistrictExtractor.ExtractRegions(voronoiTexture, 0);
        List<List<Vector2Int>> regionsSegmentMarks = PrepareSegments(extractedRegions);

        width = roadWidth;
        Vector2Int[] chosenSegments = ChooseStartpoints(regionsSegmentMarks);

        voronoiTexture = GenerateRoads(extractedRegions, chosenSegments, voronoiTexture);

        return voronoiTexture;
    }

    private static List<List<Vector2Int>> PrepareSegments(List<List<Vector2Int>> extractedRegions)
    {
        List<List<Vector2Int>> extractedSegments = new List<List<Vector2Int>>();

        foreach (List<Vector2Int> regionPixels in extractedRegions)
        {
            HashSet<Vector2Int> regionSet = new HashSet<Vector2Int>(regionPixels);
            List<Vector2Int> segmentPoints = new List<Vector2Int>();

            foreach (Vector2Int regionPixel in regionPixels)
            {
                foreach (Vector2Int neighbor in GetNeighbors(regionPixel, false))
                {
                    if (!regionPixels.Contains(neighbor))
                    {
                        segmentPoints.Add(neighbor);
                        continue;
                    }
                }
                continue;
            }

            extractedSegments.Add(segmentPoints);
        }

        return extractedSegments;
    }

    private static List<Vector2Int> GetNeighbors(Vector2Int point, bool includeDiagonals)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>
        {
            new Vector2Int(point.x, point.y + 1),
            new Vector2Int(point.x + 1, point.y),
            new Vector2Int(point.x, point.y - 1),
            new Vector2Int(point.x - 1, point.y),
        };

        if (includeDiagonals)
        {
            neighbors.Add(new Vector2Int(point.x - 1, point.y + 1));
            neighbors.Add(new Vector2Int(point.x + 1, point.y + 1));
            neighbors.Add(new Vector2Int(point.x + 1, point.y - 1));
            neighbors.Add(new Vector2Int(point.x - 1, point.y - 1));
        }

        return neighbors;
    }

    private static Vector2Int[] ChooseStartpoints(List<List<Vector2Int>> regionsSegmentMarks)
    {
        Vector2Int[] chosenSegments = new Vector2Int[regionsSegmentMarks.Count];

        for (int i = 0; i < regionsSegmentMarks.Count; i++)
        {
            int randomNumber = Random.Range(0, regionsSegmentMarks[i].Count);
            chosenSegments[i] = regionsSegmentMarks[i][randomNumber];
        }

        return chosenSegments;
    }

    private static Texture2D GenerateRoads(List<List<Vector2Int>> extractedRegions, Vector2Int[] chosenSegments, Texture2D voronoiTexture)
    {
        string axiom = "A";
        float angle = 90f;
        int segmentLength = 50 + width * 2;

        int regionsCount = extractedRegions.Count;
        List<Vector2Int> allPixelsToDraw = new List<Vector2Int>();

        Parallel.For(0, regionsCount, i =>
        {
            List<Vector2Int> pixelsToDraw = LSystem.GenerateLSystem(axiom, angle, segmentLength, extractedRegions[i], chosenSegments[i]);
            lock (allPixelsToDraw)
            {
                allPixelsToDraw.AddRange(pixelsToDraw);
            }
        });

        ApplyChanges(voronoiTexture, allPixelsToDraw);
        voronoiTexture.Apply();
        return voronoiTexture;
    }

    private static void ApplyChanges(Texture2D texture, List<Vector2Int> pixelsToDraw)
    {
        foreach (Vector2Int pixel in pixelsToDraw)
        {
            for (int dx = -width; dx <= width; dx++)
            {
                for (int dy = -width; dy <= width; dy++)
                {
                    int newX = pixel.x + dx;
                    int newY = pixel.y + dy;
                    if (newX >= 0 && newX < texture.width && newY >= 0 && newY < texture.height)
                    {
                        texture.SetPixel(newX, newY, Color.black);
                    }
                }
            }
        }
    }
}
