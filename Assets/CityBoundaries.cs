using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CityBoundaries : MonoBehaviour
{
    [SerializeField] private float outerBoundaryRadius = 100f;
    [SerializeField] private float innerBoundaryRadius;
    [SerializeField] private int segments = 100;

    private LineRenderer lineRenderer;

    // Start is called before the first frame update
    void Start()
    {
        innerBoundaryRadius = outerBoundaryRadius / 3;
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        InitializeLineRenderer(lineRenderer, Color.red);
        UpdateBoundaries();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateBoundaries();
    }

    // 
    void InitializeLineRenderer(LineRenderer lineRenderer, Color color)
    {
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.widthMultiplier = 0.1f;
        lineRenderer.positionCount = (segments + 1) * 2;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }

    // 
    void UpdateBoundaries()
    {
        Vector3 centerPosition = transform.position;
        float angle = 2 * Mathf.PI / segments;
        for (int i = 0; i <= segments; i++)
        {
            float outerX = Mathf.Sin(i * angle) * outerBoundaryRadius;
            float outerZ = Mathf.Cos(i * angle) * outerBoundaryRadius;
            float innerX = Mathf.Sin(i * angle) * innerBoundaryRadius;
            float innerZ = Mathf.Cos(i * angle) * innerBoundaryRadius;

            lineRenderer.SetPosition(i, new Vector3(outerX, 0, outerZ) + centerPosition);
            lineRenderer.SetPosition(i + segments + 1, new Vector3(innerX, 0, innerZ) + centerPosition);
        }
    }
}
