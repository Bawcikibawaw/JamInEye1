// WaypointPathRegistry.cs
using UnityEngine;
using System.Collections.Generic;

public class WaypointPathRegistry : MonoBehaviour
{
    public static WaypointPathRegistry Instance { get; private set; }

    [Header("Default prefab pool — used for paths with no assigned prefab")]
    public List<GameObject> moverPrefabs;

    private List<WaypointPath> _allPaths = new List<WaypointPath>();

    void Awake()
    {
        Instance = this;
        foreach (var path in FindObjectsByType<WaypointPath>(FindObjectsSortMode.None))
            _allPaths.Add(path);
    }

    void Start()
    {
        if (moverPrefabs == null || moverPrefabs.Count == 0)
        {
            Debug.LogWarning("WaypointPathRegistry: no prefabs assigned!");
            return;
        }

        // Separate paths into assigned and unassigned
        List<WaypointPath> assignedPaths = _allPaths.FindAll(p => p.assignedPrefab != null);
        List<WaypointPath> randomPaths = _allPaths.FindAll(p => p.assignedPrefab == null);

        // Spawn assigned paths first — specific prefab per path
        foreach (var path in assignedPaths)
        {
            Spawn(path, path.assignedPrefab);
        }

        // Spawn random paths using the guaranteed shuffle system
        List<GameObject> spawnList = BuildSpawnList(randomPaths.Count);
        for (int i = 0; i < randomPaths.Count; i++)
        {
            Spawn(randomPaths[i], spawnList[i]);
        }
    }

    void Spawn(WaypointPath path, GameObject prefab)
    {
        GameObject obj = Instantiate(prefab);
        WaypointMover mover = obj.GetComponent<WaypointMover>();
        if (mover != null)
            mover.ClaimSpecificPath(path);
        else
            Debug.LogWarning("Prefab " + prefab.name + " has no WaypointMover!");
    }

    List<GameObject> BuildSpawnList(int pathCount)
    {
        List<GameObject> result = new List<GameObject>();

        List<GameObject> shuffled = new List<GameObject>(moverPrefabs);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        int guaranteedCount = Mathf.Min(shuffled.Count, pathCount);
        for (int i = 0; i < guaranteedCount; i++)
            result.Add(shuffled[i]);

        for (int i = guaranteedCount; i < pathCount; i++)
            result.Add(moverPrefabs[Random.Range(0, moverPrefabs.Count)]);

        return result;
    }

    public void ReleasePath(WaypointPath path)
    {
        if (path != null) path.isOccupied = false;
    }
}