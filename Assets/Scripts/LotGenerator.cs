using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LotGenerator : MonoBehaviour
{
    public static Texture2D GenerateLots(Texture2D voronoiTexture, int roadWidth, int minLotSquareSize)
    {
        List<List<Vector2Int>> extractedRegions = DistrictExtractor.ExtractRegions(voronoiTexture, roadWidth);

        List<List<Vector2Int>> validLots = new List<List<Vector2Int>>();

        List<HashSet<Vector2Int>> allregionEdges = GetAllRegionEdges(extractedRegions);


        for (int i = 0; i < extractedRegions.Count; i++)
        {
            List<List<Vector2Int>> regionLots = SubdivideIntoLots(extractedRegions[i], minLotSquareSize);

            regionLots = SortLots(regionLots);

            validLots.AddRange(RemoveInvalidLots(regionLots, minLotSquareSize, allregionEdges[i]));
        }

        // TEST
        foreach (var v in validLots)
        {
            HashSet<Vector2Int> edgePixels = GetEdges(v);
            foreach (var pos in edgePixels)
            {
                voronoiTexture.SetPixel(pos.x, pos.y, Color.green); 
            }
        }

        voronoiTexture.Apply();

        return voronoiTexture;
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

    private static List<List<Vector2Int>> SubdivideIntoLots(List<Vector2Int> region, int minBlockSize)
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

        int numCellsX = Mathf.CeilToInt((float)regionWidth / minBlockSize);
        int numCellsY = Mathf.CeilToInt((float)regionHeight / minBlockSize);

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
        // Hier weiter: um das Problem zu lösen, nimm die lots die um den unvalid lot liegen und combine die??

        List<List<Vector2Int>> validLots = new List<List<Vector2Int>>();
        

        foreach (var lot in lots)
        {
            if (HasStreetConnection(lot, regionsEdge))
            {
                if (lot.Count >= minLotSize && IsWideEnough(lot, minLotSize))
                {
                    validLots.Add(lot);
                }
                else
                {
                    foreach (var otherLot in validLots)
                    {
                        List<Vector2Int> combinedLot = new List<Vector2Int>(lot);
                        combinedLot.AddRange(otherLot);

                        combinedLot.Sort((a, b) =>
                        {
                            int result = a.x.CompareTo(b.x);
                            if (result == 0)
                            {
                                result = a.y.CompareTo(b.y);
                            }
                            return result;
                        });

                        if (combinedLot.Count >= minLotSize && IsWideEnough(lot, minLotSize))
                        {
                            validLots.Remove(otherLot);
                            validLots.Add(combinedLot);
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


    private static List<HashSet<Vector2Int>> GetAllRegionEdges(List<List<Vector2Int>> regions)
    {
        List<HashSet<Vector2Int>> allEdges = new List<HashSet<Vector2Int>>();

        foreach (var region in regions)
        {
            allEdges.Add(GetEdges(region));
        }

        return allEdges;
    }

    /*&& höhe und breite mind half of minlotsize an weitesten stellen*/
    private static bool IsWideEnough(List<Vector2Int> lot, int minLotSize)
    {
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (var point in lot)
        {
            if (point.x < minX) minX = point.x;
            if (point.x > maxX) maxX = point.x;
            if (point.y < minY) minY = point.y;
            if (point.y > maxY) maxY = point.y;
        }

        int lotWidth = maxX - minX;
        int lotHeight = maxY - minY;

        return lotWidth >= minLotSize/2 && lotHeight >= minLotSize/2;
    }

}
