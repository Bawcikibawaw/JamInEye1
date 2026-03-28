using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class TilemapShadowAutoBuilder : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private Tilemap sourceTilemap;

    [Header("Shadow Tilemap")]
    [SerializeField] private string shadowObjectName = "Shadow Tilemap";
    [SerializeField] private bool createAsSibling = true;

    [Header("Visual")]
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] private Vector3 worldOffset = new Vector3(0.15f, -0.15f, 0f);

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int orderInLayer = -1;

    [Header("Shadow Shape")]
    [SerializeField] private bool mirrorX = false;
    [SerializeField] private bool mirrorY = true;
    [SerializeField][Min(0.01f)] private float squashX = 1f;
    [SerializeField][Min(0.01f)] private float squashY = 0.55f;
    [SerializeField] private float skewXFromY = 0.35f;
    [SerializeField] private Vector2 localTileOffset = Vector2.zero;

    [Header("Copy Settings")]
    [SerializeField] private bool copyTileColor = false;
    [SerializeField] private bool copyTileTransformMatrix = true;
    [SerializeField] private bool clearBeforeRebuild = true;

    [Header("Collider")]
    [SerializeField] private bool addTilemapCollider2D = true;
    [SerializeField] private bool tilemapColliderIsTrigger = true;
    [SerializeField] private bool useCompositeCollider = false;

    [Header("Editor")]
    [SerializeField] private bool rebuildOnEnable = true;
    [SerializeField] private bool rebuildInEditorWhenChanged = true;

    [Header("Runtime")]
    [SerializeField] private Tilemap shadowTilemap;

    private TilemapRenderer shadowRenderer;

    private void OnEnable()
    {
        ResolveSource();

        if (rebuildOnEnable)
            RebuildShadowTilemap();
    }

    private void OnValidate()
    {
        ResolveSource();

#if UNITY_EDITOR
        if (!Application.isPlaying && rebuildInEditorWhenChanged)
        {
            EditorApplication.delayCall -= DelayedEditorRebuild;
            EditorApplication.delayCall += DelayedEditorRebuild;
        }
#endif
    }

#if UNITY_EDITOR
    private void DelayedEditorRebuild()
    {
        if (this == null || !rebuildInEditorWhenChanged)
            return;

        RebuildShadowTilemap();
    }
