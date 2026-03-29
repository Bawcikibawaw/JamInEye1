// WaypointPath.cs — put this on each waypoint group in the level
using UnityEngine;

public class WaypointPath : MonoBehaviour
{
    [HideInInspector] public bool isOccupied = false;

    public Transform[] GetWaypoints()
    {
        // All children of this group ARE the waypoints, in order
        Transform[] points = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            points[i] = transform.GetChild(i);
        return points;
    }
}