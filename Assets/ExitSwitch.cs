using System.Collections;
using UnityEngine;

public class ExitSwitch : MonoBehaviour
{
    public GameObject caveDoor;
    [SerializeField] private Collider2D caveCeiling;
    [SerializeField, Min(0.1f)] private float openingDuration = 1.5f;

    private bool activated;
    private GroundDragPlayer player;
    private CapsuleCollider2D ball;
    private Collider2D contact;
    private GameObject landing;
    private SwitchButtonVisual buttonVisual;
    private GameObject openingMask;
    private Sprite maskSprite;
    private SpriteRenderer[] doorSprites;
    private SpriteMaskInteraction[] originalMaskInteractions;

    private void Start()
    {
        player = FindObjectOfType<GroundDragPlayer>();
        if (player != null) ball = player.GetComponent<CapsuleCollider2D>();
        landing = GameObject.Find("Wall B Landing");
        if (caveCeiling == null && caveDoor != null)
        {
            Transform mountain = caveDoor.transform.parent;
            Transform roof = mountain != null ? mountain.Find("MountainTop") : null;
            if (roof != null) caveCeiling = roof.GetComponent<Collider2D>();
        }

        Transform visual = transform.Find("SwitchVisual");
        if (visual != null)
        {
            buttonVisual = visual.GetComponent<SwitchButtonVisual>();
            if (buttonVisual == null) buttonVisual = visual.gameObject.AddComponent<SwitchButtonVisual>();
            buttonVisual.InitializeVisual();
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
        else contact = GetComponent<Collider2D>();
    }

    private void LateUpdate()
    {
        if (activated || player == null || player.IsShowingPillars || ball == null || contact == null ||
            !contact.enabled || !contact.gameObject.activeInHierarchy)
            return;

        Physics2D.SyncTransforms();
        ColliderDistance2D distance = ball.Distance(contact);
        if (distance.isOverlapped || distance.distance <= 0.01f) Activate();
    }

    private void Activate()
    {
        if (activated || (player != null && player.IsShowingPillars)) return;
        if (caveDoor == null || !caveDoor.activeInHierarchy) return;
        if (caveCeiling == null)
        {
            Debug.LogWarning("ExitSwitch needs the MountainTop collider as its cave ceiling.", this);
            return;
        }

        Physics2D.SyncTransforms();
        Bounds bounds = GetDoorBounds();
        float ceilingY = caveCeiling.bounds.min.y;
        if (ceilingY <= bounds.min.y) return;

        activated = true;
        if (buttonVisual != null) buttonVisual.Press();
        Bounds view = bounds;
        view.Encapsulate(new Vector3(bounds.center.x, caveCeiling.bounds.max.y, bounds.center.z));
        view.Expand(new Vector3(2f, 0.5f, 0f));
        System.Action beginOpening = () =>
        {
            if (landing != null) landing.SetActive(false);
            if (player != null) player.StartFalling();
            StartCoroutine(RaiseDoor(bounds, ceilingY));
        };

        // Reuse the existing camera presentation lock, including Player/spirit pause and camera restoration.
        if (player != null && player.isActiveAndEnabled && player.FollowCamera != null)
            player.ShowPillarAnimation(view, Mathf.Max(0.1f, openingDuration), beginOpening);
        else beginOpening();
    }

    private Bounds GetDoorBounds()
    {
        Collider2D body = caveDoor.GetComponent<Collider2D>();
        Bounds bounds = body != null ? body.bounds : new Bounds(caveDoor.transform.position, Vector3.zero);
        foreach (SpriteRenderer sprite in caveDoor.GetComponentsInChildren<SpriteRenderer>())
        {
            if (sprite.enabled && sprite.sprite != null) bounds.Encapsulate(sprite.bounds);
        }
        return bounds;
    }

    private IEnumerator RaiseDoor(Bounds bounds, float ceilingY)
    {
        CreateOpeningMask(bounds, ceilingY);
        Vector3 start = caveDoor.transform.position;
        // Slide the entire door into the roof; the mask clips it at the roof's underside.
        float distance = ceilingY - bounds.min.y;
        float duration = Mathf.Max(0.1f, openingDuration);
        float elapsed = 0f;
        try
        {
            while (elapsed < duration && caveDoor != null)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                caveDoor.transform.position = start + Vector3.up * (distance * progress);
                Physics2D.SyncTransforms();
                yield return null;
            }

            if (caveDoor != null)
            {
                caveDoor.transform.position = start + Vector3.up * distance;
                caveDoor.SetActive(false);
                Physics2D.SyncTransforms();
            }
        }
        finally { ClearOpeningMask(); }
    }

    private void CreateOpeningMask(Bounds bounds, float ceilingY)
    {
        Texture2D white = Texture2D.whiteTexture;
        maskSprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height),
            new Vector2(0.5f, 0.5f), white.width);
        openingMask = new GameObject("Cave Door Ceiling Clip");
        float bottom = bounds.min.y - 0.05f;
        openingMask.transform.position = new Vector3(bounds.center.x, (bottom + ceilingY) * 0.5f, bounds.center.z);
        // Keep world dimensions independent of the switch and door's nonuniform scales.
        openingMask.transform.localScale = new Vector3(bounds.size.x + 0.1f, ceilingY - bottom, 1f);
        SpriteMask mask = openingMask.AddComponent<SpriteMask>();
        mask.sprite = maskSprite;
        mask.alphaCutoff = 0.5f;

        doorSprites = caveDoor.GetComponentsInChildren<SpriteRenderer>(true);
        originalMaskInteractions = new SpriteMaskInteraction[doorSprites.Length];
        for (int i = 0; i < doorSprites.Length; i++)
        {
            originalMaskInteractions[i] = doorSprites[i].maskInteraction;
            doorSprites[i].maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }
    }

    private void ClearOpeningMask()
    {
        if (doorSprites != null)
            for (int i = 0; i < doorSprites.Length; i++)
                if (doorSprites[i] != null) doorSprites[i].maskInteraction = originalMaskInteractions[i];
        doorSprites = null;
        originalMaskInteractions = null;
        if (openingMask != null) Destroy(openingMask);
        if (maskSprite != null) Destroy(maskSprite);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        ClearOpeningMask();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Older scenes can still activate the same opening sequence with WindSpirit.
        if (other.transform.root.CompareTag("WindSpirit")) Activate();
    }
}
