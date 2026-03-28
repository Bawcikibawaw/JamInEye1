using UnityEngine;
using System.Collections.Generic;

public class WayPointRegistry : MonoBehaviour
{
    public static WayPointRegistry Instance { get; private set; }

    [Header("Prefabs — each will appear at least once")]
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

        List<GameObject> spawnList = BuildSpawnList(_allPaths.Count);

        for (int i = 0; i < _allPaths.Count; i++)
        {
            GameObject prefab = spawnList[i];
            GameObject obj = Instantiate(prefab);
            WaypointMover mover = obj.GetComponent<WaypointMover>();
            if (mover != null)
                mover.ClaimSpecificPath(_allPaths[i]);
            else
                Debug.LogWarning("Prefab " + prefab.name + " has no WaypointMover!");
        }
    }

    List<GameObject> BuildSpawnList(int pathCount)
    {
        List<GameObject> result = new List<GameObject>();

        // Step 1 — shuffle a copy of prefabs so guaranteed slots are random order
        List<GameObject> shuffled = new List<GameObject>(moverPrefabs);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        // Step 2 — fill first N slots with one of each prefab (N = prefab count)
        int guaranteedCount = Mathf.Min(shuffled.Count, pathCount);
        for (int i = 0; i < guaranteedCount; i++)
            result.Add(shuffled[i]);

        // Step 3 — fill remaining slots by picking randomly from full list
        for (int i = guaranteedCount; i < pathCount; i++)
            result.Add(moverPrefabs[Random.Range(0, moverPrefabs.Count)]);

        return result;
    }

    public void ReleasePath(WaypointPath path)
    {
        if (path != null) path.isOccupied = false;
    }
}