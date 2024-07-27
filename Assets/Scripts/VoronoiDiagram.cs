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

    public Texture2D GenerateVoronoiDiagram(IDictionary<int, District> districts, int cellDistortion, Vector2 center, float radius)
    {
        int size = (int)transform.localScale.x * 10;

        // Setze die Parameter für das Stadtzentrum und den Radius
        cityCenter = center;

        cityRadius = radius;

        // Bestimme die Anzahl der Regionen
        int regionCount = districts.Count;
        districtPoints = new Vector2[regionCount];
        Color[] regionColors = new Color[regionCount];
        int[] ids = new int[regionCount];

        // Initialisiere die Bezirksdaten
        int index = 0;
        foreach (KeyValuePair<int, District> kvp in districts)
        {
            District district = kvp.Value;
            districtPoints[kvp.Key] = new Vector2(district.position.x, district.position.z);
            Color regionColor = district.type.color;
            // regionColor.a = 0.5f; // Setze die Transparenz
            regionColors[kvp.Key] = regionColor;
            ids[index] = kvp.Key;
            index++;
        }

        // Erzeuge das verzerrte Voronoi-Diagramm
        Color[] pixelColors = GenerateDistortedVoronoi(size, regionCount, regionColors, ids, cellDistortion);

        // Setze das Material auf transparent
        SetMaterialToTransparent();

        // Erzeuge die Textur und wende sie auf das Material an
        Texture2D voronoiTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };

        if (pixelColors.Length == size * size)
        {
            voronoiTexture.SetPixels(pixelColors);
            voronoiTexture.Apply();

            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial.mainTexture = voronoiTexture;
            }
            else
            {
                Debug.LogError("Renderer component not found.");
            }
        }
        else
        {
            Debug.LogError("Pixel color array size does not match the texture size.");
        }

        return voronoiTexture;
    }

    public Color[] GenerateDistortedVoronoi(int size, int regionCount, Color[] regionColors, int[] ids, int randomPointCount)
    {
        bool borders = false;
        if (randomPointCount <= 0)
        {
            borders = true;
        }

        // Berechne das anfängliche Voronoi-Diagramm basierend auf den Bezirkspositionen
        Color[] initialVoronoi = GenerateVoronoi(size, regionCount, districtPoints, regionColors, ids, borders);

        if (randomPointCount <= 0)
        {
            allPoints = null;
            return initialVoronoi;
        }

        // Initialisiere Arrays für alle Punkte, Farben und IDs
        int totalPoints = randomPointCount + regionCount;
        allPoints = new Vector2[totalPoints];
        allPointColors = new Color[totalPoints];
        int[] allIds = new int[totalPoints];

        // Kopiere ursprüngliche Punkte und Farben
        for (int i = 0; i < regionCount; i++)
        {
            allPoints[i] = districtPoints[ids[i]];
            allPointColors[i] = regionColors[ids[i]];
            allIds[i] = ids[i];
        }

        // Füge zufällige Punkte hinzu
        for (int i = 0; i < randomPointCount; i++)
        {
            Vector2 randomPosition = cityCenter + UnityEngine.Random.insideUnitCircle * cityRadius;
            int pixelIndex = Mathf.Clamp((int)randomPosition.x + (int)randomPosition.y * size, 0, initialVoronoi.Length - 1);
            Color closestColor = initialVoronoi[pixelIndex];

            allPoints[i + regionCount] = randomPosition;
            allPointColors[i + regionCount] = closestColor;

            // Finde die ID des nächsten ursprünglichen Bezirks
            int closestOriginalId = Array.FindIndex(regionColors, color => color == closestColor);
            allIds[i + regionCount] = closestOriginalId >= 0 ? ids[closestOriginalId] : -1;
        }

        // Berechne das neue Voronoi-Diagramm basierend auf den neuen und alten Punkten
        return GenerateVoronoi(size, totalPoints, allPoints, allPointColors, allIds, true);
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

            float centerDistance = Vector2.Distance(new Vector2(pixelPosition.x, pixelPosition.y), cityCenter);
            if (centerDistance < cityRadius)
            {

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
            } else
            {
                pixelColors[index] = Color.clear;
            }
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

            //float distance = Vector2.Distance(new Vector2(x,y), cityCenter);
            //if (distance < cityRadius)
            //{
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
            //}
            //else
            //{
            //    pixelColors[index] = Color.clear;
            //}
        });

        return pixelColors;
    }

    private void SetMaterialToTransparent()
    {
        Material material = GetComponent<MeshRenderer>().sharedMaterial;
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
