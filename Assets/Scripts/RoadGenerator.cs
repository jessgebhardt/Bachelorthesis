using System.Collections.Generic;
using UnityEngine;
using RoadArchitect;
using RoadArchitect.Roads;

public class RoadGenerator : MonoBehaviour
{
    public static void GenerateRoad(List<Border> borderList)
    {
        List<Road> newRoads = new List<Road>();

        RemoveOldRoadsAndRoadSystems();

        // Add new RoadArchitectSystem
        // EditorMenu.CreateRoadSystem();
        GameObject roadSystemObject = new GameObject("RoadArchitectSystem");
        RoadSystem roadSystem = roadSystemObject.AddComponent<RoadSystem>();

        // Find the RoadArchitectSystem in your scene
        // RoadSystem roadSystem = FindObjectOfType<RoadSystem>();

        // Ensure road updates are disabled initially
        roadSystem.isAllowingRoadUpdates = false;


        RoadArchitectUnitTest2(roadSystem);

        // Generate roads
        // newRoads.AddRange(GenerateDistrictBorderRoads(roadSystem, borderList));

        // Not working yet because of missing vertecies..
        //foreach (Road road in newRoads)
        //{
        // RoadAutomation.CreateIntersectionsProgrammaticallyForRoad(newRoads[0]);
        //}

        // Enable road updates and update the road system
        roadSystem.isAllowingRoadUpdates = true;
        roadSystem.UpdateAllRoads();
    }

    public static void RemoveOldRoadsAndRoadSystems()
    {
        // Delete RoadArchitectSystem
        RoadSystem oldRoadSystem = FindObjectOfType<RoadSystem>();
        if (oldRoadSystem != null)
        {
            DestroyImmediate(oldRoadSystem.gameObject);
        }

        // Delete existing roads
        Road[] oldRoads = FindObjectsOfType<Road>();
        if (oldRoads != null)
        {
            foreach (var oldRoad in oldRoads)
            {
                DestroyImmediate(oldRoad.gameObject);
            }
        }
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
            road.isUsingMeshColliders = false;
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


    private static void RoadArchitectUnitTest2(RoadSystem roadSystem)
    {
        //Create node locations:
        float startLocX = 800f;
        float startLocY = 200f;
        float startLocYSep = 200f;
        float height = 20f;
        Road road1 = null;
        Road road2 = null;

        //Create base road:
        List<Vector3> nodeLocations = new List<Vector3>();
        for (int index = 0; index < 9; index++)
        {
            nodeLocations.Add(new Vector3(startLocX + (index * 200f), height, 600f));
        }
        road1 = RoadAutomation.CreateRoadProgrammatically(roadSystem, ref nodeLocations);

        //Get road system, create road #1:
        nodeLocations.Clear();
        for (int index = 0; index < 5; index++)
        {
            nodeLocations.Add(new Vector3(startLocX, height, startLocY + (index * startLocYSep)));
        }
        road2 = RoadAutomation.CreateRoadProgrammatically(roadSystem, ref nodeLocations);
        //UnitTest_IntersectionHelper(bRoad, tRoad, RoadIntersection.iStopTypeEnum.TrafficLight1, RoadIntersection.RoadTypeEnum.NoTurnLane);

        //Get road system, create road #2:
        nodeLocations.Clear();
        for (int index = 0; index < 5; index++)
        {
            nodeLocations.Add(new Vector3(startLocX + (startLocYSep * 2f), height, startLocY + (index * startLocYSep)));
        }
        road2 = RoadAutomation.CreateRoadProgrammatically(roadSystem, ref nodeLocations);
        //UnitTest_IntersectionHelper(bRoad, tRoad, RoadIntersection.iStopTypeEnum.TrafficLight1, RoadIntersection.RoadTypeEnum.TurnLane);

        //Get road system, create road #3:
        nodeLocations.Clear();
        for (int index = 0; index < 5; index++)
        {
            nodeLocations.Add(new Vector3(startLocX + (startLocYSep * 4f), height, startLocY + (index * startLocYSep)));
        }
        road2 = RoadAutomation.CreateRoadProgrammatically(roadSystem, ref nodeLocations);
        //UnitTest_IntersectionHelper(bRoad, tRoad, RoadIntersection.iStopTypeEnum.TrafficLight1, RoadIntersection.RoadTypeEnum.BothTurnLanes);

        //Get road system, create road #4:
        nodeLocations.Clear();
        for (int index = 0; index < 5; index++)
        {
            nodeLocations.Add(new Vector3(startLocX + (startLocYSep * 6f), height, startLocY + (index * startLocYSep)));
        }
        road2 = RoadAutomation.CreateRoadProgrammatically(roadSystem, ref nodeLocations);
        //UnitTest_IntersectionHelper(bRoad, tRoad, RoadIntersection.iStopTypeEnum.TrafficLight1, RoadIntersection.RoadTypeEnum.TurnLane);

        //Get road system, create road #4:
        nodeLocations.Clear();
        for (int index = 0; index < 5; index++)
        {
            nodeLocations.Add(new Vector3(startLocX + (startLocYSep * 8f), height, startLocY + (index * startLocYSep)));
        }
        road2 = RoadAutomation.CreateRoadProgrammatically(roadSystem, ref nodeLocations);
        //UnitTest_IntersectionHelper(bRoad, tRoad, RoadIntersection.iStopTypeEnum.TrafficLight1, RoadIntersection.RoadTypeEnum.TurnLane);

        RoadAutomation.CreateIntersectionsProgrammaticallyForRoad(road1, RoadIntersection.iStopTypeEnum.None, RoadIntersection.RoadTypeEnum.NoTurnLane);

        //Now count road intersections, if not 5 throw error
        int intersctionsCount = 0;
        foreach (SplineN node in road1.spline.nodes)
        {
            if (node.isIntersection)
            {
                intersctionsCount += 1;
            }
        }

        if (intersctionsCount != 5)
        {
            Debug.LogError("Unit Test #2 failed: " + intersctionsCount.ToString() + " intersections instead of 5.");
        }
    }



}
