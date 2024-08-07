using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SecondaryRoadsGenerator : MonoBehaviour
{
    private static int width;

    /// <summary>
    /// Generates secondary roads on the Voronoi diagram texture.
    /// </summary>
    /// <param name="voronoiTexture">The Voronoi diagram texture.</param>
    /// <param name="roadData">The road data.</param>
    /// <returns>The texture with secondary roads.</returns>
    public static Texture2D GenerateSecondaryRoads(Texture2D voronoiTexture, RoadData roadData)
    {
        width = roadData.roadWidth;

        List<List<Vector2Int>> extractedRegions = DistrictExtractor.ExtractRegions(voronoiTexture, 0);

        List<List<Vector2Int>> regionsSegmentMarks = PrepareSegments(extractedRegions);

        Vector2Int[] chosenSegments = ChooseStartpoints(regionsSegmentMarks);

        voronoiTexture = GenerateRoads(extractedRegions, chosenSegments, voronoiTexture, roadData);

        return voronoiTexture;
    }

    /// <summary>
    /// Prepares segment points for each extracted region.
    /// </summary>
    /// <param name="extractedRegions">The list of extracted regions.</param>
    /// <returns>The list of segment points for each region.</returns>
    private static List<List<Vector2Int>> PrepareSegments(List<List<Vector2Int>> extractedRegions)
    {
        List<List<Vector2Int>> extractedSegments = new List<List<Vector2Int>>();

        foreach (List<Vector2Int> regionPixels in extractedRegions)
        {
            List<Vector2Int> segmentPoints = new List<Vector2Int>();

            foreach (Vector2Int regionPixel in regionPixels)
            {
                foreach (Vector2Int neighbor in GetNeighbors(regionPixel, false))
                {
                    if (!regionPixels.Contains(neighbor))
                    {
                        segmentPoints.Add(neighbor);
                    }
                }
            }

            extractedSegments.Add(segmentPoints);
        }

        return extractedSegments;
    }

    /// <summary>
    /// Gets the neighboring points of a given point.
    /// </summary>
    /// <param name="point">The point to find neighbors for.</param>
    /// <param name="includeDiagonals">Whether to include diagonal neighbors.</param>
    /// <returns>A list of neighboring points.</returns>
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

    /// <summary>
    /// Chooses random start points from the segment marks for each region.
    /// </summary>
    /// <param name="regionsSegmentMarks">The segment marks of each region.</param>
    /// <returns>An array of chosen start points.</returns>
    private static Vector2Int[] ChooseStartpoints(List<List<Vector2Int>> regionsSegmentMarks)
    {
        int regionCount = regionsSegmentMarks.Count;
        Vector2Int[] chosenSegments = new Vector2Int[regionCount];
        var random = new System.Random(); // Thread-sicherer Zufallszahlengenerator

        Parallel.For(0, regionCount, i =>
        {
            List<Vector2Int> regionMarks = regionsSegmentMarks[i];
            int randomIndex = random.Next(0, regionMarks.Count); // System.Random wird hier verwendet
            chosenSegments[i] = regionMarks[randomIndex];
        });

        return chosenSegments;
    }


    /// <summary>
    /// Generates roads on the Voronoi texture based on the extracted regions and chosen segments.
    /// </summary>
    /// <param name="extractedRegions">The list of extracted regions.</param>
    /// <param name="chosenSegments">The chosen segments for each region.</param>
    /// <param name="voronoiTexture">The Voronoi texture.</param>
    /// <param name="roadData">The road data.</param>
    /// <returns>The updated Voronoi texture with roads.</returns>
    private static Texture2D GenerateRoads(List<List<Vector2Int>> extractedRegions, Vector2Int[] chosenSegments, Texture2D voronoiTexture, RoadData roadData)
    {
        int regionsCount = extractedRegions.Count;
        List<Vector2Int> allPixelsToDraw = new List<Vector2Int>();

        Parallel.For(0, regionsCount, i =>
        {
            List<Vector2Int> pixelsToDraw = LSystem.GenerateLSystem(roadData.axiom, roadData.angle, roadData.segmentLength, extractedRegions[i], chosenSegments[i]);
            lock (allPixelsToDraw)
            {
                allPixelsToDraw.AddRange(pixelsToDraw);
            }
        });

        ApplyChanges(voronoiTexture, allPixelsToDraw);
        voronoiTexture.Apply();
        return voronoiTexture;
    }

    /// <summary>
    /// Applies the changes to the texture by setting the pixels to draw.
    /// </summary>
    /// <param name="texture">The texture to apply changes to.</param>
    /// <param name="pixelsToDraw">The list of pixels to draw.</param>
    private static void ApplyChanges(Texture2D texture, List<Vector2Int> pixelsToDraw)
    {
        Color[] pixels = texture.GetPixels();
        int texWidth = texture.width;
        int texHeight = texture.height;
        object lockObject = new object();

        Parallel.ForEach(pixelsToDraw, pixel =>
        {
            int minX = Mathf.Max(0, pixel.x - width);
            int maxX = Mathf.Min(texWidth - 1, pixel.x + width);
            int minY = Mathf.Max(0, pixel.y - width);
            int maxY = Mathf.Min(texHeight - 1, pixel.y + width);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    int index = x + y * texWidth;
                    lock (lockObject)
                    {
                        pixels[index] = Color.black;
                    }
                }
            }
        });

        texture.SetPixels(pixels);
        texture.Apply();
    }
}
