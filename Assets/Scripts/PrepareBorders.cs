using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PrepareBorders : MonoBehaviour
{
    private Texture2D voronoiTexture;

    private List<Vector2Int> roadSegmentPoints = new List<Vector2Int>();

    private List<Vector2Int> splitMarks = new List<Vector2Int>();
    private Vector2Int startPoint;

    private HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
    private List<Vector2Int> boundaryPoints = new List<Vector2Int>();

    Vector2Int cityCenterInt;
    float cityRadius;

    public void GenerateRoad(Texture2D texture, float outerBoundaryRadius, Vector3 cityCenter, int segmentLength)
    {
        ClearAllLists();

        voronoiTexture = texture;
        cityCenterInt = new Vector2Int(Mathf.RoundToInt(cityCenter.x), Mathf.RoundToInt(cityCenter.z));
        cityRadius = outerBoundaryRadius;

        GetBoundaryPoints();

        startPoint = boundaryPoints[0];

        splitMarks.AddRange(MarkSegments(startPoint, segmentLength));

        // AddRoad();
    }

    void ClearAllLists ()
    {
        splitMarks.Clear();
        visited.Clear();
        boundaryPoints.Clear();
        roadSegmentPoints.Clear();
    }

    private void GetBoundaryPoints()
    {
        List<Vector2Int> points = new List<Vector2Int>();

        for (int y = 0; y < voronoiTexture.height; y++)
        {
            for (int x = 0; x < voronoiTexture.width; x++)
            {
                Vector2Int point = new Vector2Int(x, y);

                if (IsBlackPixel(point) && directNeighborIsClear(point))
                {
                    points.Add(point);
                }
            }
        }

        List<Vector2Int> reducedPoints = ReducePoints(points);
        boundaryPoints = SortPoints(reducedPoints);
    }

    bool directNeighborIsClear(Vector2Int point)
    {
        Vector2Int[] neighbors = new Vector2Int[]
        {
                point + Vector2Int.up,
                point + Vector2Int.down,
                point + Vector2Int.left,
                point + Vector2Int.right,
        };

        foreach (Vector2Int neighbor in neighbors) 
        {
            if (IsClearPixel(neighbor))
            {
                return true;
            }
        }
        return false;
    }

    private List<Vector2Int> ReducePoints(List<Vector2Int> points)
    {
        List<HashSet<Vector2Int>> neighborhoods = new List<HashSet<Vector2Int>>();
        HashSet<Vector2Int> visitedPoints = new HashSet<Vector2Int>();

        foreach (Vector2Int point in points)
        {
            if (!visitedPoints.Contains(point))
            {
                HashSet<Vector2Int> neighborhood = new HashSet<Vector2Int>();
                GetNeighborhood(point, points, neighborhood, visitedPoints);
                neighborhoods.Add(neighborhood);
            }
        }

        List<Vector2Int> reducedPoints = new List<Vector2Int>();
        foreach (var neighborhood in neighborhoods)
        {
            Vector2Int bestPoint = GetBestPoint(neighborhood);
            if (!reducedPoints.Contains(bestPoint))
            {
                reducedPoints.Add(bestPoint);
            }
        }

        return reducedPoints;
    }

    private void GetNeighborhood(Vector2Int point, List<Vector2Int> points, HashSet<Vector2Int> neighborhood, HashSet<Vector2Int> visitedPoints)
    {
        if (visitedPoints.Contains(point))
            return;

        visitedPoints.Add(point);
        neighborhood.Add(point);

        foreach (Vector2Int neighbor in GetNeighbors(point))
        {
            if (points.Contains(neighbor) && !visitedPoints.Contains(neighbor))
            {
                GetNeighborhood(neighbor, points, neighborhood, visitedPoints);
            }
        }
    }

    private Vector2Int GetBestPoint(HashSet<Vector2Int> neighborhood)
    {
        Vector2Int bestPoint = neighborhood.First();
        int maxClearNeighbors = -1;
        int minBlackNeighbors = int.MaxValue;

        foreach (var point in neighborhood)
        {
            int clearNeighbors = GetNeighbors(point).Count(n => voronoiTexture.GetPixel(point.x, point.y) == Color.clear);
            int blackNeighbors = GetNeighbors(point).Count(n => voronoiTexture.GetPixel(point.x, point.y) == Color.black);

            if (clearNeighbors > maxClearNeighbors && blackNeighbors < minBlackNeighbors)
            {
                bestPoint = point;
                maxClearNeighbors = clearNeighbors;
                minBlackNeighbors = blackNeighbors;
            }
        }

        return bestPoint;
    }

    private List<Vector2Int> SortPoints(List<Vector2Int> points)
    {
        return points.OrderBy(point => Mathf.Atan2(point.y - cityCenterInt.y, point.x - cityCenterInt.x)).ToList();
    }

    public List<Vector2Int> MarkSegments(Vector2Int startPoint, int segmentLength)
    {

        List<Border> borderList = new List<Border>();
        List<BorderToTrace> bordersToTrace = new List<BorderToTrace>();


        List<Vector2Int> segmentMarks = new List<Vector2Int>();

        List<Vector2Int> nextPoints = new List<Vector2Int>();

        List<Vector2Int> foundSplitMarks = new List<Vector2Int>(); // remove later ?

        nextPoints.Add(startPoint);
        float startPointDistance = Vector2Int.Distance(startPoint, cityCenterInt);

        Vector2Int nextPoint = Vector2Int.zero;

        foreach (Vector2Int neighbor in GetNeighbors(startPoint))
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
                foreach (BorderToTrace foundBorder in newBordersToTrace)
                {
                    if (foundBorder.nextPoint == Vector2Int.zero)
                    {
                        newBordersToTrace.Remove(foundBorder);
                    }
                }

                Border newBorder = new Border
                {
                    startPoint = currentBorder.startPoint,
                    segments = segmentPoints,
                    endPoint = newBordersToTrace[0].startPoint,
                };
                borderList.Add(newBorder);

                bordersToTrace.AddRange(newBordersToTrace);
            }
        }

        return foundSplitMarks;
    }

    public (List<BorderToTrace>, List<Vector2Int>) TraceBorder(BorderToTrace borderToTrace, int segmentLength)
    {
        bool noSplitMarkFound = true;

        List<BorderToTrace> bordersToTrace = new List<BorderToTrace>();

        List<Vector2Int> segmentPoints = new List<Vector2Int>();

        List<Vector2Int> nextPoints = new List<Vector2Int>();

        Vector2Int splitMark = Vector2Int.zero;

        nextPoints.Add(borderToTrace.nextPoint);

        int segmentIndex = 0;

        while (noSplitMarkFound && nextPoints.Count > 0)
        {
            Vector2Int current = nextPoints[0];
            nextPoints.RemoveAt(0);

            if (boundaryPoints.Contains(current) && current != borderToTrace.startPoint)
            {
                splitMark = current;
                noSplitMarkFound = false;

            } else if (current == borderToTrace.startPoint || !visited.Contains(current))
            {
                if (!visited.Contains(current))
                {
                    visited.Add(current);
                }
                List<Vector2Int> remainingNeighbors = new List<Vector2Int>();

                foreach (Vector2Int neighbor in GetNeighbors(current)) 
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

        return (bordersToTrace, segmentPoints);
    }

    public List<Vector2Int> FindNextBorderPoints(Vector2Int startPoint)
    {
        List<Vector2Int> nextBorderPoints = new List<Vector2Int>();
        List<Vector2Int> remainingNeighbors = new List<Vector2Int>();

        foreach (Vector2Int neighbor in GetNeighbors(startPoint))
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
        foreach (Vector2Int neighbor in GetNeighbors(remainingNeighbor))
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
            Vector2Int[] neighbors = new Vector2Int[]
            {
                point + Vector2Int.up,
                point + Vector2Int.down,
                point + Vector2Int.left,
                point + Vector2Int.right,
            };

            foreach (Vector2Int neighbor in neighbors)
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

    List<Vector2Int> GetNeighbors(Vector2Int point)
    {
        return new List<Vector2Int>
        {
            new Vector2Int(point.x, point.y + 1),
            new Vector2Int(point.x + 1, point.y),
            new Vector2Int(point.x, point.y - 1),
            new Vector2Int(point.x - 1, point.y),

            new Vector2Int(point.x - 1, point.y + 1),
            new Vector2Int(point.x + 1, point.y + 1),
            new Vector2Int(point.x + 1, point.y - 1),
            new Vector2Int(point.x - 1, point.y - 1),
        };
    }

    public void AddRoad()
    {

    }

    private void OnDrawGizmos()
    {
        if (boundaryPoints != null)
        {
            Gizmos.color = Color.blue;
            foreach (Vector2Int point in boundaryPoints)
            {
                Gizmos.DrawSphere(new Vector3(point.x+ 0.5f, 1, point.y + 0.5f), 7f);
            }
        }

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
    public Vector2Int nextPoint;
}
