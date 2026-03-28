using UnityEngine;
using UnityEngine.UI;
public class PlayerStats : MonoBehaviour
{
    [Header("Eye Visuals")]
    public SpriteRenderer eyeRenderer; // Drag the Eye child object here
    public Color healthyColor = Color.white;
    public Color dangerColor = Color.red;

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
    private float _shadowTimer = 0f;
    private Collider2D _lastShadow;
    private SlimeThrower _mover;
    private bool _isDead = false;

    void Awake()
    {
        _mover = GetComponent<SlimeThrower>();
        currentHP = maxHP;

        // Auto-find eyes if not assigned
        if (eyeRenderer == null) eyeRenderer = GetComponentInChildren<SpriteRenderer>();
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
        
        if(inDanger) currentHP = Mathf.Clamp(currentHP, 0, maxHpInDanger);

        UpdateEyeColor();

        if (currentHP <= 0) Die(inShadow ? "THE DARKNESS CONSUMED YOU" : "VAPORIZED BY SUNLIGHT");
    }

    private void UpdateEyeColor()
    {
        if (eyeRenderer == null) return;

        Color targetColor = healthyColor;

        // ── COLOR PRIORITY ──
        // 1. DANGER (Low HP or massive Sun Debt)
        if (currentHP < (maxHP * 0.3f) || _currentSunMultiplier > 4f)
        {
            targetColor = dangerColor;
        }
        else
        {
            targetColor = healthyColor;
        }
        
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

        if (_shadowTimer >= 3)
        {
            inDanger = true;
        }
        else
        {
            inDanger = false; 
        }
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
    }
}