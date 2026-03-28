using UnityEngine;

public class ShadowZone : MonoBehaviour
{
    [Header("Shadow Physics")]
    public float shadowBrakingForce = 45f; 
    public float suctionSpeed = 10f;

    private Collider2D _zoneCollider;

    void Awake() => _zoneCollider = GetComponent<Collider2D>();

    void OnTriggerStay2D(Collider2D other)
    {
        SlimeThrower player = other.GetComponent<SlimeThrower>();
        if (player == null) return;

        // 1. Check if player is immune or busy
        if (player.launchGraceTimer > 0 || player.IsDragging) return;

        // 2. If already swimming, just make sure this shadow is in the list
        if (player.canMoveWASD) {
            player.AddShadow(_zoneCollider);
            return; 
        }

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        float speed = rb.linearVelocity.magnitude;

        // 3. Braking Phase (Slow down the launch)
        if (speed > player.stopThreshold)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, Time.deltaTime * shadowBrakingForce);
            // Removed SetFrictionVisual here because we don't want color changes!
        }
        // 4. Center Check Phase (Suction and Squeeze)
        else if (_zoneCollider.OverlapPoint(player.transform.position))
        {
            Vector2 targetXY = (Vector2)transform.position;
            Vector3 targetWorld = new Vector3(targetXY.x, targetXY.y, player.transform.position.z);
            
            // Start Squeeze so the bones fit into the area
            player.SqueezeBones(0.6f); 

            player.transform.position = Vector3.Lerp(player.transform.position, targetWorld, Time.deltaTime * suctionSpeed);
            player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, 0f);

            // 5. Final Handover to WASD
            if (Vector2.Distance((Vector2)player.transform.position, targetXY) < 0.15f && AreAllBonesInside(player))
            {
                player.FreezeSlime();
                player.canMoveWASD = true;
                player.AddShadow(_zoneCollider);
            }
        }
    }

    private bool AreAllBonesInside(SlimeThrower player)
    {
        if (!_zoneCollider.OverlapPoint(player.transform.position)) return false;
        foreach (var bone in player.edgeBones)
            if (bone != null && !_zoneCollider.OverlapPoint(bone.transform.position)) return false;
        return true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        SlimeThrower p = other.GetComponent<SlimeThrower>();
        if (p != null && other == p.GetComponent<Collider2D>())
        {
            p.RemoveShadow(_zoneCollider);
            if (p.activeShadows.Count == 0) p.canMoveWASD = false;
        }
    }
}