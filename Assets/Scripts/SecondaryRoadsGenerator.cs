using System.Collections.Generic;
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
        float segmentLength = 50f;

        for (int i = 0; i < extractedRegions.Count; i++)
        {
            voronoiTexture = LSystem.GenerateLSystem(axiom, angle, segmentLength, voronoiTexture, extractedRegions[i], chosenSegments[i]);
            Debug.Log("DONE");
        }

        return voronoiTexture;
    }
}
