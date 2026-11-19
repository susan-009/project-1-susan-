using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D), typeof(BurnableThorns))]
[DefaultExecutionOrder(650)]
public sealed class RegrowingThorns : MonoBehaviour
{
    [Min(0.1f)] public float regrowSeconds = 5f;
    public Collider2D respawnLanding;

    private GroundDragPlayer player;
    private CapsuleCollider2D playerBody;
    private Collider2D thorn;
    private Renderer[] visuals;
    private bool[] originalVisibility;
    private bool burned;
    private float regrowAt;
    private Vector2 previousPlayerCenter;
    private Vector2 previousBushPosition;
    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();

    private void Awake()
    {
        thorn = GetComponent<BoxCollider2D>();
        thorn.isTrigger = true;
        visuals = GetComponentsInChildren<Renderer>(true);
        originalVisibility = new bool[visuals.Length];
        for (int i = 0; i < visuals.Length; i++) originalVisibility[i] = visuals[i].enabled;
    }

    private void Start()
    {
        player = FindObjectOfType<GroundDragPlayer>();
        if (player != null) playerBody = player.GetComponent<CapsuleCollider2D>();
        if (respawnLanding == null)
        {
            GameObject landing = GameObject.Find("Wall A Landing");
            if (landing != null) respawnLanding = landing.GetComponent<Collider2D>();
        }
        if (playerBody == null || respawnLanding == null)
            Debug.LogError("Regrowing thorns need a player capsule and Wall A Landing collider.", this);
        if (playerBody != null) previousPlayerCenter = playerBody.bounds.center;
        previousBushPosition = transform.position;
    }

    public bool Burn()
    {
        if (burned || !isActiveAndEnabled) return false;
        burned = true;
        regrowAt = Time.time + Mathf.Max(0.1f, regrowSeconds);
        thorn.enabled = false;
        foreach (Renderer visual in visuals) visual.enabled = false;
        // Keep this object active so the timer runs and it continues riding the lift.
        return true;
    }

    private void Update()
    {
        if (!burned || Time.time < regrowAt) return;
        for (int i = 0; i < visuals.Length; i++)
            if (visuals[i] != null) visuals[i].enabled = originalVisibility[i];
        thorn.enabled = true;
        burned = false;
        Physics2D.SyncTransforms();
    }

    private void LateUpdate()
    {
        if (playerBody == null || respawnLanding == null) return;
        Physics2D.SyncTransforms();
        Vector2 center = playerBody.bounds.center;
        Vector2 bushPosition = transform.position;
        Vector2 playerTravel = center - previousPlayerCenter;
        Vector2 liftTravel = bushPosition - previousBushPosition;
        if (!burned && thorn.enabled)
        {
            bool touched = playerBody.Distance(thorn).distance <= 0.001f;
            Vector2 relativeTravel = playerTravel - liftTravel;
            // Query against the bush's current position, accounting for its lift motion.
            if (!touched && playerTravel.sqrMagnitude < 9f && relativeTravel.sqrMagnitude > 0.000001f)
            {
                ContactFilter2D filter = new ContactFilter2D();
                filter.NoFilter();
                filter.useTriggers = true;
                hits.Clear();
                PlayerCapsuleShape.Sweep(playerBody, previousPlayerCenter + liftTravel,
                    relativeTravel.normalized, filter, hits, relativeTravel.magnitude);
                foreach (RaycastHit2D hit in hits) if (hit.collider == thorn) touched = true;
            }
            if (touched)
            {
                Vector3 destination = player.transform.position;
                destination.x += respawnLanding.bounds.center.x - playerBody.bounds.center.x;
                destination.y += respawnLanding.bounds.max.y + 0.01f - playerBody.bounds.min.y;
                player.ReturnToLanding(destination);
            }
        }
        previousPlayerCenter = playerBody.bounds.center;
        previousBushPosition = bushPosition;
    }
}
