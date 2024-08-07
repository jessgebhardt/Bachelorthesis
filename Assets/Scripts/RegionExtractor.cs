using System.Collections.Generic;
using UnityEngine;

public class RegionExtractor : MonoBehaviour
{
    /// <summary>
    /// Extracts regions from the given texture.
    /// </summary>
    /// <param name="texture">The texture to extract regions from.</param>
    /// <param name="inset">The inset to avoid regions near the borders.</param>
    /// <returns>A list of regions, where each region is represented by a list of Vector2Int points.</returns>
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
                if (IsUnvisitedRegionStart(texture, x, y, visited))
                {
                    List<Vector2Int> region = FindRegionAreas(texture, x, y, visited, inset);

                    if (region.Count > 0)
                    {
                        regions.Add(region);
                    }
                }
            }
        }

        return regions;
    }

    /// <summary>
    /// Determines if the current pixel is the start of an unvisited region.
    /// </summary>
    /// <param name="texture">The texture being examined.</param>
    /// <param name="x">The x-coordinate of the pixel.</param>
    /// <param name="y">The y-coordinate of the pixel.</param>
    /// <param name="visited">A 2D array indicating which pixels have been visited.</param>
    /// <returns>True if the pixel is the start of an unvisited region, otherwise false.</returns>
    private static bool IsUnvisitedRegionStart(Texture2D texture, int x, int y, bool[,] visited)
    {
        Color pixelColor = texture.GetPixel(x, y);
        return !visited[x, y] && pixelColor != Color.black && pixelColor != Color.clear;
    }

    /// <summary>
    /// Finds and returns all the points in a region starting from the given point.
    /// </summary>
    /// <param name="texture">The texture being examined.</param>
    /// <param name="startX">The x-coordinate of the starting point.</param>
    /// <param name="startY">The y-coordinate of the starting point.</param>
    /// <param name="visited">A 2D array indicating which pixels have been visited.</param>
    /// <param name="inset">The inset to avoid regions near the borders.</param>
    /// <returns>A list of points in the region.</returns>
    private static List<Vector2Int> FindRegionAreas(Texture2D texture, int startX, int startY, bool[,] visited, int inset)
    {
        int width = texture.width;
        int height = texture.height;
        Color targetColor = texture.GetPixel(startX, startY);

        List<Vector2Int> region = new List<Vector2Int>();
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        stack.Push(new Vector2Int(startX, startY));

        while (stack.Count > 0)
        {
            Vector2Int point = stack.Pop();
            int x = point.x;
            int y = point.y;

            if (IsOutOfBounds(x, y, width, height) || visited[x, y])
                continue;

            Color currentColor = texture.GetPixel(x, y);
            if (!IsSameRegion(currentColor, targetColor) || IsNearBlackBorder(texture, x, y, inset))
            {
                visited[x, y] = true;
                continue;
            }

            visited[x, y] = true;
            region.Add(point);

            AddNeighborsToStack(stack, x, y);
        }

        return region;
    }

    /// <summary>
    /// Checks if the coordinates are out of bounds.
    /// </summary>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <param name="width">The width of the texture.</param>
    /// <param name="height">The height of the texture.</param>
    /// <returns>True if the coordinates are out of bounds, otherwise false.</returns>
    private static bool IsOutOfBounds(int x, int y, int width, int height)
    {
        return x < 0 || x >= width || y < 0 || y >= height;
    }

    /// <summary>
    /// Determines if the current color is the same as the target region color.
    /// </summary>
    /// <param name="currentColor">The current color.</param>
    /// <param name="targetColor">The target color.</param>
    /// <returns>True if the colors are the same, otherwise false.</returns>
    private static bool IsSameRegion(Color currentColor, Color targetColor)
    {
        return currentColor == targetColor && currentColor != Color.black && currentColor != Color.clear;
    }

    /// <summary>
    /// Adds the neighboring points to the stack for further processing.
    /// </summary>
    /// <param name="stack">The stack of points to process.</param>
    /// <param name="x">The x-coordinate of the current point.</param>
    /// <param name="y">The y-coordinate of the current point.</param>
    private static void AddNeighborsToStack(Stack<Vector2Int> stack, int x, int y)
    {
        stack.Push(new Vector2Int(x + 1, y));
        stack.Push(new Vector2Int(x - 1, y));
        stack.Push(new Vector2Int(x, y + 1));
        stack.Push(new Vector2Int(x, y - 1));
    }

    /// <summary>
    /// Checks if the point is near a black border within the given inset.
    /// </summary>
    /// <param name="texture">The texture being examined.</param>
    /// <param name="x">The x-coordinate of the point.</param>
    /// <param name="y">The y-coordinate of the point.</param>
    /// <param name="inset">The inset distance to check.</param>
    /// <returns>True if the point is near a black border, otherwise false.</returns>
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

                if (IsOutOfBounds(checkX, checkY, width, height))
                    continue;

                if (texture.GetPixel(checkX, checkY) == Color.black)
                    return true;
            }
        }
        return false;
    }
}
