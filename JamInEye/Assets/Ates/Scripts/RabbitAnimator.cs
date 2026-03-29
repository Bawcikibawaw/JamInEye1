using UnityEngine;
using DG.Tweening;

public class RabbitAnimator : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer spriteRenderer;

    [Header("Bounce Settings (Horizontal)")]
    public float bounceHeight = 0.3f;
    public float bounceDuration = 0.35f;
    public float scaleUpX = 1.1f;
    public float scaleUpY = 1.2f;
    public float scaleDownX = 1.2f;
    public float scaleDownY = 0.85f;

    [Header("Top Down Scale Settings")]
    public float tdScaleUpX = 1.15f;
    public float tdScaleUpY = 1.15f;
    public float tdScalePulseSpeed = 0.4f;

    private WaypointMover _mover;
    private Vector3 _baseScale;
    private Vector3 _baseSpriteScale;  // store sprite's base scale separately
    private Tween _bounceTween;
    private Tween _scaleTween;

    // Dedicated bounce object — child of root, parent of sprite
    // This never gets flipped so Y always means up
    private GameObject _bouncepivot;

    void Awake()
    {
        // Force reset sprite scale before capturing base — 
        // WaypointMover may have already modified it in Awake
        if (spriteRenderer != null)
            spriteRenderer.transform.localScale = Vector3.one;
    
        _baseSpriteScale = Vector3.one; // always start from (1,1,1)
    }
    
    void Start()
    {
        _mover = GetComponent<WaypointMover>();
        _baseScale = transform.localScale;
    
        SetupBouncePivot();
    
        _mover.OnDirectionChanged += HandleDirectionChanged;
        StartHorizontalBounce();
    }

    void OnDestroy()
    {
        if (_mover != null)
            _mover.OnDirectionChanged -= HandleDirectionChanged;

        _bounceTween?.Kill();
        _scaleTween?.Kill();
    }

    void SetupBouncePivot()
    {
        // Create a pivot between root and sprite
        // Root → BouncePivot → SpritePivot (created by WaypointMover) → SpriteRenderer
        _bouncepivot = new GameObject("BouncePivot");
        _bouncepivot.transform.SetParent(transform);
        _bouncepivot.transform.localPosition = Vector3.zero;
        _bouncepivot.transform.localRotation = Quaternion.identity;
        _bouncepivot.transform.localScale = Vector3.one;

        // Re-parent sprite under bounce pivot
        spriteRenderer.transform.SetParent(_bouncepivot.transform);
        spriteRenderer.transform.localPosition = Vector3.zero;

        _baseSpriteScale = spriteRenderer.transform.localScale;
    }

    private void HandleDirectionChanged(bool isHorizontal)
    {
        _bounceTween?.Kill();
        _scaleTween?.Kill();

        // Reset position and scale only — never touch flip here
        _bouncepivot.transform.localPosition = Vector3.zero;
        spriteRenderer.transform.localScale = _baseSpriteScale;

        // DON'T reset flipX/flipY here — WaypointMover handles that

        if (isHorizontal)
            StartHorizontalBounce();
        else
            StartTopDownPulse();
    }

    private void StartHorizontalBounce()
    {
        // Bounce the PIVOT up and down — never affected by sprite flip
        Sequence bounce = DOTween.Sequence();
        bounce.Append(
            _bouncepivot.transform.DOLocalMoveY(bounceHeight, bounceDuration * 0.45f)
                .SetEase(Ease.OutQuad)
        );
        bounce.Append(
            _bouncepivot.transform.DOLocalMoveY(0f, bounceDuration * 0.55f)
                .SetEase(Ease.InQuad)
        );
        bounce.SetLoops(-1, LoopType.Restart);
        _bounceTween = bounce;

        // Scale the SPRITE — stretch up, squash landing
        Sequence scale = DOTween.Sequence();
        scale.Append(
            spriteRenderer.transform.DOScale(
                new Vector3(_baseSpriteScale.x * scaleUpX, _baseSpriteScale.y * scaleUpY, 1f),
                bounceDuration * 0.45f
            ).SetEase(Ease.OutQuad)
        );
        scale.Append(
            spriteRenderer.transform.DOScale(
                new Vector3(_baseSpriteScale.x * scaleDownX, _baseSpriteScale.y * scaleDownY, 1f),
                bounceDuration * 0.2f
            ).SetEase(Ease.InQuad)
        );
        scale.Append(
            spriteRenderer.transform.DOScale(
                _baseSpriteScale,
                bounceDuration * 0.35f
            ).SetEase(Ease.OutBack)
        );
        scale.SetLoops(-1, LoopType.Restart);
        _scaleTween = scale;
    }

    private void StartTopDownPulse()
    {
        // No bounce — just scale pulse on sprite
        Sequence pulse = DOTween.Sequence();
        pulse.Append(
            spriteRenderer.transform.DOScale(
                new Vector3(_baseSpriteScale.x * tdScaleUpX,
                            _baseSpriteScale.y * tdScaleUpY, 1f),
                tdScalePulseSpeed * 0.5f
            ).SetEase(Ease.OutSine)
        );
        pulse.Append(
            spriteRenderer.transform.DOScale(
                _baseSpriteScale,
                tdScalePulseSpeed * 0.5f
            ).SetEase(Ease.InSine)
        );
        pulse.SetLoops(-1, LoopType.Restart);
        _scaleTween = pulse;
    }
}