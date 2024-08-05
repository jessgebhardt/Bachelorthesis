using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static VoronoiDiagram;

public class LotGenerator : MonoBehaviour
{
    public static Dictionary<int, List<List<Vector2Int>>> GenerateLots(Texture2D voronoiTexture, Dictionary<int, Region> regions, IDictionary<int, District> districtsDictionary, int roadWidth)
    {
        List<List<Vector2Int>> areas = DistrictExtractor.ExtractRegions(voronoiTexture, 0);

        Dictionary<int, List<List<Vector2Int>>> regionAreas = new Dictionary<int, List<List<Vector2Int>>>();
        foreach (var region in regions)
        {
            regionAreas[region.Key] = new List<List<Vector2Int>>();
        }

        Dictionary<int, HashSet<Vector2Int>> regionPixelSets = new Dictionary<int, HashSet<Vector2Int>>();
        foreach (var region in regions)
        {
            regionPixelSets[region.Key] = new HashSet<Vector2Int>(region.Value.Pixels);
        }

        foreach (var area in areas)
        {
            foreach (var region in regionPixelSets)
            {
                if (area.Any(pixel => region.Value.Contains(pixel)))
                {
                    regionAreas[region.Key].Add(area);
                    break;
                }
            }
        }

        Dictionary<int, List<List<Vector2Int>>> regionLots = new Dictionary<int, List<List<Vector2Int>>>();

        foreach (var region in regions)
        {
            District district;
            districtsDictionary.TryGetValue(region.Key, out district);
            int minLotSize = district.type.minLotSizeSquared;

            int regionId = region.Key;

            foreach (var area in regionAreas[region.Key])
            {
                List<List<Vector2Int>> lotList = SubdivideIntoLots(area, minLotSize);

                lotList = SortLots(lotList);

                HashSet<Vector2Int> regionEdges = GetEdges(area);

                List<List<Vector2Int>> validLots = RemoveInvalidLots(lotList, minLotSize, regionEdges);

                regionLots[regionId] = validLots;
            }
            
        }

        return regionLots;
    }

