using System.Collections.Generic;
using UnityEngine;

public class LSystem : MonoBehaviour
{
    public static Texture2D GenerateLSystem(string axiom, int iterations, float angle, float segmentLength, Texture2D texture, List<Vector2Int> region, Vector2Int[] startPositons)
    {
        Texture2D test = texture; 
        foreach (Vector2Int startPosition in startPositons)
        {
            string currentLSystem = axiom;

            for (int i = 0; i < iterations; i++)
            {
                string newLSystem = "";

                foreach (char c in currentLSystem)
                {
                    newLSystem += ApplyRules(c);
                }

                currentLSystem = newLSystem;
            }
            DrawRoads(currentLSystem, texture, segmentLength, angle, region, startPosition);
        }

        return texture;
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

    private static void DrawRoads(string currentLSystem, Texture2D texture, float segmentLength, float angle, List<Vector2Int> region, Vector2 start)
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
                    DrawLine(texture, position, newPosition, region);
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

    private static void DrawLine(Texture2D texture, Vector2 start, Vector2 end, List<Vector2Int> region)
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

        HashSet<Vector2Int> regionSet = new HashSet<Vector2Int>(region);

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
