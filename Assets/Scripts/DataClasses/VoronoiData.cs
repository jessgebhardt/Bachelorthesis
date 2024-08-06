using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VoronoiData
{
    [Min(0)] public int distictCellDistortion;
    public GameObject voronoiDiagram;
    [HideInInspector] public Texture2D voronoiTexture;
    [HideInInspector] public Dictionary<int, Region> regions;
    [HideInInspector] public Material voronoiMaterial;
}
