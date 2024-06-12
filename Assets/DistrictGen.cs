using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistrictGen : MonoBehaviour
{
    [SerializeField] private int numberOfDistricts = 20;
    private float cityBoundaryRadius = 450f; // exchange with the one of citybound script
    private Vector3 cityCenter = new Vector3(500, 1, 500); // exchange with the one of citybound script
    private List<Vector3> candidateLocations = new List<Vector3>();
    public GameObject districtCandidateLocation;

    // Start is called before the first frame update
    void Start()
    {
        GenerateDistrictCandidates();
        PlaceDisticts(candidateLocations);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void GenerateDistrictCandidates()
    {
        int candidates = numberOfDistricts * 2;
        for (int i = 0; i < candidates; i++)
        {
            Vector3 randomPos = new Vector3(Random.Range(-cityBoundaryRadius, cityBoundaryRadius), 1, Random.Range(-cityBoundaryRadius, cityBoundaryRadius));
            Vector3 candidatePos = cityCenter + randomPos;
            if (randomPos.magnitude <= cityBoundaryRadius)
            {
                Debug.Log(candidatePos);
                candidateLocations.Add(candidatePos);
            }
        }
    }

    void PlaceDisticts(List<Vector3> locations)
    {
        foreach (Vector3 location in locations)
        {
            Instantiate(districtCandidateLocation, location, Quaternion.identity);
        }
    }
}
