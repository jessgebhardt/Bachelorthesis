using System.Collections.Generic;
using UnityEngine;
using RoadArchitect;
using RoadArchitect.Roads;
using System;

public class RoadGenerator : MonoBehaviour
{
    public static void GenerateRoad(List<Vector2Int> boundaryPositions, List<Border> borderList)
    {

        // Delete RoadArchitectSystem
        RoadSystem oldRoadSystem = FindObjectOfType<RoadSystem>();
        if (oldRoadSystem != null)
        {
            DestroyImmediate(oldRoadSystem.gameObject);
        }

        // Add new RoadArchitectSystem
        EditorMenu.CreateRoadSystem();

        // Find the RoadArchitectSystem in your scene
        RoadSystem roadSystem = FindObjectOfType<RoadSystem>();

        Road[] oldRoads = FindObjectsOfType<Road>();
        if (oldRoads != null)
        {
            foreach (var oldRoad in oldRoads)
            {
                DestroyImmediate(oldRoad.gameObject);
            }
        }

        // Ensure road updates are disabled initially
        roadSystem.isAllowingRoadUpdates = false;

        GenerateCityBorderRoad(roadSystem, boundaryPositions);

        GenerateDistrictBorderRoads(roadSystem, borderList);

        // Enable road updates and update the road system
        roadSystem.isAllowingRoadUpdates = true;
        roadSystem.UpdateAllRoads();

    }

    private static void GenerateCityBorderRoad(RoadSystem roadSystem, List<Vector2Int> positions)
    {
        // Convert Vector2Int positions to Vector3

        List<Vector3> vector3Positions = new List<Vector3>()
        {
            new Vector3(positions[0].x, 0.1f, positions[0].y)
        };
    
        foreach (var pos in positions)
        {
            vector3Positions.Add(new Vector3(pos.x, 0.1f, pos.y));
        }

        // Create the road programmatically
        Road road = RoadAutomation.CreateRoadProgrammatically(roadSystem, ref vector3Positions);

        // Connect the last node to the first to form a loop
        RoadAutomation.CreateNodeProgrammatically(road, vector3Positions[0]);
    }

    private static void GenerateDistrictBorderRoads(RoadSystem roadSystem, List<Border> borderList)
    {
        Debug.Log("Borderlist count: "+borderList.Count);
        int index = 0;
        foreach (var border in borderList) 
        {
            index++;
            List<Vector3> vector3Positions = new List<Vector3>()
            {
                new Vector3(border.startPoint.x, 0.1f, border.startPoint.y)
            };

            foreach (var pos in border.segments)
            {
                vector3Positions.Add(new Vector3(pos.x, 0.1f, pos.y));
            }

            vector3Positions.Add(new Vector3(border.endPoint.x, 0.1f, border.endPoint.y));

            Debug.Log("VECTOR3: "+ vector3Positions.Count);

            // Create the road programmatically
            RoadAutomation.CreateRoadProgrammatically(roadSystem, ref vector3Positions);
        }
        Debug.Log("index"+index);
    }
}