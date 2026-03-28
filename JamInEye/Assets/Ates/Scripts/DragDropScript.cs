using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class SlimeThrower : MonoBehaviour
{
    [Header("Flight & Physics")]
    public float launchForce = 35f;
    public float maxDragDistance = 4.0f;
    public float stopThreshold = 0.5f;
    public float airDrag = 1.5f;
    public float jumpPowerCost = 20f;

    [Header("Magnetic Pull (Anti-Pancake)")]
    [Range(5, 60)] public float boneElasticity = 35f; // Higher = stiffer/rounder, lower = more jelly
    public float maxBoneDrift = 1.1f; // How far a bone can stretch from center

    [Header("Shadow Swimming")]
    public bool canMoveWASD = false;
    public float wasdSpeed = 15f;
    public List<Collider2D> activeShadows = new List<Collider2D>();
    [HideInInspector] public float launchGraceTimer = 0f;
    [HideInInspector] public float bounceGraceTimer = 0f;

    [Header("Visual Settings (Opacity Only)")]
    public float normalAlpha = 1.0f;
    public float swimmingAlpha = 0.3f;
    private float _currentAlpha = 1.0f;
    private float _currentSqueeze = 1.0f;

    [Header("Bones")]
    public Rigidbody2D[] edgeBones; // Assign Bones 1-8
    public LineRenderer trajectoryLine;
    public int trajectoryPoints = 20;
    public float trajectoryTimeStep = 0.05f;

    private Rigidbody2D _rb; // Bone 9
    private SpriteRenderer _mainSR; // Looks up to SOFT parent
    private Vector2[] _initialOffsets;
    private Rigidbody2D[] _edgeRBs;
    private Vector2 _dragStartWorld;
    private bool _isDragging, _isFlying;
    private int _playerLayer, _wallLayer;
    private PlayerStats _stats;
    
    [Header("Shadow Collider Settings")]
    public float normalColliderRadius = 0.3f;   // bone_9 radius normally
    public float shadowColliderRadius = 1.2f;   // bone_9 radius inside shadow

    [Header("Bone Drag Physics")]
    public float dragBoneSpring = 140f;
    public float dragBoneDamping = 18f;
    public float dragBoneMaxForce = 80f;
    public float dragBoneMaxSpeed = 12f;
    public float dragAngleThreshold = 65f;
    public float homeSpringMultiplier = 0.7f;

    private CircleCollider2D _centerCollider;

    private Vector2 _dragStartScreen;
    public bool IsDragging => _isDragging;
    public void SqueezeBones(float ratio) => _currentSqueeze = ratio;
    
    public void EnterShadowColliderMode()
    {
        // Grow bone_9 collider
        if (_centerCollider != null)
            _centerCollider.radius = shadowColliderRadius;

        // Disable all edge bone colliders
        foreach (var bone in edgeBones)
        {
            var col = bone.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }
    }

    public void ExitShadowColliderMode()
    {
        // Restore bone_9 collider
        if (_centerCollider != null)
            _centerCollider.radius = normalColliderRadius;

        // Re-enable all edge bone colliders
        foreach (var bone in edgeBones)
        {
            var col = bone.GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
        }
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _mainSR = GetComponentInParent<SpriteRenderer>();
        _stats = GetComponent<PlayerStats>();
        _centerCollider = GetComponent<CircleCollider2D>();

        _playerLayer = gameObject.layer;
        _wallLayer = LayerMask.NameToLayer("Walls");

        _initialOffsets = new Vector2[edgeBones.Length];
        _edgeRBs = new Rigidbody2D[edgeBones.Length];

        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        for (int i = 0; i < edgeBones.Length; i++)
        {
            _initialOffsets[i] = edgeBones[i].transform.localPosition;
            _edgeRBs[i] = edgeBones[i].GetComponent<Rigidbody2D>();
            if (_edgeRBs[i] != null) 
            {
                _edgeRBs[i].collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                _edgeRBs[i].gravityScale = 0;
            }
        }
    }

    void Update()
    {
        // Force Z-Lock to plane
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0f);

        if (launchGraceTimer > 0) launchGraceTimer -= Time.deltaTime;
        if (bounceGraceTimer > 0) bounceGraceTimer -= Time.deltaTime;

        if (_isFlying) UpdateFlight();
        if (canMoveWASD && !_isDragging) HandleWASD();

        if (Input.GetMouseButtonDown(0)) StartDragging();
        if (Input.GetMouseButton(0) && _isDragging) UpdateDragging();
        if (Input.GetMouseButtonUp(0) && _isDragging) Launch();

        UpdateVisualsAndMagneticPull();
    }

    private void UpdateVisualsAndMagneticPull()
    {
        // 1. Transparency Fade — fade whenever inside a shadow, not just when swimming
        if (_mainSR != null)
        {
            float targetAlpha = (activeShadows.Count > 0) ? swimmingAlpha : normalAlpha;
            _currentAlpha = Mathf.Lerp(_currentAlpha, targetAlpha, Time.deltaTime * 6f);
            Color c = _mainSR.color;
            c.a = _currentAlpha;
            _mainSR.color = c;
        }

        // 2. Squeeze transition
        _currentSqueeze = Mathf.Lerp(_currentSqueeze, 1.0f, Time.deltaTime * 4f);

        // 3. Magnetic pull
        if (!_isDragging)
        {
            for (int i = 0; i < edgeBones.Length; i++)
            {
                Rigidbody2D boneRb = _edgeRBs[i];
                if (boneRb == null) continue;

                Vector2 homePos = _initialOffsets[i] * _currentSqueeze;
                ApplyIdleBoneSpring(boneRb, homePos, !_isFlying);
            }
        }
    }

    private void ApplyIdleBoneSpring(Rigidbody2D boneRb, Vector2 targetLocal, bool stronglyDamp)
    {
        Vector2 targetWorld = transform.TransformPoint(targetLocal);
        Vector2 toTarget = targetWorld - boneRb.position;

        float spring = boneElasticity * 8f;
        float damping = stronglyDamp ? dragBoneDamping * 1.4f : dragBoneDamping;

        Vector2 force = toTarget * spring - boneRb.linearVelocity * damping;

        float maxForce = stronglyDamp ? dragBoneMaxForce : dragBoneMaxForce * 0.8f;
        if (force.magnitude > maxForce)
            force = force.normalized * maxForce;

        boneRb.AddForce(force, ForceMode2D.Force);

        if (stronglyDamp)
            boneRb.linearVelocity *= 0.9f;
    }

    private void UpdateDragging()
    {
        Vector2 delta = GetDragDeltaWorldLike();

        Vector2 launchDirWorld = delta.sqrMagnitude > 0.0001f ? (-delta).normalized : Vector2.zero;
        Vector2 pullDirWorld = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.zero;

        // Convert pull direction into local space because initial offsets are local
        Vector2 pullDirLocal = delta.sqrMagnitude > 0.0001f
            ? (Vector2)transform.InverseTransformDirection(pullDirWorld).normalized
            : Vector2.zero;

        //for (int i = 0; i < edgeBones.Length; i++)
        //{
        //    Rigidbody2D boneRb = _edgeRBs[i];
        //    if (boneRb == null) continue;

        //    Vector2 boneHomeLocal = _initialOffsets[i];
        //    Vector2 targetLocal = boneHomeLocal;

        //    if (delta.sqrMagnitude > 0.0001f)
        //    {
        //        float boneAngle = Mathf.Atan2(boneHomeLocal.y, boneHomeLocal.x) * Mathf.Rad2Deg;
        //        float pullAngle = Mathf.Atan2(pullDirLocal.y, pullDirLocal.x) * Mathf.Rad2Deg;

        //        if (Mathf.Abs(Mathf.DeltaAngle(boneAngle, pullAngle)) < dragAngleThreshold)
        //        {
        //            targetLocal = boneHomeLocal + pullDirLocal * maxBoneDrift;
        //        }
        //    }

        //    ApplyBoneSpringToLocalTarget(boneRb, targetLocal, delta.sqrMagnitude > 0.0001f);
        //}

        if (trajectoryLine != null)
            DrawTrajectory(delta.sqrMagnitude > 0.0001f ? launchDirWorld * (delta.magnitude * launchForce) : Vector2.zero);
    }
    private void ApplyBoneSpringToLocalTarget(Rigidbody2D boneRb, Vector2 targetLocal, bool isPulledBonePhase)
    {
        Vector2 targetWorld = transform.TransformPoint(targetLocal);
        Vector2 toTarget = targetWorld - boneRb.position;

        float spring = isPulledBonePhase ? dragBoneSpring : dragBoneSpring * homeSpringMultiplier;
        float damping = dragBoneDamping;

        Vector2 force = toTarget * spring - boneRb.linearVelocity * damping;

        if (force.magnitude > dragBoneMaxForce)
            force = force.normalized * dragBoneMaxForce;

        boneRb.AddForce(force, ForceMode2D.Force);

        if (boneRb.linearVelocity.magnitude > dragBoneMaxSpeed)
            boneRb.linearVelocity = boneRb.linearVelocity.normalized * dragBoneMaxSpeed;
    }

    private void Launch()
    {
        PlayerStats stats = GetComponent<PlayerStats>();

        if (stats != null && !stats.ConsumeJumpPower(jumpPowerCost))
        {
            Debug.Log("Not enough power to jump!");
            _isDragging = false;
            if (trajectoryLine != null) trajectoryLine.enabled = false;
            return;
        }

        _isDragging = false;

        Vector2 delta = GetDragDeltaWorldLike();
        Vector2 launchVel = -delta * launchForce;

        if (_wallLayer != -1) Physics2D.IgnoreLayerCollision(_playerLayer, _wallLayer, false);
        ToggleColliders(true);

        _rb.isKinematic = false;
        _rb.linearVelocity = launchVel;
        _rb.angularVelocity = 0f;

        for (int i = 0; i < edgeBones.Length; i++)
        {
            _edgeRBs[i].isKinematic = false;
            _edgeRBs[i].linearVelocity = launchVel;
            _edgeRBs[i].MovePosition(transform.TransformPoint(_initialOffsets[i]));
        }

        _isFlying = true;
        launchGraceTimer = 0.5f;
        activeShadows.Clear();

        if (trajectoryLine != null) trajectoryLine.enabled = false;
        MainAudioManager.Instance.Play("RelaseSFX");
    }


    private void HandleWASD()
    {
        if (activeShadows.Count == 0) { canMoveWASD = false; return; }

        float h = Input.GetAxisRaw("Horizontal"), v = Input.GetAxisRaw("Vertical");
        Vector2 inputDir = new Vector2(h, v).normalized;

        // Apply movement force
        if (inputDir != Vector2.zero)
            _rb.AddForce(inputDir * wasdSpeed, ForceMode2D.Impulse);

        // Damp velocity so it doesn't slide forever
        _rb.linearVelocity *= 0.75f;

        // Check if currently outside all shadows
        bool inside = false;
        foreach (var s in activeShadows)
            if (s != null && s.OverlapPoint(_rb.position)) inside = true;

        if (!inside && activeShadows.Count > 0)
        {
            // Find closest point on the nearest shadow and push back in
            Collider2D nearest = activeShadows[0];
            Vector2 closest = nearest.ClosestPoint(_rb.position);
            Vector2 pushDir = (closest - _rb.position).normalized;

            // Kill outward velocity and push back
            _rb.linearVelocity = Vector2.zero;
            _rb.AddForce(pushDir * wasdSpeed * 2f, ForceMode2D.Impulse);
        }
    }


    private void StartDragging()
    {
        _isDragging = true;
        _isFlying = false;
        canMoveWASD = false;

        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;

        _dragStartScreen = Input.mousePosition;

        if (trajectoryLine != null) trajectoryLine.enabled = true;
        if (_wallLayer != -1) Physics2D.IgnoreLayerCollision(_playerLayer, _wallLayer, true);
        ToggleColliders(false);
    }

    private Vector2 GetDragDeltaWorldLike()
    {
        Vector2 currentScreen = Input.mousePosition;
        Vector2 screenDelta = currentScreen - _dragStartScreen;

        // Convert screen delta to world-size delta
        Camera cam = Camera.main;

        Vector3 a = cam.ScreenToWorldPoint(new Vector3(0f, 0f, Mathf.Abs(cam.transform.position.z - transform.position.z)));
        Vector3 b = cam.ScreenToWorldPoint(new Vector3(screenDelta.x, screenDelta.y, Mathf.Abs(cam.transform.position.z - transform.position.z)));

        Vector2 worldDelta = (Vector2)(b - a);
        return Vector2.ClampMagnitude(worldDelta, maxDragDistance);
    }

    private Vector2 GetMouseWorldPos() {
        Vector3 m = Input.mousePosition; m.z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        return Camera.main.ScreenToWorldPoint(m);
    }
    private void ToggleColliders(bool s) {
        GetComponent<Collider2D>().enabled = s;
        foreach (var b in edgeBones) b.GetComponent<Collider2D>().enabled = s;
    }
    void OnCollisionEnter2D(Collision2D col) { if (_isFlying) bounceGraceTimer = 0.25f; }
    private void UpdateFlight()
    {
        _rb.linearVelocity *= (1f - Time.deltaTime * airDrag);
        if (_rb.linearVelocity.magnitude <=2f)
        {
            //Debug.Log("SOundShouldStop");
            //MainAudioManager.Instance.Stop("FlySFX");
            //MainAudioManager.Instance.Stop("FlySFX",0.3f);
            
            _isFlying = false;
        }
    }
    public void AddShadow(Collider2D c) { if (!activeShadows.Contains(c)) activeShadows.Add(c); }
    public void RemoveShadow(Collider2D c) { activeShadows.Remove(c); }
    void DrawTrajectory(Vector2 vel) {
        trajectoryLine.positionCount = trajectoryPoints; Vector2 p = transform.position;
        for (int i = 0; i < trajectoryPoints; i++) { trajectoryLine.SetPosition(i, new Vector3(p.x, p.y, transform.position.z)); p += vel * trajectoryTimeStep; }
    }
}