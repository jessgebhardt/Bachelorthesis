using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class VoronoiDiagram : MonoBehaviour
{
    private Dictionary<int, List<Vector2>> regionCorners = new Dictionary<int, List<Vector2>>();
    private List<Vector2> sortedVectors = new List<Vector2>();

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

    public Color[] GenerateVoronoi(int size, int regionAmount, Vector2[] points, Color[] regionColors)
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

        //
        regionCorners.Clear();
        for (int i = 0; i < regionAmount; i++)
        {
            regionCorners[i] = new List<Vector2>();
        }

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

        // Find Corners
        Parallel.For(0, size * size, index => 
        {
            int x = index % size;
            int y = index / size;

            int currentRegionIndex = closestRegionIndices[index];

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
                        if (!regionCorners[currentRegionIndex].Contains(pixelPositions[index]))
                        {
                            regionCorners[currentRegionIndex].Add(pixelPositions[index]);
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

        if (sortedVectors != null)
        {
            Gizmos.color = Color.red;
            //string allCorners = "";
            foreach (var corner in sortedVectors)
            {
                //allCorners = allCorners + corner.ToString() + "; ";
                Gizmos.DrawSphere(new Vector3(corner.x, 1, corner.y), 10f);
            }
            //Debug.Log(allCorners);
        }
    }
}
