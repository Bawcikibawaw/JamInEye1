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

    public bool IsDragging => _isDragging;
    public void SqueezeBones(float ratio) => _currentSqueeze = ratio;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _mainSR = GetComponentInParent<SpriteRenderer>();

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
        // 1. Transparency Fade
        if (_mainSR != null)
        {
            float targetAlpha = canMoveWASD ? swimmingAlpha : normalAlpha;
            _currentAlpha = Mathf.Lerp(_currentAlpha, targetAlpha, Time.deltaTime * 6f);
            Color c = _mainSR.color;
            c.a = _currentAlpha;
            _mainSR.color = c;
        }

        // 2. Squeeze transition
        _currentSqueeze = Mathf.Lerp(_currentSqueeze, 1.0f, Time.deltaTime * 4f);

        // 3. ── THE MAGNETIC PULL ──
        // If we aren't dragging, all bones MUST snap back to their circular home.
        if (!_isDragging)
        {
            for (int i = 0; i < edgeBones.Length; i++)
            {
                Vector2 homePos = _initialOffsets[i] * _currentSqueeze;
                
                // Use elasticity to pull them back home like strong rubber bands
                edgeBones[i].transform.localPosition = Vector2.Lerp(
                    edgeBones[i].transform.localPosition, 
                    homePos, 
                    Time.deltaTime * boneElasticity
                );

                // Stop drifting bones from floating away during flight
                if (!_isFlying) _edgeRBs[i].linearVelocity = Vector2.zero;
            }
        }
    }

    private void UpdateDragging()
    {
        Vector2 delta = Vector2.ClampMagnitude(GetMouseWorldPos() - _dragStartWorld, maxDragDistance);
        Vector2 localDelta = transform.InverseTransformDirection(delta);
        
        for (int i = 0; i < edgeBones.Length; i++)
        {
            float bAngle = Mathf.Atan2(_initialOffsets[i].y, _initialOffsets[i].x) * Mathf.Rad2Deg;
            float dAngle = Mathf.Atan2(localDelta.y, localDelta.x) * Mathf.Rad2Deg;
            
            // Check if this specific bone is in the "pulling zone" of the mouse
            if (Mathf.Abs(Mathf.DeltaAngle(bAngle, dAngle)) < 65f)
            {
                // Pull out toward mouse, but CLAMP the distance
                Vector2 stretchTarget = _initialOffsets[i] + (localDelta.normalized * maxBoneDrift);
                edgeBones[i].transform.localPosition = Vector2.Lerp(edgeBones[i].transform.localPosition, stretchTarget, Time.deltaTime * 20f);
            }
            else
            {
                // If mouse isn't pulling this bone, it snaps back home IMMEDIATELY
                edgeBones[i].transform.localPosition = Vector2.Lerp(edgeBones[i].transform.localPosition, _initialOffsets[i], Time.deltaTime * boneElasticity);
            }
        }
        if (trajectoryLine != null) DrawTrajectory(-delta * launchForce);
    }

    private void Launch()
    {
        _isDragging = false;
        Vector2 delta = Vector2.ClampMagnitude(GetMouseWorldPos() - _dragStartWorld, maxDragDistance);
        Vector2 launchVel = -delta * launchForce;

        if (_wallLayer != -1) Physics2D.IgnoreLayerCollision(_playerLayer, _wallLayer, false);
        ToggleColliders(true);

        _rb.isKinematic = false; 
        _rb.linearVelocity = launchVel;

        // Force all bones to snap home and match the center's speed
        for (int i = 0; i < edgeBones.Length; i++) {
            _edgeRBs[i].isKinematic = false;
            _edgeRBs[i].linearVelocity = launchVel; 
            edgeBones[i].transform.localPosition = _initialOffsets[i]; 
        }
        _isFlying = true; 
        launchGraceTimer = 0.5f; 
        activeShadows.Clear();
        if (trajectoryLine != null) trajectoryLine.enabled = false;
    }

    public void FreezeSlime()
    {
        _isFlying = false; 
        _rb.linearVelocity = Vector2.zero;
        if (_wallLayer != -1) Physics2D.IgnoreLayerCollision(_playerLayer, _wallLayer, false);
        ToggleColliders(true);
        for (int i = 0; i < edgeBones.Length; i++) {
            edgeBones[i].transform.localPosition = _initialOffsets[i];
            _edgeRBs[i].linearVelocity = Vector2.zero;
        }
    }

    private void HandleWASD()
    {
        if (activeShadows.Count == 0) { canMoveWASD = false; return; }
        float h = Input.GetAxisRaw("Horizontal"), v = Input.GetAxisRaw("Vertical");
        Vector2 target = _rb.position + new Vector2(h, v).normalized * wasdSpeed * Time.deltaTime;
        bool inside = false;
        foreach (var s in activeShadows) if (s != null && s.OverlapPoint(target)) inside = true;
        if (!inside && activeShadows.Count > 0) {
            Vector2 closest = activeShadows[0].ClosestPoint(target);
            Vector2 toCenter = ((Vector2)activeShadows[0].transform.position - closest).normalized;
            target = closest + (toCenter * 0.2f);
        }
        _rb.MovePosition(target);
    }

    private void StartDragging()
    {
        _isDragging = true; _isFlying = false; canMoveWASD = false;
        _rb.isKinematic = true; _rb.linearVelocity = Vector2.zero;
        _dragStartWorld = GetMouseWorldPos();
        if (trajectoryLine != null) trajectoryLine.enabled = true;
        if (_wallLayer != -1) Physics2D.IgnoreLayerCollision(_playerLayer, _wallLayer, true);
        ToggleColliders(false);
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
    private void UpdateFlight() {
        _rb.linearVelocity *= (1f - Time.deltaTime * airDrag);
        if (launchGraceTimer <= 0.4f && bounceGraceTimer <= 0 && _rb.linearVelocity.magnitude < stopThreshold) FreezeSlime();
    }
    public void AddShadow(Collider2D c) { if (!activeShadows.Contains(c)) activeShadows.Add(c); }
    public void RemoveShadow(Collider2D c) { activeShadows.Remove(c); }
    void DrawTrajectory(Vector2 vel) {
        trajectoryLine.positionCount = trajectoryPoints; Vector2 p = transform.position;
        for (int i = 0; i < trajectoryPoints; i++) { trajectoryLine.SetPosition(i, new Vector3(p.x, p.y, transform.position.z)); p += vel * trajectoryTimeStep; }
    }
}