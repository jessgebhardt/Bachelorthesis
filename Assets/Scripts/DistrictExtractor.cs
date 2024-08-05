using System.Collections.Generic;
using UnityEngine;

public class DistrictExtractor : MonoBehaviour
{
    public static List<List<Vector2Int>> ExtractRegions(Texture2D texture, int inset)
    {
        int width = texture.width;
        int height = texture.height;
        bool[,] visited = new bool[width, height];
        List<List<Vector2Int>> regions = new List<List<Vector2Int>>();

        for (int y = inset; y < height; y++)
        {
            for (int x = inset; x < width; x++)
            {
                if (!visited[x, y] && texture.GetPixel(x, y) != Color.black && texture.GetPixel(x, y) != Color.clear)
                {
                    List<Vector2Int> region = new List<Vector2Int>();
                    FindRegionAreas(texture, x, y, visited, region, inset);

                    if (region.Count > 0)
                    {
                        regions.Add(region);
                    }
                }
            }
        }

        return regions;
    }

    private static void FindRegionAreas(Texture2D texture, int startX, int startY, bool[,] visited, List<Vector2Int> region, int inset)
    {
        int width = texture.width;
        int height = texture.height;
        Color targetColor = texture.GetPixel(startX, startY);

        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        stack.Push(new Vector2Int(startX, startY));

        while (stack.Count > 0)
        {
            Vector2Int point = stack.Pop();
            int x = point.x;
            int y = point.y;

            if (x < 0 || x >= width || y < 0 || y >= height || visited[x, y])
                continue;

            Color currentColor = texture.GetPixel(x, y);
            if (currentColor != targetColor || currentColor == Color.black || currentColor == Color.clear)
                continue;

            if (IsNearBlackBorder(texture, x, y, inset))
            {
                visited[x, y] = true; // Mark near border as visited
                continue;
            }

            visited[x, y] = true;
            region.Add(point);

            stack.Push(new Vector2Int(x + 1, y));
            stack.Push(new Vector2Int(x - 1, y));
            stack.Push(new Vector2Int(x, y + 1));
            stack.Push(new Vector2Int(x, y - 1));
        }
    }

    private static bool IsNearBlackBorder(Texture2D texture, int x, int y, int inset)
    {
        int width = texture.width;
        int height = texture.height;

        for (int i = -inset; i <= inset; i++)
        {
            for (int j = -inset; j <= inset; j++)
            {
                int checkX = x + i;
                int checkY = y + j;

                if (checkX < 0 || checkX >= width || checkY < 0 || checkY >= height)
                    continue;

                if (texture.GetPixel(checkX, checkY) == Color.black)
                    return true;
            }
        }
        return false;
    }
}
