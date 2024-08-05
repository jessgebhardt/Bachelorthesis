using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class VoronoiDiagram : MonoBehaviour
{
    //private Vector2Int[] districtPoints;
    //private Dictionary<int, List<Vector2Int>> regionCorners = new Dictionary<int, List<Vector2Int>>();
    //private List<Vector2Int> sortedVectors = new List<Vector2Int>();
    //private Vector2Int[] allPoints;
    //private Color[] allPointColors;
    //private Vector2Int cityCenter = new Vector2Int(0, 0);
    //private float cityRadius = 1;

    public class Region
    {
        public int Id;
        public List<Vector2Int> Pixels = new List<Vector2Int>();
    }

    public static (Texture2D, Dictionary<int, Region>) GenerateVoronoiDiagram(DistrictsData districtsData, VoronoiData voronoiData, CityBoundariesData boundariesData, Renderer renderer)
    {
        int size = (int)voronoiData.voronoiDiagram.transform.localScale.x * 10;

        int regionCount = districtsData.districtsDictionary.Count;
        voronoiData.districtPoints = new Vector2Int[regionCount];
        Color[] regionColors = new Color[regionCount];
        int[] ids = new int[regionCount];

        int index = 0;
        foreach (KeyValuePair<int, District> kvp in districtsData.districtsDictionary)
        {
            District district = kvp.Value;
            voronoiData.districtPoints[kvp.Key] = new Vector2Int((int)district.position.x, (int)district.position.z);
            Color regionColor = district.type.color;
            regionColor.a = 0.2f;
            regionColors[kvp.Key] = regionColor;
            ids[index] = kvp.Key;
            index++;
        }

        (Color[] pixelColors, Dictionary<int, Region> regions) = GenerateDistortedVoronoi(size, regionCount, regionColors, ids, voronoiData, boundariesData);

        SetMaterialToTransparent(renderer.sharedMaterial);

        Texture2D voronoiTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };

        if (pixelColors.Length == size * size)
        {
            voronoiTexture.SetPixels(pixelColors);
            voronoiTexture.Apply();

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

    public static (Color[], Dictionary<int, Region>) GenerateDistortedVoronoi(int size, int regionCount, Color[] regionColors, int[] ids, VoronoiData voronoiData, CityBoundariesData boundariesData)
    {
        bool borders = voronoiData.distictCellDistortion <= 0;

        (Color[] initialVoronoi, Dictionary<int, Region> initialRegions) = GenerateVoronoi(size, regionCount, voronoiData.districtPoints, regionColors, ids, borders, boundariesData, voronoiData);

        if (borders)
        {
            voronoiData.allPoints = null;
            return (initialVoronoi, initialRegions);
        }

        int totalPoints = voronoiData.distictCellDistortion + regionCount;
        voronoiData.allPoints = new Vector2Int[totalPoints];
        voronoiData.allPointColors = new Color[totalPoints];
        int[] allIds = new int[totalPoints];

        for (int i = 0; i < regionCount; i++)
        {
            voronoiData.allPoints[i] = voronoiData.districtPoints[ids[i]];
            voronoiData.allPointColors[i] = regionColors[ids[i]];
            allIds[i] = ids[i];
        }

        for (int i = 0; i < voronoiData.distictCellDistortion; i++)
        {
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * boundariesData.outerBoundaryRadius;
            Vector2Int randomPosition = new Vector2Int((int)boundariesData.center.x, (int)boundariesData.center.z) + Vector2Int.RoundToInt(randomOffset);

            int pixelIndex = Mathf.Clamp((int)randomPosition.x + (int)randomPosition.y * size, 0, initialVoronoi.Length - 1);
            Color closestColor = initialVoronoi[pixelIndex];

            voronoiData.allPoints[i + regionCount] = randomPosition;
            voronoiData.allPointColors[i + regionCount] = closestColor;

            int closestOriginalId = Array.FindIndex(regionColors, color => color == closestColor);
            allIds[i + regionCount] = closestOriginalId >= 0 ? ids[closestOriginalId] : -1;
        }

        (Color[] finalVoronoi, Dictionary<int, Region> finalRegions) = GenerateVoronoi(size, totalPoints, voronoiData.allPoints, voronoiData.allPointColors, allIds, true, boundariesData, voronoiData);
        return (finalVoronoi, finalRegions);
    }

    private static (Color[], Dictionary<int, Region>) GenerateVoronoi(int size, int regionAmount, Vector2Int[] points, Color[] regionColors, int[] ids, bool borders, CityBoundariesData boundariesData, VoronoiData voronoiData)
    {
        Vector2Int cityCenter = new Vector2Int((int)boundariesData.center.x, (int)boundariesData.center.z);
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
            float centerDistance = Vector2Int.Distance(new Vector2Int(pixelPosition.x, pixelPosition.y), new Vector2Int((int)boundariesData.center.x, (int)boundariesData.center.z));

            if (centerDistance < boundariesData.outerBoundaryRadius)
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
                for (int i = 0; i < voronoiData.districtPoints.Length; i++)
                {
                    float distance = Vector2.Distance(closestPoint, voronoiData.districtPoints[i]);
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
            pixelColors = GenerateBorders(size, closestRegionIds, pixelColors, cityCenter, boundariesData.outerBoundaryRadius, regions);
        }

        return (pixelColors, regions);
    }

    public static Color[] GenerateBorders(int size, int[] closestRegionIds, Color[] pixelColors, Vector2Int cityCenter, float cityRadius, Dictionary<int, Region> regions)
    {
        Parallel.For(0, size * size, index =>
        {
            int x = index % size;
            int y = index / size;
            int currentRegionIndex = closestRegionIds[index];
            bool isBorder = false;

            if (index - 1 >= 0 && index - 1 < pixelColors.Length && pixelColors[index - 1] != Color.black && x > 0 && closestRegionIds[index - 1] != currentRegionIndex)
            {
                isBorder = true;
            }
            if (index + 1 >= 0 && index + 1 < pixelColors.Length && pixelColors[index + 1] != null && pixelColors[index + 1] != Color.black && x < size - 1 && closestRegionIds[index + 1] != currentRegionIndex)
            {
                isBorder = true;
            }
            if (index - size >= 0 && index - size < pixelColors.Length && pixelColors[index - size] != Color.black && y > 0 && closestRegionIds[index - size] != currentRegionIndex)
            {
                isBorder = true;
            }
            if (index + size >= 0 && index + size < pixelColors.Length && pixelColors[index + size] != Color.black && y < size - 1 && closestRegionIds[index + size] != currentRegionIndex)
            {
                isBorder = true;
            }

            if (isBorder)
            {
                pixelColors[index] = Color.black;
                lock (regions)
                {
                    regions[closestRegionIds[index]].Pixels.Remove(new Vector2Int(x, y));

                }
            }
        });

        return pixelColors;
    }

    private static void SetMaterialToTransparent(Material sharedMaterial)
    {
        if (sharedMaterial != null)
        {
            sharedMaterial.SetFloat("_Mode", 3);
            sharedMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            sharedMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            sharedMaterial.SetInt("_ZWrite", 0);
            sharedMaterial.DisableKeyword("_ALPHATEST_ON");
            sharedMaterial.EnableKeyword("_ALPHABLEND_ON");
            sharedMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            sharedMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}
