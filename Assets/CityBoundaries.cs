using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CityBoundaries : MonoBehaviour
{
    [Range(10, 490)] public float outerBoundaryRadius = 450f;
    [SerializeField, Range(3, 480)] private float innerBoundaryRadius;
    [SerializeField] private int segments = 500;

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
        lineRenderer.widthMultiplier = 2f;
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

    // 
    public List<Vector3> CheckWithinBoundaries(List<Vector3> points, string radiusType)
    {
        List<Vector3> pointsInRadius = new List<Vector3>();
        Vector3 center = transform.position;
        float radius = 0f;

        switch (radiusType)
        {
            case "inner":
                radius = innerBoundaryRadius;
                break;
            case "outer":
                radius = outerBoundaryRadius;
                break;
            default:
                Debug.Log("radiusType is not inner or outer");
                break;
        }

        if (radius != 0f)
        {
            foreach (Vector3 point in points)
            {
                float distance = Vector3.Distance(point, center);
                if (distance < radius)
                {
                    pointsInRadius.Add(point);
                }
            }
        }

        return pointsInRadius;
    }
}
