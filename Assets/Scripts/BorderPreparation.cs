using System.Collections.Generic;
using UnityEngine;

public class BorderPreparation : MonoBehaviour
{
    private Texture2D voronoiTexture;

    private List<Vector2Int> roadSegmentPoints = new List<Vector2Int>();

    private List<Vector2Int> splitMarks = new List<Vector2Int>();
    private Vector2Int startPoint;

    private HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

    private Vector2Int cityCenterInt;
    private float cityRadius;

    public Texture2D GenerateRoads(Texture2D texture, float outerBoundaryRadius, Vector3 cityCenter, int segmentLength)
    {
        ClearAllLists();

        voronoiTexture = texture;
        cityCenterInt = new Vector2Int(Mathf.RoundToInt(cityCenter.x), Mathf.RoundToInt(cityCenter.z));
        cityRadius = outerBoundaryRadius;


        startPoint = (Vector2Int)GetStartPoint();

        List<List<Vector2Int>> extractedRegions = DistrictExtractor.ExtractRegions(voronoiTexture, 0);
        List<List<Vector2Int>> segments = PrepareSegments(extractedRegions);
        voronoiTexture = SecondaryRoadsGenerator.GenerateSecondaryRoads(extractedRegions, segments, voronoiTexture);

        List<Border> borderList = MarkSegments(startPoint, segmentLength);

        splitMarks.Add(startPoint);

        // Add back later
        // RoadGenerator.GenerateRoad(borderList);

        return voronoiTexture;
    }

    void ClearAllLists ()
    {
        splitMarks.Clear();
        visited.Clear();
        roadSegmentPoints.Clear();
    }

    private Vector2Int? GetStartPoint()
    {
        List<Vector2Int> points = new List<Vector2Int>();

        for (int y = 0; y < voronoiTexture.height; y++)
        {
            for (int x = 0; x < voronoiTexture.width; x++)
            {
                Vector2Int point = new Vector2Int(x, y);

                if (IsBlackPixel(point) && IsDirectNeighborClear(point))
                {
                    return point;
                }
            }
        }

        Debug.LogError("No start point found!");
        return null;

    }

    bool IsDirectNeighborClear(Vector2Int point)
    {
        foreach (Vector2Int neighbor in GetNeighbors(point, false)) 
        {
            if (IsClearPixel(neighbor))
            {
                return true;
            }
        }
        return false;
    }

    private void GetNeighborhood(Vector2Int point, List<Vector2Int> points, HashSet<Vector2Int> neighborhood, HashSet<Vector2Int> visitedPoints)
    {
        if (visitedPoints.Contains(point))
            return;

        visitedPoints.Add(point);
        neighborhood.Add(point);

        foreach (Vector2Int neighbor in GetNeighbors(point, true))
        {
            if (points.Contains(neighbor) && !visitedPoints.Contains(neighbor))
            {
                GetNeighborhood(neighbor, points, neighborhood, visitedPoints);
            }
        }
    }

    public List<Border> MarkSegments(Vector2Int startPoint, int segmentLength)
    {

        List<Border> borderList = new List<Border>();
        List<BorderToTrace> bordersToTrace = new List<BorderToTrace>();

        List<Vector2Int> segmentMarks = new List<Vector2Int>();

        List<Vector2Int> nextPoints = new List<Vector2Int>();

        List<Vector2Int> foundSplitMarks = new List<Vector2Int>(); // remove later ?

        nextPoints.Add(startPoint);
        float startPointDistance = Vector2Int.Distance(startPoint, cityCenterInt);

        Vector2Int? nextPoint = null;

        foreach (Vector2Int neighbor in GetNeighbors(startPoint, true))
        {
            float neighborDistance = Vector2Int.Distance(neighbor, cityCenterInt);

            if (IsBlackPixel(neighbor) && !visited.Contains(neighbor) && neighborDistance < startPointDistance)
            {
                nextPoint = neighbor;
                break;
            }
        }

        BorderToTrace newBorderToTrace = new BorderToTrace
        {
            startPoint = startPoint,
            nextPoint = nextPoint
        };


        visited.Add(startPoint);

        bordersToTrace.Add(newBorderToTrace);

        while (bordersToTrace.Count > 0)
        {
            BorderToTrace currentBorder = bordersToTrace[0];
            bordersToTrace.RemoveAt(0);

            (List<BorderToTrace> newBordersToTrace, List<Vector2Int> segmentPoints) = TraceBorder(currentBorder, segmentLength);

            if (newBordersToTrace.Count != 0)
            {
                Border newBorder = new Border
                {
                    startPoint = currentBorder.startPoint,
                    segments = segmentPoints,
                    endPoint = newBordersToTrace[0].startPoint,
                };
                borderList.Add(newBorder);

                List<BorderToTrace> bordersToCheck = new List<BorderToTrace>();
                bordersToCheck.AddRange(newBordersToTrace);

                foreach (BorderToTrace border in bordersToCheck)
                {
                    if (border.nextPoint == null)
                    {
                        newBordersToTrace.Remove(border);
                    }
                }

                bordersToTrace.AddRange(newBordersToTrace);
            }
        }

        return borderList;
    }

