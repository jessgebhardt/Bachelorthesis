using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PoissonData
{
    [Min(0)] public int rejectionSamples = 30;
    [HideInInspector] public List<Vector3> candidatePoints;
}
