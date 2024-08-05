using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SecondaryRoadsGenerator : MonoBehaviour
{
    private static int width;
    public static Texture2D GenerateSecondaryRoads(List<List<Vector2Int>> extractedRegions, List<List<Vector2Int>> regionsSegmentMarks, Texture2D voronoiTexture, int roadWidth)
    {
        width = roadWidth;
        Vector2Int[] chosenSegments = ChooseStartpoints(regionsSegmentMarks);

        voronoiTexture = GenerateRoads(extractedRegions, chosenSegments, voronoiTexture);

        return voronoiTexture;
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
        foreach (var pixel in pixelsToDraw)
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
