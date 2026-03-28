using UnityEngine;

public class ShadowZone : MonoBehaviour
{
    [Header("Shadow Settings")]
    public float shadowFriction = 0.92f;
    public float suctionSpeed = 5f;
    
    private Collider2D _zoneCollider;
    private bool _isCapturing = false;

    void Awake()
    {
        _zoneCollider = GetComponent<Collider2D>();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        ShadowThrower player = other.GetComponent<ShadowThrower>();
        if (player == null) return;

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();

        // 1. If player is moving fast, apply friction
        if (!player.canMoveWASD && playerRb.linearVelocity.magnitude > player.stopThreshold)
        {
            playerRb.linearVelocity *= shadowFriction;
        }

        // 2. If slow and inside, start the capture process
        if (!player.canMoveWASD && playerRb.linearVelocity.magnitude <= player.stopThreshold)
        {
            if (AreAllBonesInside(player))
            {
                CapturePlayer(player);
            }
        }

        // 3. If player is in WASD mode, keep them trapped inside the collider
        if (player.canMoveWASD)
        {
            ClampPlayerToZone(player);
        }
    }

    void CapturePlayer(ShadowThrower player)
    {
        _isCapturing = true;
    
        // 1. Get the target center but keep the player's CURRENT Z
        Vector3 targetPos = transform.position; 
        targetPos.z = player.transform.position.z; 

        // 2. Move using Vector3.Lerp so Z doesn't get reset to 0
        player.transform.position = Vector3.Lerp(player.transform.position, targetPos, Time.deltaTime * suctionSpeed);

        if (Vector2.Distance(player.transform.position, targetPos) < 0.05f)
        {
            player.transform.position = targetPos;
            player.FreezeSlime();
            player.canMoveWASD = true;
            _isCapturing = false;
        }
    }
    void ClampPlayerToZone(ShadowThrower player)
    {
        // Get the closest point on the edge of the shadow collider
        // If the player tries to move outside, this snaps them back to the edge
        Vector3 playerPos = player.transform.position;
        if (!_zoneCollider.OverlapPoint(playerPos))
        {
            Vector3 closestPoint = _zoneCollider.ClosestPoint(playerPos);
            player.transform.position = closestPoint;
        }
    }

    bool AreAllBonesInside(ShadowThrower player)
    {
        if (!_zoneCollider.OverlapPoint(player.transform.position)) return false;
        foreach (var bone in player.edgeBones)
        {
            if (!_zoneCollider.OverlapPoint(bone.transform.position)) return false;
        }
        return true;
    }

    // Reset player state if they leave (e.g., via Drag & Drop)
    void OnTriggerExit2D(Collider2D other)
    {
        ShadowThrower player = other.GetComponent<ShadowThrower>();
        if (player != null)
        {
            player.canMoveWASD = false;
        }
    }
}