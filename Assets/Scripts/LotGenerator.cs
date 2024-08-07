using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LotGenerator : MonoBehaviour
{
    /// <summary>
    /// Generates lots within the given regions using the Voronoi diagram texture.
    /// <para>
    /// Extracts regions from the Voronoi texture, assigns these regions to different areas,
    /// and then subdivides the areas into lots based on the minimum lot size and road width.
    /// </para>
    /// </summary>
    /// <param name="voronoiTexture">The Voronoi diagram texture.</param>
    /// <param name="regions">Dictionary of region IDs and their corresponding regions.</param>
    /// <param name="districtsDictionary">Dictionary of district IDs and their corresponding districts.</param>
    /// <param name="roadWidth">The width of the roads.</param>
    /// <returns>Dictionary of region IDs and their corresponding lists of lots.</returns>
    public static Dictionary<int, List<List<Vector2Int>>> GenerateLots(Texture2D voronoiTexture, Dictionary<int, Region> regions, IDictionary<int, District> districtsDictionary, int roadWidth)
    {
        List<List<Vector2Int>> areas = RegionExtractor.ExtractRegions(voronoiTexture, 0);

        Dictionary<int, List<List<Vector2Int>>> regionAreas = InitializeRegionAreas(regions);
        Dictionary<int, HashSet<Vector2Int>> regionPixelSets = InitializeRegionPixelSets(regions);

        AssignAreasToRegions(areas, regionPixelSets, regionAreas);

        return CreateRegionLots(regions, districtsDictionary, regionAreas);
    }

    /// <summary>
    /// Initializes a dictionary to store lists of areas for each region.
    /// <para>
    /// The dictionary maps region IDs to lists of areas (lots) within that region.
    /// </para>
    /// </summary>
    /// <param name="regions">Dictionary of region IDs and their corresponding regions.</param>
    /// <returns>Initialized dictionary of region IDs to lists of areas.</returns>
    private static Dictionary<int, List<List<Vector2Int>>> InitializeRegionAreas(Dictionary<int, Region> regions)
    {
        return regions.ToDictionary(region => region.Key, region => new List<List<Vector2Int>>());
    }

    /// <summary>
    /// Initializes a dictionary to store sets of pixels for each region.
    /// <para>
    /// The dictionary maps region IDs to sets of pixels belonging to that region.
    /// </para>
    /// </summary>
    /// <param name="regions">Dictionary of region IDs and their corresponding regions.</param>
    /// <returns>Initialized dictionary of region IDs to sets of pixels.</returns>
    private static Dictionary<int, HashSet<Vector2Int>> InitializeRegionPixelSets(Dictionary<int, Region> regions)
    {
        return regions.ToDictionary(region => region.Key, region => new HashSet<Vector2Int>(region.Value.Pixels));
    }

    /// <summary>
    /// Assigns extracted areas to their corresponding regions based on pixel membership.
    /// <para>
    /// Iterates through the extracted areas and adds them to the appropriate region's list if
    /// any pixel in the area is contained within the region's pixel set.
    /// </para>
    /// </summary>
    /// <param name="areas">List of extracted areas.</param>
    /// <param name="regionPixelSets">Dictionary of region IDs to sets of pixels.</param>
    /// <param name="regionAreas">Dictionary of region IDs to lists of areas.</param>
    private static void AssignAreasToRegions(List<List<Vector2Int>> areas, Dictionary<int, HashSet<Vector2Int>> regionPixelSets, Dictionary<int, List<List<Vector2Int>>> regionAreas)
    {
        foreach (List<Vector2Int> area in areas)
        {
            foreach (KeyValuePair<int, HashSet<Vector2Int>> region in regionPixelSets)
            {
                if (area.Any(pixel => region.Value.Contains(pixel)))
                {
                    regionAreas[region.Key].Add(area);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Creates lots for each region based on the extracted areas.
    /// <para>
    /// Subdivides areas into lots, sorts the lots, and filters out invalid lots based on minimum lot size
    /// and road connectivity.
    /// </para>
    /// </summary>
    /// <param name="regions">Dictionary of region IDs and their corresponding regions.</param>
    /// <param name="districtsDictionary">Dictionary of district IDs and their corresponding districts.</param>
    /// <param name="regionAreas">Dictionary of region IDs to lists of areas.</param>
    /// <returns>Dictionary of region IDs and their corresponding lists of lots.</returns>
    private static Dictionary<int, List<List<Vector2Int>>> CreateRegionLots(Dictionary<int, Region> regions, IDictionary<int, District> districtsDictionary, Dictionary<int, List<List<Vector2Int>>> regionAreas)
    {
        Dictionary<int, List<List<Vector2Int>>> regionLots = new Dictionary<int, List<List<Vector2Int>>>();

        foreach (KeyValuePair<int, Region> region in regions)
        {
            int regionId = region.Key;
            if (!districtsDictionary.TryGetValue(regionId, out District district)) continue;

            int minLotSize = district.type.minLotSizeSquared;

            List<List<Vector2Int>> lots = new List<List<Vector2Int>>();
            foreach (List<Vector2Int> area in regionAreas[regionId])
            {
                List<List<Vector2Int>> subdividedLots = SubdivideIntoLots(area, minLotSize);
                List<List<Vector2Int>> sortedLots = SortLots(subdividedLots);
                HashSet<Vector2Int> regionEdges = GetEdges(area);
                List<List<Vector2Int>> validLots = RemoveInvalidLots(sortedLots, minLotSize, regionEdges);

                lots.AddRange(validLots);
            }

            regionLots[regionId] = lots;
        }

        return regionLots;
    }

    /// <summary>
    /// Identifies the edge pixels of a given lot.
    /// <para>
    /// An edge pixel is one that has at least one neighbor outside the lot.
    /// </para>
    /// </summary>
    /// <param name="lot">List of pixels representing the lot.</param>
    /// <returns>Set of edge pixels.</returns>
    private static HashSet<Vector2Int> GetEdges(List<Vector2Int> lot)
    {
        HashSet<Vector2Int> edges = new HashSet<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        foreach (Vector2Int point in lot)
        {
            bool isEdge = false;
            for (int i = 0; i < 4; i++)
            {
                Vector2Int neighbor = new Vector2Int(point.x + dx[i], point.y + dy[i]);
                if (!lot.Contains(neighbor))
                {
                    isEdge = true;
                    break;
                }
            }
            if (isEdge)
            {
                edges.Add(point);
            }
        }

        return edges;
    }

    /// <summary>
    /// Subdivides a region into smaller lots based on the minimum lot size.
    /// <para>
    /// Creates a grid over the region and groups pixels into lots based on grid cells.
    /// </para>
    /// </summary>
    /// <param name="region">List of pixels representing the region.</param>
    /// <param name="minSize">Minimum lot size in pixels.</param>
    /// <returns>List of subdivided lots.</returns>
    private static List<List<Vector2Int>> SubdivideIntoLots(List<Vector2Int> region, int minSize)
    {
        var bounds = CalculateRegionBounds(region);

        int regionWidth = bounds.maxX - bounds.minX;
        int regionHeight = bounds.maxY - bounds.minY;

        int numCellsX = Mathf.CeilToInt((float)regionWidth / minSize);
        int numCellsY = Mathf.CeilToInt((float)regionHeight / minSize);

        float cellSizeX = (float)regionWidth / numCellsX + 1;
        float cellSizeY = (float)regionHeight / numCellsY + 1;

        Dictionary<Vector2Int, List<Vector2Int>> grid = CreateGrid(region, bounds.minX, bounds.minY, cellSizeX, cellSizeY);

        return grid.Values.ToList();
    }

    /// <summary>
    /// Calculates the bounding box of a region.
    /// <para>
    /// Determines the minimum and maximum x and y coordinates of the region's pixels.
    /// </para>
    /// </summary>
    /// <param name="region">List of pixels representing the region.</param>
    /// <returns>Tuple containing the minimum and maximum x and y coordinates.</returns>
    private static (int minX, int minY, int maxX, int maxY) CalculateRegionBounds(List<Vector2Int> region)
    {
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (Vector2Int point in region)
        {
            if (point.x < minX) minX = point.x;
            if (point.x > maxX) maxX = point.x;
            if (point.y < minY) minY = point.y;
            if (point.y > maxY) maxY = point.y;
        }

        return (minX, minY, maxX, maxY);
    }

    /// <summary>
    /// Creates a grid over a region for lot subdivision.
    /// <para>
    /// Maps each pixel in the region to a grid cell based on its coordinates.
    /// </para>
    /// </summary>
    /// <param name="region">List of pixels representing the region.</param>
    /// <param name="minX">Minimum x coordinate of the region.</param>
    /// <param name="minY">Minimum y coordinate of the region.</param>
    /// <param name="cellSizeX">Width of each grid cell.</param>
    /// <param name="cellSizeY">Height of each grid cell.</param>
    /// <returns>Dictionary of grid cells to lists of pixels.</returns>
    private static Dictionary<Vector2Int, List<Vector2Int>> CreateGrid(List<Vector2Int> region, int minX, int minY, float cellSizeX, float cellSizeY)
    {
        Dictionary<Vector2Int, List<Vector2Int>> grid = new Dictionary<Vector2Int, List<Vector2Int>>();

        foreach (Vector2Int point in region)
        {
            int gridX = Mathf.FloorToInt((point.x - minX) / cellSizeX);
            int gridY = Mathf.FloorToInt((point.y - minY) / cellSizeY);
            Vector2Int gridCoord = new Vector2Int(gridX, gridY);

            if (!grid.ContainsKey(gridCoord))
            {
                grid[gridCoord] = new List<Vector2Int>();
            }

            grid[gridCoord].Add(point);
        }

        return grid;
    }

    /// <summary>
    /// Removes invalid lots based on their size and street connectivity.
    /// <para>
    /// Filters out lots that do not meet the minimum size requirements or are not connected to streets.
    /// </para>
    /// </summary>
    /// <param name="lots">List of lots to be validated, where each lot is a list of points (Vector2Int).</param>
    /// <param name="minLotSize">Minimum lot size required for a lot to be considered valid.</param>
    /// <param name="regionsEdge">Set of edge pixels representing the boundary of the region.</param>
    /// <returns>List of valid lots that meet the size and connectivity requirements.</returns>
    private static List<List<Vector2Int>> RemoveInvalidLots(List<List<Vector2Int>> lots, int minLotSize, HashSet<Vector2Int> regionsEdge)
    {
        List<List<Vector2Int>> validLots = new List<List<Vector2Int>>();
        HashSet<Vector2Int> processedPoints = new HashSet<Vector2Int>();

        foreach (List<Vector2Int> lot in lots)
        {
            if (!IsValidLot(lot, minLotSize, regionsEdge, processedPoints))
            {
                ProcessInvalidLot(lot, minLotSize, validLots, processedPoints, lots, regionsEdge);
            }
            else
            {
                validLots.Add(lot);
                AddPointsToProcessed(lot, processedPoints);
            }
        }

        return validLots;
    }

    /// <summary>
    /// Determines whether a given lot is valid based on its size, street connectivity, and whether its points have been processed.
    /// <para>
    /// A lot is considered valid if it meets the minimum size requirement, is connected to streets, and has not been processed yet.
    /// </para>
    /// </summary>
    /// <param name="lot">List of points (Vector2Int) representing the lot to be validated.</param>
    /// <param name="minLotSize">Minimum lot size required for a lot to be considered valid.</param>
    /// <param name="regionsEdge">Set of edge pixels representing the boundary of the region.</param>
    /// <param name="processedPoints">Set of points that have already been processed.</param>
    /// <returns>True if the lot is valid; otherwise, false.</returns>
    private static bool IsValidLot(List<Vector2Int> lot, int minLotSize, HashSet<Vector2Int> regionsEdge, HashSet<Vector2Int> processedPoints)
    {
        return HasStreetConnection(lot, regionsEdge)
               && lot.Count >= minLotSize
               && IsWideEnough(lot, minLotSize)
               && !processedPoints.Overlaps(lot);
    }

    /// <summary>
    /// Processes lots that are initially invalid by attempting to combine them with neighboring lots to form a valid lot.
    /// <para>
    /// This method tries to combine the invalid lot with its neighboring lots to meet the size and connectivity requirements.
    /// </para>
    /// </summary>
    /// <param name="lot">List of points (Vector2Int) representing the invalid lot to be processed.</param>
    /// <param name="minLotSize">Minimum lot size required for a lot to be considered valid.</param>
    /// <param name="validLots">List of currently validated lots where valid lots are added or removed.</param>
    /// <param name="processedPoints">Set of points that have already been processed.</param>
    /// <param name="lots">List of all lots to find neighbors and potential combinations.</param>
    /// <param name="regionsEdge">Set of edge pixels representing the boundary of the region.</param>
    private static void ProcessInvalidLot(List<Vector2Int> lot, int minLotSize, List<List<Vector2Int>> validLots, HashSet<Vector2Int> processedPoints, List<List<Vector2Int>> lots, HashSet<Vector2Int> regionsEdge)
    {
        List<List<Vector2Int>> neighborLots = GetNeighborLots(lot, lots);

        foreach (List<Vector2Int> neighborLot in neighborLots)
        {
            if (processedPoints.Overlaps(neighborLot))
            {
                continue;
            }

            List<Vector2Int> combinedLot = CombineLots(lot, neighborLot);

            if (IsValidLot(combinedLot, minLotSize, regionsEdge, processedPoints))
            {
                validLots.Remove(neighborLot);
                validLots.Add(combinedLot);
                AddPointsToProcessed(neighborLot, processedPoints);
                AddPointsToProcessed(lot, processedPoints);
                break;
            }
        }
    }


    /// <summary>
    /// Combines two lots and sorts their points
    /// </summary>
    /// <param name="lot1">Lot to combine with lot2.</param>
    /// <param name="lot2">Lot to combine with lot1.</param>
    /// <returns>Combined Lot.</returns>
    private static List<Vector2Int> CombineLots(List<Vector2Int> lot1, List<Vector2Int> lot2)
    {
        HashSet<Vector2Int> combinedLot = new HashSet<Vector2Int>(lot1);
        combinedLot.UnionWith(lot2);
        return combinedLot.OrderBy(p => p.x).ThenBy(p => p.y).ToList();
    }

    /// <summary>
    /// Adds the points of lots to a HashSet.
    /// </summary>
    /// <param name="lot">Lots to be added.</param>
    /// <param name="processedPoints">HashSet to add lot points to.</param>
    private static void AddPointsToProcessed(IEnumerable<Vector2Int> lot, HashSet<Vector2Int> processedPoints)
    {
        foreach (Vector2Int point in lot)
        {
            processedPoints.Add(point);
        }
    }

    /// <summary>
    /// Sorts lots based on the coordinates of their lower-left corner.
    /// <para>
    /// This sorting helps in organizing lots in a consistent order.
    /// </para>
    /// </summary>
    /// <param name="lots">List of lots to be sorted.</param>
    /// <returns>Sorted list of lots.</returns>
    private static List<List<Vector2Int>> SortLots(List<List<Vector2Int>> lots)
    {
        lots.Sort((lot1, lot2) =>
        {
            Vector2Int lowerLeft1 = GetLowerLeftPoint(lot1);
            Vector2Int lowerLeft2 = GetLowerLeftPoint(lot2);
            int result = lowerLeft1.x.CompareTo(lowerLeft2.x);
            return result != 0 ? result : lowerLeft1.y.CompareTo(lowerLeft2.y);
        });
        return lots;
    }

    /// <summary>
    /// Finds the lower-left corner point of a lot.
    /// <para>
    /// This point is used for sorting lots and determining their position relative to others.
    /// </para>
    /// </summary>
    /// <param name="lot">List of pixels representing the lot.</param>
    /// <returns>Lower-left corner point of the lot.</returns>
    private static Vector2Int GetLowerLeftPoint(List<Vector2Int> lot)
    {
        return lot.Aggregate((min, point) => point.x < min.x || (point.x == min.x && point.y < min.y) ? point : min);
    }

    /// <summary>
    /// Checks if a lot has any street connection based on its edge pixels.
    /// <para>
    /// A lot is considered to have a street connection if any of its pixels are on the edge of the region.
    /// </para>
    /// </summary>
    /// <param name="lot">List of pixels representing the lot.</param>
    /// <param name="regionEdges">Set of edge pixels for the region.</param>
    /// <returns>True if the lot has a street connection; otherwise, false.</returns>
    private static bool HasStreetConnection(List<Vector2Int> lot, HashSet<Vector2Int> regionEdges)
    {
        return lot.Any(point => regionEdges.Contains(point));
    }

    /// <summary>
    /// Checks if a lot is wide enough based on its dimensions.
    /// <para>
    /// A lot is considered wide enough if its width and height are both at least half of the minimum lot size.
    /// </para>
    /// </summary>
    /// <param name="lot">List of pixels representing the lot.</param>
    /// <param name="minLotSize">Minimum lot size in pixels.</param>
    /// <returns>True if the lot is wide enough; otherwise, false.</returns>
    private static bool IsWideEnough(List<Vector2Int> lot, int minLotSize)
    {
        int halfMinLotSize = minLotSize / 2;
        Vector2Int center = FindCentroid(lot);

        int width = GetWidth(lot, center);
        int height = GetHeight(lot, center);

        return width >= halfMinLotSize && height >= halfMinLotSize;
    }

    /// <summary>
    /// Calculates the width of a lot based on its center point.
    /// <para>
    /// Determines the width by finding the horizontal extent of the lot around the center.
    /// </para>
    /// </summary>
    /// <param name="lot">List of pixels representing the lot.</param>
    /// <param name="center">Center point of the lot.</param>
    /// <returns>Width of the lot.</returns>
    private static int GetWidth(List<Vector2Int> lot, Vector2Int center)
    {
        int left = center.x;
        int right = center.x;
        while (lot.Contains(new Vector2Int(left - 1, center.y))) left--;
        while (lot.Contains(new Vector2Int(right + 1, center.y))) right++;
        return right - left;
    }

    /// <summary>
    /// Calculates the height of a lot based on its center point.
    /// <para>
    /// Determines the height by finding the vertical extent of the lot around the center.
    /// </para>
    /// </summary>
    /// <param name="lot">List of pixels representing the lot.</param>
    /// <param name="center">Center point of the lot.</param>
    /// <returns>Height of the lot.</returns>
    private static int GetHeight(List<Vector2Int> lot, Vector2Int center)
    {
        int down = center.y;
        int up = center.y;
        while (lot.Contains(new Vector2Int(center.x, down - 1))) down--;
        while (lot.Contains(new Vector2Int(center.x, up + 1))) up++;
        return up - down;
    }

    /// <summary>
    /// Finds lots that are neighbors to the given original lot.
    /// <para>
    /// Neighbors are lots that share edges with the original lot.
    /// </para>
    /// </summary>
    /// <param name="originalLot">The original lot to find neighbors for.</param>
    /// <param name="allLots">List of all lots to search for neighbors.</param>
    /// <returns>List of neighboring lots.</returns>
    private static List<List<Vector2Int>> GetNeighborLots(List<Vector2Int> originalLot, List<List<Vector2Int>> allLots)
    {
        HashSet<Vector2Int> edges = GetEdges(originalLot);
        List<List<Vector2Int>> neighborLots = new List<List<Vector2Int>>();

        foreach (List<Vector2Int> lot in allLots)
        {
            if (originalLot == lot) continue;

            if (edges.Any(edge => GetDirectNeighbors(edge).Any(neighbor => lot.Contains(neighbor))))
            {
                neighborLots.Add(lot);
            }
        }

        return neighborLots;
    }

    /// <summary>
    /// Gets the direct neighbors of a given pixel.
    /// <para>
    /// Direct neighbors are the pixels adjacent to the given pixel in the four cardinal directions.
    /// </para>
    /// </summary>
    /// <param name="point">The pixel whose neighbors are to be found.</param>
    /// <returns>List of direct neighbors.</returns>
    private static List<Vector2Int> GetDirectNeighbors(Vector2Int point)
    {
        return new List<Vector2Int>
        {
            new Vector2Int(point.x, point.y + 1),
            new Vector2Int(point.x + 1, point.y),
            new Vector2Int(point.x, point.y - 1),
            new Vector2Int(point.x - 1, point.y)
        };
    }

    /// <summary>
    /// Finds the centroid of a list of points.
    /// <para>
    /// The centroid is the average position of all the points.
    /// </para>
    /// </summary>
    /// <param name="points">List of points to calculate the centroid for.</param>
    /// <returns>Centroid of the points.</returns>
    private static Vector2Int FindCentroid(List<Vector2Int> points)
    {
        int sumX = points.Sum(point => point.x);
        int sumY = points.Sum(point => point.y);
        int count = points.Count;

        int centerX = sumX / count;
        int centerY = sumY / count;

        return new Vector2Int(centerX, centerY);
    }
}
