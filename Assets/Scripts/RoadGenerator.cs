using System.Collections.Generic;
using UnityEngine;
using RoadArchitect;
using RoadArchitect.Roads;

public class RoadGenerator : MonoBehaviour
{
    public static void GenerateRoad(List<Border> borderList)
    {
        List<Road> newRoads = new List<Road>();

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

        // Delete existing roads
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

        // Generate roads
        newRoads.AddRange(GenerateDistrictBorderRoads(roadSystem, borderList));

        // Not working yet because of missing vertecies..
        //foreach (Road road in newRoads) 
        //{
        //    RoadAutomation.CreateIntersectionsProgrammaticallyForRoad(road);
        //}

        // Enable road updates and update the road system
        roadSystem.isAllowingRoadUpdates = true;
        roadSystem.UpdateAllRoads();
    }

    private static List<Road> GenerateDistrictBorderRoads(RoadSystem roadSystem, List<Border> borderList)
    {
        List<Road> newRoads = new List<Road>();
        foreach (var border in borderList)
        {
            List<Vector3> vector3Positions = new List<Vector3>
            {
                new Vector3(border.startPoint.x, 0.1f, border.startPoint.y)
            };

            if (border.segments.Count != 0)
            {
                foreach (var pos in border.segments)
                {
                    vector3Positions.Add(new Vector3(pos.x, 0.1f, pos.y));
                }
            } else
            {
                vector3Positions.Add(FindMidpoint(new Vector3(border.startPoint.x, 0.1f, border.startPoint.y), new Vector3(border.endPoint.x, 0.1f, border.endPoint.y)));
            }

            vector3Positions.Add(new Vector3(border.endPoint.x, 0.1f, border.endPoint.y));

            // Create the road programmatically
            Road road = RoadAutomation.CreateRoadProgrammatically(roadSystem, ref vector3Positions);
            newRoads.Add(road);
        }
        return newRoads;
    }

    public static Vector3 FindMidpoint(Vector3 pointA, Vector3 pointB)
    {
        return new Vector3(
            (pointA.x + pointB.x) / 2,
            (pointA.y + pointB.y) / 2,
            (pointA.z + pointB.z) / 2
        );
    }
}
