using UnityEngine;

// Selected spirits stay on the player's side of solid scene geometry.
public static class SpiritHeldPosition
{
    public static Vector3 Resolve(Transform player, Transform spirit, Vector3 desired)
    {
        if (spirit == null) return desired;
        SpriteRenderer sprite = spirit.GetComponent<SpriteRenderer>();
        Collider2D playerBody = player.GetComponent<Collider2D>();
        if (sprite == null || playerBody == null) return desired;

        Physics2D.SyncTransforms();
        Bounds bounds = sprite.bounds;
        Vector2 visualOffset = bounds.center - spirit.position;
        Vector2 size = new Vector2(Mathf.Max(0.01f, bounds.size.x), Mathf.Max(0.01f, bounds.size.y));
        Vector2 origin = playerBody.bounds.center;
        Vector2 end = (Vector2)desired + visualOffset;
        Vector2 travel = end - origin;
        float distance = travel.magnitude;
        if (distance < 0.0001f) return desired;

        const float gap = 0.015f;
        float allowed = distance;
        foreach (RaycastHit2D hit in Physics2D.BoxCastAll(origin, size, 0f, travel / distance, distance + gap))
        {
            if (!Blocks(hit.collider, player)) continue;
            // Tangential/away motion at a surface should not pin a held spirit.
            Vector2 normal = hit.normal;
            if (hit.distance < 0.0001f)
            {
                Vector2 separation = origin - hit.collider.ClosestPoint(origin);
                if (separation.sqrMagnitude > 0.000001f) normal = separation.normalized;
            }
            if (Vector2.Dot(normal, travel) >= 0f) continue;
            allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - gap));
        }
        Vector2 center = origin + travel / distance * allowed;
        // Validate the endpoint as well, including initial overlap and moving platforms.
        if (!Clear(center, size, player))
        {
            bool found = false;
            for (int i = 0; i <= 12; i++)
            {
                Vector2 candidate = Vector2.Lerp(center, origin, i / 12f);
                if (!Clear(candidate, size, player)) continue;
                center = candidate;
                found = true;
                break;
            }
            if (!found) center = origin;
        }
        return new Vector3(center.x - visualOffset.x, center.y - visualOffset.y, desired.z);
    }

    private static bool Clear(Vector2 center, Vector2 size, Transform player)
    {
        foreach (Collider2D other in Physics2D.OverlapBoxAll(center, size, 0f))
            if (Blocks(other, player)) return false;
        return true;
    }

    private static bool Blocks(Collider2D other, Transform player)
    {
        return other != null && other.enabled && !other.isTrigger && other.gameObject.activeInHierarchy &&
            !other.transform.IsChildOf(player);
    }
}
