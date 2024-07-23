using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistrictExtractor : MonoBehaviour
{
    public static List<List<Vector2Int>> ExtractDistrictsForRoads(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        bool[,] visited = new bool[width, height];
        List<List<Vector2Int>> regions = new List<List<Vector2Int>>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!visited[x, y] && texture.GetPixel(x, y) != Color.black && texture.GetPixel(x, y) != Color.clear)
                {
                    List<Vector2Int> region = new List<Vector2Int>();
                    FindRegionAreas(texture, x, y, visited, region);
                    regions.Add(region);
                }
            }
        }

        return regions;
    }

    private static void FindRegionAreas(Texture2D texture, int x, int y, bool[,] visited, List<Vector2Int> region)
    {
        int width = texture.width;
        int height = texture.height;
        Color targetColor = texture.GetPixel(x, y);

        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        stack.Push(new Vector2Int(x, y));

        while (stack.Count > 0)
        {
            Vector2Int point = stack.Pop();
            int px = point.x;
            int py = point.y;

            if (px < 0 || px >= width || py < 0 || py >= height || visited[px, py])
                continue;

            Color currentColor = texture.GetPixel(px, py);
            if (currentColor != targetColor || currentColor == Color.black || currentColor == Color.clear)
                continue;

            visited[px, py] = true;
            region.Add(point);

            stack.Push(new Vector2Int(px + 1, py));
            stack.Push(new Vector2Int(px - 1, py));
            stack.Push(new Vector2Int(px, py + 1));
            stack.Push(new Vector2Int(px, py - 1));
        }
    }
}
