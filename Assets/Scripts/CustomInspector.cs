using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CityGenerator))]
public class CustomInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CityGenerator cityGenerator = (CityGenerator)target;

        if (GUILayout.Button("Generate Districts"))
        {
            cityGenerator.GenerateDistricts();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Secondary Roads"))
        {
            cityGenerator.GenerateSecondaryRoads();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Lots and Buildings"))
        {
            cityGenerator.GenerateLotsAndBuildings();
        }
        if (GUILayout.Button("Remove Buildings"))
        {
            cityGenerator.RemoveBuildings();
        }
    }
}