using Palmmedia.ReportGenerator.Core;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class VoronoiDiagram : MonoBehaviour
{
    public void GenerateVoronoiDiagram(List<District> districts, int size)
    {
        int regionAmount = districts.Count;

        Vector2[] points = new Vector2[regionAmount];
        Color[] regionColors = new Color[regionAmount];
        for (int i = 0; i < regionAmount; i++)
        {
            points[i] = new Vector2(districts[i].position.x, districts[i].position.z);
            regionColors[i] = districts[i].type.color;
        }

        Color[] pixelColors = GenerateVoronoi(size, regionAmount, points, regionColors);

        Texture2D myTexture = new Texture2D(size, size)
        {
            filterMode = FilterMode.Point
        };
        myTexture.SetPixels(pixelColors);
        myTexture.Apply();

        GetComponent<Renderer>().material.mainTexture = myTexture;
    }

    public static Color[] GenerateVoronoi(int size, int regionAmount, Vector2[] points, Color[] regionColors)
    {
        Color[] pixelColors = new Color[size * size];
        Vector2[] pixelPositions = new Vector2[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                pixelPositions[x + y * size] = new Vector2(x, y);
            }
        }

        Parallel.For(0, size * size, index =>
        {
            Vector2 pixelPosition = pixelPositions[index];
            float minDistance = float.MaxValue;
            int closestRegionIndex = 0;

            // Find closest region
            for (int i = 0; i < regionAmount; i++)
            {
                float distance = Vector2.Distance(pixelPosition, points[i]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestRegionIndex = i;
                }
            }

            // Assign color
            pixelColors[index] = regionColors[closestRegionIndex];
        });

        return pixelColors;
    }
}
