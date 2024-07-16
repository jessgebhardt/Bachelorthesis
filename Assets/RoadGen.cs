using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadGen : MonoBehaviour
{
    private Texture2D voronoiTexture;
    private List<Vector2> roadSegmentPoints = new List<Vector2>();
    private List<Vector2Int> splitMarks = new List<Vector2Int>();
    private Vector2Int startPoint;
    private List<Vector2Int> example4 = new List<Vector2Int>();
    private List<Vector2Int> example3 = new List<Vector2Int>();
    private List<Vector2Int> example2 = new List<Vector2Int>();

    public void GenerateRoad(Texture2D texture, float segmentLength)
    {
        voronoiTexture = texture;
        startPoint = FindStartPoint();
        splitMarks.Clear();
        splitMarks = MarkSegments(startPoint, segmentLength);
        // AddRoad();
    }

    public Vector2Int FindStartPoint()
    {
        for (int y = 0; y < voronoiTexture.height; y++)
        {
            for (int x = 0; x < voronoiTexture.width; x++)
            {
                if (voronoiTexture.GetPixel(x, y) == Color.black)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return Vector2Int.zero;
    }

    public List<Vector2Int> MarkSegments(Vector2Int startPoint, float segmentLength)
    {
        example4.Clear();
        example3.Clear();
        example2.Clear();

        List<Vector2Int> splitMarks = new List<Vector2Int>();
        List<Vector2Int> segmentMarks = new List<Vector2Int>();
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        stack.Push(startPoint);

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Pop();

            if (!visited.Contains(current))
            {
                visited.Add(current);
                List<Vector2Int> remainingNeighbors = new List<Vector2Int>();

                foreach (Vector2Int neighbor in GetNeighbors(current))
                {
                    if (IsBlackPixel(neighbor) && !visited.Contains(neighbor))
                    {
                        remainingNeighbors.Add(neighbor);
                        stack.Push(neighbor);
                    }
                }

                if (remainingNeighbors.Count >= 4) { splitMarks.Add(current); example4.Add(current); }
                else if (remainingNeighbors.Count == 3 && AreNeighbors(remainingNeighbors)) { splitMarks.Add(current); example3.Add(current); }
                else if (remainingNeighbors.Count == 2 && !AreNeighbors(remainingNeighbors)) { splitMarks.Add(current); example2.Add(current); }
            }
        }
        return splitMarks;
    }

    bool AreNeighbors(List<Vector2Int> points)
    {
        HashSet<Vector2Int> pointSet = new HashSet<Vector2Int>(points);

        foreach (Vector2Int point in points)
        {
            // Check the 4 potential neighbors
            Vector2Int[] neighbors = new Vector2Int[]
            {
                point + Vector2Int.up,
                point + Vector2Int.down,
                point + Vector2Int.left,
                point + Vector2Int.right,
                point + new Vector2Int(1, 1),
                point + new Vector2Int(1, -1),
                point + new Vector2Int(-1, 1),
                point + new Vector2Int(-1, -1)
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
            new Vector2Int(point.x - 1, point.y + 1),
            new Vector2Int(point.x, point.y + 1),
            new Vector2Int(point.x + 1, point.y + 1),
            new Vector2Int(point.x + 1, point.y),
            new Vector2Int(point.x + 1, point.y - 1),
            new Vector2Int(point.x, point.y - 1),
            new Vector2Int(point.x - 1, point.y - 1),
            new Vector2Int(point.x - 1, point.y),
        };
    }

    public void AddRoad()
    {

    }

    private void OnDrawGizmos()
    {
        if (example4 != null)
        {
            Gizmos.color = Color.red;
            foreach (Vector2Int split in example4)
            {
                Gizmos.DrawSphere(new Vector3(split.x + 0.5f, 1, split.y + 0.5f), 0.5f);
            }
        }

        if (example3 != null)
        {
            Gizmos.color = Color.yellow;
            foreach (Vector2Int split in example3)
            {
                Gizmos.DrawSphere(new Vector3(split.x + 0.5f, 1, split.y + 0.5f), 0.5f);
            }
        }

        if (example2 != null)
        {
            Gizmos.color = Color.blue;
            foreach (Vector2Int split in example2)
            {
                Gizmos.DrawSphere(new Vector3(split.x+ 0.5f, 1, split.y + 0.5f), 0.5f);
            }
        }

        //if (splitMarks != null)
        //{
        //    Gizmos.color = Color.red;
        //    foreach (Vector2Int split in splitMarks)
        //    {
        //        Gizmos.DrawSphere(new Vector3(split.x, 1, split.y), 5f);
        //    }
        //}
    }
}