    private static HashSet<Vector2Int> GetEdges(List<Vector2Int> lot)
    {
        HashSet<Vector2Int> edges = new HashSet<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        foreach (var point in lot)
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

    private static List<List<Vector2Int>> SubdivideIntoLots(List<Vector2Int> region, int minSize)
    {
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (var point in region)
        {
            if (point.x < minX) minX = point.x;
            if (point.x > maxX) maxX = point.x;
            if (point.y < minY) minY = point.y;
            if (point.y > maxY) maxY = point.y;
        }

        int regionWidth = maxX - minX;
        int regionHeight = maxY - minY;

        int numCellsX = Mathf.CeilToInt((float)regionWidth / minSize);
        int numCellsY = Mathf.CeilToInt((float)regionHeight / minSize);

        float cellSizeX = (float)regionWidth / numCellsX + 1;
        float cellSizeY = (float)regionHeight / numCellsY + 1;

        Dictionary<Vector2Int, List<Vector2Int>> grid = new Dictionary<Vector2Int, List<Vector2Int>>();

        foreach (var point in region)
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

        List<List<Vector2Int>> blocks = new List<List<Vector2Int>>();
        foreach (var cell in grid.Values)
        {
            cell.Sort((a, b) =>
            {
                int result = a.x.CompareTo(b.x);
                if (result == 0)
                {
                    result = a.y.CompareTo(b.y);
                }
                return result;
            });

            blocks.Add(cell);
        }

        return blocks;
    }

    private static List<List<Vector2Int>> RemoveInvalidLots(List<List<Vector2Int>> lots, int minLotSize, HashSet<Vector2Int> regionsEdge)
    {
        List<List<Vector2Int>> validLots = new List<List<Vector2Int>>();
        HashSet<Vector2Int> processedPoints = new HashSet<Vector2Int>();

        foreach (var lot in lots)
        {
            if (HasStreetConnection(lot, regionsEdge))
            {
                if (lot.Count >= minLotSize && IsWideEnough(lot, minLotSize) && !processedPoints.Overlaps(lot))
                {
                    validLots.Add(lot);

                    foreach (var point in lot)
                    {
                        processedPoints.Add(point);
                    }
                }
                else if (lot.Count < minLotSize || !IsWideEnough(lot, minLotSize))
                {
                    List<List<Vector2Int>> neighborLots = GetNeighborLots(lot, lots);

                    foreach (var otherLot in neighborLots)
                    {
                        if (processedPoints.Overlaps(otherLot))
                        {
                            continue;
                        }

                        List<Vector2Int> combinedLot = new List<Vector2Int>(lot);
                        combinedLot.AddRange(otherLot);

                        combinedLot = combinedLot.Distinct().ToList();
                        combinedLot.Sort((a, b) =>
                        {
                            int result = a.x.CompareTo(b.x);
                            if (result == 0)
                            {
                                result = a.y.CompareTo(b.y);
                            }
                            return result;
                        });

                        if (combinedLot.Count >= minLotSize && IsWideEnough(combinedLot, minLotSize) && !processedPoints.Overlaps(combinedLot))
                        {
                            validLots.Remove(otherLot);
                            validLots.Add(combinedLot);

                            foreach (var point in otherLot)
                            {
                                processedPoints.Add(point);
                            }
                            foreach (var point in lot)
                            {
                                processedPoints.Add(point);
                            }

                            break;
                        }
                    }
                }
            }
        }

        return validLots;
    }



    private static List<List<Vector2Int>> SortLots(List<List<Vector2Int>> lots)
    {
        lots.Sort((lot1, lot2) =>
        {
            Vector2Int lowerLeft1 = GetLowerLeftPoint(lot1);
            Vector2Int lowerLeft2 = GetLowerLeftPoint(lot2);
            int result = lowerLeft1.x.CompareTo(lowerLeft2.x);
            if (result == 0)
            {
                result = lowerLeft1.y.CompareTo(lowerLeft2.y);
            }
            return result;
        });
        return lots;
    }

    private static Vector2Int GetLowerLeftPoint(List<Vector2Int> lot)
    {
        Vector2Int lowerLeft = lot[0];
        foreach (var point in lot)
        {
            if (point.x < lowerLeft.x || (point.x == lowerLeft.x && point.y < lowerLeft.y))
            {
                lowerLeft = point;
            }
        }
        return lowerLeft;
    }

    private static bool HasStreetConnection(List<Vector2Int> lot, HashSet<Vector2Int> regionsEdge)
    {
        foreach (var point in regionsEdge)
        {
            if (lot.Contains(point))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsWideEnough(List<Vector2Int> lot, int minLotSize)
    {
        int halfMinLotSize = minLotSize / 2;

        Vector2Int center = FindCentroid(lot);

        int left = center.x;
        int right = center.x;
        while (lot.Contains(new Vector2Int(left - 1, center.y)))
        {
            left--;
        }
        while (lot.Contains(new Vector2Int(right + 1, center.y)))
        {
            right++;
        }
        int width = right - left;

        int down = center.y;
        int up = center.y;
        while (lot.Contains(new Vector2Int(center.x, down - 1)))
        {
            down--;
        }
        while (lot.Contains(new Vector2Int(center.x, up + 1)))
        {
            up++;
        }
        int height = up - down;

        return width >= halfMinLotSize && height >= halfMinLotSize;
    }

    private static List<List<Vector2Int>> GetNeighborLots(List<Vector2Int> originalLot, List<List<Vector2Int>> allLots)
    {
        HashSet<Vector2Int> edges = GetEdges(originalLot);
        List<List<Vector2Int>> neighborLots = new List<List<Vector2Int>>();

        foreach (var lot in allLots)
        {
            if (originalLot != lot)
            {
                foreach (var edge in edges)
                {
                    List<Vector2Int> neighbors = GetDirectNeighbors(edge);

                    foreach (var neighbor in neighbors)
                    {
                        if (lot.Contains(neighbor))
                        {
                            neighborLots.Add(lot);
                        }
                    }
                }
            }
        }

        return neighborLots;
    }

    private static List<Vector2Int> GetDirectNeighbors(Vector2Int point)
    {
        return new List<Vector2Int>
        {
            new Vector2Int(point.x, point.y + 1),
            new Vector2Int(point.x + 1, point.y),
            new Vector2Int(point.x, point.y - 1),
            new Vector2Int(point.x - 1, point.y),
        };
    }

    public static Vector2Int FindCentroid(List<Vector2Int> points)
    {
        int sumX = 0;
        int sumY = 0;
        int count = points.Count;

        foreach (var point in points)
        {
            sumX += point.x;
            sumY += point.y;
        }

        int centerX = sumX / count;
        int centerY = sumY / count;

        return new Vector2Int(centerX, centerY);
    }
}
