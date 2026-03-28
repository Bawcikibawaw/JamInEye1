using UnityEngine;

public class ShadowZone : MonoBehaviour
{
    public float shadowBrakingForce = 45f; 
    public float suctionSpeed = 8f;
    private Collider2D _col;

    void Awake() => _col = GetComponent<Collider2D>();

    void OnTriggerStay2D(Collider2D other)
    {
        SlimeThrower player = other.GetComponent<SlimeThrower>();
        if (player == null) return;

        if (player.launchGraceTimer > 0 || player.IsDragging) return;

        if (player.canMoveWASD) { player.AddShadow(_col); return; }

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        float speed = rb.linearVelocity.magnitude;

        if (speed > player.stopThreshold)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, Time.deltaTime * shadowBrakingForce);
            player.SetFrictionVisual(1.0f);
        }
        else if (AreAllBonesInside(player))
        {
            Vector2 target = (Vector2)transform.position;
            player.transform.position = Vector3.Lerp(player.transform.position, new Vector3(target.x, target.y, player.transform.position.z), Time.deltaTime * suctionSpeed);
            player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, 0f);

            if (Vector2.Distance(player.transform.position, target) < 0.15f)
            {
                player.FreezeSlime();
                player.canMoveWASD = true;
                player.AddShadow(_col);
            }
        }
    }

    private bool AreAllBonesInside(SlimeThrower player)
    {
        if (!_col.OverlapPoint(player.transform.position)) return false;
        foreach (var b in player.edgeBones)
            if (b != null && !_col.OverlapPoint(b.transform.position)) return false;
        return true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        SlimeThrower p = other.GetComponent<SlimeThrower>();
        if (p != null) { p.RemoveShadow(_col); if (p.activeShadows.Count == 0) p.canMoveWASD = false; }
    }
}