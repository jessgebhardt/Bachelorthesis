using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class VoronoiDiagram : MonoBehaviour
{
    private Vector2[] districtPoints;
    private Dictionary<int, List<Vector2>> regionCorners = new Dictionary<int, List<Vector2>>();
    private List<Vector2> sortedVectors = new List<Vector2>();
    private Vector2[] allPoints;
    private Color[] allPointColors;
    private Vector2 cityCenter = new Vector2(0, 0);
    private float cityRadius = 1;

    public Texture2D GenerateVoronoiDiagram(IDictionary<int, District> districts, int size, int cellDistortion, Vector2 center, float radius)
    {
        int regionAmount = districts.Keys.Max() + 1;

        cityCenter = center;
        cityRadius = radius;

        int[] ids = new int[regionAmount];
        districtPoints = new Vector2[regionAmount];
        Color[] regionColors = new Color[regionAmount];

        int index = 0;
        foreach (KeyValuePair<int, District> kvp in districts)
        {
            District district = kvp.Value;
            districtPoints[kvp.Key] = new Vector2(district.position.x, district.position.z);
            regionColors[kvp.Key] = district.type.color;
            ids[index] = kvp.Key;
            index++;
        }

        Color[] pixelColors = GenerateDistortedVoronoi(size, regionAmount, regionColors, ids, cellDistortion);

        SetMaterialToTransparent();

        Texture2D voronoiTexture = new Texture2D(size, size)
        {
            filterMode = FilterMode.Point
        };
        voronoiTexture.SetPixels(pixelColors);
        voronoiTexture.Apply();

        GetComponent<Renderer>().material.mainTexture = voronoiTexture;
        return voronoiTexture;
    }

    public Color[] GenerateDistortedVoronoi(int size, int regionAmount, Color[] regionColors, int[] ids, int randomPointCount)
    {
        // Initiales Voronoi-Diagramm berechnen (nur Bezirkspositionen)
        Color[] initialVoronoi = GenerateVoronoi(size, regionAmount, districtPoints, regionColors, ids, false);

        if (randomPointCount <= 0) 
        {
            allPoints = null;
            return initialVoronoi;
        }
        // Zufälliges Verformen von den Bezirken/Voronoizellen

        allPoints = new Vector2[randomPointCount+regionAmount];
        allPointColors = new Color[randomPointCount+regionAmount];
        int[] allIds = new int[randomPointCount + regionAmount];

        // Ursprüngliche Punkte und Farben hinzufügen
        foreach (int id in ids)
        {
            allPoints[id] = districtPoints[id];
            allPointColors[id] = regionColors[id];
            allIds[id] = id;
        }

        for (int i = 0; i < randomPointCount; i++) 
        {
            Vector2 randomPosition = cityCenter + UnityEngine.Random.insideUnitCircle * cityRadius;

            int pixelIndex = (int)randomPosition.x + (int)randomPosition.y * size;

            Color closestColor = initialVoronoi[pixelIndex];


            allPoints[i + regionAmount] = randomPosition;
            allPointColors[i + regionAmount] = closestColor;
            int closestOriginalId = Array.IndexOf(regionColors, closestColor);
            allIds[i + regionAmount] = ids[closestOriginalId];
        }

        //int indexid = 0;
        //foreach (int id in allIds)
        //{
        //    Debug.Log(indexid+". ID:"+id);
        //    indexid++;
        //}

        // Neues Voronoi-Diagramm basierend auf neuen und alten Punkten berechnen
        Color[] distortedVoronoi = GenerateVoronoi(size, randomPointCount + regionAmount, allPoints, allPointColors, allIds, true);

        return distortedVoronoi;
    }

    public Color[] GenerateVoronoi(int size, int regionAmount, Vector2[] points, Color[] regionColors, int[] ids, bool borders)
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

        int[] closestRegionIds = new int[size * size]; // ids der nächsten bezirke

        Parallel.For(0, size * size, index =>
        {
            // Position des aktuellen Pixels
            Vector2 pixelPosition = pixelPositions[index];

            // Kleinste gefundene Distanz zum nächsten Bezirkpunkt
            float minDistance = float.MaxValue;


            Vector2 closestPoint = new Vector2();

            // Index des nächsten Bezirkpunkts
            int closestRegionId = 0;

            // Berechnung der Distanz zu jedem Punkt & Zuweisung
            for (int i = 0; i < points.Length; i++)
            {
                float distance = Vector2.Distance(pixelPosition, points[i]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPoint = points[i];
                    //closestRegionId = i;
                }
            }

            float minOriginalPointDistance = float.MaxValue;
            for (int i = 0; i < districtPoints.Length; i++) 
            {
                float distance = Vector2.Distance(closestPoint, districtPoints[i]);
                if (distance < minOriginalPointDistance)
                {
                    minOriginalPointDistance = distance;
                    closestRegionId = i;
                }
            }

            // Zuweisung des nächsten Bezirks für Bezirksgrenzen
            closestRegionIds[index] = ids[closestRegionId];

            // Zuweisung der Farbe des Bezirks
            pixelColors[index] = regionColors[closestRegionId];
        });

        sortedVectors.Clear();
        if (borders)
        {
            pixelColors = GenerateBorders(size, closestRegionIds, pixelColors, cityCenter, cityRadius);
            // pixelColors = GenerateCorners(size, regionAmount, closestRegionIds, pixelColors, pixelPositions);
        }

        return pixelColors;
    }

    public static Color[] GenerateBorders(int size, int[] closestRegionIds, Color[] pixelColors, Vector2 cityCenter, float cityRadius)
    {
        // Generierung der Bezirksgrenzen
        Parallel.For(0, size * size, index =>
        {
            int x = index % size;
            int y = index / size;

            float distance = Vector2.Distance(new Vector2(x,y), cityCenter);
            if (distance < cityRadius)
            {
                int currentRegionIndex = closestRegionIds[index];

                // Überprüfung der Nachbarpixel
                bool isBorder = false;

                // Links
                if (index - 1 >= 0 && index - 1 < pixelColors.Length && pixelColors[index - 1] != Color.black && x > 0 && closestRegionIds[index - 1] != currentRegionIndex)
                {
                    isBorder = true;
                }

                // Rechts
                if (index + 1 >= 0 && index + 1 < pixelColors.Length && pixelColors[index + 1] != null && pixelColors[index + 1] != Color.black && x < size - 1 && closestRegionIds[index + 1] != currentRegionIndex)
                {
                    isBorder = true;
                }

                // Oben
                if (index - size >= 0 && index - size < pixelColors.Length && pixelColors[index - size] != Color.black && y > 0 && closestRegionIds[index - size] != currentRegionIndex)
                {
                    isBorder = true;
                }

                // Unten
                if (index + size >= 0 && index + size < pixelColors.Length && pixelColors[index + size] != Color.black && y < size - 1 && closestRegionIds[index + size] != currentRegionIndex)
                {
                    isBorder = true;
                }

                if (isBorder)
                {
                    pixelColors[index] = Color.black;
                }
            }
            else
            {
                pixelColors[index] = Color.clear;
            }
        });

        return pixelColors;
    }

    private void SetMaterialToTransparent()
    {
        Material material = GetComponent<MeshRenderer>().material;
        if (material != null)
        {
            material.SetFloat("_Mode", 3); // Setze den Modus auf Transparent
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }


    private void OnDrawGizmos()
    {
        //if (allPoints != null)
        //{
        //    Gizmos.color = Color.white;

        //    for (int i = 0; i < allPoints.Length; i++)
        //    {
        //        Gizmos.color = allPointColors[i];
        //        Gizmos.DrawSphere(new Vector3(allPoints[i].x, 1, allPoints[i].y), 5f);
        //    }
        //}
    }
}
