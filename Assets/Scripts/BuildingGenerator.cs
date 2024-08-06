using System.Collections.Generic;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
    public static void GenerateBuildings(Dictionary<int, List<List<Vector2Int>>> regionLots, IDictionary<int, District> districtsDictionary)
    {
        RemoveOldBuildings();

        GameObject buildingsParent = new GameObject("BuildingsParent");

        foreach (KeyValuePair<int, List<List<Vector2Int>>> region in regionLots)
        {
            List<GameObject> regionsBuildingPrefabs = new List<GameObject>();

            District district;
            districtsDictionary.TryGetValue(region.Key, out district);

            regionsBuildingPrefabs = district.type.buildingTypes;

            GenerateRegionsBuildings(region.Value, regionsBuildingPrefabs, buildingsParent);
        }
    }

    private static void GenerateRegionsBuildings(List<List<Vector2Int>> lots, List<GameObject> buildingPrefabs, GameObject buildingsParent)
    {
        foreach (List<Vector2Int> lot in lots)
        {
            Vector3 center = FindCentroid(lot);
            Vector3 position = new Vector3(center.x, 0, center.z);

            int randomNumber = Random.Range(0, buildingPrefabs.Count);
            GameObject buildingPrefab = buildingPrefabs[randomNumber];

            GameObject newBuilding = Instantiate(buildingPrefab, position, Quaternion.identity);

            newBuilding.transform.parent = buildingsParent.transform;
        }
    }

    public static void RemoveOldBuildings()
    {
        GameObject oldParent = GameObject.Find("BuildingsParent");
        if (oldParent != null)
        {
            DestroyImmediate(oldParent);
        }
    }

    private static Vector3 FindCentroid(List<Vector2Int> points)
    {
        float sumX = 0;
        float sumZ = 0;
        int count = points.Count;

        foreach (Vector2Int point in points)
        {
            sumX += point.x;
            sumZ += point.y;
        }

        float centerX = sumX / count;
        float centerZ = sumZ / count;

        return new Vector3(centerX, 0, centerZ);
    }

    private static List<GameObject> FilterSuitablePrefabs(List<Vector2Int> lot, List<GameObject> buildingPrefabs)
    {
        List<GameObject> suitablePrefabs = new List<GameObject>();

        foreach (GameObject prefab in buildingPrefabs)
        {
            if (PrefabFitsLot(lot, prefab))
            {
                suitablePrefabs.Add(prefab);
            }
        }

        return suitablePrefabs;
    }

    private static bool PrefabFitsLot(List<Vector2Int> lot, GameObject prefab)
    {
        Bounds lotBounds = CalculateLotBounds(lot);
        Bounds prefabBounds = CalculatePrefabBounds(prefab);

        Debug.Log("lotBounds: " + lotBounds + " prefabBounds: " + prefabBounds);
        Debug.Log("MIN: " + lotBounds.Contains(prefabBounds.min));
        Debug.Log("MAX: " + lotBounds.Contains(prefabBounds.max));
        Debug.Log(lotBounds.Contains(prefabBounds.min) && lotBounds.Contains(prefabBounds.max));

        return lotBounds.Contains(prefabBounds.min) && lotBounds.Contains(prefabBounds.max);
    }

    private static Bounds CalculateLotBounds(List<Vector2Int> lot)
    {
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        foreach (Vector2Int point in lot)
        {
            if (point.x < minX) minX = point.x;
            if (point.y < minY) minY = point.y;
            if (point.x > maxX) maxX = point.x;
            if (point.y > maxY) maxY = point.y;
        }

        Vector3 center = new Vector3((minX + maxX) / 2f, 0, (minY + maxY) / 2f);
        Vector3 size = new Vector3(maxX - minX, 0, maxY - minY);

        return new Bounds(center, size);
    }

    private static Bounds CalculatePrefabBounds(GameObject prefab)
    {
        Renderer renderer = prefab.GetComponent<Renderer>();

        if (renderer == null)
        {
            Debug.LogWarning($"Prefab {prefab.name} does not have a Renderer component. Assuming it fits.");
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        return renderer.bounds;
    }

    private static Quaternion DetermineBestRotation(List<Vector2Int> lot, GameObject prefab)
    {
        Quaternion[] rotations = {
            Quaternion.Euler(0, 0, 0),
            Quaternion.Euler(0, 90, 0),
            Quaternion.Euler(0, 180, 0),
            Quaternion.Euler(0, 270, 0)
        };

        Bounds lotBounds = CalculateLotBounds(lot);

        foreach (Quaternion rotation in rotations)
        {
            Bounds rotatedBounds = CalculateRotatedBounds(prefab, rotation);

            if (lotBounds.Contains(rotatedBounds.min) && lotBounds.Contains(rotatedBounds.max))
            {
                return rotation;
            }
        }

        return Quaternion.identity;
    }

    private static Bounds CalculateRotatedBounds(GameObject prefab, Quaternion rotation)
    {
        Renderer renderer = prefab.GetComponent<Renderer>();

        if (renderer == null)
        {
            Debug.LogWarning($"Prefab {prefab.name} does not have a Renderer component. Assuming it fits.");
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        Vector3 size = renderer.bounds.size;
        Vector3 rotatedSize = rotation * size;
        return new Bounds(Vector3.zero, rotatedSize);
    }
}