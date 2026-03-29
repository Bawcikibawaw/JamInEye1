// PlayerStats.cs
using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    [Header("Eye Visuals")]
    public SpriteRenderer eyeRenderer;
    public Color healthyColor = Color.white;
    public Color dangerColor = Color.red;
    private Transform _transform;
    private Transform _parentTransform;

    [Header("HP & Power")]
    public float maxHP = 100f;
    public float maxHpInDanger = 100f;
    public float currentHP;

    [Header("Overcharge")]
    public float maxOvercharge = 50f;   // tune in Inspector
    public float currentOvercharge = 0f;
    public float overchargeRate = 15f;  // units per second while HP is full in shadow

    [Header("Shadow Benefits")]
    public float shadowRegenRate = 20f;
    public bool inDanger = false;

    [Header("Sun Penalty (The Cliffhangers)")]
    public float sunTickDamageBase = 12f;
    public float penaltyIncreaseRate = 1.5f;

    private float _currentSunMultiplier = 1f;
    private Collider2D _lastShadow;
    private SlimeThrower _mover;
    private bool _isDead = false;

    private ChargeBar _chargeBar;
    // Public read-only helpers for the UI
    public float TotalCharge => currentHP + currentOvercharge;
    public float MaxTotalCharge => maxHP + maxOvercharge;

    void Awake()
    {
        _mover = GetComponent<SlimeThrower>();
        currentHP = maxHP;
        _transform = GetComponent<Transform>();
        _parentTransform = transform.parent;

        if (eyeRenderer == null)
            eyeRenderer = GetComponentInChildren<SpriteRenderer>();

        _chargeBar = FindFirstObjectByType<ChargeBar>();
    }

    void Update()
    {
        if (_isDead) return;

        bool inShadow = _mover.activeShadows.Count > 0;

        if (inShadow)
            HandleInsideShadow(_mover.activeShadows[0]);
        else
            HandleOutsideShadow();

        // Clamp base HP
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        if (inDanger) currentHP = Mathf.Clamp(currentHP, 0, maxHpInDanger);

        // Clamp overcharge
        currentOvercharge = Mathf.Clamp(currentOvercharge, 0, maxOvercharge);

        UpdateEyeColor();

        // Death checks
        if (currentOvercharge >= maxOvercharge)
            Die("THE DARKNESS CONSUMED YOU");
        else if (currentHP <= 0 && currentOvercharge <= 0)
            Die("VAPORIZED BY SUNLIGHT");

        if (_isDead) DOVirtual.DelayedCall(3f, Respawn);
    }

    private void UpdateEyeColor()
    {
        if (eyeRenderer == null) return;

        Color targetColor = (currentHP < maxHP * 0.3f || currentOvercharge > maxOvercharge * 0.5f)
            ? dangerColor
            : healthyColor;

        eyeRenderer.color = Color.Lerp(eyeRenderer.color, targetColor, Time.deltaTime * 5f);
    }

    private void HandleInsideShadow(Collider2D currentShadow)
    {
        // Reset sun multiplier when entering a new shadow
        if (currentShadow != _lastShadow)
        {
            _lastShadow = currentShadow;
            _currentSunMultiplier = 1f;
        }

        // Regen base HP first
        if (currentHP < maxHP)
        {
            currentHP += shadowRegenRate * Time.deltaTime;
        }
        else
        {
            // Base HP is full → start filling overcharge
            currentOvercharge += overchargeRate * Time.deltaTime;
            _currentSunMultiplier += penaltyIncreaseRate * Time.deltaTime;
        }

        // inDanger as soon as overcharge begins accumulating
        inDanger = currentOvercharge > 0f;
    }

    private void HandleOutsideShadow()
    {
        // Drain overcharge first, then base HP
        float damage = sunTickDamageBase * _currentSunMultiplier * Time.deltaTime;

        if (currentOvercharge > 0f)
        {
            float overchargeDrain = Mathf.Min(currentOvercharge, damage);
            currentOvercharge -= overchargeDrain;
            damage -= overchargeDrain;
        }

        currentHP -= damage;

        // Once out of shadow, reset sun multiplier ramp
        _currentSunMultiplier = Mathf.Max(1f, _currentSunMultiplier - Time.deltaTime);
        inDanger = currentOvercharge > 0f;
    }

    /// <summary>
    /// Consumes from total charge (overcharge first, then base HP).
    /// </summary>
    public bool ConsumeJumpPower(float cost)
    {
        if (TotalCharge >= cost)
        {
            float overchargeDrain = Mathf.Min(currentOvercharge, cost);
            currentOvercharge -= overchargeDrain;
            cost -= overchargeDrain;
            currentHP -= cost;
            return true;
        }
        return false;
    }

    private void Die(string reason)
    {
        if (_isDead) return;
        _isDead = true;
        Debug.Log("<color=red>GAME OVER: </color>" + reason);

        _mover.enabled = false;
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        if (eyeRenderer != null) eyeRenderer.color = Color.black;

        StartCoroutine(DieRoutine());
    }

    private IEnumerator DieRoutine()
    {
        Image fadeImage = GetFadeImage();
        if (fadeImage != null)
        {
            fadeImage.DOKill();
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
            fadeImage.DOFade(1f, 0.4f).SetEase(Ease.InQuad).SetUpdate(true);
            yield return new WaitForSecondsRealtime(0.4f);
        }

        _transform.position = _parentTransform.position;
        Respawn();

        yield return null;
        yield return null;

        if (fadeImage != null)
        {
            fadeImage.DOKill();
            fadeImage.color = new Color(0f, 0f, 0f, 1f);
            fadeImage.DOFade(0f, 0.4f).SetEase(Ease.OutQuad).SetUpdate(true);
        }
    }

    private void Respawn()
    {
        inDanger = false;
        currentOvercharge = 0f;
        _currentSunMultiplier = 1f;
        _isDead = false;
        currentHP = maxHP;

        // Force the vignette to snap instantly on respawn
        //if (_chargeBar != null)
        //{
        //    _chargeBar.ForceResetVignette();
        //}

        if (_mover != null)
        {
            _mover.ResetPhysicsState();
            _mover.enabled = true;
        }

        if (eyeRenderer != null)
            eyeRenderer.color = healthyColor;
    }

    private Image GetFadeImage()
    {
        GameObject canvas = GameObject.Find("FadeCanvas");
        if (canvas != null) return canvas.GetComponentInChildren<Image>();
        return null;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("BrightObs"))
            Die("Beyaz");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("BrightObs"))
            Respawn();
    }
}