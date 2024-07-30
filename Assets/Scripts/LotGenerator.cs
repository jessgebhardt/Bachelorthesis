using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LotGenerator : MonoBehaviour
{
    public static Texture2D GenerateLots(Texture2D voronoiTexture, int roadWidth, int minBlockSquareSize, int minLotSquareSize)
    {

        // make flexible -> vllt für jeden Bezirk different?
        int areaThreshold = 20;



        List<List<Vector2Int>> extractedRegions = DistrictExtractor.ExtractRegions(voronoiTexture, roadWidth);
        List<List<Vector2Int>> blocks = new List<List<Vector2Int>>();
        List<List<Vector2Int>> lots = new List<List<Vector2Int>>();

        List<List<Vector2Int>> valid = new List<List<Vector2Int>>();
        List<List<Vector2Int>> unvalid = new List<List<Vector2Int>>();


        foreach (List<Vector2Int> region in extractedRegions)
        {
            List<List<Vector2Int>> regionBlocks = new List<List<Vector2Int>>();




            regionBlocks = SubdivideIntoBlocks(region, minBlockSquareSize);

            regionBlocks = SortLots(regionBlocks);

            Debug.Log("Anzahl regionBlocks:" + regionBlocks.Count);
            (blocks, lots) = RemoveInvalidLots(regionBlocks, minBlockSquareSize);

            valid.AddRange(blocks);
            unvalid.AddRange(lots);

            //foreach (List<Vector2Int> block in regionBlocks)
            //{
            //    lots = SubdivideBlockIntoLots(block, areaThreshold);
            //    lots = RemoveInvalidLots(lots, extractedRegions[0], minLotSize);
            //    //blocks.AddRange(lots);
            //}
            //blocks.AddRange(regionBlocks);

        }


        foreach (var v in valid)
        {
            HashSet<Vector2Int> edgePixels = GetEdges(v);
            foreach (var pos in edgePixels)
            {
                voronoiTexture.SetPixel(pos.x, pos.y, Color.green); 
            }
        }

        foreach (var u in unvalid)
        {
            HashSet<Vector2Int> uedgePixels = GetEdges(u);
            foreach (var pos in uedgePixels)
            {
                voronoiTexture.SetPixel(pos.x, pos.y, Color.red);
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

    private static List<List<Vector2Int>> SubdivideIntoBlocks(List<Vector2Int> region, int minBlockSize)
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



    private static List<List<Vector2Int>> SubdivideBlockIntoLots(List<Vector2Int> block, int areaThreshold)
    {
        List<List<Vector2Int>> lots = new List<List<Vector2Int>>();

        if (IsConvex(block) && IsRectangular(block) && block.Count < areaThreshold)
        {
            lots.Add(block);
        }
        else
        {
            List<Vector2Int> dividedBlock1, dividedBlock2;
            DivideAlongLongestEdge(block, out dividedBlock1, out dividedBlock2);

            lots.AddRange(SubdivideBlockIntoLots(dividedBlock1, areaThreshold));
            lots.AddRange(SubdivideBlockIntoLots(dividedBlock2, areaThreshold));
        }

        return lots;
    }

    private static bool IsConvex(List<Vector2Int> block)
    {
        int n = block.Count;
        if (n < 4) return true; //  -> doesnt work like that here

        bool isConvex = true;
        bool sign = false;

        for (int i = 0; i < n; i++)
        {
            Vector2Int p1 = block[i];
            Vector2Int p2 = block[(i + 1) % n];
            Vector2Int p3 = block[(i + 2) % n];

            int dx1 = p2.x - p1.x;
            int dy1 = p2.y - p1.y;
            int dx2 = p3.x - p2.x;
            int dy2 = p3.y - p2.y;

            int crossProduct = dx1 * dy2 - dy1 * dx2;

            if (i == 0)
            {
                sign = crossProduct > 0;
            }
            else
            {
                if (sign != (crossProduct > 0))
                {
                    isConvex = false;
                    break;
                }
            }
        }

        return isConvex;
    }

    private static bool IsRectangular(List<Vector2Int> block)
    {
        int n = block.Count;
        if (n != 4) return false; // -> doesnt work like that here

        for (int i = 0; i < n; i++)
        {
            Vector2Int p1 = block[i];
            Vector2Int p2 = block[(i + 1) % n];
            Vector2Int p3 = block[(i + 2) % n];

            int dx1 = p2.x - p1.x;
            int dy1 = p2.y - p1.y;
            int dx2 = p3.x - p2.x;
            int dy2 = p3.y - p2.y;

            int dotProduct = dx1 * dx2 + dy1 * dy2;
            if (dotProduct != 0) 
            {
                return false;
            }
        }

        int side1 = (block[1] - block[0]).sqrMagnitude;
        int side2 = (block[2] - block[1]).sqrMagnitude;
        int side3 = (block[3] - block[2]).sqrMagnitude;
        int side4 = (block[0] - block[3]).sqrMagnitude;

        return (side1 == side3) && (side2 == side4);
    }
    private static void DivideAlongLongestEdge(List<Vector2Int> block, out List<Vector2Int> dividedBlock1, out List<Vector2Int> dividedBlock2)
    {
        int n = block.Count;
        int longestEdgeIndex = -1;
        float longestEdgeLength = -1;

        for (int i = 0; i < n; i++)
        {
            Vector2Int p1 = block[i];
            Vector2Int p2 = block[(i + 1) % n];
            float edgeLength = (p2 - p1).sqrMagnitude;

            if (edgeLength > longestEdgeLength)
            {
                longestEdgeLength = edgeLength;
                longestEdgeIndex = i;
            }
        }

        Vector2Int startPoint = block[longestEdgeIndex];
        Vector2Int endPoint = block[(longestEdgeIndex + 1) % n];
        Vector2Int midPoint = (startPoint + endPoint) / 2;

        dividedBlock1 = new List<Vector2Int>();
        dividedBlock2 = new List<Vector2Int>();

        for (int i = 0; i < n; i++)
        {
            Vector2Int current = block[i];
            dividedBlock1.Add(current);

            if (current == startPoint)
            {
                dividedBlock1.Add(midPoint);
                dividedBlock2.Add(midPoint);
            }
            else if (current == endPoint)
            {
                dividedBlock2.Add(current);
                dividedBlock2.Add(midPoint);
            }
            else if (i > longestEdgeIndex)
            {
                dividedBlock2.Add(current);
            }
        }
    }

    private static (List<List<Vector2Int>>, List<List<Vector2Int>>) RemoveInvalidLots(List<List<Vector2Int>> lots, int minLotSize)
    {
        List<List<Vector2Int>> validLots = new List<List<Vector2Int>>();
        List<List<Vector2Int>> unvalidLots = new List<List<Vector2Int>>();

        foreach (var lot in lots)
        {

            if (lot.Count >= minLotSize)
            {
                Debug.Log("SIZE OF LOT: " + lot.Count);
                validLots.Add(lot);


                int minX = int.MaxValue;
                int maxX = int.MinValue;
                int minY = int.MaxValue;
                int maxY = int.MinValue;

                foreach (var point in lot)
                {
                    if (point.x < minX) minX = point.x;
                    if (point.x > maxX) maxX = point.x;
                    if (point.y < minY) minY = point.y;
                    if (point.y > maxY) maxY = point.y;
                }


                Debug.Log(new Vector3(minX, 1, minY) + "; " + new Vector3(maxX, 1, minY) + "; " + new Vector3(maxX, 1, maxY) + "; " + new Vector3(minX, 1, maxY));
            }
            else
            {
                bool merged = false;
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

                    if (combinedLot.Count >= minLotSize)
                    {
                        validLots.Remove(otherLot);
                        validLots.Add(combinedLot);
                        merged = true;
                        break;
                    }
                }
                if (!merged)
                {

                }
            }
        }

        return (validLots, unvalidLots);
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

    private static bool HasStreetConnection(List<Vector2Int> lot, List<Vector2Int> region)
    {
        // doesnt make sense
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (var point in region)
        {
            if (point.x < minX) minX = point.x;
            if (point.x > maxX) maxX = point.x;
            if (point.y < minY) minY = point.y;
            if (point.y > maxY) maxY = point.y;
        }

        foreach (var point in lot)
        {
            if (point.x == minX || point.x == maxX || point.y == minY || point.y == maxY)
            {
                return true;
            }
        }

        return false;
    }
}
