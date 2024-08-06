using System;
using UnityEngine;

public class CityBoundaries : MonoBehaviour
{
    public static void InitializeBoundaries(BoundariesData boundariesData)
    {
        boundariesData.lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        boundariesData.lineRenderer.widthMultiplier = 2f;
        boundariesData.lineRenderer.positionCount = boundariesData.segments + 1;
        boundariesData.lineRenderer.startColor = boundariesData.color;
        boundariesData.lineRenderer.endColor = boundariesData.color;
        boundariesData.lineRenderer.loop = true;
    }

    public static void UpdateBoundaries(BoundariesData boundariesData)
    {
        int segments = boundariesData.segments;
        float outerBoundaryRadius = boundariesData.outerBoundaryRadius;

        float angle = 2 * Mathf.PI / segments;
        for (int i = 0; i < segments; i++)
        {
            float outerX = Mathf.Sin(i * angle) * outerBoundaryRadius;
            float outerZ = Mathf.Cos(i * angle) * outerBoundaryRadius;

            boundariesData.lineRenderer.SetPosition(i, new Vector3(outerX, 0, outerZ) + boundariesData.center);
        }
        boundariesData.lineRenderer.SetPosition(segments, boundariesData.lineRenderer.GetPosition(0));
    }
}
