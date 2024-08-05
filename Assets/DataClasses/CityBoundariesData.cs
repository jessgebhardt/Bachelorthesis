using System;
using UnityEngine;

[System.Serializable]
public class CityBoundariesData
{
    [Range(10, 1490)] public float outerBoundaryRadius = 450f;
    public int segments = 500;
    [NonSerialized] public LineRenderer lineRenderer;
    public Color color = Color.red;
    [NonSerialized] public Vector3 center;
}
