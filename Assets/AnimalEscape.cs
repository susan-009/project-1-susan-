
using UnityEngine;

public class AnimalEscape : MonoBehaviour
{
    [Header("�ĸ�ʯ��")]
    public GameObject[] pillars;

    [Header("ɽ������")]
    public GameObject caveDoor;
    public Transform exitPoint;

    [Header("�ƶ��ٶ�")]
    public float moveSpeed = 2f;

    [SerializeField, Min(0.1f)] private float pickupDistance = 0.9f;
    [SerializeField, Min(0.1f)] private float followDistance = 0.6f;
    [SerializeField, Min(0.1f)] private float leashDistance = 3f;

    private bool accompanying;
    private GroundDragPlayer player;

    private void Start()
    {
        player = FindObjectOfType<GroundDragPlayer>();
    }

    private void Update()
    {
        if (player == null || exitPoint == null || !AllPillarsDown() || !DoorIsOpen())
            return;

        Vector2 separation = player.transform.position - transform.position;
        if (!accompanying)
        {
            if (separation.magnitude > pickupDistance)
                return;
            accompanying = true;
        }

        // Wait when the player flies away or gets too far ahead.
        if (separation.magnitude > leashDistance || Mathf.Abs(separation.y) > 1f)
            return;

        float gap = Mathf.Abs(separation.x);
        if (gap > followDistance)
        {
            float distance = Mathf.Min(gap - followDistance, moveSpeed * Time.deltaTime);
            Vector2 direction = Vector2.right * Mathf.Sign(separation.x);
            SpriteRenderer sprite = GetComponent<SpriteRenderer>();
            float radius = sprite != null ? Mathf.Min(sprite.bounds.extents.x, sprite.bounds.extents.y) : 0.1f;
            Physics2D.SyncTransforms();
            foreach (RaycastHit2D hit in Physics2D.CircleCastAll(transform.position, radius, direction, distance))
            {
                if (hit.collider.isTrigger || hit.collider.transform.IsChildOf(transform) ||
                    hit.collider.transform.IsChildOf(player.transform))
                    continue;
                if (Vector2.Dot(hit.normal, direction) < -0.01f)
                    distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - 0.005f));
            }
            transform.position += (Vector3)(direction * distance);
        }

        // Rescue completes only when both animal and escort reach the exit.
        if (Mathf.Abs(transform.position.x - exitPoint.position.x) < 0.8f &&
            Mathf.Abs(transform.position.y - exitPoint.position.y) < 1f &&
            Vector2.Distance(player.transform.position, exitPoint.position) < 1.5f)
            Destroy(gameObject);
    }

    bool AllPillarsDown()
    {
        foreach (GameObject pillar in pillars)
        {
            if (pillar == null) return false;

            SpriteRenderer sr = pillar.GetComponent<SpriteRenderer>();
            Collider2D col = pillar.GetComponent<Collider2D>();

            if (sr == null || sr.enabled) return false;
            if (col != null && col.enabled) return false;
        }

        return pillars.Length == 4;
    }

    // ���ɽ�����Ƿ��
    bool DoorIsOpen()
    {
        if (caveDoor == null) return false;

        if (!caveDoor.activeInHierarchy) return true;

        SpriteRenderer sr = caveDoor.GetComponent<SpriteRenderer>();
        Collider2D col = caveDoor.GetComponent<Collider2D>();

        bool hidden = sr != null && !sr.enabled;
        bool noCollision = col == null || !col.enabled;

        return hidden && noCollision;
    }
}
