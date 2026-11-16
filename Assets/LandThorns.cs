using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(600)]
public sealed class LandThorns : MonoBehaviour
{
    private GroundDragPlayer player;
    private CircleCollider2D ball;
    private Collider2D home;
    private GameObject bush;
    private BoxCollider2D thornCollider;
    private Vector2 previousCenter;
    private Mesh mesh;
    private Material material;
    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();

    private void Start()
    {
        player = GetComponent<GroundDragPlayer>();
        ball = GetComponent<CircleCollider2D>();
        GameObject land = null;
        foreach (Transform item in FindObjectsOfType<Transform>())
            if (string.Equals(item.name.Trim(), "Land1", System.StringComparison.OrdinalIgnoreCase))
                land = item.gameObject;
        GameObject ground = GameObject.Find("new ground");
        if (ground != null) home = ground.GetComponent<Collider2D>();
        if (land == null || home == null)
        {
            Debug.LogWarning(land == null
                ? "Land1 thorns: no active Land1 platform was found in the scene."
                : "Land1 thorns: new ground needs a Collider2D for the respawn point.", this);
            enabled = false;
            return;
        }
        Collider2D floor = land.GetComponent<Collider2D>();
        if (floor == null) floor = land.AddComponent<BoxCollider2D>();
        Transform existing = land.transform.Find("Land1 Thorn Bush");
        if (existing != null)
        {
            bush = existing.gameObject;
            thornCollider = bush.GetComponent<BoxCollider2D>();
            BurnableThorns burnable = bush.GetComponent<BurnableThorns>();
            if (thornCollider == null || burnable == null)
            {
                Debug.LogError("Land1 Thorn Bush needs its trigger and BurnableThorns component.", bush);
                enabled = false;
                return;
            }
            burnable.owner = this;
            previousCenter = ball.bounds.center;
            return;
        }
        Bounds bounds = floor.bounds;
        bush = new GameObject("Land1 Thorn Bush");
        bush.transform.position = new Vector3(bounds.center.x, bounds.max.y, land.transform.position.z);
        bush.transform.SetParent(land.transform, true);
        thornCollider = bush.AddComponent<BoxCollider2D>();
        thornCollider.size = new Vector2(bounds.size.x, 0.5f);
        thornCollider.offset = new Vector2(0f, 0.25f);
        thornCollider.isTrigger = true;
        bush.AddComponent<BurnableThorns>().owner = this;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        int count = Mathf.Max(3, Mathf.CeilToInt(bounds.size.x / 0.18f));
        float spacing = bounds.size.x / count;
        for (int i = 0; i < count; i++)
        {
            float x = -bounds.extents.x + spacing * i;
            int index = vertices.Count;
            vertices.Add(new Vector3(x, 0f));
            vertices.Add(new Vector3(x + spacing * 0.5f, i % 2 == 0 ? 0.5f : 0.38f));
            vertices.Add(new Vector3(x + spacing, 0f));
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
        }
        mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        bush.AddComponent<MeshFilter>().sharedMesh = mesh;
        material = new Material(Shader.Find("Sprites/Default"));
        material.color = new Color(0.2f, 0.55f, 0.18f, 1f);
        MeshRenderer renderer = bush.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = 7;
        previousCenter = ball.bounds.center;
    }

    private void LateUpdate()
    {
        if (bush == null || !bush.activeSelf) return;
        Physics2D.SyncTransforms();
        Vector2 center = ball.bounds.center;
        Vector2 travel = center - previousCenter;
        hits.Clear();
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        filter.useTriggers = true;
        if (travel.sqrMagnitude > 0f && travel.sqrMagnitude < 9f)
        {
            float radius = ball.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            Physics2D.CircleCast(previousCenter, radius, travel.normalized, filter, hits, travel.magnitude);
        }
        bool touched = ball.Distance(thornCollider).isOverlapped;
        foreach (RaycastHit2D hit in hits) touched |= hit.collider == thornCollider;
        if (touched)
        {
            Vector3 position = transform.position;
            position.x += home.bounds.center.x - ball.bounds.center.x;
            position.y += home.bounds.max.y + 0.01f - ball.bounds.min.y;
            player.ReturnToLanding(position);
        }
        previousCenter = ball.bounds.center;
    }

    public bool Burn()
    {
        if (bush == null || !bush.activeSelf) return false;
        bush.SetActive(false);
        return true;
    }

    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
    }
}

