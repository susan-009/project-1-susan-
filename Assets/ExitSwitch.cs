using UnityEngine;

public class ExitSwitch : MonoBehaviour
{
    public GameObject caveDoor;

    private bool activated;
    private GroundDragPlayer player;
    private CircleCollider2D ball;
    private Collider2D contact;
    private GameObject landing;

    private void Start()
    {
        player = FindObjectOfType<GroundDragPlayer>();
        if (player != null) ball = player.GetComponent<CircleCollider2D>();
        landing = GameObject.Find("Wall B Landing");
        Transform visual = transform.Find("SwitchVisual");
        if (visual != null)
        {
            contact = visual.GetComponent<Collider2D>();
            if (contact == null)
            {
                CircleCollider2D circle = visual.gameObject.AddComponent<CircleCollider2D>();
                if (visual.TryGetComponent(out SpriteRenderer sprite) && sprite.sprite != null)
                {
                    circle.radius = Mathf.Max(sprite.sprite.bounds.extents.x, sprite.sprite.bounds.extents.y);
                    circle.offset = sprite.sprite.bounds.center;
                }
                circle.isTrigger = true;
                contact = circle;
            }
        }
        else
            contact = GetComponent<Collider2D>();
    }

    private void LateUpdate()
    {
        if (activated || player == null || ball == null || contact == null ||
            !contact.enabled || !contact.gameObject.activeInHierarchy)
            return;

        Physics2D.SyncTransforms();
        ColliderDistance2D distance = ball.Distance(contact);
        if (!distance.isOverlapped && distance.distance > 0.01f) return;
        activated = true;
        if (landing != null) landing.SetActive(false);
        player.StartFalling();
        if (caveDoor != null) caveDoor.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Preserve the existing WindSpirit interaction for older scenes.
        if (!activated && other.transform.root.CompareTag("WindSpirit"))
        {
            if (caveDoor != null) caveDoor.SetActive(false);
        }
    }
}
