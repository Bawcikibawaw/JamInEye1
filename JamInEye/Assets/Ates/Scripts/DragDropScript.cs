using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ShadowThrower : MonoBehaviour
{
    [Header("Flight Settings")]
    public float launchForce = 15f;
    public float maxDragDistance = 3f;
    
    public float stopThreshold = 0.2f; // Speed below which the slime freezes

    [Header("Dynamic Animation (Flight)")]
    public float stretchIntensity = 0.05f;
    public float sideSqueezeIntensity = 0.03f;
    public float deformationLerpSpeed = 10f; // Smooths the shape during bounces

    [Header("Edge Bones (Bone 1-8)")]
    public Rigidbody2D[] edgeBones;

    [Header("Trajectory Settings")]
    public LineRenderer trajectoryLine;
    public int trajectoryPoints = 25;
    public float trajectoryTimeStep = 0.06f;
    
    [Header("Shadow WASD Settings")]
    public bool canMoveWASD = false;
    public float wasdSpeed = 5f;

    [Header("Drag Deformation")]
    public float pullAngleWidth = 100f;
    public float pullStrength = 0.6f;
    public bool flipDeformation = false;

    private Rigidbody2D _rb;
    private Collider2D _mainCollider;
    public Collider2D[] _edgeColliders;
    private Vector2 _dragStartMouse;
    private Vector2[] _initialLocalOffsets;
    private bool _isDragging;
    private bool _isFlying;

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _mainCollider = GetComponent<Collider2D>();

        _initialLocalOffsets = new Vector2[edgeBones.Length];
        _edgeColliders = new Collider2D[edgeBones.Length];

        for (int i = 0; i < edgeBones.Length; i++)
        {
            _initialLocalOffsets[i] = edgeBones[i].transform.localPosition;
            _edgeColliders[i] = edgeBones[i].GetComponent<Collider2D>();
        }

        FreezeSlime();
        if (trajectoryLine != null) trajectoryLine.enabled = false;
    }

    void Update()
    {
        if (_isFlying)
        {
            UpdateFlight();
            return;
        }
        
        // --- WASD MOVEMENT ---
        if (canMoveWASD && !_isDragging)
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");
            Vector2 moveDir = new Vector2(moveX, moveY).normalized;
    
            // MovePosition is the safest for physics, 
            // but let's ensure the transform doesn't drift
            Vector2 newPos = _rb.position + moveDir * wasdSpeed * Time.deltaTime;
            _rb.MovePosition(newPos);
    
            // FORCE the Z to stay at your specific layer (e.g., 45)
            Vector3 currentPos = transform.position;
            currentPos.z = 45f; // Or whatever your preferred Z is
            transform.position = currentPos;
        }

        if (Input.GetMouseButtonDown(0)) StartDragging();
        if (Input.GetMouseButton(0) && _isDragging) UpdateDragging();
        if (Input.GetMouseButtonUp(0) && _isDragging) Launch();
    }

    void LateUpdate()
    {
        void LateUpdate()
        {
            // This acts like a "Z-Anchor"
            // No matter what physics or lerping does, 
            // it forces the shadow back to its depth plane every frame.
            transform.position = new Vector3(transform.position.x, transform.position.y, 0f); 
        }
    }

    private void UpdateFlight()
    {
        ApplyDynamicDeformation();

        // STOP CONDITIONS: 
        // 1. Time runs out OR 2. Speed is almost zero (it stopped bouncing/sliding)
        if (_rb.linearVelocity.magnitude < stopThreshold)
        {
            FreezeSlime();
        }
    }

    private void ApplyDynamicDeformation()
    {
        Vector2 vel = _rb.linearVelocity;
        float speed = vel.magnitude;
        
        Vector2 velDir = speed > 0.1f ? vel.normalized : Vector2.zero;
        Vector2 sideDir = new Vector2(-velDir.y, velDir.x);

        for (int i = 0; i < edgeBones.Length; i++)
        {
            Vector2 basePos = _initialLocalOffsets[i];
            
            // Calculate target position based on velocity
            float forwardDot = Vector2.Dot(basePos, velDir);
            float sideDot = Vector2.Dot(basePos, sideDir);

            Vector2 stretch = velDir * (forwardDot * speed * stretchIntensity);
            Vector2 squeeze = sideDir * (sideDot * speed * -sideSqueezeIntensity);
            Vector2 targetLocalPos = basePos + stretch + squeeze;

            // LERP the position so the "bounce" deformation isn't an instant pop
            edgeBones[i].transform.localPosition = Vector2.Lerp(
                edgeBones[i].transform.localPosition, 
                targetLocalPos, 
                Time.deltaTime * deformationLerpSpeed
            );
        }
    }

    private void StartDragging()
    {
        _isDragging = true;
        canMoveWASD = false; // Disable WASD as soon as we start aiming to leave
        _dragStartMouse = MouseToWorld();
        if (trajectoryLine != null) trajectoryLine.enabled = true;
        
        _rb.isKinematic = true;
        ToggleAllColliders(false);
        foreach (var bone in edgeBones) bone.isKinematic = true;
    }

    private void UpdateDragging()
    {
        Vector2 mouseWorld = MouseToWorld();
        Vector2 delta = mouseWorld - _dragStartMouse;
        delta = Vector2.ClampMagnitude(delta, maxDragDistance);

        Vector2 localDelta = transform.InverseTransformDirection(delta);
        float dragAngle = Mathf.Atan2(localDelta.y, localDelta.x) * Mathf.Rad2Deg;
        if (flipDeformation) dragAngle += 180f;

        float dragAmount = delta.magnitude / maxDragDistance;

        for (int i = 0; i < edgeBones.Length; i++)
        {
            Vector2 baseOffset = _initialLocalOffsets[i];
            float boneAngle = Mathf.Atan2(baseOffset.y, baseOffset.x) * Mathf.Rad2Deg;
            float angleDiff = Mathf.Abs(Mathf.DeltaAngle(boneAngle, dragAngle));

            if (angleDiff < pullAngleWidth * 0.5f)
            {
                float influence = Mathf.SmoothStep(0f, 1f, 1f - (angleDiff / (pullAngleWidth * 0.5f)));
                Vector2 stretch = localDelta.normalized * pullStrength * influence * dragAmount;
                edgeBones[i].transform.localPosition = baseOffset + stretch;
            }
            else
            {
                edgeBones[i].transform.localPosition = Vector2.Lerp(edgeBones[i].transform.localPosition, baseOffset, Time.deltaTime * 15f);
            }
        }
        DrawTrajectory(-delta * launchForce);
    }

    private void Launch()
    {
        _isDragging = false;
        Vector2 delta = MouseToWorld() - _dragStartMouse;
        delta = Vector2.ClampMagnitude(delta, maxDragDistance);
        Vector2 launchVelocity = -delta * launchForce;

        _isFlying = true;
        if (trajectoryLine != null) trajectoryLine.enabled = false;

        ToggleAllColliders(true);
        _rb.isKinematic = false;
        _rb.linearVelocity = launchVelocity;

        foreach (var bone in edgeBones)
        {
            bone.isKinematic = false;
            bone.linearVelocity = launchVelocity;
        }
    }

    public void FreezeSlime()
    {
        _isFlying = false;
        _isDragging = false;
        _rb.isKinematic = true;
        _rb.linearVelocity = Vector2.zero;

        ToggleAllColliders(true);
        // for (int i = 0; i < edgeBones.Length; i++)
        // {
        //     edgeBones[i].isKinematic = true;
        //    /edgeBones[i].transform.localPosition = _initialLocalOffsets[i];
        // }
    }

    private void ToggleAllColliders(bool state)
    {
        if (_mainCollider != null) _mainCollider.enabled = state;
        for (int i = 0; i < _edgeColliders.Length; i++)
        {
            if (_edgeColliders[i] != null) _edgeColliders[i].enabled = state;
        }
    }

    // REMOVED: OnCollisionEnter2D no longer calls FreezeSlime()
    // This allows the Physics Material to handle the bounce naturally.

    Vector2 MouseToWorld()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = Mathf.Abs(transform.position.z - Camera.main.transform.position.z);
        return Camera.main.ScreenToWorldPoint(mouse);
    }

    void DrawTrajectory(Vector2 launchVelocity)
    {
        if (trajectoryLine == null) return;
        trajectoryLine.positionCount = trajectoryPoints;
        Vector2 pos = transform.position;
        Vector2 vel = launchVelocity;
        for (int i = 0; i < trajectoryPoints; i++)
        {
            trajectoryLine.SetPosition(i, new Vector3(pos.x, pos.y, transform.position.z));
            pos += vel * trajectoryTimeStep;
        }
    }
}