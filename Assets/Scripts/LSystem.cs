using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class LSystem : MonoBehaviour
{
    public static List<Vector2Int> GenerateLSystem(string axiom, float angle, int segmentLength, List<Vector2Int> region, Vector2Int startPosition)
    {
        HashSet<Vector2Int> regionSet = new HashSet<Vector2Int>(region);

        string currentLSystem = axiom;
        int iterations = CalculateIterations(axiom, regionSet.Count, segmentLength);

        Dictionary<char, string> ruleCache = new Dictionary<char, string>
        {
            { 'A', "A+B++B-A--AA-B+" },
            { 'B', "-A+BB++B+A--A-B" }
        };

        for (int i = 0; i < iterations; i++)
        {
            StringBuilder newLSystem = new StringBuilder();

            foreach (char c in currentLSystem)
            {
                if (ruleCache.TryGetValue(c, out string rule))
                {
                    newLSystem.Append(rule);
                }
                else
                {
                    newLSystem.Append(c);
                }
            }

            currentLSystem = newLSystem.ToString();
        }

        return DrawRoads(currentLSystem, segmentLength, angle, regionSet, startPosition);
    }

    private static int CalculateIterations(string axiom, int regionSize, float segmentLength)
    {
        int iterations = 0;
        int currentLength = axiom.Length;

        while (currentLength * segmentLength < regionSize)
        {
            iterations++;
            currentLength *= 4;
        }

        return iterations + 2;
    }

    private static List<Vector2Int> DrawRoads(string currentLSystem, int segmentLength, float angle, HashSet<Vector2Int> regionSet, Vector2Int start)
    {
        Vector2Int position = start;
        float currentAngle = 0f;

        Dictionary<float, Vector2Int> angleCache = new Dictionary<float, Vector2Int>();
        List<Vector2Int> pixelsToDraw = new List<Vector2Int>();

        foreach (char c in currentLSystem)
        {
            switch (c)
            {
                case 'A':
                case 'B':
                    Vector2Int direction;
                    if (!angleCache.TryGetValue(currentAngle, out direction))
                    {
                        direction = new Vector2Int((int)Mathf.Cos(currentAngle * Mathf.Deg2Rad), (int)Mathf.Sin(currentAngle * Mathf.Deg2Rad));
                        angleCache[currentAngle] = direction;
                    }
                    Vector2Int newPosition = position + direction * segmentLength;
                    pixelsToDraw.AddRange(GetLinePixels(position, newPosition, regionSet));
                    position = newPosition;
                    break;
                case '+':
                    currentAngle += angle;
                    break;
                case '-':
                    currentAngle -= angle;
                    break;
            }
        }
        return pixelsToDraw;
    }

    private static List<Vector2Int> GetLinePixels(Vector2Int start, Vector2Int end, HashSet<Vector2Int> regionSet)
    {
        List<Vector2Int> pixels = new List<Vector2Int>();
        int x0 = (int)start.x;
        int y0 = (int)start.y;
        int x1 = (int)end.x;
        int y1 = (int)end.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            Vector2Int pixelPos = new Vector2Int(x0, y0);
            if (regionSet.Contains(pixelPos))
            {
                pixels.Add(pixelPos);
            }

            if (x0 == x1 && y0 == y1) break;
            int e2 = err * 2;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
        return pixels;
    }
}
