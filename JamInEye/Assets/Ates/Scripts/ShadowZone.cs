// ShadowZone.cs
using UnityEngine;

public class ShadowZone : MonoBehaviour
{
    [Header("Shadow Physics")]
    public float shadowBrakingForce = 45f;
    public float suctionSpeed = 10f;

    private Collider2D _zoneCollider;

    void Awake() => _zoneCollider = GetComponent<Collider2D>();

    void OnTriggerEnter2D(Collider2D other)
    {
        SlimeThrower player = other.GetComponent<SlimeThrower>();
        if (player == null) return;

        player.AddShadow(_zoneCollider);
        player.EnterShadowColliderMode();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        SlimeThrower player = other.GetComponent<SlimeThrower>();
        if (player == null) return;

        // Fallback in case Enter was missed due to collider being disabled during drag
        player.AddShadow(_zoneCollider);

        if (player.launchGraceTimer > 0) return;

        if (player.IsDragging)
        {
            player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            return;
        }

        if (player.canMoveWASD) return;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        float speed = rb.linearVelocity.magnitude;

        if (speed > player.stopThreshold)
        {
            rb.linearVelocity = Vector2.MoveTowards(
                rb.linearVelocity,
                Vector2.zero,
                Time.deltaTime * shadowBrakingForce
            );
        }
        else if (_zoneCollider.OverlapPoint(player.transform.position))
        {
            Vector2 targetXY = (Vector2)transform.position;

            player.SqueezeBones(0.6f);
            rb.MovePosition(Vector2.Lerp(rb.position, targetXY, Time.deltaTime * suctionSpeed));

            if (Vector2.Distance(rb.position, targetXY) < 0.15f && AreAllBonesInside(player))
                player.canMoveWASD = true;
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
        if (p == null) return;

        if (p.IsDragging) return;

        if (other == p.GetComponent<Collider2D>())
        {
            p.RemoveShadow(_zoneCollider);
            if (p.activeShadows.Count == 0)
            {
                p.canMoveWASD = false;
                p.ExitShadowColliderMode();
            }
        }
    }
}