using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(900)]
public sealed class SensorTorch : MonoBehaviour
{
    public Sprite unlitSprite;
    public Sprite detectedSprite;
    public Sprite burningSprite;
    [Tooltip("Only this region is illuminated when a fireball ignites this torch.")]
    public TorchLightRegion lightRegion;
    [SerializeField] private SpriteRenderer original;
    [SerializeField] private SpriteRenderer artwork;
    [SerializeField] private BoxCollider2D fireTarget;
    [SerializeField, HideInInspector] private bool configured;
    private PlayerSpiritFollowers player;
    private bool burning;

    public bool IsConfigured => configured && original != null && artwork != null && fireTarget != null;
    public bool IsBurning => burning;
    public Sprite CurrentSprite => original != null && original.enabled ? original.sprite : artwork != null ? artwork.sprite : null;

    private void Awake() { InitializeVisual(); }

    public void InitializeVisual()
    {
        if (original == null) original = GetComponentInChildren<SpriteRenderer>(true);
        if (original == null) return;
        if (unlitSprite == null) unlitSprite = original.sprite;
        if (detectedSprite == null) detectedSprite = Resources.Load<Sprite>("SensorTorch/Detected");
        if (burningSprite == null) burningSprite = Resources.Load<Sprite>("SensorTorch/Burning");
        if (unlitSprite == null || detectedSprite == null || burningSprite == null) return;

        if (!configured)
        {
            // Map the full state canvases to the user's original sprite, even if it was trimmed.
            float width = unlitSprite.texture.width;
            float height = unlitSprite.texture.height;
            float ppu = unlitSprite.pixelsPerUnit;
            Vector2 origin = unlitSprite.rect.position + unlitSprite.pivot;
            GameObject visual = new GameObject("Torch State Artwork");
            visual.layer = original.gameObject.layer;
            visual.transform.SetParent(original.transform, false);
            artwork = visual.AddComponent<SpriteRenderer>();
            artwork.sharedMaterial = original.sharedMaterial;
            artwork.sortingLayerID = original.sortingLayerID;
            artwork.sortingOrder = original.sortingOrder;
            artwork.color = original.color;
            artwork.flipX = original.flipX;
            artwork.flipY = original.flipY;
            Vector2 shift = (new Vector2(width, height) * 0.5f - origin) / ppu;
            visual.transform.localPosition = new Vector3(original.flipX ? -shift.x : shift.x,
                original.flipY ? -shift.y : shift.y, 0f);
            visual.transform.localScale = new Vector3(
                width / detectedSprite.texture.width * detectedSprite.pixelsPerUnit / ppu,
                height / detectedSprite.texture.height * detectedSprite.pixelsPerUnit / ppu, 1f);

            // Cover the physical basket/handle, excluding transparent padding above the wick.
            GameObject target = new GameObject("Torch Fire Target");
            target.layer = original.gameObject.layer;
            target.transform.SetParent(original.transform, false);
            fireTarget = target.AddComponent<BoxCollider2D>();
            fireTarget.isTrigger = true;
            Vector2 center = (new Vector2(width * 0.5f, height * 0.305f) - origin) / ppu;
            fireTarget.offset = new Vector2(original.flipX ? -center.x : center.x,
                original.flipY ? -center.y : center.y);
            fireTarget.size = new Vector2(width * 0.36f, height * 0.45f) / ppu;
            configured = true;
        }
        RefreshVisual(false);
    }

    private void LateUpdate()
    {
        if (!IsConfigured) return;
        if (player == null) player = FindObjectOfType<PlayerSpiritFollowers>();
        RefreshVisual(player != null && player.IsFireSelected);
    }

    public void RefreshVisual(bool fireSelected)
    {
        if (!IsConfigured) return;
        original.enabled = !burning && !fireSelected;
        artwork.enabled = !original.enabled;
        artwork.sprite = burning ? burningSprite : detectedSprite;
    }

    public bool TryIgnite()
    {
        if (!isActiveAndEnabled || !IsConfigured || burning) return false;
        burning = true;
        RefreshVisual(false);
        if (lightRegion != null) lightRegion.Illuminate();
        return true;
    }
}
