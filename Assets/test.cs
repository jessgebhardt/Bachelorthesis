using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    public float radius = 100;
    public Vector3 regionSize = new Vector3(1000, 1, 1000);
    public int rejectionSamples = 30;
    public float displayRadius = 10;

    List<Vector3> points;

    private void OnValidate()
    {
        points = PoissonDiskSampling.GenerateCandiatePoints(radius, regionSize, rejectionSamples);
        Debug.Log(points.Count);
    }

    private void OnDrawGizmos()
    {
        if (points != null)
        {
            foreach (Vector3 point in points)
            {
                Gizmos.DrawSphere(point, displayRadius);
            }
        }
    }
}
