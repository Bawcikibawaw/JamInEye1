using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
[DisallowMultipleComponent]
public class TilemapGroupedShadowBuilder : MonoBehaviour
{
    public enum GroupPivotMode
    {
        BottomCenterOfGroup,
        CenterOfGroup,
        ManualWorldPosition
    }

    [Serializable]
    public class ShadowGroup
    {
        public string name = "Group";
        public Color gizmoColor = Color.cyan;
        public bool enabled = true;
        public List<Vector3Int> cells = new List<Vector3Int>();

        [Header("Pivot")]
        public GroupPivotMode pivotMode = GroupPivotMode.BottomCenterOfGroup;
        public Vector2 pivotOffset = Vector2.zero;
        public Vector2 manualPivotWorldPosition = Vector2.zero;

        [Header("Rotation")]
        public Vector3 manualEulerRotation = Vector3.zero;
    }

    [Header("Source")]
    [SerializeField] private Tilemap sourceTilemap;

    [Header("Container")]
    [SerializeField] private Transform shadowContainer;
    [SerializeField] private string shadowContainerName = "Tile Shadows";
    [SerializeField] private int shadowContainerLayerId = 0;

    [Header("Visual")]
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int orderInLayer = -1;
    [SerializeField] private Material shadowMaterial;

    [Header("Placement")]
    [SerializeField] private Vector2 worldOffset = new Vector2(0.15f, -0.15f);

    [Header("Auto Rotation")]
    [SerializeField] private Vector2 horizonPoint = Vector2.zero;
    [SerializeField] private Vector2 horizonDirection = Vector2.right;
    [SerializeField] private float baseRotationDeg = 0f;
    [SerializeField] private float rotationPerUnit = 10f;
    [SerializeField] private float maxAbsRotation = 70f;
    [SerializeField] private bool useAutoRotation = true;

    [Header("Shadow Shape")]
    [SerializeField] private bool mirrorX = false;
    [SerializeField] private bool mirrorY = true;
    [SerializeField] private Vector2 shadowScale = new Vector2(1f, 0.5f);

    [Header("Collider")]
    [SerializeField] private bool createCollider = false;
    [SerializeField] private bool colliderIsTrigger = true;
    [SerializeField] private Vector2 colliderLocalOffset = Vector2.zero;

    [Header("Build")]
    [SerializeField] private bool rebuildOnEnable = true;
    [SerializeField] private bool clearBeforeBuild = true;

    [Header("Groups")]
    [SerializeField] private List<ShadowGroup> groups = new List<ShadowGroup>();

    public Tilemap SourceTilemap => sourceTilemap;
    public Transform ShadowContainer => shadowContainer;
    public List<ShadowGroup> Groups => groups;

    private void OnEnable()
    {
        if (rebuildOnEnable)
            Rebuild();
    }

    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        ResolveReferences();

        if (sourceTilemap == null)
        {
            Debug.LogError($"{nameof(TilemapGroupedShadowBuilder)}: Source Tilemap is missing.");
            return;
        }

        if (clearBeforeBuild)
            ClearShadows();

