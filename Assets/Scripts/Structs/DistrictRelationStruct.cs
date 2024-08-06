using System;
using UnityEngine;

[System.Serializable]
public struct DistrictRelation
{
    [NonSerialized] public int districtTypeId;

    [SerializeField, HideInInspector] public string _name;

    public string name
    {
        get { return _name; }
        private set { _name = value; }
    }

    [Range(0, 10)] public float attraction;
    [Range(0, 10)] public float repulsion;
}