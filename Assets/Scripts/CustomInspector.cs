using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(CityManager))]
public class CustomInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CityManager cityManager = (CityManager)target;

        if (GUILayout.Button("Generate Districts"))
        {
            cityManager.GenerateDistricts();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Roads"))
        {
            cityManager.GenerateRoads();
        }
        if (GUILayout.Button("Remove Roads"))
        {
            cityManager.RemoveRoads();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Lots and Buildings"))
        {
            cityManager.GenerateLotsAndBuildings();
        }
        if (GUILayout.Button("Remove Buildings"))
        {
            cityManager.RemoveBuildings();
        }
    }
}
