using System;
using UnityEngine;

[System.Serializable]
public class BoundariesData
{
    [HideInInspector] public Vector3 center;
    [Range(10, 1490)] public float outerBoundaryRadius = 450f;
    public int segments = 500;
    public Color color = Color.red;
    [HideInInspector] public LineRenderer lineRenderer;
}
