using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class RoadGen : MonoBehaviour
{
    private Texture2D voronoiTexture;
    private List<Vector2> roadSegmentPoints = new List<Vector2>();
    private List<Vector2Int> splitMarks = new List<Vector2Int>();
    private Vector2Int startPoint;

    private HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
    private List<Vector2Int> boundaryPoints = new List<Vector2Int>();

    Vector2Int cityCenterInt;
    float cityRadius;

    public void GenerateRoad(Texture2D texture, float outerBoundaryRadius, Vector3 cityCenter ,float segmentLength)
    {
        voronoiTexture = texture;
        cityCenterInt = new Vector2Int(Mathf.RoundToInt(cityCenter.x), Mathf.RoundToInt(cityCenter.z));
        cityRadius = outerBoundaryRadius;

        GetBoundaryPoints();
        Debug.Log("BOUNDARYPOINTS: "+ boundaryPoints.Count);

        //for (int i = 0; i < boundaryPoints.Count; i++) 
        //{
        //    Debug.Log(i+". BOUNDARYPOINT: " + boundaryPoints[i]);
        //}

        startPoint = boundaryPoints[0];
        splitMarks.Clear();
        visited.Clear();

        splitMarks.AddRange(MarkSegments(startPoint, segmentLength));
        Debug.Log("SplitmarkANZAHL: " + splitMarks.Count);

        // AddRoad();
    }

    private void GetBoundaryPoints()
    {
        boundaryPoints.Clear();
        List<Vector2Int> points = new List<Vector2Int>();

        for (int y = 0; y < voronoiTexture.height; y++)
        {
            for (int x = 0; x < voronoiTexture.width; x++)
            {
                Vector2Int point = new Vector2Int(x, y);
                float distance = Vector2Int.Distance(point, cityCenterInt);

                if (Mathf.Abs(distance - cityRadius) <= 1f && IsBlackPixel(point))
                {
                    points.Add(point);
                }
            }
        }

        points = ReducePoints(points);
        boundaryPoints = SortPoints(points);
    }

    private List<Vector2Int> ReducePoints(List<Vector2Int> points)
    {
        HashSet<Vector2Int> visitedPoints = new HashSet<Vector2Int>();
        List<Vector2Int> reducedPoints = new List<Vector2Int>();

        foreach (Vector2Int point in points)
        {
            if (!visitedPoints.Contains(point))
            {
                reducedPoints.Add(point);
                visitedPoints.Add(point);

                foreach (Vector2Int neighbor in GetNeighbors(point))
                {
                    if (points.Contains(neighbor))
                    {
                        visitedPoints.Add(neighbor);
                    }
                }
            }
        }

        return reducedPoints;
    }

    private List<Vector2Int> SortPoints(List<Vector2Int> points)
    {
        return points.OrderBy(point => Mathf.Atan2(point.y - cityCenterInt.y, point.x - cityCenterInt.x)).ToList();
    }

    public List<Vector2Int> MarkSegments(Vector2Int startPoint, float segmentLength)
    {

        List<Border> borderList = new List<Border>();
        List<BorderToTrace> bordersToTrace = new List<BorderToTrace>();


        List<Vector2Int> segmentMarks = new List<Vector2Int>();

        List<Vector2Int> nextPoints = new List<Vector2Int>();
        visited.Clear();

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
        bordersToTrace.Add(newBorderToTrace);

        Debug.Log("STARTBORDER to trace: "+bordersToTrace[0].startPoint + "; " + bordersToTrace[0].nextPoint);
        Debug.Log("BORDERSTOTRACELENGTH: "+ bordersToTrace.Count);


        while (bordersToTrace.Count > 0)
        {
            BorderToTrace currentBorder = bordersToTrace[0];
            bordersToTrace.RemoveAt(0);

            List<BorderToTrace> foundBordersToTrace = new List<BorderToTrace>();

            // 6
            foundBordersToTrace.AddRange(TraceBorder(currentBorder));

            // 7
            if (foundBordersToTrace.Count > 0)
            {
                foreach (BorderToTrace foundBorder in foundBordersToTrace)
                {
                    //bool isBoundaryPoint = boundaryPoints.Contains(foundBorder.startPoint);
                    //if (isBoundaryPoint) 
                    //{
                    //    Border endBorder = new Border
                    //    {
                    //        startPoint = currentBorder.startPoint,
                    //        endPoint = foundBorder.startPoint,
                    //    };
                    //    borderList.Add(endBorder);
                    //}

                    if (foundBorder.nextPoint == Vector2Int.zero /*|| isBoundaryPoint*/)
                    {
                        foundBordersToTrace.Remove(foundBorder);
                    }

                    Debug.Log("FOUND border: " + foundBorder.startPoint + "; " + foundBorder.nextPoint);
                }

                //if (foundBordersToTrace.Count > 0)
                //{
                    // 7.1 & 8.1 & 9.1 save Border
                    Border newBorder = new Border
                    {
                        startPoint = currentBorder.startPoint,
                        endPoint = foundBordersToTrace[0].startPoint,
                    };
                    borderList.Add(newBorder);

                    bordersToTrace.AddRange(foundBordersToTrace);
                //}
            }
        }


        // TEST
        for (int i = 0; i < borderList.Count; i++)
        {
            Debug.Log(i + ": " + borderList[i].startPoint + "; " + borderList[i].endPoint);
        }


        return foundSplitMarks;
    }

    public List<BorderToTrace> TraceBorder(BorderToTrace borderToTrace)
    {
        bool noSplitMarkFound = true;

        List<BorderToTrace> bordersToTrace = new List<BorderToTrace>();

        List<Vector2Int> nextPoints = new List<Vector2Int>();

        Vector2Int splitMark = Vector2Int.zero;

        nextPoints.Add(borderToTrace.startPoint);

        while (noSplitMarkFound && nextPoints.Count > 0)
        {
            if (!noSplitMarkFound || nextPoints.Count == 0)
            {
                break;
            }

            Vector2Int current = nextPoints[0];
            nextPoints.RemoveAt(0);

            if (boundaryPoints.Contains(current) && current != borderToTrace.startPoint)
            {
                splitMark = current; // save end
                noSplitMarkFound = false;

            } else if (current == borderToTrace.startPoint || !visited.Contains(current))
            {
                if (!visited.Contains(current))
                {
                    visited.Add(current);
                }
                List<Vector2Int> remainingNeighbors = new List<Vector2Int>();

                foreach (Vector2Int neighbor in GetNeighbors(current)) // maybe potential problem in here
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

                if (current != borderToTrace.startPoint && (IsBorderSplit(remainingNeighbors) && HaveUnvisitedNeighbors(remainingNeighbors)) || remainingNeighbors.Count == 0)
                {
                    splitMark = current; // save end
                    noSplitMarkFound = false;
                }
            }
        }

        Debug.Log("SPLITMARK: "+ splitMark);
        Debug.Log("SPLITMARK FOUND?: " + !noSplitMarkFound);


        if (!noSplitMarkFound)
        {
            splitMarks.Add(splitMark);
            // Überprüfen wie viele borders von dem ende weiter führen und save nextpoint for each
            // nextpoint null machen, wenn rand
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

        Debug.Log("FOUND borders to trace: "+ bordersToTrace.Count);

        return bordersToTrace;
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
        if (remainingNeighbors.Count >= 4) // 3 & 4
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
        else if (remainingNeighbors.Count == 3 && FindPointWithoutNeighbor(remainingNeighbors) != null) // 2
        {
            Vector2Int? neighborlessPoint = FindPointWithoutNeighbor(remainingNeighbors);
            if (neighborlessPoint != null)
            {
                nextBorderPoints.Add((Vector2Int)neighborlessPoint);
                remainingNeighbors.Remove((Vector2Int)neighborlessPoint);
            }
            nextBorderPoints.Add(remainingNeighbors[0]);

        }
        else if (remainingNeighbors.Count == 2 && !AreNeighbors(remainingNeighbors)) // 1
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

        if (remainingNeighbors.Count == 3 && FindPointWithoutNeighbor(remainingNeighbors) != null)
        {
            Debug.Log("EXACTLY ONE NEIGHBOR: " + hasExactlyOneNeighbor);
        }

        if (remainingNeighbors.Count >= 4) { return true; }
        else if (remainingNeighbors.Count == 3 && FindPointWithoutNeighbor(remainingNeighbors) != null) { return true; }
        else if (remainingNeighbors.Count == 2 && !AreNeighbors(remainingNeighbors)) { return true; }
        return false;
    }


    /*Haben andere neighbors nichtvisited*/
    bool HaveUnvisitedNeighbors(List<Vector2Int> remainingNeighbors)
    {
        List<Vector2Int> haveUnvisitedNeighbors = new List<Vector2Int>();

        foreach (Vector2Int remaining in remainingNeighbors)
        {
            haveUnvisitedNeighbors.Add(remaining);
            foreach (Vector2Int neighbor in GetNeighbors(remaining))
            {
                if (IsBlackPixel(neighbor))
                {
                    if (!visited.Contains(neighbor) && !remainingNeighbors.Contains(neighbor))
                    {
                        haveUnvisitedNeighbors.Remove(remaining);
                    }
                }
            }
        }

        if (haveUnvisitedNeighbors.Count > 0)
        {
            visited.AddRange(haveUnvisitedNeighbors);
            return false;
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
                //point + new Vector2Int(1, 1),
                //point + new Vector2Int(1, -1),
                //point + new Vector2Int(-1, 1),
                //point + new Vector2Int(-1, -1)
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
    }
}

[System.Serializable]
public struct Border
{
    public Vector2Int startPoint;
    public Vector2Int endPoint;
}

[System.Serializable]
public struct BorderToTrace
{
    public Vector2Int startPoint;
    public Vector2Int nextPoint;
}
