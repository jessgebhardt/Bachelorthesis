using System;
using System.Collections.Generic;
using UnityEngine;
using static VoronoiDiagram; //add new

[System.Serializable]
public class VoronoiData
{
    [Min(0)] public int distictCellDistortion;
    public GameObject voronoiDiagram;
    [HideInInspector] public Texture2D voronoiTexture;
    [HideInInspector] public Dictionary<int, Region> regions;
    [HideInInspector] public Material voronoiMaterial;
}