    public (List<BorderToTrace>, List<Vector2Int>) TraceBorder(BorderToTrace borderToTrace, int segmentLength)
    {
        bool noSplitMarkFound = true;

        List<BorderToTrace> bordersToTrace = new List<BorderToTrace>();

        List<Vector2Int> segmentPoints = new List<Vector2Int>();

        List<Vector2Int> nextPoints = new List<Vector2Int>();

        Vector2Int splitMark = Vector2Int.zero;

        nextPoints.Add((Vector2Int)borderToTrace.nextPoint);

        int segmentIndex = 0;

        while (noSplitMarkFound && nextPoints.Count > 0)
        {
            Vector2Int current = nextPoints[0];
            nextPoints.RemoveAt(0);

            if (current == borderToTrace.startPoint || !visited.Contains(current))
            {
                if (!visited.Contains(current))
                {
                    visited.Add(current);
                }
                List<Vector2Int> remainingNeighbors = new List<Vector2Int>();

                foreach (Vector2Int neighbor in GetNeighbors(current, true)) 
                {
                    if (IsBlackPixel(neighbor) && !visited.Contains(neighbor))
                    {
                        remainingNeighbors.Add(neighbor);
                        if (!nextPoints.Contains(neighbor))
                        {
                            nextPoints.Add(neighbor);
                        }
                    }
                }
                
                if (current != borderToTrace.startPoint && IsBorderSplit(remainingNeighbors) || nextPoints.Count == 0)
                {
                    splitMark = current;
                    noSplitMarkFound = false;
                }

                if (segmentIndex < segmentLength)
                {
                    segmentIndex++;

                }
                else if (noSplitMarkFound && segmentIndex == segmentLength)
                {
                    segmentPoints.Add(current);
                    roadSegmentPoints.Add(current);
                    segmentIndex = 0;
                }
            }
        }

        if (!noSplitMarkFound)
        {
            splitMarks.Add(splitMark);

            List<Vector2Int> nextBorderPoints = FindNextBorderPoints(splitMark);

            if (nextBorderPoints.Count == 0)
            {
                BorderToTrace newBorderToTrace = new BorderToTrace
                {
                    startPoint = splitMark,
                    nextPoint = null
                };
                bordersToTrace.Add(newBorderToTrace);
            } else
            {
                foreach (Vector2Int nextPoint in nextBorderPoints)
                {
                    BorderToTrace newBorderToTrace = new BorderToTrace
                    {
                        startPoint = splitMark,
                        nextPoint = nextPoint
                    };
                    bordersToTrace.Add(newBorderToTrace);
                }
            }
        }

        return (bordersToTrace, segmentPoints);
    }

    public List<Vector2Int> FindNextBorderPoints(Vector2Int startPoint)
    {
        List<Vector2Int> nextBorderPoints = new List<Vector2Int>();
        List<Vector2Int> remainingNeighbors = new List<Vector2Int>();

        foreach (Vector2Int neighbor in GetNeighbors(startPoint, true))
        {
            if (IsBlackPixel(neighbor) && !visited.Contains(neighbor))
            {
                remainingNeighbors.Add(neighbor);
            }
        }

        (bool hasExactlyOneNeighbor, List<(Vector2Int, Vector2Int)> neighborPairs) = HasExactlyOneNeighbor(remainingNeighbors);
        if (remainingNeighbors.Count >= 4)
        {
            if (hasExactlyOneNeighbor)
            {
                foreach ((Vector2Int, Vector2Int) neighborPair in neighborPairs)
                {
                    nextBorderPoints.Add(neighborPair.Item1);
                }
            } else
            {
                nextBorderPoints.AddRange(FindPointsWithExactlyOneNeighbor(remainingNeighbors));
            }
        }
        else if (remainingNeighbors.Count == 3 && FindPointWithoutNeighbor(remainingNeighbors) != null)
        {
            Vector2Int? neighborlessPoint = FindPointWithoutNeighbor(remainingNeighbors);
            if (neighborlessPoint != null)
            {
                nextBorderPoints.Add((Vector2Int)neighborlessPoint);
                remainingNeighbors.Remove((Vector2Int)neighborlessPoint);
            }
            nextBorderPoints.Add(remainingNeighbors[0]);

        }
        else if (remainingNeighbors.Count == 2 && !AreNeighbors(remainingNeighbors) && !HasDeadEndNeighbor(remainingNeighbors))
        { 
            nextBorderPoints.AddRange(remainingNeighbors);
        }

        return nextBorderPoints;
    }


