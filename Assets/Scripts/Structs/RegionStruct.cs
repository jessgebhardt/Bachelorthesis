using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Region
{
    public int Id;
    public List<Vector2Int> Pixels = new List<Vector2Int>();
}
