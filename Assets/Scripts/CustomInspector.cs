using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DistrictGenerator))]
public class CustomInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DistrictGenerator districtGenerator = (DistrictGenerator)target;

        if (GUILayout.Button("Generate Districts"))
        {
            districtGenerator.GenerateDistricts();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Secondary Roads"))
        {
            districtGenerator.GenerateRoads();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Lots and Buildings"))
        {
            districtGenerator.GenerateLotsAndBuildings();
        }
        if (GUILayout.Button("Remove Buildings"))
        {
            districtGenerator.RemoveBuildings();
        }
    }
}