    public List<Vector2Int> FindPointsWithExactlyOneNeighbor(List<Vector2Int> points)
    {
        var pointsWithOneNeighbor = new List<Vector2Int>();

        foreach (var point in points)
        {
            int neighborCount = 0;

            foreach (var other in points)
            {
                if (point != other)
                {
                    if ((Mathf.Abs(point.x - other.x) == 1 && point.y == other.y) ||
                        (Mathf.Abs(point.y - other.y) == 1 && point.x == other.x))
                    {
                        neighborCount++;
                    }
                }
            }
            if (neighborCount == 1)
            {
                pointsWithOneNeighbor.Add(point);
            }
        }

        return pointsWithOneNeighbor;
    }

    public (bool, List<(Vector2Int, Vector2Int)>) HasExactlyOneNeighbor(List<Vector2Int> points)
    {
        List<(Vector2Int, Vector2Int)> neighborPairs = new List<(Vector2Int, Vector2Int)>();

        foreach (Vector2Int point in points)
        {
            int neighborCount = 0;
            Vector2Int? neighbor = null;

            foreach (Vector2Int other in points)
            {
                if (point != other)
                {
                    if ((Mathf.Abs(point.x - other.x) == 1 && point.y == other.y) ||
                        (Mathf.Abs(point.y - other.y) == 1 && point.x == other.x))
                    {
                        neighborCount++;
                        neighbor = other;
                    }
                }
            }

            // ein Punkt hat nicht genau einen Nachbarn
            if (neighborCount != 1)
            {
                return (false, null);
            }
            if (neighbor.HasValue)
            {
                neighborPairs.Add((point, neighbor.Value));
            }
        }
        return (true, neighborPairs);
    }

    public Vector2Int? FindPointWithoutNeighbor(List<Vector2Int> points)
    {
        foreach (Vector2Int point in points)
        {
            bool hasNeighbor = false;

            foreach (Vector2Int other in points)
            {
                if (point != other)
                {
                    // ist der andere Punkt ein horizontaler oder vertikaler Nachbar?
                    if ((Mathf.Abs(point.x - other.x) == 1 && point.y == other.y) ||
                        (Mathf.Abs(point.y - other.y) == 1 && point.x == other.x))
                    {
                        hasNeighbor = true;
                        break;
                    }
                }
            }

            if (!hasNeighbor)
            {
                return point; //Punkt ohne Nachbarn
            }
        }

        return null; // Falls alle Punkte Nachbarn haben
    }

    bool IsBorderSplit(List<Vector2Int> remainingNeighbors)
    {
        (bool hasExactlyOneNeighbor, List<(Vector2Int, Vector2Int)> neighborPairs) = HasExactlyOneNeighbor(remainingNeighbors);

        if (remainingNeighbors.Count >= 4) { return true; }
        else if (remainingNeighbors.Count == 3 && FindPointWithoutNeighbor(remainingNeighbors) != null) { return true; }
        else if (remainingNeighbors.Count == 2 && !AreNeighbors(remainingNeighbors) && !HasDeadEndNeighbor(remainingNeighbors)) { return true; }
        return false;
    }


    bool HasDeadEndNeighbor(List<Vector2Int> remainingNeighbors)
    {
        foreach (Vector2Int remaining in remainingNeighbors)
        {
            if(IsDeadEnd(remaining, remainingNeighbors))
            {
                return true;
            }
        }

        return false;
    }

    bool IsDeadEnd(Vector2Int remainingNeighbor, List<Vector2Int> remainingNeighbors)
    {
        foreach (Vector2Int neighbor in GetNeighbors(remainingNeighbor, true))
        {
            if (IsBlackPixel(neighbor))
            {
                if (!visited.Contains(neighbor) && !remainingNeighbors.Contains(neighbor))
                {
                    return false;
                }
            }
        }

        return true;
    }


