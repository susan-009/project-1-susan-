using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(500)]
public sealed class PlayerTrapReturn : MonoBehaviour
{
    private GroundDragPlayer player;
    private CapsuleCollider2D ball;
    private Collider2D landing;
    private readonly List<Collider2D> traps = new List<Collider2D>();
    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
    private Vector2 previousCenter;

    private void Start()
    {
        player = GetComponent<GroundDragPlayer>();
        ball = GetComponent<CapsuleCollider2D>();
        GameObject destination = GameObject.Find("Wall A Landing");
        if (destination != null) landing = destination.GetComponent<Collider2D>();
        foreach (Transform item in FindObjectsOfType<Transform>())
        {
            if (item.name != "trap" && !item.name.StartsWith("trap ("))
                continue;
            Collider2D collider = item.GetComponent<Collider2D>();
            if (collider == null && item.TryGetComponent(out SpriteRenderer sprite) && sprite.sprite != null)
            {
                BoxCollider2D box = item.gameObject.AddComponent<BoxCollider2D>();
                box.size = sprite.sprite.bounds.size;
                box.offset = sprite.sprite.bounds.center;
                box.isTrigger = true;
                collider = box;
            }
            if (collider != null) traps.Add(collider);
        }
        previousCenter = ball.bounds.center;
        if (landing == null)
        {
            Debug.LogError("Trap return requires a collider on Wall A Landing.", this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        Physics2D.SyncTransforms();
        Vector2 center = ball.bounds.center;
        Vector2 travel = center - previousCenter;
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        filter.useTriggers = true;
        hits.Clear();
        // Large instantaneous relocations are teleports, not travel through traps.
        if (travel.sqrMagnitude > 0f && travel.sqrMagnitude < 9f)
        {
            PlayerCapsuleShape.Sweep(ball, previousCenter, travel.normalized, filter, hits, travel.magnitude);
        }

        foreach (Collider2D trap in traps)
        {
            if (trap == null || !trap.enabled || !trap.gameObject.activeInHierarchy)
                continue;
            bool touched = ball.Distance(trap).isOverlapped;
            foreach (RaycastHit2D hit in hits)
                touched |= hit.collider == trap;
            if (!touched) continue;

            Bounds floor = landing.bounds;
            Vector3 position = transform.position;
            position.x += floor.center.x - ball.bounds.center.x;
            position.y += floor.max.y + 0.01f - ball.bounds.min.y;
            player.ReturnToLanding(position);
            break;
        }
        previousCenter = ball.bounds.center;
    }
}
