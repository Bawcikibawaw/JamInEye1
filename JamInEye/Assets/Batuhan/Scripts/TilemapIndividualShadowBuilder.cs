using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
public class TilemapIndividualShadowBuilder : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private Tilemap sourceTilemap;

    [Header("Container")]
    [SerializeField] private Transform shadowContainer;
    [SerializeField] private string shadowContainerName = "Tile Shadows";

    [Header("Visual")]
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int orderInLayer = -1;

    [Header("Placement")]
    [SerializeField] private Vector2 worldOffset = new Vector2(0.15f, -0.15f);
    [SerializeField] private bool useSourceTileTransformMatrix = true;

    [Header("Horizon Line")]
    [Tooltip("A point on the horizon/reference line in world space.")]
    [SerializeField] private Vector2 horizonPoint = new Vector2(0f, 0f);

    [Tooltip("Direction of the horizon/reference line.")]
    [SerializeField] private Vector2 horizonDirection = Vector2.right;

    [Tooltip("Base rotation added to every shadow.")]
    [SerializeField] private float baseRotationDeg = 0f;

    [Tooltip("How much rotation changes per world unit of signed distance to the horizon line.")]
    [SerializeField] private float rotationPerUnit = 10f;

    [Tooltip("Clamp final per-tile rotation.")]
    [SerializeField] private float maxAbsRotation = 70f;

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

    private readonly Dictionary<Vector3Int, GameObject> spawnedShadows = new();

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
            Debug.LogError($"{nameof(TilemapIndividualShadowBuilder)}: Source Tilemap is missing.");
            return;
        }

        if (clearBeforeBuild)
            ClearShadows();

        BoundsInt bounds = sourceTilemap.cellBounds;

        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            TileBase tile = sourceTilemap.GetTile(cell);
            if (tile == null)
                continue;

            Sprite sprite = sourceTilemap.GetSprite(cell);
            if (sprite == null)
                continue;

            GameObject shadowGO = GetOrCreateShadow(cell);
            ApplyShadow(shadowGO, cell, sprite);
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

        spawnedShadows.Clear();
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

    private GameObject GetOrCreateShadow(Vector3Int cell)
    {
        if (spawnedShadows.TryGetValue(cell, out GameObject existing) && existing != null)
            return existing;

        string objectName = $"Shadow_{cell.x}_{cell.y}_{cell.z}";
        Transform child = shadowContainer.Find(objectName);

        GameObject go;
        if (child != null)
        {
            go = child.gameObject;
        }
        else
        {
            go = new GameObject(objectName);
            go.transform.SetParent(shadowContainer, false);
            go.AddComponent<SpriteRenderer>();
        }

        spawnedShadows[cell] = go;
        return go;
    }

    private void ApplyShadow(GameObject shadowGO, Vector3Int cell, Sprite sprite)
    {
        SpriteRenderer sr = shadowGO.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = shadowGO.AddComponent<SpriteRenderer>();

        sr.sprite = sprite;
        sr.color = shadowColor;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = orderInLayer;
        sr.flipX = false;
        sr.flipY = false;

        Vector3 centerWorld = sourceTilemap.GetCellCenterWorld(cell);
        shadowGO.transform.position = centerWorld + (Vector3)worldOffset;

        float angle = ComputeShadowAngle(centerWorld);

        shadowGO.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Vector3 localScale = new Vector3(
            (mirrorX ? -1f : 1f) * shadowScale.x,
            (mirrorY ? -1f : 1f) * shadowScale.y,
            1f
        );
        shadowGO.transform.localScale = localScale;

        if (useSourceTileTransformMatrix)
        {
            Matrix4x4 tileMatrix = sourceTilemap.GetTransformMatrix(cell);
            Vector3 tileRight = tileMatrix.MultiplyVector(Vector3.right);
            if (tileRight.sqrMagnitude > 0.0001f)
            {
                float tileAngle = Mathf.Atan2(tileRight.y, tileRight.x) * Mathf.Rad2Deg;
                shadowGO.transform.rotation = Quaternion.Euler(0f, 0f, tileAngle + angle);
            }
        }

        SyncCollider(shadowGO, sprite);
    }

    private float ComputeShadowAngle(Vector2 worldPos)
    {
        Vector2 dir = horizonDirection.sqrMagnitude > 0.0001f ? horizonDirection.normalized : Vector2.right;

        // Signed distance to line:
        // distance sign comes from the 2D perpendicular.
        Vector2 fromLinePoint = worldPos - horizonPoint;
        float signedDistance = Cross(dir, fromLinePoint);

        float angle = baseRotationDeg + signedDistance * rotationPerUnit;
        angle = Mathf.Clamp(angle, -maxAbsRotation, maxAbsRotation);
        return angle;
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
        Vector2 dir = horizonDirection.sqrMagnitude > 0.0001f ? horizonDirection.normalized : Vector2.right;
        Vector2 p0 = horizonPoint - dir * 100f;
        Vector2 p1 = horizonPoint + dir * 100f;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(p0, p1);

        Vector2 normal = new Vector2(-dir.y, dir.x);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(horizonPoint, horizonPoint + normal * 2f);
    }
#endif
}