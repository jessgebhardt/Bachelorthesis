using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static VoronoiDiagram;

[System.Serializable]
public class VoronoiData
{
    public GameObject voronoiDiagram;
    [Min(0)] public int distictCellDistortion;
    [NonSerialized] public Texture2D voronoiTexture;
    [NonSerialized] public Vector2Int[] districtPoints;
    [NonSerialized] public Dictionary<int, List<Vector2Int>> regionCorners = new Dictionary<int, List<Vector2Int>>();
    [NonSerialized] public List<Vector2Int> sortedVectors = new List<Vector2Int>();
    [NonSerialized] public Vector2Int[] allPoints;
    [NonSerialized] public Color[] allPointColors;
    [NonSerialized] public Vector2Int cityCenter = new Vector2Int(0, 0);
    [NonSerialized] public float cityRadius = 1;
    [NonSerialized] public Dictionary<int, Region> regions;
}