    bool AreNeighbors(List<Vector2Int> points)
    {
        HashSet<Vector2Int> pointSet = new HashSet<Vector2Int>(points);

        foreach (Vector2Int point in points)
        {
            foreach (Vector2Int neighbor in GetNeighbors(point, false))
            {
                if (pointSet.Contains(neighbor))
                {
                    return true;
                }
            }
        }
        return false;
    }

    bool IsBlackPixel(Vector2Int point)
    {
        if (point.x < 0 || point.x >= voronoiTexture.width || point.y < 0 || point.y >= voronoiTexture.height)
        {
            return false;
        }

        return voronoiTexture.GetPixel(point.x, point.y) == Color.black;
    }

    bool IsClearPixel(Vector2Int point)
    {
        if (point.x < 0 || point.x >= voronoiTexture.width || point.y < 0 || point.y >= voronoiTexture.height)
        {
            return false;
        }

        return voronoiTexture.GetPixel(point.x, point.y) == Color.clear;
    }

    private List<Vector2Int> GetNeighbors(Vector2Int point, bool includeDiagonals)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>
        {
            new Vector2Int(point.x, point.y + 1),
            new Vector2Int(point.x + 1, point.y),
            new Vector2Int(point.x, point.y - 1),
            new Vector2Int(point.x - 1, point.y),
        };

        if (includeDiagonals)
        {
            neighbors.Add(new Vector2Int(point.x - 1, point.y + 1));
            neighbors.Add(new Vector2Int(point.x + 1, point.y + 1));
            neighbors.Add(new Vector2Int(point.x + 1, point.y - 1));
            neighbors.Add(new Vector2Int(point.x - 1, point.y - 1));
        }

        return neighbors;
    }

    private List<List<Vector2Int>> PrepareSegments(List<List<Vector2Int>> extractedRegions)
    {
        // Precompute neighbors for all segment points
        // Dictionary<Vector2Int, List<Vector2Int>> segmentPointNeighbors = roadSegmentPoints.ToDictionary(point => point, point => neighbor(point, false));

        List<List<Vector2Int>> extractedSegments = new List<List<Vector2Int>>();

        foreach (List<Vector2Int> regionPixels in extractedRegions)
        {
            HashSet<Vector2Int> regionSet = new HashSet<Vector2Int>(regionPixels);
            List<Vector2Int> segmentPoints = new List<Vector2Int>();

            //foreach (var segmentPointNeighbor in segmentPointNeighbors)
            //{
            //    foreach (var neighbor in segmentPointNeighbor.Value)
            //    {
            //        if (regionSet.Contains(neighbor))
            //        {
            //            segmentPoints.Add(neighbor);
            //        }
            //    }
            //}
            foreach (Vector2Int regionPixel in regionPixels)
            {
                foreach (Vector2Int neighbor in GetNeighbors(regionPixel, false))
                {
                    if (!regionPixels.Contains(neighbor))
                    {
                        segmentPoints.Add(neighbor);
                        continue;
                    }
                }
                continue;
            }



            extractedSegments.Add(segmentPoints);
        }

        return extractedSegments;
    }



    private void OnDrawGizmos()
    {
        //if (boundaryPoints != null)
        //{
        //    Gizmos.color = Color.blue;
        //    foreach (Vector2Int point in boundaryPoints)
        //    {
        //        Gizmos.DrawSphere(new Vector3(point.x+ 0.5f, 1, point.y + 0.5f), 7f);
        //    }
        //}

        if(startPoint != null) { Gizmos.color = Color.magenta; Gizmos.DrawSphere(new Vector3(startPoint.x, 1, startPoint.y), 10f); }

        if (splitMarks != null)
        {
            Gizmos.color = Color.red;
            foreach (Vector2Int split in splitMarks)
            {
                Gizmos.DrawSphere(new Vector3(split.x+0.5f, 1, split.y+0.5f), 5f);
            }
        }


        if (roadSegmentPoints != null)
        {
            Gizmos.color = Color.green;
            foreach (Vector2Int segment in roadSegmentPoints)
            {
                Gizmos.DrawSphere(new Vector3(segment.x + 0.5f, 1, segment.y + 0.5f), 5f);

            }
        }
    }
}

[System.Serializable]
public struct Border
{
    public Vector2Int startPoint;
    public List<Vector2Int> segments;
    public Vector2Int endPoint;
}

[System.Serializable]
public struct BorderToTrace
{
    public Vector2Int startPoint;
    public Vector2Int? nextPoint;
}
