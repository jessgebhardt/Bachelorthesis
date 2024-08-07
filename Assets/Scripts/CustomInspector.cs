using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CityGenerator))]
public class CustomInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CityGenerator cityGenerator = (CityGenerator)target;
        if (cityGenerator == null)
        {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("District and Primary Road Generation", EditorStyles.boldLabel);

        if (GUILayout.Button("Generate Districts and Primary Roads"))
        {
            cityGenerator.GenerateDistrictsAndPrimaryRoads();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Secondary Road Generation", EditorStyles.boldLabel);

        if (GUILayout.Button("Generate Secondary Roads"))
        {
            cityGenerator.GenerateSecondaryRoads();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Lot and Building Generation", EditorStyles.boldLabel);

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
