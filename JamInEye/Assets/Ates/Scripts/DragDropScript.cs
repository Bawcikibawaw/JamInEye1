using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class SlimeThrower : MonoBehaviour
{
    [Header("Flight & Physics")]
    public float launchForce = 32f; // Increased for a punchier release
    public float maxDragDistance = 4.5f;
    public float stopThreshold = 0.5f;
    public float airDrag = 1.3f;

    [Header("Shadow Swimming")]
    public bool canMoveWASD = false;
    public float wasdSpeed = 15f;
    public List<Collider2D> activeShadows = new List<Collider2D>();
    [HideInInspector] public float launchGraceTimer = 0f;
    [HideInInspector] public float bounceGraceTimer = 0f;

    [Header("Bones & Visuals")]
    public Rigidbody2D[] edgeBones;
    public LineRenderer trajectoryLine;
    public int trajectoryPoints = 22;
    public float trajectoryTimeStep = 0.05f;

    private Rigidbody2D _rb; // Bone 9
    private SpriteRenderer _mainSR;
    private Vector2[] _initialOffsets;
    private Rigidbody2D[] _edgeRBs;
    private Vector2 _dragStartWorld;
    private bool _isDragging, _isFlying;
    private float _targetFrictionVisual = 0f;

    private int _playerLayer;
    private int _wallLayer;

    public bool IsDragging => _isDragging;
    public void SetFrictionVisual(float val) => _targetFrictionVisual = val;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _mainSR = GetComponent<SpriteRenderer>();
        _initialOffsets = new Vector2[edgeBones.Length];
        _edgeRBs = new Rigidbody2D[edgeBones.Length];

        // Identify layers
        _playerLayer = gameObject.layer;
        _wallLayer = LayerMask.NameToLayer("Walls");
        
        // Safety check: if you didn't name your layer "Walls", it defaults to ignoring nothing
        if (_wallLayer == -1) Debug.LogError("Layer 'Walls' not found! Please create it in Unity.");

        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        for (int i = 0; i < edgeBones.Length; i++)
        {
            _initialOffsets[i] = edgeBones[i].transform.localPosition;
            _edgeRBs[i] = edgeBones[i].GetComponent<Rigidbody2D>();
            if (_edgeRBs[i] != null) {
                _edgeRBs[i].collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                _edgeRBs[i].gravityScale = 0;
            }
        }
    }

    void Update()
    {
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0f);

        if (launchGraceTimer > 0) launchGraceTimer -= Time.deltaTime;
        if (bounceGraceTimer > 0) bounceGraceTimer -= Time.deltaTime;

        if (_isFlying) { UpdateFlight(); return; }
        if (canMoveWASD && !_isDragging) HandleWASD();

        if (Input.GetMouseButtonDown(0)) StartDragging();
        if (Input.GetMouseButton(0) && _isDragging) UpdateDragging();
        if (Input.GetMouseButtonUp(0) && _isDragging) Launch();

        UpdateVisuals();
    }

    private void StartDragging()
    {
        _isDragging = true;
        _isFlying = false;
        canMoveWASD = false;
        _rb.isKinematic = true;
        _rb.linearVelocity = Vector2.zero;
        _dragStartWorld = GetMouseWorldPos();
        
        if (trajectoryLine != null) trajectoryLine.enabled = true;

        // GHOST MODE: Ignore walls during drag
        if (_wallLayer != -1) Physics2D.IgnoreLayerCollision(_playerLayer, _wallLayer, true);
        ToggleAllColliders(false);
    }

    private void UpdateDragging()
    {
        Vector2 currentM = GetMouseWorldPos();
        Vector2 delta = Vector2.ClampMagnitude(currentM - _dragStartWorld, maxDragDistance);
        Vector2 localDelta = transform.InverseTransformDirection(delta);

        for (int i = 0; i < edgeBones.Length; i++)
        {
            float bAngle = Mathf.Atan2(_initialOffsets[i].y, _initialOffsets[i].x) * Mathf.Rad2Deg;
            float dAngle = Mathf.Atan2(localDelta.y, localDelta.x) * Mathf.Rad2Deg;
            if (Mathf.Abs(Mathf.DeltaAngle(bAngle, dAngle)) < 65f)
                edgeBones[i].transform.localPosition = _initialOffsets[i] + (localDelta.normalized * 0.9f);
            else
                edgeBones[i].transform.localPosition = Vector2.Lerp(edgeBones[i].transform.localPosition, _initialOffsets[i], Time.deltaTime * 12f);
        }
        if (trajectoryLine != null) DrawTrajectory(-delta * launchForce);
    }

    private void Launch()
    {
        _isDragging = false;
        Vector2 delta = Vector2.ClampMagnitude(GetMouseWorldPos() - _dragStartWorld, maxDragDistance);
        Vector2 launchVel = -delta * launchForce;

        // 1. BECOME SOLID IMMEDIATELY
        if (_wallLayer != -1) Physics2D.IgnoreLayerCollision(_playerLayer, _wallLayer, false);
        ToggleAllColliders(true);

        // 2. APPLY FORCE TO ALL RIGIDBODIES
        _rb.isKinematic = false;
        _rb.linearVelocity = launchVel;

        for (int i = 0; i < edgeBones.Length; i++)
        {
            _edgeRBs[i].isKinematic = false; // Ensure they aren't kinematic
            edgeBones[i].transform.localPosition = _initialOffsets[i];
            _edgeRBs[i].linearVelocity = launchVel;
        }

        // 3. START FLIGHT
        _isFlying = true;
        launchGraceTimer = 0.5f; 
        activeShadows.Clear();
        if (trajectoryLine != null) trajectoryLine.enabled = false;
        
        Debug.Log("<color=green>[Launch]</color> Release successful. Velocity: " + launchVel.magnitude);
    }

    private void UpdateFlight()
    {
        _rb.linearVelocity *= (1f - Time.deltaTime * airDrag);
        
        // --- THE FIX: LAUNCH PROTECTION ---
        // We wait until launchGraceTimer is below 0.4s to allow the physics to "start"
        if (launchGraceTimer <= 0.4f && bounceGraceTimer <= 0 && _rb.linearVelocity.magnitude < stopThreshold)
        {
            FreezeSlime();
        }
    }

    public void FreezeSlime()
    {
        _isFlying = false;
        _rb.isKinematic = false; // Keep it dynamic so it can move via WASD/Shadows
        _rb.linearVelocity = Vector2.zero;
        
        if (_wallLayer != -1) Physics2D.IgnoreLayerCollision(_playerLayer, _wallLayer, false);
        ToggleAllColliders(true);

        for (int i = 0; i < edgeBones.Length; i++)
        {
            edgeBones[i].transform.localPosition = _initialOffsets[i];
            _edgeRBs[i].linearVelocity = Vector2.zero;
        }
    }

    private Vector2 GetMouseWorldPos()
    {
        Vector3 m = Input.mousePosition;
        m.z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        return Camera.main.ScreenToWorldPoint(m);
    }

    void DrawTrajectory(Vector2 vel)
    {
        trajectoryLine.positionCount = trajectoryPoints;
        Vector2 p = transform.position;
        for (int i = 0; i < trajectoryPoints; i++)
        {
            trajectoryLine.SetPosition(i, new Vector3(p.x, p.y, transform.position.z));
            p += vel * trajectoryTimeStep;
        }
    }

    private void ToggleAllColliders(bool s)
    {
        GetComponent<Collider2D>().enabled = s;
        foreach (var b in edgeBones) b.GetComponent<Collider2D>().enabled = s;
    }

    // ... (Keep existing HandleWASD, AddShadow, RemoveShadow, UpdateVisuals)
    
    private void HandleWASD() {
        if (activeShadows.Count == 0) { canMoveWASD = false; return; }
        float h = Input.GetAxisRaw("Horizontal"), v = Input.GetAxisRaw("Vertical");
        Vector2 target = _rb.position + new Vector2(h, v).normalized * wasdSpeed * Time.deltaTime;
        bool inside = false;
        foreach (var s in activeShadows) if (s != null && s.OverlapPoint(target)) inside = true;
        if (!inside && activeShadows.Count > 0) {
            Vector2 closest = activeShadows[0].ClosestPoint(target);
            Vector2 toCenter = ((Vector2)activeShadows[0].transform.position - closest).normalized;
            target = closest + (toCenter * 0.15f);
        }
        _rb.MovePosition(target);
    }

    void OnCollisionEnter2D(Collision2D col) { if (_isFlying) bounceGraceTimer = 0.25f; }

    private void UpdateVisuals() {
        if (_mainSR != null) {
            Color t = _targetFrictionVisual > 0.1f ? Color.black : new Color(0, 0, 0, 0.7f);
            _mainSR.color = Color.Lerp(_mainSR.color, t, Time.deltaTime * 5f);
        }
        _targetFrictionVisual = Mathf.Lerp(_targetFrictionVisual, 0, Time.deltaTime * 2f);
    }

    public void AddShadow(Collider2D c) { if (!activeShadows.Contains(c)) activeShadows.Add(c); }
    public void RemoveShadow(Collider2D c) { activeShadows.Remove(c); }
}