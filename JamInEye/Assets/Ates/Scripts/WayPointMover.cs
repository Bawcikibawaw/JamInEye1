using UnityEngine;
using DG.Tweening;

public class WaypointMover : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 3f;

    [Header("Sprites")]
    public SpriteRenderer spriteRenderer;
    public Sprite spriteRight;
    public Sprite spriteTop;

    [Header("Collider Child")]
    public Transform colliderChild;

    [Header("Shake")]
    public float shakeStrength = 0.05f;
    public int shakeVibrato = 20;
    public float shakeRandomness = 90f;

    private Transform[] _waypoints;
    private WaypointPath _claimedPath;
    private int _currentIndex = 0;
    private int _direction = 1;
    private Tween _moveTween;
    private Tween _shakeTween;
    private bool _lastIsHorizontal = true;

    // Shake pivot — created at runtime, sprite becomes its child
    private GameObject _shakePivot;
    
    public event System.Action<bool> OnDirectionChanged; // true = horizontal
    

    public void ClaimSpecificPath(WaypointPath path)
    {
        if (path == null)
        {
            Debug.LogWarning("ClaimSpecificPath: path is null!");
            Destroy(gameObject);
            return;
        }

        _claimedPath = path;
        _claimedPath.isOccupied = true;
        _waypoints = _claimedPath.GetWaypoints();

        if (_waypoints.Length < 2)
        {
            Debug.LogWarning("Path needs at least 2 waypoints: " + path.name);
            return;
        }

        SetupShakePivot();
        transform.position = _waypoints[0].position;
        StartShake();
        MoveToNext();
    }

    void SetupShakePivot()
    {
        // Create a pivot child that sits at local zero
        _shakePivot = new GameObject("ShakePivot");
        _shakePivot.transform.SetParent(transform);
        _shakePivot.transform.localPosition = Vector3.zero;
        _shakePivot.transform.localRotation = Quaternion.identity;
        _shakePivot.transform.localScale = Vector3.one;

        // Re-parent sprite under shake pivot
        spriteRenderer.transform.SetParent(_shakePivot.transform);
        spriteRenderer.transform.localPosition = Vector3.zero;
    }

    void StartShake()
    {
        _shakeTween?.Kill();

        // Reset pivot position before starting new shake
        _shakePivot.transform.localPosition = Vector3.zero;

        // Shake the PIVOT — sprite follows, root is never touched
        _shakeTween = _shakePivot.transform
            .DOShakePosition(
                duration: 1f,
                strength: new Vector3(shakeStrength, shakeStrength, 0f),
                vibrato: shakeVibrato,
                randomness: shakeRandomness,
                snapping: false,
                fadeOut: false
            )
            .SetLoops(-1, LoopType.Restart)
            .OnStepComplete(() => _shakePivot.transform.localPosition = Vector3.zero);
    }

    void MoveToNext()
    {
        int nextIndex = _currentIndex + _direction;

        if (nextIndex >= _waypoints.Length)
        {
            _direction = -1;
            nextIndex = _currentIndex + _direction;
        }
        else if (nextIndex < 0)
        {
            _direction = 1;
            nextIndex = _currentIndex + _direction;
        }

        _currentIndex = nextIndex;
        Vector3 target = _waypoints[_currentIndex].position;

        UpdateSpriteDirection(target);

        float distance = Vector3.Distance(transform.position, target);
        float duration = distance / speed;

        // Move ONLY the root — pivot and sprite are children, they follow automatically
        _moveTween = transform.DOMove(target, duration)
            .SetEase(Ease.Linear)
            .OnComplete(MoveToNext);
    }

    void UpdateSpriteDirection(Vector3 target)
    {
        if (spriteRenderer == null) return;

        Vector3 moveDir = (target - transform.position).normalized;
        bool isHorizontal = Mathf.Abs(moveDir.x) >= Mathf.Abs(moveDir.y);

        if (isHorizontal)
        {
            spriteRenderer.sprite = spriteRight;
            spriteRenderer.flipX = moveDir.x < 0f;
            spriteRenderer.flipY = false;
            if (colliderChild != null)
                colliderChild.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }
        else
        {
            spriteRenderer.sprite = spriteTop;
            spriteRenderer.flipY = moveDir.y < 0f;
            spriteRenderer.flipX = false;
            if (colliderChild != null)
                colliderChild.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        spriteRenderer.transform.localScale = Vector3.one;

        // Only fire event when direction type actually changes
        if (isHorizontal != _lastIsHorizontal)
        {
            _lastIsHorizontal = isHorizontal;
            OnDirectionChanged?.Invoke(isHorizontal);
        }
    }
    
        void OnDestroy() 
        {
        _moveTween?.Kill();
        _shakeTween?.Kill();
        WaypointPathRegistry.Instance?.ReleasePath(_claimedPath);
        }

    void OnDrawGizmos()
    {
        if (_waypoints == null) return;
        for (int i = 0; i < _waypoints.Length; i++)
        {
            if (_waypoints[i] == null) continue;
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(_waypoints[i].position, 0.15f);
            if (i < _waypoints.Length - 1 && _waypoints[i + 1] != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(_waypoints[i].position, _waypoints[i + 1].position);
            }
        }
    }
}