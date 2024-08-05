using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PoissonDiskData
{
    [NonSerialized] public List<Vector3> candidatePoints;
    [Min(0)] public int rejectionSamples = 30;
}
