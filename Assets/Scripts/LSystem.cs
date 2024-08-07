using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class LSystem : MonoBehaviour
{
    /// <summary>
    /// Generates an L-System based road network.
    /// </summary>
    /// <param name="axiom">The axiom or initial string of the L-System.</param>
    /// <param name="angle">The angle to turn for each '+' or '-' character.</param>
    /// <param name="segmentLength">The length of each segment in the L-System.</param>
    /// <param name="region">The list of region points.</param>
    /// <param name="startPosition">The starting position for the L-System generation.</param>
    /// <returns>A list of Vector2Int points representing the road network.</returns>
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
            currentLSystem = ApplyRules(currentLSystem, ruleCache);
        }

        return DrawRoads(currentLSystem, segmentLength, angle, regionSet, startPosition);
    }

    /// <summary>
    /// Calculates the number of iterations needed based on the region size and segment length.
    /// </summary>
    /// <param name="axiom">The initial string of the L-System.</param>
    /// <param name="regionSize">The size of the region.</param>
    /// <param name="segmentLength">The length of each segment in the L-System.</param>
    /// <returns>The number of iterations.</returns>
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

    /// <summary>
    /// Applies the L-System rules to the current string.
    /// </summary>
    /// <param name="currentLSystem">The current L-System string.</param>
    /// <param name="ruleCache">The dictionary containing the rules.</param>
    /// <returns>The new L-System string after applying the rules.</returns>
    private static string ApplyRules(string currentLSystem, Dictionary<char, string> ruleCache)
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

        return newLSystem.ToString();
    }

    /// <summary>
    /// Draws the road network based on the L-System string.
    /// </summary>
    /// <param name="currentLSystem">The current L-System string.</param>
    /// <param name="segmentLength">The length of each segment in the L-System.</param>
    /// <param name="angle">The angle to turn for each '+' or '-' character.</param>
    /// <param name="regionSet">The set of points representing the region.</param>
    /// <param name="start">The starting position for the L-System generation.</param>
    /// <returns>A list of Vector2Int points representing the road network.</returns>
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
                    Vector2Int direction = GetDirection(currentAngle, angleCache);
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

    /// <summary>
    /// Gets the direction vector based on the current angle.
    /// </summary>
    /// <param name="currentAngle">The current angle.</param>
    /// <param name="angleCache">The cache of precomputed direction vectors.</param>
    /// <returns>The direction vector.</returns>
    private static Vector2Int GetDirection(float currentAngle, Dictionary<float, Vector2Int> angleCache)
    {
        if (!angleCache.TryGetValue(currentAngle, out Vector2Int direction))
        {
            direction = new Vector2Int((int)Mathf.Cos(currentAngle * Mathf.Deg2Rad), (int)Mathf.Sin(currentAngle * Mathf.Deg2Rad));
            angleCache[currentAngle] = direction;
        }

        return direction;
    }

    /// <summary>
    /// Gets the pixels for a line between two points using Bresenham's line algorithm.
    /// </summary>
    /// <param name="start">The starting point.</param>
    /// <param name="end">The ending point.</param>
    /// <param name="regionSet">The set of points representing the region.</param>
    /// <returns>A list of Vector2Int points representing the line.</returns>
    private static List<Vector2Int> GetLinePixels(Vector2Int start, Vector2Int end, HashSet<Vector2Int> regionSet)
    {
        List<Vector2Int> pixels = new List<Vector2Int>();
        int x0 = start.x;
        int y0 = start.y;
        int x1 = end.x;
        int y1 = end.y;

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
