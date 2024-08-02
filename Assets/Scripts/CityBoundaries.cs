using System;
using UnityEngine;

[ExecuteInEditMode]
public class CityBoundaries : MonoBehaviour
{
    [Range(10, 1490)] public float outerBoundaryRadius = 450f;
    [SerializeField] private int segments = 500;

    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        InitializeLineRenderer(lineRenderer, Color.red);
        UpdateBoundaries();
    }

    void Update()
    {
        UpdateBoundaries();
    }

    void InitializeLineRenderer(LineRenderer lineRenderer, Color color)
    {
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.widthMultiplier = 2f;
        lineRenderer.positionCount = segments + 1;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.loop = true; // Makes sure the line renderer forms a closed loop
    }

    void UpdateBoundaries()
    {
        Vector3 centerPosition = transform.position;
        float angle = 2 * Mathf.PI / segments;
        for (int i = 0; i < segments; i++)
        {
            float outerX = Mathf.Sin(i * angle) * outerBoundaryRadius;
            float outerZ = Mathf.Cos(i * angle) * outerBoundaryRadius;

            lineRenderer.SetPosition(i, new Vector3(outerX, 0, outerZ) + centerPosition);
        }
        // Make sure the last position connects back to the first position
        lineRenderer.SetPosition(segments, lineRenderer.GetPosition(0));
    }
}
