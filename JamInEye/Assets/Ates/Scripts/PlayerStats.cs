// PlayerStats.cs
using UnityEngine;
using DG.Tweening;

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

    [Header("Shadow Benefits")]
    public float shadowRegenRate = 20f;
    public bool inDanger = false;

    [Header("Sun Penalty (The Cliffhangers)")]
    public float sunTickDamageBase = 12f;
    public float penaltyIncreaseRate = 1.5f;
    public float maxShadowTime = 8f;

    private float _currentSunMultiplier = 1f;
    public float _shadowTimer = 0f;
    private Collider2D _lastShadow;
    private SlimeThrower _mover;
    private bool _isDead = false;

    void Awake()
    {
        _mover = GetComponent<SlimeThrower>();
        currentHP = maxHP;
        _transform = GetComponent<Transform>();
        _parentTransform = transform.parent;

        if (eyeRenderer == null)
            eyeRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (_isDead) return;

        bool inShadow = _mover.activeShadows.Count > 0;

        if (inShadow)
            HandleInsideShadow(_mover.activeShadows[0]);
        else
            HandleOutsideShadow();

        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        if (inDanger) currentHP = Mathf.Clamp(currentHP, 0, maxHpInDanger);

        UpdateEyeColor();

        if (currentHP <= 0)
            Die(inShadow ? "THE DARKNESS CONSUMED YOU" : "VAPORIZED BY SUNLIGHT");

        if (_isDead) DOVirtual.DelayedCall(3f, () => {
            Respawn();
        });
    }

    private void UpdateEyeColor()
    {
        if (eyeRenderer == null) return;

        Color targetColor = (currentHP < maxHP * 0.3f || _currentSunMultiplier > 4f)
            ? dangerColor
            : healthyColor;

        eyeRenderer.color = Color.Lerp(eyeRenderer.color, targetColor, Time.deltaTime * 5f);
    }

    private void HandleInsideShadow(Collider2D currentShadow)
    {
        if (currentShadow != _lastShadow)
        {
            _lastShadow = currentShadow;
            _currentSunMultiplier = 1f;
            _shadowTimer = 0f;
        }

        currentHP += shadowRegenRate * Time.deltaTime;
        _currentSunMultiplier += penaltyIncreaseRate * Time.deltaTime;

        _shadowTimer += Time.deltaTime;
        if (_shadowTimer >= maxShadowTime) currentHP = 0;

        inDanger = _shadowTimer >= 3;
    }

    private void HandleOutsideShadow()
    {
        float damage = sunTickDamageBase * _currentSunMultiplier * Time.deltaTime;
        currentHP -= damage;
    }

    public bool ConsumeJumpPower(float cost)
    {
        if (currentHP >= cost)
        {
            currentHP -= cost;
            return true;
        }
        return false;
    }

    private void Die(string reason)
    {
        _isDead = true;
        Debug.Log("<color=red>GAME OVER: </color>" + reason);
        _mover.enabled = false;
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        if (eyeRenderer != null) eyeRenderer.color = Color.black; // Dead eyes
        _transform.position = _parentTransform.position;
    }

    private void Respawn()
    {
        inDanger = false;
        _shadowTimer = 0f;
        _currentSunMultiplier = 1f;
        _isDead = false;
        currentHP = maxHP;

        // ── THE PHYSICS RESET ──
        // Call the new cleanup function before starting the engine
        if (_mover != null)
        {
            _mover.ResetPhysicsState();
            _mover.enabled = true;
        }

        if (eyeRenderer != null)
            eyeRenderer.color = healthyColor;
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