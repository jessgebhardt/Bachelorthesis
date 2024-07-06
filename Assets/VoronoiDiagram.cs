using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class VoronoiDiagram : MonoBehaviour
{
    public void GenerateVoronoiDiagram(List<District> districts, int size, LineRenderer lineRenderer)
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

        int[] closestRegionIndices = new int[size * size];

        Parallel.For(0, size * size, index =>
        {
            // Position des aktuellen Pixels
            Vector2 pixelPosition = pixelPositions[index];

            // Kleinste gefundene Distanz zum nächsten Bezirkpunkt
            float minDistance = float.MaxValue;

            // Index des nächsten Bezirkpunkts
            int closestRegionIndex = 0;

            // Berechnung der Distanz zu jedem Punkt & Zuweisung
            for (int i = 0; i < regionAmount; i++)
            {
                float distance = Vector2.Distance(pixelPosition, points[i]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestRegionIndex = i;
                }
            }

            // Zuweisung des nächsten Bezirks für Bezirksgrenzen
            closestRegionIndices[index] = closestRegionIndex;

            // Zuweisung der Farbe des Bezirks
            pixelColors[index] = regionColors[closestRegionIndex];
        });

        // Generierung der Bezirksgrenzen
        Parallel.For(0, size * size, index =>
        {
            int x = index % size;
            int y = index / size;

            int currentRegionIndex = closestRegionIndices[index];

            // Überprüfung der Nachbarpixel
            bool isBorder = false;

            // Links
            if (x > 0 && closestRegionIndices[index - 1] != currentRegionIndex)
            {
                isBorder = true;
            }

            // Rechts
            if (x < size - 1 && closestRegionIndices[index + 1] != currentRegionIndex)
            {
                isBorder = true;
            }

            // Oben
            if (y > 0 && closestRegionIndices[index - size] != currentRegionIndex)
            {
                isBorder = true;
            }

            // Unten
            if (y < size - 1 && closestRegionIndices[index + size] != currentRegionIndex)
            {
                isBorder = true;
            }

            if (isBorder)
            {
                pixelColors[index] = Color.black;
            }
        });

        return pixelColors;
    }

}
