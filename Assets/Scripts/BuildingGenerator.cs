using System.Collections.Generic;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
    /// <summary>
    /// Generates buildings in regions based on provided lots and district information.
    /// </summary>
    /// <param name="regionLots">Dictionary mapping region IDs to lists of lots.</param>
    /// <param name="districtsDictionary">Dictionary mapping region IDs to district information.</param>
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

    /// <summary>
    /// Generates buildings for each lot in a region using suitable prefabs.
    /// </summary>
    /// <param name="lots">List of lots within a region.</param>
    /// <param name="buildingPrefabs">List of building prefabs available for the region.</param>
    /// <param name="buildingsParent">Parent GameObject for the generated buildings.</param>
    private static void GenerateRegionsBuildings(List<List<Vector2Int>> lots, List<GameObject> buildingPrefabs, GameObject buildingsParent)
    {
        foreach (List<Vector2Int> lot in lots)
        {
            List<GameObject> suitablePrefabs = FilterSuitablePrefabs(lot, buildingPrefabs);
            if (suitablePrefabs.Count == 0)
            {
                Debug.LogWarning("No suitable prefabs found for the given lot.");
                continue;
            }

            Vector3 center = FindCentroid(lot);
            Vector3 position = new Vector3(center.x, 0, center.z);

            int randomNumber = Random.Range(0, suitablePrefabs.Count);
            GameObject buildingPrefab = suitablePrefabs[randomNumber];

            GameObject newBuilding = Instantiate(buildingPrefab, position, Quaternion.identity);

            newBuilding.transform.parent = buildingsParent.transform;
        }
    }

    /// <summary>
    /// Removes old buildings by destroying the existing parent GameObject.
    /// </summary>
    public static void RemoveOldBuildings()
    {
        GameObject oldParent = GameObject.Find("BuildingsParent");
        if (oldParent != null)
        {
            DestroyImmediate(oldParent);
        }
    }

    /// <summary>
    /// Calculates the centroid of a list of 2D points.
    /// </summary>
    /// <param name="points">List of 2D points representing a lot.</param>
    /// <returns>Centroid as a Vector3.</returns>
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

    /// <summary>
    /// Filters building prefabs to find those suitable for the given lot.
    /// </summary>
    /// <param name="lot">List of 2D points representing a lot.</param>
    /// <param name="buildingPrefabs">List of building prefabs available for the region.</param>
    /// <returns>List of suitable building prefabs.</returns>
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

    /// <summary>
    /// Determines if a building prefab fits within a given lot.
    /// </summary>
    /// <param name="lot">List of 2D points representing a lot.</param>
    /// <param name="prefab">Building prefab to be checked.</param>
    /// <returns>True if the prefab fits the lot; otherwise, false.</returns>
    private static bool PrefabFitsLot(List<Vector2Int> lot, GameObject prefab)
    {
        Bounds lotBounds = CalculateLotBounds(lot);
        Bounds prefabBounds = CalculatePrefabBounds(prefab);
        Debug.Log("lotbounds: "+lotBounds);
        Debug.Log("prefabBounds: " + prefabBounds);
        bool fits = lotBounds.extents.x > prefabBounds.extents.x && lotBounds.extents.z > prefabBounds.extents.z;
        return fits;
    }

    /// <summary>
    /// Calculates the bounds of a given lot based on its 2D points.
    /// </summary>
    /// <param name="lot">List of 2D points representing a lot.</param>
    /// <returns>Bounds of the lot.</returns>
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

    /// <summary>
    /// Calculates the bounds of a given building prefab.
    /// </summary>
    /// <param name="prefab">Building prefab to be checked.</param>
    /// <returns>Bounds of the prefab.</returns>
    private static Bounds CalculatePrefabBounds(GameObject prefab)
    {
        MeshRenderer renderer = prefab.GetComponentInChildren<MeshRenderer>();

        if (renderer == null)
        {
            Debug.LogWarning($"Prefab {prefab.name} does not have a Renderer component. Assuming it fits.");
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        GameObject prefabInstance = Instantiate(prefab);
        string prefabName = prefab.name;
        

        string numberPart = ExtractNumberPart(prefabName);
        Transform childTransform = prefabInstance.transform.Find("building-" + numberPart);

        Vector3 childLocalScale = Vector3.one;
        if (childTransform != null)
        {
            childLocalScale = childTransform.localScale;
        }

        Bounds bounds = renderer.bounds;
        Vector3 size = bounds.size;
        Vector3 scaledSize = Vector3.Scale(size, childLocalScale);
        Bounds scaledBounds = new Bounds(bounds.center, scaledSize);

        DestroyImmediate(prefabInstance);

        return scaledBounds;
    }

    /// <summary>
    /// Extracts the numerical part from the name of a prefab.
    /// </summary>
    /// <param name="name">Name of the prefab.</param>
    /// <returns>Extracted numerical part as a string.</returns>
    private static string ExtractNumberPart(string name)
    {
        string[] parts = name.Split('-');
        if (parts.Length > 1)
        {
            return string.Join("-", parts, 1, parts.Length - 1);
        }
        return "";
    }
}
