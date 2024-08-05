using UnityEngine;

public class CityBoundaries : MonoBehaviour
{
    public static void InitializeLineRenderer(CityBoundariesData boundariesData)
    {
        LineRenderer lineRenderer = boundariesData.lineRenderer;

        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.widthMultiplier = 2f;
        lineRenderer.positionCount = boundariesData.segments + 1;
        lineRenderer.startColor = boundariesData.color;
        lineRenderer.endColor = boundariesData.color;
        lineRenderer.loop = true;

        boundariesData.lineRenderer = lineRenderer;
    }

    public static void UpdateBoundaries(CityBoundariesData boundariesData)
    {
        Vector3 centerPosition = boundariesData.center;
        float angle = 2 * Mathf.PI / boundariesData.segments;
        for (int i = 0; i < boundariesData.segments; i++)
        {
            float outerX = Mathf.Sin(i * angle) * boundariesData.outerBoundaryRadius;
            float outerZ = Mathf.Cos(i * angle) * boundariesData.outerBoundaryRadius;

            boundariesData.lineRenderer.SetPosition(i, new Vector3(outerX, 0, outerZ) + centerPosition);
        }
        boundariesData.lineRenderer.SetPosition(boundariesData.segments, boundariesData.lineRenderer.GetPosition(0));
    }
}
