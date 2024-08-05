using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DistrictsData
{
    [Range(0, 1)] public float importanceOfNeighbours = 0.4f;
    [Range(0, 1)] public float importanceOfCityCenterDistance = 0.3f;

    public List<DistrictType> districtTypes = new List<DistrictType>();
    public List<District> generatedDistricts = new List<District>();
    [Min(0)] public int numberOfDistricts;
    [NonSerialized] public int minNumberOfDistricts;
    [NonSerialized] public int maxNumberOfDistricts;
    [NonSerialized] public IDictionary<int, District> districtsDictionary;
    [NonSerialized] public int counter = -1; //war vorher static
    [NonSerialized] public bool relationsAndIDsInitialized = false; //war vorher static
}
