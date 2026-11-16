using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(550)]
public sealed class PlayerGateSwitch : MonoBehaviour
{
    private CircleCollider2D ball;
    private Collider2D contact;
    private GameObject gate;
    private Vector2 previousCenter;
    private bool activated;
    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();

    private void Start()
    {
        ball = GetComponent<CircleCollider2D>();
        GameObject button = null;
        foreach (Transform item in FindObjectsOfType<Transform>())
        {
            if (item.name.Trim() == "机关门开关") button = item.gameObject;
            if (item.name.Trim() == "机关门") gate = item.gameObject;
        }
        if (button == null || gate == null)
        {
            Debug.LogWarning("Gate switch requires 机关门开关 and 机关门.", this);
            enabled = false;
            return;
        }
        contact = button.GetComponent<Collider2D>();
        if (contact == null)
        {
            CircleCollider2D circle = button.AddComponent<CircleCollider2D>();
            if (button.TryGetComponent(out SpriteRenderer renderer) && renderer.sprite != null)
            {
                Bounds bounds = renderer.sprite.bounds;
                circle.offset = bounds.center;
                circle.radius = Mathf.Max(bounds.extents.x, bounds.extents.y);
            }
            circle.isTrigger = true;
            contact = circle;
        }
        previousCenter = ball.bounds.center;
    }

    private void LateUpdate()
    {
        if (activated || contact == null || gate == null ||
            !contact.enabled || !contact.gameObject.activeInHierarchy)
            return;
        Physics2D.SyncTransforms();
        Vector2 center = ball.bounds.center;
        Vector2 travel = center - previousCenter;
        bool touched = ball.Distance(contact).distance <= 0.01f;
        hits.Clear();
        if (travel.sqrMagnitude > 0f && travel.sqrMagnitude < 9f)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.NoFilter();
            filter.useTriggers = true;
            float radius = ball.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            Physics2D.CircleCast(previousCenter, radius, travel.normalized, filter, hits, travel.magnitude);
            foreach (RaycastHit2D hit in hits) touched |= hit.collider == contact;
        }
        previousCenter = center;
        if (!touched) return;
        activated = true;
        gate.SetActive(false);
        Physics2D.SyncTransforms();
    }
}