#endif

    [ContextMenu("Rebuild Shadow Tilemap")]
    public void RebuildShadowTilemap()
    {
        ResolveSource();

        if (sourceTilemap == null)
        {
            Debug.LogError($"{nameof(TilemapShadowAutoBuilder)} on {name}: Source Tilemap is missing.");
            return;
        }

        EnsureShadowTilemapExists();
        SyncShadowTilemapObject();
        CopySourceToShadow();
        ApplyVisualSettings();
        SetupCollider();
    }

    private void ResolveSource()
    {
        if (sourceTilemap == null)
            sourceTilemap = GetComponent<Tilemap>();
    }

    private void EnsureShadowTilemapExists()
    {
        if (shadowTilemap != null)
        {
            shadowRenderer = shadowTilemap.GetComponent<TilemapRenderer>();
            if (shadowRenderer == null)
                shadowRenderer = shadowTilemap.gameObject.AddComponent<TilemapRenderer>();
            return;
        }

        Transform parent = createAsSibling ? sourceTilemap.transform.parent : sourceTilemap.transform;
        Transform existing = parent != null ? parent.Find(shadowObjectName) : null;

        if (existing != null)
        {
            shadowTilemap = existing.GetComponent<Tilemap>();
            if (shadowTilemap == null)
                shadowTilemap = existing.gameObject.AddComponent<Tilemap>();

            shadowRenderer = existing.GetComponent<TilemapRenderer>();
            if (shadowRenderer == null)
                shadowRenderer = existing.gameObject.AddComponent<TilemapRenderer>();
            return;
        }

        GameObject go = new GameObject(shadowObjectName);
        if (parent != null)
            go.transform.SetParent(parent, false);

        shadowTilemap = go.AddComponent<Tilemap>();
        shadowRenderer = go.AddComponent<TilemapRenderer>();
    }

    private void SyncShadowTilemapObject()
    {
        shadowTilemap.orientation = sourceTilemap.orientation;
        shadowTilemap.tileAnchor = sourceTilemap.tileAnchor;
        shadowTilemap.animationFrameRate = sourceTilemap.animationFrameRate;

        shadowTilemap.transform.position = sourceTilemap.transform.position + worldOffset;
        shadowTilemap.transform.rotation = sourceTilemap.transform.rotation;
        shadowTilemap.transform.localScale = Vector3.one; // important: keep whole tilemap unflipped
    }

    private void CopySourceToShadow()
    {
        if (clearBeforeRebuild)
            shadowTilemap.ClearAllTiles();

        BoundsInt bounds = sourceTilemap.cellBounds;

        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            TileBase tile = sourceTilemap.GetTile(cell);
            if (tile == null)
                continue;

            shadowTilemap.SetTile(cell, tile);

            if (copyTileColor)
                shadowTilemap.SetColor(cell, sourceTilemap.GetColor(cell));
            else
                shadowTilemap.SetColor(cell, Color.white);

            Matrix4x4 sourceMatrix = copyTileTransformMatrix
                ? sourceTilemap.GetTransformMatrix(cell)
                : Matrix4x4.identity;

            Matrix4x4 shadowMatrix = BuildShadowMatrix(sourceMatrix);
            shadowTilemap.SetTransformMatrix(cell, shadowMatrix);

            // Needed so our custom per-cell matrix is respected.
            shadowTilemap.SetTileFlags(cell, TileFlags.None);
        }

        shadowTilemap.CompressBounds();
    }

    private Matrix4x4 BuildShadowMatrix(Matrix4x4 sourceMatrix)
    {
        // Build shadow deformation in tile-local space so visuals stay aligned to the grid.
        Matrix4x4 local =
            Matrix4x4.Translate((Vector3)localTileOffset) *
            CreateSkewXFromY(skewXFromY) *
            Matrix4x4.Scale(new Vector3(
                (mirrorX ? -1f : 1f) * squashX,
                (mirrorY ? -1f : 1f) * squashY,
                1f));

        return sourceMatrix * local;
    }

    private static Matrix4x4 CreateSkewXFromY(float skew)
    {
        Matrix4x4 m = Matrix4x4.identity;
        m.m01 = skew; // x += y * skew
        return m;
    }

    private void ApplyVisualSettings()
    {
        shadowTilemap.color = shadowColor;

        if (shadowRenderer == null)
            shadowRenderer = shadowTilemap.GetComponent<TilemapRenderer>();

        shadowRenderer.sortingLayerName = sortingLayerName;
        shadowRenderer.sortingOrder = orderInLayer;
        shadowRenderer.mode = TilemapRenderer.Mode.Chunk;
    }

    private void SetupCollider()
    {
        TilemapCollider2D tilemapCollider = shadowTilemap.GetComponent<TilemapCollider2D>();
        CompositeCollider2D compositeCollider = shadowTilemap.GetComponent<CompositeCollider2D>();
        Rigidbody2D rb = shadowTilemap.GetComponent<Rigidbody2D>();

        if (!addTilemapCollider2D)
        {
            if (tilemapCollider != null) tilemapCollider.enabled = false;
            if (compositeCollider != null) compositeCollider.enabled = false;
            if (rb != null) rb.simulated = false;
            return;
        }

        if (tilemapCollider == null)
            tilemapCollider = shadowTilemap.gameObject.AddComponent<TilemapCollider2D>();

        tilemapCollider.enabled = true;
        tilemapCollider.isTrigger = tilemapColliderIsTrigger;

        if (useCompositeCollider)
        {
            if (rb == null)
                rb = shadowTilemap.gameObject.AddComponent<Rigidbody2D>();

            rb.bodyType = RigidbodyType2D.Static;
            rb.simulated = true;

            if (compositeCollider == null)
                compositeCollider = shadowTilemap.gameObject.AddComponent<CompositeCollider2D>();

            compositeCollider.enabled = true;
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
        }
        else
        {
            if (compositeCollider != null)
                compositeCollider.enabled = false;

            if (rb != null)
                rb.simulated = false;

            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.None;
        }
    }
}