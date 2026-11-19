using System.Collections.Generic;
using UnityEngine;

// Shape queries use the existing player capsule, including its offset and scale.
public static class PlayerCapsuleShape
{
    public static Vector2 Size(CapsuleCollider2D body, float padding = 0f)
    {
        Vector3 scale = body.transform.lossyScale;
        return new Vector2(Mathf.Max(0.001f, body.size.x * Mathf.Abs(scale.x) + padding * 2f),
            Mathf.Max(0.001f, body.size.y * Mathf.Abs(scale.y) + padding * 2f));
    }

    public static RaycastHit2D[] CastAll(CapsuleCollider2D body, Vector2 center,
        Vector2 direction, float distance, float padding = 0f)
    {
        return Physics2D.CapsuleCastAll(center, Size(body, padding), body.direction,
            body.transform.eulerAngles.z, direction, distance);
    }

    public static void Sweep(CapsuleCollider2D body, Vector2 origin, Vector2 direction,
        ContactFilter2D filter, List<RaycastHit2D> hits, float distance)
    {
        Physics2D.CapsuleCast(origin, Size(body), body.direction,
            body.transform.eulerAngles.z, direction, filter, hits, distance);
    }

    public static Collider2D[] OverlapAll(CapsuleCollider2D body, Vector2 center, float padding = 0f)
    {
        return Physics2D.OverlapCapsuleAll(center, Size(body, padding), body.direction,
            body.transform.eulerAngles.z);
    }
}
