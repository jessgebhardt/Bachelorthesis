using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;

[RequireComponent(typeof(MeshFilter))]
public class BuildingGenerator : MonoBehaviour
{
    public int iterations = 5;
    public float heightIncrement = 2.0f;

    Mesh mesh;

    List<Vector3> vertices;
    List<int> triangles;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        CreateCube();

        UpdateMesh();
    }

    void CreateCube()
    {
        vertices = new List<Vector3>
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 1),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 1),

            new Vector3(0, 1, 0),
            new Vector3(0, 1, 1),
            new Vector3(1, 1, 0),
            new Vector3(1, 1, 1),
        };

        triangles = new List<int>
        {
            0, 1, 2,
            1, 3, 2,

            4, 5, 6,
            5, 7, 6,

            0, 4, 2,
            4, 6, 2,

            2, 6, 3,
            6, 7, 3,

            7, 1, 3,
            1, 5, 7,

            0, 4, 1,
            4, 5, 1
        };

    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }



}
