using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(700)]
public class AnimalEscape : MonoBehaviour
{
    // Keep old scene references compatible. Contact now controls pickup.
    [HideInInspector] public GameObject[] pillars;
    [HideInInspector] public GameObject caveDoor;
    [HideInInspector] public Transform exitPoint;

    [Header("跟随设置")]
    [Min(0.1f)] public float moveSpeed = 2f;
    [SerializeField, Min(0.1f)] private float followDistance = 0.6f;

    private bool accompanying;
    private GroundDragPlayer player;
    private CapsuleCollider2D playerBody;
    private CircleCollider2D animalBody;
    private Vector2 previousPlayerCenter;
    private Vector3 previousTarget;
    private float pathLength;
    private readonly Queue<Vector3> path = new Queue<Vector3>();
    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();

    private void Start()
    {
        player = FindObjectOfType<GroundDragPlayer>();
        if (player == null) return;
        playerBody = player.GetComponent<CapsuleCollider2D>();
        animalBody = GetComponent<CircleCollider2D>();
        if (animalBody == null)
        {
            animalBody = gameObject.AddComponent<CircleCollider2D>();
            SpriteRenderer sprite = GetComponent<SpriteRenderer>();
            if (sprite != null && sprite.sprite != null)
            {
                animalBody.radius = Mathf.Min(sprite.sprite.bounds.extents.x, sprite.sprite.bounds.extents.y);
                animalBody.offset = sprite.sprite.bounds.center;
            }
        }
        animalBody.isTrigger = true;
        Physics2D.SyncTransforms();
        if (playerBody != null) previousPlayerCenter = playerBody.bounds.center;
    }

    private Vector3 FollowTarget()
    {
        return new Vector3(playerBody.bounds.center.x,
            playerBody.bounds.min.y + animalBody.bounds.extents.y + 0.005f, transform.position.z);
    }

    private void LateUpdate()
    {
        if (playerBody == null || animalBody == null) return;
        Physics2D.SyncTransforms();
        Vector2 playerCenter = playerBody.bounds.center;
        Vector2 travel = playerCenter - previousPlayerCenter;
        previousPlayerCenter = playerCenter;

        if (!accompanying)
        {
            bool touched = playerBody.Distance(animalBody).distance <= 0.01f;
            if (!touched && travel.sqrMagnitude > 0.000001f && travel.sqrMagnitude < 9f)
            {
                ContactFilter2D filter = new ContactFilter2D();
                filter.NoFilter();
                filter.useTriggers = true;
                hits.Clear();
                PlayerCapsuleShape.Sweep(playerBody, playerCenter - travel, travel.normalized,
                    filter, hits, travel.magnitude);
                foreach (RaycastHit2D hit in hits) if (hit.collider == animalBody) touched = true;
            }
            if (!touched) return;
            accompanying = true;
            previousTarget = FollowTarget();
            path.Enqueue(previousTarget);
            pathLength = Vector3.Distance(animalBody.bounds.center, previousTarget);
        }

        Vector3 target = FollowTarget();
        float travelled = Vector3.Distance(previousTarget, target);
        if (travelled > 3f)
        {
            // Respawns and Stone relocations are discontinuities, not walkable paths.
            SetCenter(target);
            path.Clear();
            pathLength = 0f;
            previousTarget = target;
            return;
        }
        if (travelled > 0.01f)
        {
            path.Enqueue(target);
            pathLength += travelled;
            previousTarget = target;
        }
        if (player.IsShowingPillars) return;

        // Match fast player movement so sprinting never breaks the follow state.
        float budget = Mathf.Min(Mathf.Max(0f, pathLength - followDistance),
            Mathf.Max(moveSpeed * Time.deltaTime, travelled));
        int iterations = 0;
        while (path.Count > 0 && budget > 0f && iterations++ < 128)
        {
            Vector3 center = animalBody.bounds.center;
            Vector3 next = path.Peek();
            Vector2 step = next - center;
            float distance = step.magnitude;
            if (distance < 0.0001f) { path.Dequeue(); continue; }
            float requested = Mathf.Min(distance, budget);
            float allowed = ClearDistance(center, step / distance, requested);
            if (allowed <= 0.00001f) break;
            SetCenter(center + (Vector3)(step / distance * allowed));
            pathLength = Mathf.Max(0f, pathLength - allowed);
            budget -= allowed;
            if (allowed + 0.0001f >= distance) path.Dequeue();
            if (allowed + 0.0001f < requested) break;
        }
    }

    private float ClearDistance(Vector2 center, Vector2 direction, float distance)
    {
        float radius = Mathf.Min(animalBody.bounds.extents.x, animalBody.bounds.extents.y);
        float allowed = distance;
        foreach (RaycastHit2D hit in Physics2D.CircleCastAll(center, Mathf.Max(0.001f, radius - 0.003f),
            direction, distance + 0.003f))
        {
            if (hit.collider.isTrigger || hit.collider.transform.IsChildOf(transform) ||
                hit.collider.transform.IsChildOf(player.transform)) continue;
            Vector2 normal = hit.normal;
            if (hit.distance < 0.0001f)
            {
                Vector2 separation = center - hit.collider.ClosestPoint(center);
                if (separation.sqrMagnitude > 0.000001f) normal = separation.normalized;
            }
            if (Vector2.Dot(normal, direction) < -0.01f)
                allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - 0.003f));
        }
        return allowed;
    }

    private void SetCenter(Vector3 center)
    {
        Vector3 offset = animalBody.bounds.center - transform.position;
        Vector3 position = center - offset;
        position.z = transform.position.z;
        transform.position = position;
        Physics2D.SyncTransforms();
    }
}
