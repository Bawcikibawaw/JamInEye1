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
    public float maxOvercharge = 50f;   
    public float currentOvercharge = 0f;
    public float overchargeRate = 15f;  

    [Header("Shadow Benefits")]
    public float shadowRegenRate = 20f;
    public bool inDanger = false;

    [Header("Sun Penalty")]
    public float sunTickDamageBase = 12f;
    public float penaltyIncreaseRate = 1.5f;

    private float _currentSunMultiplier = 1f;
    private Collider2D _lastShadow;
    private SlimeThrower _mover;
    private bool _isDead = false;

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
        currentOvercharge = Mathf.Clamp(currentOvercharge, 0, maxOvercharge);

        UpdateEyeColor();

        // Death checks
        if (currentOvercharge >= maxOvercharge)
            Die("THE DARKNESS CONSUMED YOU");
        else if (currentHP <= 0 && currentOvercharge <= 0)
            Die("VAPORIZED BY SUNLIGHT");
    }

    private void UpdateEyeColor()
    {
        if (eyeRenderer == null) return;
        Color targetColor = (currentHP < maxHP * 0.3f || currentOvercharge > maxOvercharge * 0.5f) ? dangerColor : healthyColor;
        eyeRenderer.color = Color.Lerp(eyeRenderer.color, targetColor, Time.deltaTime * 5f);
    }

    private void HandleInsideShadow(Collider2D currentShadow)
    {
        if (currentShadow != _lastShadow)
        {
            _lastShadow = currentShadow;
            _currentSunMultiplier = 1f;
        }

        if (currentHP < maxHP)
            currentHP += shadowRegenRate * Time.deltaTime;
        else
        {
            currentOvercharge += overchargeRate * Time.deltaTime;
            _currentSunMultiplier += penaltyIncreaseRate * Time.deltaTime;
        }

        inDanger = currentOvercharge > 0f;
    }

    private void HandleOutsideShadow()
    {
        float damage = sunTickDamageBase * _currentSunMultiplier * Time.deltaTime;

        if (currentOvercharge > 0f)
        {
            float overchargeDrain = Mathf.Min(currentOvercharge, damage);
            currentOvercharge -= overchargeDrain;
            damage -= overchargeDrain;
        }

        currentHP -= damage;
        _currentSunMultiplier = Mathf.Max(1f, _currentSunMultiplier - Time.deltaTime);
        inDanger = currentOvercharge > 0f;
    }

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

        // Stop movement immediately
        _mover.enabled = false;
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        if (eyeRenderer != null) eyeRenderer.color = Color.black;


        MainAudioManager.Instance.Play("PlayerDeadSFX");
        StartCoroutine(DieRoutine());
    }

    private IEnumerator DieRoutine()
    {
        // 1. Fade out using the Global Manager
        yield return FadeManager.Instance.FadeOut(0.4f).WaitForCompletion();

        // 2. The 3-second "Death Delay" happens while screen is black
        yield return new WaitForSecondsRealtime(2.0f); 

        // 3. Teleport and Reset Physics
        _transform.position = _parentTransform.position;
        Respawn();

        // 4. Wait for physics to settle
        yield return new WaitForSecondsRealtime(0.2f);

        // 5. Fade back in
        FadeManager.Instance.FadeIn(0.4f);

    }

    private void Respawn()
    {
        inDanger = false;
        currentOvercharge = 0f;
        _currentSunMultiplier = 1f;
        _isDead = false;
        currentHP = maxHP;

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
        // Optional: If you want to keep the player dead until the routine finishes,
        // leave this blank or add logic here.
    }
}