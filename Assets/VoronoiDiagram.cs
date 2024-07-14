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

    public void GenerateVoronoiDiagram(IDictionary<int, District> districts, int size, int cellDistortion)
    {
        int regionAmount = districts.Keys.Max() + 1;

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

        Texture2D myTexture = new Texture2D(size, size)
        {
            filterMode = FilterMode.Point
        };
        myTexture.SetPixels(pixelColors);
        myTexture.Apply();

        GetComponent<Renderer>().material.mainTexture = myTexture;
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

        System.Random rand = new System.Random();

        for (int i = 0; i < randomPointCount; i++) 
        {
            Vector2 randomPosition = new Vector2(rand.Next(size), rand.Next(size));

            int pixelIndex = (int)randomPosition.x + (int)randomPosition.y * size;

            Color closestColor = initialVoronoi[pixelIndex];


            allPoints[i + regionAmount] = randomPosition;
            allPointColors[i + regionAmount] = closestColor;
            int closestOriginalId = Array.IndexOf(regionColors, closestColor);
            allIds[i + regionAmount] = ids[closestOriginalId];
        }

        int indexid = 0;
        foreach (int id in allIds)
        {
            Debug.Log(indexid+". ID:"+id);
            indexid++;
        }

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
            pixelColors = GenerateBorders(size, closestRegionIds, pixelColors);
            // pixelColors = GenerateCorners(size, regionAmount, closestRegionIds, pixelColors, pixelPositions);
        }

        return pixelColors;
    }

    public static Color[] GenerateBorders(int size, int[] closestRegionIds, Color[] pixelColors)
    {
        // Generierung der Bezirksgrenzen
        Parallel.For(0, size * size, index =>
        {
            int x = index % size;
            int y = index / size;

            int currentRegionIndex = closestRegionIds[index];

            // Überprüfung der Nachbarpixel
            bool isBorder = false;

            // Links
            if (x > 0 && closestRegionIds[index - 1] != currentRegionIndex)
            {
                isBorder = true;
            }

            // Rechts
            if (x < size - 1 && closestRegionIds[index + 1] != currentRegionIndex)
            {
                isBorder = true;
            }

            // Oben
            if (y > 0 && closestRegionIds[index - size] != currentRegionIndex)
            {
                isBorder = true;
            }

            // Unten
            if (y < size - 1 && closestRegionIds[index + size] != currentRegionIndex)
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

    public Color[] GenerateCorners(int size, int regionAmount, int[] closestRegionIds, Color[] pixelColors, Vector2[] pixelPositions)
    {
        //
        regionCorners.Clear();
        for (int i = 0; i < regionAmount; i++)
        {
            regionCorners[i] = new List<Vector2>();
        }

        // Find Corners
        Parallel.For(0, size * size, index =>
        {
            int x = index % size;
            int y = index / size;

            int currentRegionId = closestRegionIds[index];

            if (pixelColors[index] == Color.black)
            {
                int borderCount = 0;
                if (x > 0 && pixelColors[index - 1] == Color.black) borderCount++; // left neighbor
                if (x < size - 1 && pixelColors[index + 1] == Color.black) borderCount++; // right neighbor
                if (y > 0 && pixelColors[index - size] == Color.black) borderCount++; // upper neighbor
                if (y < size - 1 && pixelColors[index + size] == Color.black) borderCount++; // lower neighbor

                // Check diagonal neighbors
                if (x > 0 && y > 0 && pixelColors[index - 1 - size] == Color.black) borderCount++; // upper-left neighbor
                if (x < size - 1 && y > 0 && pixelColors[index + 1 - size] == Color.black) borderCount++; // upper-right neighbor
                if (x > 0 && y < size - 1 && pixelColors[index - 1 + size] == Color.black) borderCount++; // lower-left neighbor
                if (x < size - 1 && y < size - 1 && pixelColors[index + 1 + size] == Color.black) borderCount++; // lower-right neighbor

                if (borderCount >= 6)
                {
                    lock (regionCorners)
                    {
                        if (!regionCorners[currentRegionId].Contains(pixelPositions[index]))
                        {
                            regionCorners[currentRegionId].Add(pixelPositions[index]);
                        }
                    }
                }
            }
        });

        sortedVectors.Clear();
        sortedVectors = GetSortedVectorList(regionCorners, 5f);

        return pixelColors;
    }

    static List<Vector2> GetSortedVectorList(Dictionary<int, List<Vector2>> regionCorners, float similarityThreshold)
    {
        // Alle Werte in einem HashSet, um Duplikate zu vermeiden
        HashSet<Vector2> uniqueVectors = new HashSet<Vector2>();
        foreach (var entry in regionCorners)
        {
            foreach (var vector in entry.Value)
            {
                uniqueVectors.Add(vector);
            }
        }

        // Konvertiere das HashSet in eine Liste und sortiere die Werte
        List<Vector2> sortedVectors = uniqueVectors.ToList();
        sortedVectors.Sort((v1, v2) =>
        {
            int compareX = v1.x.CompareTo(v2.x);
            return compareX != 0 ? compareX : v1.y.CompareTo(v2.y);
        });

        // Liste ohne ähnliche Werte
        List<Vector2> result = new List<Vector2>();
        for (int i = 0; i < sortedVectors.Count; i++)
        {
            bool isSimilar = false;
            for (int j = 0; j < result.Count; j++)
            {
                if (Vector2.Distance(result[j], sortedVectors[i]) < similarityThreshold)
                {
                    isSimilar = true;
                    break;
                }
            }

            if (!isSimilar)
            {
                result.Add(sortedVectors[i]);
            }
        }

        return result;
    }

    private void OnDrawGizmos()
    {
        //if (regionCorners != null)
        //{
        //    Gizmos.color = Color.red;
        //    int index = 0;
        //    foreach (var corners in regionCorners.Values)
        //    {
        //        Debug.Log("INDEX: "+index);
        //        string allCornersOfOneDistrict = "";
        //        foreach (var corner in corners)
        //        {
        //            allCornersOfOneDistrict = allCornersOfOneDistrict + corner.ToString() + "; ";
        //            Gizmos.DrawSphere(new Vector3(corner.x, 1, corner.y), 10f);
        //        }
        //        Debug.Log(allCornersOfOneDistrict);
        //        index++;
        //    }
        //}

        //if (sortedVectors != null)
        //{
        //    Gizmos.color = Color.red;
        //    //string allCorners = "";
        //    foreach (var corner in sortedVectors)
        //    {
        //        //allCorners = allCorners + corner.ToString() + "; ";
        //        Gizmos.DrawSphere(new Vector3(corner.x, 1, corner.y), 10f);
        //    }
        //    //Debug.Log(allCorners);
        //}

        if (allPoints != null)
        {
            Gizmos.color = Color.white;

            for (int i = 0; i < allPoints.Length; i++)
            {
                Gizmos.color = allPointColors[i];
                Gizmos.DrawSphere(new Vector3(allPoints[i].x, 1, allPoints[i].y), 5f);
            }
        }
    }
}
