using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(900)]
public sealed class MechanismStoneIndicator : MonoBehaviour
{
    public const int CurrentArtworkVersion = 3;
    public Sprite unlitSprite;
    public Sprite litSprite;
    [SerializeField] private SpriteRenderer display;
    [SerializeField, HideInInspector] private int artworkVersion;
    public int ArtworkVersion => artworkVersion;
    private GroundDragPlayer player;
    private StoneAbility stone;
    public SpriteRenderer Display => display;

    private void Awake() { InitializeVisual(); }

    public void InitializeVisual()
    {
        if (unlitSprite == null || litSprite == null)
        {
            foreach (Sprite sprite in Resources.LoadAll<Sprite>("Mechanism/MechanismLamp"))
            {
                if (sprite.name == "Mechanism_Off") unlitSprite = sprite;
                if (sprite.name == "Mechanism_On") litSprite = sprite;
            }
        }
        if (unlitSprite == null || litSprite == null) return;
        SpriteRenderer original = GetComponent<SpriteRenderer>();
        if (display == null)
        {
            Transform existing = transform.Find("Mechanism Visual");
            if (existing != null) display = existing.GetComponent<SpriteRenderer>();
            if (display == null)
            {
                GameObject visual = new GameObject("Mechanism Visual");
                visual.transform.SetParent(transform, false);
                display = visual.AddComponent<SpriteRenderer>();
            }
            if (original != null)
            {
                display.sharedMaterial = original.sharedMaterial;
                display.sortingLayerID = original.sortingLayerID;
                display.sortingOrder = original.sortingOrder;
            }
            display.color = Color.white;
        }
        // Refit existing displays once when upgrading the atlas; later manual edits are retained.
        if (artworkVersion < CurrentArtworkVersion)
        {
            BoxCollider2D body = GetComponent<BoxCollider2D>();
            if (body != null)
            {
                display.transform.localPosition = new Vector3(body.offset.x, body.offset.y, 0f);
                Vector2 imageSize = unlitSprite.bounds.size;
                Vector3 scale = body.transform.lossyScale;
                bool horizontal = body.size.x * Mathf.Abs(scale.x) > body.size.y * Mathf.Abs(scale.y);
                display.transform.localRotation = Quaternion.Euler(0f, 0f, horizontal ? 90f : 0f);
                display.transform.localScale = horizontal
                    ? new Vector3(body.size.y / imageSize.x, body.size.x / imageSize.y, 1f)
                    : new Vector3(body.size.x / imageSize.x, body.size.y / imageSize.y, 1f);
            }
            artworkVersion = CurrentArtworkVersion;
        }
        if (original != null) original.enabled = false;
        display.sprite = unlitSprite;
    }

    private void LateUpdate()
    {
        if (display == null || unlitSprite == null || litSprite == null) return;
        if (player == null) player = FindObjectOfType<GroundDragPlayer>();
        // StoneAbility is added by the followers at runtime, after some Start calls.
        if (stone == null && player != null) stone = player.GetComponent<StoneAbility>();
        Sprite desired = stone != null && stone.isActiveAndEnabled && !stone.IsFollowing ? litSprite : unlitSprite;
        if (display.sprite != desired) display.sprite = desired;
    }
}
