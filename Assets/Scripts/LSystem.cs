using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class LSystem : MonoBehaviour
{
    public static Texture2D GenerateLSystem(string axiom, float angle, float segmentLength, Texture2D texture, List<Vector2Int> region, Vector2Int startPosition)
    {
        Texture2D resultTexture = texture;
        HashSet<Vector2Int> regionSet = new HashSet<Vector2Int>(region);

        string currentLSystem = axiom;

        int iterations = CalculateIterations(axiom, regionSet.Count, segmentLength);

        for (int i = 0; i < iterations; i++)
        {
            StringBuilder newLSystem = new StringBuilder();

            foreach (char c in currentLSystem)
            {
                newLSystem.Append(ApplyRules(c));
            }

            currentLSystem = newLSystem.ToString();
        }

        DrawRoads(currentLSystem, resultTexture, segmentLength, angle, regionSet, startPosition);

        resultTexture.Apply();
        return resultTexture;
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

        return iterations;
    }

    private static string ApplyRules(char c)
    {
        switch (c)
        {
            case 'A': return "A+B++B-A--AA-B+";
            case 'B': return "-A+BB++B+A--A-B";
            default: return c.ToString();
        }
    }

    private static void DrawRoads(string currentLSystem, Texture2D texture, float segmentLength, float angle, HashSet<Vector2Int> regionSet, Vector2 start)
    {
        Vector2 position = start;
        float currentAngle = 0f;

        foreach (char c in currentLSystem)
        {
            switch (c)
            {
                case 'A':
                case 'B':
                    Vector2 newPosition = position + (new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad)) * segmentLength);
                    DrawLine(texture, position, newPosition, regionSet);
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
    }

    private static void DrawLine(Texture2D texture, Vector2 start, Vector2 end, HashSet<Vector2Int> regionSet)
    {
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
                texture.SetPixel(x0, y0, Color.black);
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
    }
}
