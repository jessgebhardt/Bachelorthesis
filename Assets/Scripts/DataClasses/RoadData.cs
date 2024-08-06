using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoadData
{
    [HideInInspector] public Dictionary<int, List<List<Vector2Int>>> regionLots;
    [Min(0)] public int segmentLength = 50; //roadSegmentLength
    [Min(0)] public int roadWidth = 3;
}