        for (int i = 0; i < groups.Count; i++)
        {
            ShadowGroup group = groups[i];
            if (group == null || !group.enabled || group.cells == null || group.cells.Count == 0)
                continue;

            BuildGroup(i, group);
        }
    }

    [ContextMenu("Refresh Group Roots Only")]
    public void RefreshGroupRootsOnly()
    {
        ResolveReferences();

        for (int i = 0; i < groups.Count; i++)
        {
            ShadowGroup group = groups[i];
            if (group == null || !group.enabled || group.cells == null || group.cells.Count == 0)
                continue;

            Transform root = GetOrCreateGroupRoot(i, group.name);
            Vector2 pivotWorld = ComputeGroupPivotWorld(group);
            float autoAngle = useAutoRotation ? ComputeAngleFromWorldPoint(pivotWorld) : 0f;

            Vector3 finalEuler = group.manualEulerRotation;
            finalEuler.z += autoAngle;

            root.position = pivotWorld;
            root.rotation = Quaternion.Euler(finalEuler);
            root.localScale = Vector3.one;
        }
    }

    [ContextMenu("Clear Shadows")]
    public void ClearShadows()
    {
        ResolveReferences();

        if (shadowContainer == null)
            return;

        List<Transform> children = new List<Transform>();
        foreach (Transform child in shadowContainer)
            children.Add(child);

        for (int i = 0; i < children.Count; i++)
        {
            if (Application.isPlaying)
                Destroy(children[i].gameObject);
            else
                DestroyImmediate(children[i].gameObject);
        }
    }

    public void AddGroup()
    {
        groups.Add(new ShadowGroup
        {
            name = $"Group {groups.Count}",
            gizmoColor = UnityEngine.Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f)
        });
    }

    public void RemoveGroupAt(int index)
    {
        if (index < 0 || index >= groups.Count)
            return;

        groups.RemoveAt(index);
    }

    public bool AddCellToGroup(int groupIndex, Vector3Int cell, bool removeFromOtherGroups = true)
    {
        if (groupIndex < 0 || groupIndex >= groups.Count)
            return false;

        if (removeFromOtherGroups)
            RemoveCellFromAllGroups(cell);

        ShadowGroup group = groups[groupIndex];
        if (group.cells.Contains(cell))
            return false;

        group.cells.Add(cell);
        return true;
    }

    public bool RemoveCellFromGroup(int groupIndex, Vector3Int cell)
    {
        if (groupIndex < 0 || groupIndex >= groups.Count)
            return false;

        return groups[groupIndex].cells.Remove(cell);
    }

    public void RemoveCellFromAllGroups(Vector3Int cell)
    {
        for (int i = 0; i < groups.Count; i++)
            groups[i]?.cells?.Remove(cell);
    }

    public int FindGroupIndexForCell(Vector3Int cell)
    {
        for (int i = 0; i < groups.Count; i++)
        {
            ShadowGroup g = groups[i];
            if (g != null && g.cells != null && g.cells.Contains(cell))
                return i;
        }

        return -1;
    }

    public Vector2 GetGroupPivotWorld(int groupIndex)
    {
        if (groupIndex < 0 || groupIndex >= groups.Count)
            return Vector2.zero;

        return ComputeGroupPivotWorld(groups[groupIndex]);
    }

    public float GetGroupAutoAngle(int groupIndex)
    {
        if (groupIndex < 0 || groupIndex >= groups.Count)
            return 0f;

        Vector2 pivot = ComputeGroupPivotWorld(groups[groupIndex]);
        return useAutoRotation ? ComputeAngleFromWorldPoint(pivot) : 0f;
    }

    public Transform GetGroupRoot(int groupIndex)
    {
        if (groupIndex < 0 || groupIndex >= groups.Count)
            return null;

        ResolveReferences();
        return GetOrCreateGroupRoot(groupIndex, groups[groupIndex].name);
    }

    private void ResolveReferences()
    {
        if (sourceTilemap == null)
            sourceTilemap = GetComponent<Tilemap>();

        if (shadowContainer == null)
        {
            Transform existing = transform.Find(shadowContainerName);
            if (existing != null)
            {
                shadowContainer = existing;
            }
            else
            {
                GameObject go = new GameObject(shadowContainerName);
                go.transform.SetParent(transform, false);
                shadowContainer = go.transform;
            }
        }
    }

    private void BuildGroup(int groupIndex, ShadowGroup group)
    {
        Vector2 pivotWorld = ComputeGroupPivotWorld(group);
        float autoAngle = useAutoRotation ? ComputeAngleFromWorldPoint(pivotWorld) : 0f;

        Vector3 finalEuler = group.manualEulerRotation;
        finalEuler.z += autoAngle;

        Transform groupRoot = GetOrCreateGroupRoot(groupIndex, group.name);
        groupRoot.position = pivotWorld;
        groupRoot.rotation = Quaternion.Euler(finalEuler);
        groupRoot.localScale = Vector3.one;

        ClearChildren(groupRoot);

        for (int i = 0; i < group.cells.Count; i++)
        {
            Vector3Int cell = group.cells[i];

            if (!sourceTilemap.HasTile(cell))
                continue;

            Sprite sprite = sourceTilemap.GetSprite(cell);
            if (sprite == null)
                continue;

            GameObject shadowGO = CreateTileShadow(groupRoot, cell);
            ApplyShadowChild(shadowGO, cell, sprite, pivotWorld);
        }
    }

    private Transform GetOrCreateGroupRoot(int groupIndex, string groupName)
    {
        string safeName = string.IsNullOrWhiteSpace(groupName) ? $"Group_{groupIndex}" : groupName;
        string objectName = $"ShadowGroup_{groupIndex}_{safeName}";

        Transform existing = shadowContainer.Find(objectName);
        if (existing != null)
            return existing;

        GameObject go = new GameObject(objectName);
        go.transform.SetParent(shadowContainer, false);
        go.layer = shadowContainerLayerId;
        return go.transform;
    }

    private void ClearChildren(Transform parent)
    {
        List<Transform> children = new List<Transform>();
        foreach (Transform child in parent)
            children.Add(child);

        for (int i = 0; i < children.Count; i++)
        {
            if (Application.isPlaying)
                Destroy(children[i].gameObject);
            else
                DestroyImmediate(children[i].gameObject);
        }
    }

    private GameObject CreateTileShadow(Transform groupRoot, Vector3Int cell)
    {
        GameObject go = new GameObject($"Shadow_{cell.x}_{cell.y}_{cell.z}");
        go.transform.SetParent(groupRoot, false);
        go.layer = shadowContainerLayerId;
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<ShadowZone>();
        return go;
    }

    private void ApplyShadowChild(GameObject shadowGO, Vector3Int cell, Sprite sprite, Vector2 pivotWorld)
    {
        SpriteRenderer sr = shadowGO.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = shadowColor;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = orderInLayer;
        sr.flipX = false;
        sr.flipY = false;
        sr.sharedMaterial = shadowMaterial;

        Vector2 tileWorld = (Vector2)sourceTilemap.GetCellCenterWorld(cell) + worldOffset;
        Vector2 localPos = tileWorld - pivotWorld;

        shadowGO.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
        shadowGO.transform.localRotation = Quaternion.identity;
        shadowGO.transform.localScale = new Vector3(
            (mirrorX ? -1f : 1f) * shadowScale.x,
            (mirrorY ? -1f : 1f) * shadowScale.y,
            1f
        );

        SyncCollider(shadowGO, sprite);
    }

    private Vector2 ComputeGroupPivotWorld(ShadowGroup group)
    {
        switch (group.pivotMode)
        {
            case GroupPivotMode.CenterOfGroup:
                return ComputeGroupCenter(group) + group.pivotOffset;

            case GroupPivotMode.ManualWorldPosition:
                return group.manualPivotWorldPosition + group.pivotOffset;

            default:
                return ComputeGroupBottomCenter(group) + group.pivotOffset;
        }
    }

    private Vector2 ComputeGroupCenter(ShadowGroup group)
    {
        Vector2 sum = Vector2.zero;
        int count = 0;

        for (int i = 0; i < group.cells.Count; i++)
        {
            Vector3Int cell = group.cells[i];
            if (!sourceTilemap.HasTile(cell))
                continue;

            sum += (Vector2)sourceTilemap.GetCellCenterWorld(cell);
            count++;
        }

        return count > 0 ? sum / count : Vector2.zero;
    }

    private Vector2 ComputeGroupBottomCenter(ShadowGroup group)
    {
        bool hasAny = false;
        float minX = 0f;
        float maxX = 0f;
        float minY = 0f;

        Vector3 cellSize = sourceTilemap.layoutGrid.cellSize;

        for (int i = 0; i < group.cells.Count; i++)
        {
            Vector3Int cell = group.cells[i];
            if (!sourceTilemap.HasTile(cell))
                continue;

            Vector3 center = sourceTilemap.GetCellCenterWorld(cell);

            float left = center.x - cellSize.x * 0.5f;
            float right = center.x + cellSize.x * 0.5f;
            float bottom = center.y - cellSize.y * 0.5f;

            if (!hasAny)
            {
                minX = left;
                maxX = right;
                minY = bottom;
                hasAny = true;
            }
            else
            {
                minX = Mathf.Min(minX, left);
                maxX = Mathf.Max(maxX, right);
                minY = Mathf.Min(minY, bottom);
            }
        }

        if (!hasAny)
            return Vector2.zero;

        return new Vector2((minX + maxX) * 0.5f, minY);
    }

    private float ComputeAngleFromWorldPoint(Vector2 worldPos)
    {
        Vector2 dir = horizonDirection.sqrMagnitude > 0.0001f
            ? horizonDirection.normalized
            : Vector2.right;

        Vector2 fromLinePoint = worldPos - horizonPoint;
        float signedDistance = Cross(dir, fromLinePoint);

        float angle = baseRotationDeg + signedDistance * rotationPerUnit;
        return Mathf.Clamp(angle, -maxAbsRotation, maxAbsRotation);
    }

    private void SyncCollider(GameObject shadowGO, Sprite sprite)
    {
        PolygonCollider2D col = shadowGO.GetComponent<PolygonCollider2D>();

        if (!createCollider)
        {
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
            return;
        }

        if (col == null)
            col = shadowGO.AddComponent<PolygonCollider2D>();

        col.isTrigger = colliderIsTrigger;

        int shapeCount = sprite.GetPhysicsShapeCount();
        if (shapeCount <= 0)
        {
            col.pathCount = 0;
            return;
        }

        col.pathCount = shapeCount;

        List<Vector2> shape = new List<Vector2>(64);
        for (int i = 0; i < shapeCount; i++)
        {
            shape.Clear();
            sprite.GetPhysicsShape(i, shape);

            Vector2[] path = new Vector2[shape.Count];
            for (int p = 0; p < shape.Count; p++)
                path[p] = shape[p] + colliderLocalOffset;

            col.SetPath(i, path);
        }
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (sourceTilemap == null)
            sourceTilemap = GetComponent<Tilemap>();

        Vector2 dir = horizonDirection.sqrMagnitude > 0.0001f ? horizonDirection.normalized : Vector2.right;
        Vector2 p0 = horizonPoint - dir * 100f;
        Vector2 p1 = horizonPoint + dir * 100f;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(p0, p1);

        if (groups == null)
            return;

        for (int i = 0; i < groups.Count; i++)
        {
            ShadowGroup group = groups[i];
            if (group == null || group.cells == null || group.cells.Count == 0)
                continue;

            Gizmos.color = group.gizmoColor;
            Vector2 pivot = ComputeGroupPivotWorld(group);
            Gizmos.DrawSphere(pivot, 0.08f);
        }
    }
#endif
}