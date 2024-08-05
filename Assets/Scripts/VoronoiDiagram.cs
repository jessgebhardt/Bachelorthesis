using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class VoronoiDiagram : MonoBehaviour
{
    private Vector2Int[] districtPoints;
    private Dictionary<int, List<Vector2Int>> regionCorners = new Dictionary<int, List<Vector2Int>>();
    private List<Vector2Int> sortedVectors = new List<Vector2Int>();
    private Vector2Int[] allPoints;
    private Color[] allPointColors;
    private Vector2Int cityCenter = new Vector2Int(0, 0);
    private float cityRadius = 1;

    public class Region
    {
        public int Id;
        public List<Vector2Int> Pixels = new List<Vector2Int>();
    }

    public (Texture2D, Dictionary<int, Region>) GenerateVoronoiDiagram(IDictionary<int, District> districts, int cellDistortion, Vector2Int center, float radius, int borderWidth)
    {
        int size = (int)transform.localScale.x * 10;

        cityCenter = center;
        cityRadius = radius;

        int regionCount = districts.Count;
        districtPoints = new Vector2Int[regionCount];
        Color[] regionColors = new Color[regionCount];
        int[] ids = new int[regionCount];

        int index = 0;
        foreach (KeyValuePair<int, District> kvp in districts)
        {
            District district = kvp.Value;
            districtPoints[kvp.Key] = new Vector2Int((int)district.position.x, (int)district.position.z);
            Color regionColor = district.type.color;
            regionColor.a = 0.2f;
            regionColors[kvp.Key] = regionColor;
            ids[index] = kvp.Key;
            index++;
        }

        (Color[] pixelColors, Dictionary<int, Region> regions) = GenerateDistortedVoronoi(size, regionCount, regionColors, ids, cellDistortion, borderWidth);

        SetMaterialToTransparent();

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

        return (voronoiTexture, regions);
    }

    private (Color[], Dictionary<int, Region>) GenerateDistortedVoronoi(int size, int regionCount, Color[] regionColors, int[] ids, int randomPointCount, int borderWidth)
    {
        bool borders = randomPointCount <= 0;

        (Color[] initialVoronoi, Dictionary<int, Region> initialRegions) = GenerateVoronoi(size, regionCount, districtPoints, regionColors, ids, borders, borderWidth);

        if (borders)
        {
            allPoints = null;
            return (initialVoronoi, initialRegions);
        }

        int totalPoints = randomPointCount + regionCount;
        allPoints = new Vector2Int[totalPoints];
        allPointColors = new Color[totalPoints];
        int[] allIds = new int[totalPoints];

        for (int i = 0; i < regionCount; i++)
        {
            allPoints[i] = districtPoints[ids[i]];
            allPointColors[i] = regionColors[ids[i]];
            allIds[i] = ids[i];
        }

        for (int i = 0; i < randomPointCount; i++)
        {
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * cityRadius;
            Vector2Int randomPosition = cityCenter + Vector2Int.RoundToInt(randomOffset);

            int pixelIndex = Mathf.Clamp((int)randomPosition.x + (int)randomPosition.y * size, 0, initialVoronoi.Length - 1);
            Color closestColor = initialVoronoi[pixelIndex];

            allPoints[i + regionCount] = randomPosition;
            allPointColors[i + regionCount] = closestColor;

            int closestOriginalId = Array.FindIndex(regionColors, color => color == closestColor);
            allIds[i + regionCount] = closestOriginalId >= 0 ? ids[closestOriginalId] : -1;
        }

        (Color[] finalVoronoi, Dictionary<int, Region> finalRegions) = GenerateVoronoi(size, totalPoints, allPoints, allPointColors, allIds, true, borderWidth);
        return (finalVoronoi, finalRegions);
    }

    private (Color[], Dictionary<int, Region>) GenerateVoronoi(int size, int regionAmount, Vector2Int[] points, Color[] regionColors, int[] ids, bool borders, int borderWidth)
    {
        Dictionary<int, Region> regions = new Dictionary<int, Region>();
        Color[] pixelColors = new Color[size * size];
        Vector2Int[] pixelPositions = new Vector2Int[size * size];
        int[] closestRegionIds = new int[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                pixelPositions[x + y * size] = new Vector2Int(x, y);
            }
        }

        Parallel.For(0, size * size, index =>
        {
            Vector2Int pixelPosition = pixelPositions[index];
            float centerDistance = Vector2.Distance(new Vector2(pixelPosition.x, pixelPosition.y), cityCenter);

            if (centerDistance < cityRadius)
            {
                float minDistance = float.MaxValue;
                Vector2 closestPoint = new Vector2();
                int closestRegionId = 0;

                for (int i = 0; i < points.Length; i++)
                {
                    float distance = Vector2.Distance(pixelPosition, points[i]);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestPoint = points[i];
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

                closestRegionIds[index] = ids[closestRegionId];
                pixelColors[index] = regionColors[closestRegionId];

                lock (regions)
                {
                    if (!regions.ContainsKey(ids[closestRegionId]))
                    {
                        regions[ids[closestRegionId]] = new Region { Id = ids[closestRegionId] };
                    }
                    regions[ids[closestRegionId]].Pixels.Add(pixelPosition);
                }
            }
            else
            {
                pixelColors[index] = Color.clear;
            }
        });

        if (borders)
        {
            pixelColors = GenerateBorders(size, closestRegionIds, pixelColors, cityCenter, cityRadius, regions, borderWidth);
        }

        return (pixelColors, regions);
    }

    private static Color[] GenerateBorders(int size, int[] closestRegionIds, Color[] pixelColors, Vector2Int cityCenter, float cityRadius, Dictionary<int, Region> regions, int width)
    {
        // Berechne die Anzahl der Offsets
        int totalOffsets = (2 * width + 1) * (2 * width + 1) - 1;
        int[] xOffsets = new int[totalOffsets];
        int[] yOffsets = new int[totalOffsets];
        int index = 0;

        // Offsets berechnen
        for (int dx = -width; dx <= width; dx++)
        {
            for (int dy = -width; dy <= width; dy++)
            {
                if (dx != 0 || dy != 0)
                {
                    xOffsets[index] = dx;
                    yOffsets[index] = dy;
                    index++;
                }
            }
        }

        Parallel.ForEach(Partitioner.Create(0, size * size), range =>
        {
            for (int i = range.Item1; i < range.Item2; i++)
            {
                int x = i % size;
                int y = i / size;
                int currentRegionIndex = closestRegionIds[i];
                bool isBorder = false;

                for (int j = 0; j < xOffsets.Length; j++)
                {
                    int neighborX = x + xOffsets[j];
                    int neighborY = y + yOffsets[j];
                    int neighborIndex = neighborY * size + neighborX;

                    if (neighborX >= 0 && neighborX < size && neighborY >= 0 && neighborY < size &&
                        neighborIndex >= 0 && neighborIndex < pixelColors.Length &&
                        closestRegionIds[neighborIndex] != currentRegionIndex)
                    {
                        isBorder = true;
                        break;
                    }
                }

                if (isBorder)
                {
                    pixelColors[i] = Color.black;
                    lock (regions)
                    {
                        regions[closestRegionIds[i]].Pixels.Remove(new Vector2Int(x, y));
                    }
                }
            }
        });

        return pixelColors;
    }



    private void SetMaterialToTransparent()
    {
        Material material = GetComponent<MeshRenderer>().sharedMaterial;
        if (material != null)
        {
            material.SetFloat("_Mode", 3);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}
