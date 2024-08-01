using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingsGenerator : MonoBehaviour
{
    public static void GenerateBuildings(List<List<Vector2Int>> lots, List<GameObject> buildingPrefabs)
    {
        //RemoveGeneratedBuildings();???

        foreach (var lot in lots)
        {
            Vector3 center = FindCentroid(lot);
            Vector3 position = new Vector3(center.x, 0, center.z);

            // aus prefabs f�r diese region
            // finde die, die ins gundst�ck passen w�rden
            // w�hle eines random aus
            int randomNumber = Random.Range(0, buildingPrefabs.Count);
            GameObject buildingPrefab = buildingPrefabs[randomNumber];
            // drehe es so, dass es in das Grundst�ck passt



            // add empty gameobject first und f�ge da alle buildings ein
            GameObject newBuilding = Instantiate(buildingPrefab, position, Quaternion.identity);
            // newBuilding.transform.localScale = new Vector3(1, 1, 1); // Skaliere das Geb�ude
        }
    }

    private static Vector3 FindCentroid(List<Vector2Int> points)
    {
        float sumX = 0;
        float sumZ = 0;
        int count = points.Count;

        foreach (var point in points)
        {
            sumX += point.x;
            sumZ += point.y; // Verwende y f�r z-Koordinate in 3D-Raum
        }

        float centerX = sumX / count;
        float centerZ = sumZ / count;

        return new Vector3(centerX, 0, centerZ); // y-Wert ist 0, da wir die H�he separat festlegen
    }
}
