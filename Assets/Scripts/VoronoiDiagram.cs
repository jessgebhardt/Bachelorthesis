using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class VoronoiDiagram : MonoBehaviour
{
    private static Vector2Int[] districtPoints;
    private static int size;
    private static Vector2Int cityCenter;
    private static float cityRadius;
    private static Vector2Int[] allPoints;
    private static Color[] allPointColors;

    /// <summary>
    /// Generates a Voronoi diagram texture based on district data, Voronoi settings, boundaries, and road data.
    /// <para>
    /// Initializes necessary parameters, calculates Voronoi regions, and creates a texture.
    /// </para>
    /// </summary>
    /// <param name="districtData">Data related to districts and their properties.</param>
    /// <param name="voronoiData">Settings for generating the Voronoi diagram.</param>
    /// <param name="boundariesData">City boundaries and center information.</param>
    /// <param name="roadData">Data related to road width for distortion purposes.</param>
    /// <returns>The generated Voronoi diagram as a Texture2D.</returns>
    public static Texture2D GenerateVoronoiDiagram(DistrictData districtData, VoronoiData voronoiData, BoundariesData boundariesData, RoadData roadData)
    {
        Initialize(districtData, voronoiData, boundariesData);

        Color[] regionColors = GetRegionColors(districtData, out int[] ids);

        var (pixelColors, regions) = GenerateDistortedVoronoi(size, districtData.districtsDictionary.Count, regionColors, ids, voronoiData.distictCellDistortion, roadData.roadWidth);
        voronoiData.regions = regions;

        return CreateTexture(pixelColors);
    }

    /// <summary>
    /// Initializes parameters for the Voronoi diagram generation.
    /// <para>
    /// Sets the size of the diagram, city center, city radius, and district points.
    /// </para>
    /// </summary>
    /// <param name="districtData">Data related to districts.</param>
    /// <param name="voronoiData">Voronoi diagram settings.</param>
    /// <param name="boundariesData">City boundaries and center data.</param>
    private static void Initialize(DistrictData districtData, VoronoiData voronoiData, BoundariesData boundariesData)
    {
        size = (int)voronoiData.voronoiDiagram.transform.localScale.x * 10;
        cityCenter = new Vector2Int((int)boundariesData.center.x, (int)boundariesData.center.z);
        cityRadius = boundariesData.outerBoundaryRadius;

        districtPoints = new Vector2Int[districtData.districtsDictionary.Count];
    }

    /// <summary>
    /// Retrieves the colors and IDs for each region based on the district data.
    /// <para>
    /// Sets up an array of colors and IDs representing each district.
    /// </para>
    /// </summary>
    /// <param name="districtData">Data containing district information.</param>
    /// <param name="ids">Output array of district IDs.</param>
    /// <returns>An array of colors corresponding to each district.</returns>
    private static Color[] GetRegionColors(DistrictData districtData, out int[] ids)
    {
        int regionCount = districtData.districtsDictionary.Count;
        Color[] regionColors = new Color[regionCount];
        ids = new int[regionCount];

        foreach (KeyValuePair<int, District> kvp in districtData.districtsDictionary)
        {
            District district = kvp.Value;
            districtPoints[kvp.Key] = new Vector2Int((int)district.position.x, (int)district.position.z);
            regionColors[kvp.Key] = new Color(district.type.color.r, district.type.color.g, district.type.color.b, 0.2f);
            ids[kvp.Key] = kvp.Key;
        }

        return regionColors;
    }

    /// <summary>
    /// Creates a texture from the array of pixel colors.
    /// <para>
    /// Sets the pixel colors of the texture and applies it.
    /// </para>
    /// </summary>
    /// <param name="pixelColors">Array of colors for each pixel in the texture.</param>
    /// <returns>The generated Texture2D.</returns>
    private static Texture2D CreateTexture(Color[] pixelColors)
    {
        Texture2D voronoiTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };

        voronoiTexture.SetPixels(pixelColors);
        voronoiTexture.Apply();
        return voronoiTexture;
    }

    /// <summary>
    /// Generates the Voronoi diagram with optional distortion and border generation.
    /// <para>
    /// First creates a Voronoi diagram without distortion, then adds distortion if needed,
    /// and generates borders based on the road width parameter.
    /// </para>
    /// </summary>
    /// <param name="size">Size of the Voronoi diagram.</param>
    /// <param name="regionCount">Number of regions (districts).</param>
    /// <param name="regionColors">Colors for each region.</param>
    /// <param name="ids">IDs of the regions.</param>
    /// <param name="randomPointCount">Number of random points for distortion.</param>
    /// <param name="borderWidth">Width of the border around regions.</param>
    /// <returns>A tuple containing the pixel colors and the regions dictionary.</returns>
    private static (Color[], Dictionary<int, Region>) GenerateDistortedVoronoi(int size, int regionCount, Color[] regionColors, int[] ids, int randomPointCount, int borderWidth)
    {
        bool borders = randomPointCount <= 0;

        var (initialVoronoi, initialRegions) = GenerateVoronoi(size, regionCount, districtPoints, regionColors, ids, borders, borderWidth);

        if (borders)
        {
            allPoints = null;
            return (initialVoronoi, initialRegions);
        }

        InitializeAllPoints(regionCount, randomPointCount, regionColors, ids, initialVoronoi);

        var (finalVoronoi, finalRegions) = GenerateVoronoi(size, allPoints.Length, allPoints, allPointColors, ids, true, borderWidth);
        return (finalVoronoi, finalRegions);
    }

    /// <summary>
    /// Initializes the list of all points, including random points for Voronoi distortion.
    /// <para>
    /// Adds random points within the city and assigns them colors based on the closest original district points.
    /// </para>
    /// </summary>
    /// <param name="regionCount">Number of original regions (districts).</param>
    /// <param name="randomPointCount">Number of random points to add for distortion.</param>
    /// <param name="regionColors">Colors of each region.</param>
    /// <param name="ids">IDs of each region.</param>
    /// <param name="initialVoronoi">Colors of pixels in the initial Voronoi diagram.</param>
    private static void InitializeAllPoints(int regionCount, int randomPointCount, Color[] regionColors, int[] ids, Color[] initialVoronoi)
    {
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
            int pixelIndex = Mathf.Clamp(randomPosition.x + randomPosition.y * size, 0, initialVoronoi.Length - 1);
            Color closestColor = initialVoronoi[pixelIndex];

            allPoints[i + regionCount] = randomPosition;
            allPointColors[i + regionCount] = closestColor;

            int closestOriginalId = Array.FindIndex(regionColors, color => color == closestColor);
            allIds[i + regionCount] = closestOriginalId >= 0 ? ids[closestOriginalId] : -1;
        }
    }

    /// <summary>
    /// Generates a Voronoi diagram based on the given points, colors, and other parameters.
    /// <para>
    /// Computes the closest region for each pixel and assigns colors accordingly.
    /// </para>
    /// </summary>
    /// <param name="size">Size of the Voronoi diagram.</param>
    /// <param name="regionAmount">Number of regions (districts).</param>
    /// <param name="points">Points representing the centers of regions.</param>
    /// <param name="regionColors">Colors for each region.</param>
    /// <param name="ids">IDs of the regions.</param>
    /// <param name="borders">Flag indicating whether to generate borders.</param>
    /// <param name="borderWidth">Width of the borders around regions.</param>
    /// <returns>A tuple containing pixel colors and the regions dictionary.</returns>
    private static (Color[], Dictionary<int, Region>) GenerateVoronoi(int size, int regionAmount, Vector2Int[] points, Color[] regionColors, int[] ids, bool borders, int borderWidth)
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
            if (Vector2.Distance(pixelPosition, cityCenter) < cityRadius)
            {
                (int closestRegionId, Color color) = FindClosestRegion(points, regionColors, ids, pixelPosition);
                closestRegionIds[index] = closestRegionId;
                pixelColors[index] = color;

                lock (regions)
                {
                    if (!regions.ContainsKey(closestRegionId))
                    {
                        regions[closestRegionId] = new Region { Id = closestRegionId };
                    }
                    regions[closestRegionId].Pixels.Add(pixelPosition);
                }
            }
            else
            {
                closestRegionIds[index] = -1;
                pixelColors[index] = Color.clear;
            }
        });

        if (borders)
        {
            pixelColors = GeneratePrimaryRoads(size, closestRegionIds, pixelColors, regions, borderWidth);
        }

        return (pixelColors, regions);
    }

    /// <summary>
    /// Finds the closest region for a given pixel position.
    /// <para>
    /// Determines the closest region and its associated color by calculating distances.
    /// </para>
    /// </summary>
    /// <param name="points">Points representing the centers of regions.</param>
    /// <param name="regionColors">Colors for each region.</param>
    /// <param name="ids">IDs of the regions.</param>
    /// <param name="pixelPosition">Position of the pixel being evaluated.</param>
    /// <returns>A tuple containing the ID of the closest region and its color.</returns>
    private static (int, Color) FindClosestRegion(Vector2Int[] points, Color[] regionColors, int[] ids, Vector2Int pixelPosition)
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

        return (ids[closestRegionId], regionColors[closestRegionId]);
    }

    /// <summary>
    /// Generates primary roads around regions in the Voronoi diagram.
    /// <para>
    /// Uses offsets to determine whether a pixel lies on a border and colors it accordingly.
    /// </para>
    /// </summary>
    /// <param name="size">Size of the Voronoi diagram.</param>
    /// <param name="closestRegionIds">Array of region IDs for each pixel.</param>
    /// <param name="pixelColors">Array of colors for each pixel.</param>
    /// <param name="regions">Dictionary of regions with their pixel lists.</param>
    /// <param name="width">Width of the road to generate.</param>
    /// <returns>An array of pixel colors with borders applied.</returns>
    private static Color[] GeneratePrimaryRoads(int size, int[] closestRegionIds, Color[] pixelColors, Dictionary<int, Region> regions, int width)
    {
        int totalOffsets = (2 * width + 1) * (2 * width + 1) - 1;
        int[] xOffsets = new int[totalOffsets];
        int[] yOffsets = new int[totalOffsets];
        int index = 0;

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

                if (currentRegionIndex != -1)
                {
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
            }
        });

        return pixelColors;
    }
}
