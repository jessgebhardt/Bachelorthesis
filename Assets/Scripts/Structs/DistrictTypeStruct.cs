using System.Collections.Generic;
using System;
using UnityEngine;

[System.Serializable]
public struct DistrictType
{
    public string name;
    [HideInInspector] public int id;
    public Color color;
    [Range(0, 10)] public float distanceFromCenter;
    [Min(1)] public int minNumberOfPlacements;
    [Min(1)] public int maxNumberOfPlacements;
    public List<GameObject> buildingTypes;
    [Min(1)] public int minLotSizeSquared;
    public List<DistrictRelation> relations;
}