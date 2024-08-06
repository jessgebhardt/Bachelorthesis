using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoadData
{
    [HideInInspector] public Dictionary<int, List<List<Vector2Int>>> regionLots;
    [Min(0)] public int roadWidth = 3;

    [Header("L-System")]
    public string axiom = "A";
    public float angle = 90f;
    [Min(0)] public int segmentLength = 60;
}
