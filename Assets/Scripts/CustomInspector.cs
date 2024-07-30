using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;

[CustomEditor(typeof(DistrictGenerator))]
public class CustomInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DistrictGenerator districtGenerator = (DistrictGenerator)target;
        if(GUILayout.Button("Generate Roads"))
        {
            districtGenerator.GenerateRoads();
        }
        if (GUILayout.Button("Generate Lots"))
        {
            districtGenerator.GenerateLots();
        }
    }
}
