using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DistrictData
{
    [Range(0, 1)] public float importanceOfNeighbours = 0.4f;
    [Range(0, 1)] public float importanceOfCityCenterDistance = 0.3f;

    public List<DistrictType> districtTypes = new List<DistrictType>();
    public List<District> generatedDistricts = new List<District>();
    [HideInInspector] public IDictionary<int, District> districtsDictionary;

    [Min(0)] public int numberOfDistricts;
    [HideInInspector] public int minNumberOfDistricts;
    [HideInInspector] public int maxNumberOfDistricts;
    
    [HideInInspector] public Dictionary<int, List<List<Vector2Int>>> regionLots;

    [HideInInspector] public bool initialized = false;
}
