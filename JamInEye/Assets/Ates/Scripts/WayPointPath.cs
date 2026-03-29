// WaypointPath.cs
using UnityEngine;

public class WaypointPath : MonoBehaviour
{
    [HideInInspector] public bool isOccupied = false;

    [Header("Assigned Prefab (optional — overrides registry random)")]
    public GameObject assignedPrefab; // drag specific prefab here, leave null for random

    public Transform[] GetWaypoints()
    {
        Transform[] points = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            points[i] = transform.GetChild(i);
        return points;
    }
}