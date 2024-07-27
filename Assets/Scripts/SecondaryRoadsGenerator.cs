using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SecondaryRoadsGenerator : MonoBehaviour
{
    public static Texture2D GenerateSecondaryRoads(List<List<Vector2Int>> extractedRegions, List<List<Vector2Int>> regionsSegmentMarks, Texture2D voronoiTexture)
    {
        Debug.Log(extractedRegions.Count);
        Debug.Log(regionsSegmentMarks.Count);

        Vector2Int[] chosenSegments = ChooseStartpoints(regionsSegmentMarks);

        voronoiTexture = GenerateRoads(extractedRegions, chosenSegments, voronoiTexture);

        Debug.Log(chosenSegments.Length);

        return voronoiTexture;
    }

    private static Vector2Int[] ChooseStartpoints(List<List<Vector2Int>> regionsSegmentMarks)
    {
        Vector2Int[] chosenSegments = new Vector2Int[regionsSegmentMarks.Count];
        Debug.Log("all: "+regionsSegmentMarks.Count);

        for (int i = 0; i < regionsSegmentMarks.Count; i++)
        {
            Debug.Log(i+": "+regionsSegmentMarks[i].Count);
            int randomNumber = Random.Range(0, regionsSegmentMarks[i].Count);
            chosenSegments[i] = regionsSegmentMarks[i][randomNumber];
        }

        return chosenSegments;
    }

    private static Texture2D GenerateRoads(List<List<Vector2Int>> extractedRegions, Vector2Int[] chosenSegments, Texture2D voronoiTexture)
    {
        string axiom = "A";
        float angle = 90f;
        float segmentLength = 50f;

        int regionsCount = extractedRegions.Count;
        List<Vector2Int> allPixelsToDraw = new List<Vector2Int>();

        Parallel.For(0, regionsCount, i =>
        {
            var pixelsToDraw = LSystem.GenerateLSystem(axiom, angle, segmentLength, extractedRegions[i], chosenSegments[i]);
            lock (allPixelsToDraw)
            {
                allPixelsToDraw.AddRange(pixelsToDraw);
            }
            Debug.Log("DONE");
        });

        ApplyChanges(voronoiTexture, allPixelsToDraw);
        voronoiTexture.Apply();
        return voronoiTexture;
    }

    private static void ApplyChanges(Texture2D texture, List<Vector2Int> pixelsToDraw)
    {
        foreach (var pixel in pixelsToDraw)
        {
            texture.SetPixel(pixel.x, pixel.y, Color.black);
        }
    }
}
