using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ManualSpriteShadow2D : MonoBehaviour
{
    [Header("Shadow Object")]
    [SerializeField] private bool createShadowOnAwake = true;
    [SerializeField] private string shadowObjectNameSuffix = "_Shadow";

    [Header("Visual")]
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.35f);
    [SerializeField] private int sortingOrderOffset = -1;
    [SerializeField] private Material shadowMaterial;

    [Header("Placement")]
    [SerializeField] private Vector2 worldOffset = new Vector2(0.2f, -0.35f);

    [Header("Shape Transform")]
    [SerializeField] private bool mirrorX = true;
    [SerializeField] private bool mirrorY = false;

    [Range(0.05f, 3f)]
    [SerializeField] private float scaleX = 1f;

    [Range(0.05f, 3f)]
    [SerializeField] private float scaleY = 0.45f;

    [Header("Optional Path Offset")]
    [Tooltip("Keeps collider/sprite aligned because both use same local sprite space.")]
    [SerializeField] private Vector2 localShapeOffset = Vector2.zero;

    [Header("Collider")]
    [SerializeField] private bool createTriggerCollider = true;
    [SerializeField] private bool syncContinuously = true;

    [Header("Debug")]
    [SerializeField] private bool logWarnings = true;

    private SpriteRenderer sourceRenderer;
    private GameObject shadowObject;
    private Transform shadowTransform;
    private SpriteRenderer shadowRenderer;
    private PolygonCollider2D shadowCollider;

    private Sprite lastSprite;
    private bool initialized;

    private void Awake()
    {
        sourceRenderer = GetComponent<SpriteRenderer>();

        if (createShadowOnAwake)
        {
            EnsureShadowObject();
            RefreshAll();
        }
    }

    private void LateUpdate()
    {
        if (!initialized || sourceRenderer == null)
            return;

        bool spriteChanged = lastSprite != sourceRenderer.sprite;

        if (syncContinuously || spriteChanged)
        {
            RefreshAll();
        }
    }

    [ContextMenu("Rebuild Shadow")]
    public void RefreshAll()
    {
        EnsureShadowObject();

        if (sourceRenderer == null || sourceRenderer.sprite == null)
        {
            if (logWarnings)
                Debug.LogWarning($"{nameof(ManualSpriteShadow2D)} on {name}: No source sprite assigned.");
            return;
        }

        SyncShadowVisual();
        SyncShadowCollider();

        lastSprite = sourceRenderer.sprite;
    }

    private void EnsureShadowObject()
    {
        if (initialized && shadowObject != null && shadowRenderer != null)
            return;

        string shadowName = gameObject.name + shadowObjectNameSuffix;

        Transform existing = transform.Find(shadowName);
        if (existing != null)
        {
            shadowObject = existing.gameObject;
            shadowTransform = existing;
        }
        else
        {
            shadowObject = new GameObject(shadowName);
            shadowTransform = shadowObject.transform;
            shadowTransform.SetParent(transform, false);
        }

        shadowRenderer = shadowObject.GetComponent<SpriteRenderer>();
        if (shadowRenderer == null)
            shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();

        if (createTriggerCollider)
        {
            shadowCollider = shadowObject.GetComponent<PolygonCollider2D>();
            if (shadowCollider == null)
                shadowCollider = shadowObject.AddComponent<PolygonCollider2D>();

            shadowCollider.isTrigger = true;
        }
        else
        {
            shadowCollider = shadowObject.GetComponent<PolygonCollider2D>();
            if (shadowCollider != null)
                shadowCollider.enabled = false;
        }

        initialized = true;
    }

    private void SyncShadowVisual()
    {
        Sprite sprite = sourceRenderer.sprite;

        shadowRenderer.sprite = sprite;
        shadowRenderer.color = shadowColor;
        shadowRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        shadowRenderer.sortingOrder = sourceRenderer.sortingOrder + sortingOrderOffset;
        shadowRenderer.flipX = false;
        shadowRenderer.flipY = false;

        shadowRenderer.sharedMaterial = shadowMaterial != null ? shadowMaterial : null;

        shadowTransform.position = transform.position + (Vector3)worldOffset;
        shadowTransform.rotation = Quaternion.identity;

        // This is the important part:
        // transform handles mirroring/scaling for BOTH sprite and collider.
        Vector3 visualScale = Vector3.one;
        visualScale.x = mirrorX ? -scaleX : scaleX;
        visualScale.y = mirrorY ? -scaleY : scaleY;
        visualScale.z = 1f;

        shadowTransform.localScale = visualScale;
    }

    private void SyncShadowCollider()
    {
        if (!createTriggerCollider || shadowCollider == null)
            return;

        Sprite sprite = sourceRenderer.sprite;
        if (sprite == null)
        {
            shadowCollider.pathCount = 0;
            return;
        }

        int shapeCount = sprite.GetPhysicsShapeCount();

        if (shapeCount <= 0)
        {
            shadowCollider.pathCount = 0;

            if (logWarnings)
            {
                Debug.LogWarning(
                    $"{nameof(ManualSpriteShadow2D)} on {name}: Sprite '{sprite.name}' has no custom physics shape. " +
                    "Create one in Sprite Editor > Custom Physics Shape."
                );
            }

            return;
        }

        shadowCollider.pathCount = shapeCount;

        List<Vector2> sourceShape = new List<Vector2>(64);

        for (int shapeIndex = 0; shapeIndex < shapeCount; shapeIndex++)
        {
            sourceShape.Clear();
            sprite.GetPhysicsShape(shapeIndex, sourceShape);

            Vector2[] path = CopySpriteShapeInSpriteSpace(sourceShape, localShapeOffset);
            shadowCollider.SetPath(shapeIndex, path);
        }
    }

    private Vector2[] CopySpriteShapeInSpriteSpace(List<Vector2> sourceShape, Vector2 offset)
    {
        int count = sourceShape.Count;
        Vector2[] result = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            // Keep the physics shape exactly in the sprite's authored local space.
            result[i] = sourceShape[i] + offset;
        }

        return result;
    }
}