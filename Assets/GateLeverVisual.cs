using UnityEngine;

[DisallowMultipleComponent]
public sealed class GateLeverVisual : MonoBehaviour
{
    public Sprite idleSprite;
    public Sprite activatedSprite;
    [SerializeField] private SpriteRenderer display;
    [SerializeField, HideInInspector] private bool configured;
    public bool IsActivated { get; private set; }
    public bool IsConfigured => configured && display != null && idleSprite != null && activatedSprite != null;

    private void Awake() { InitializeVisual(); }

    public void InitializeVisual()
    {
        if (idleSprite == null || activatedSprite == null)
            foreach (Sprite sprite in Resources.LoadAll<Sprite>("GateLever/GateLever"))
            {
                if (sprite.name == "Lever_Idle") idleSprite = sprite;
                if (sprite.name == "Lever_Activated") activatedSprite = sprite;
            }
        if (idleSprite == null || activatedSprite == null) return;
        SpriteRenderer original = GetComponent<SpriteRenderer>();
        if (display == null)
        {
            Transform existing = transform.Find("Gate Lever Visual");
            if (existing != null) display = existing.GetComponent<SpriteRenderer>();
            if (display == null)
            {
                GameObject visual = new GameObject("Gate Lever Visual");
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
        if (!configured)
        {
            // Keep the old circle's bottom on its platform; the lever rises above that baseline.
            float bottom = original != null && original.sprite != null ? original.sprite.bounds.min.y : -0.5f;
            Vector2 size = idleSprite.bounds.size;
            float scale = 1.35f / Mathf.Max(size.x, size.y);
            display.transform.localPosition = new Vector3(0f, bottom, 0f);
            display.transform.localRotation = Quaternion.identity;
            display.transform.localScale = Vector3.one * scale;
            Collider2D contact = GetComponent<Collider2D>();
            if (contact == null)
            {
                BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
                Bounds shape = idleSprite.bounds;
                shape.Encapsulate(activatedSprite.bounds);
                box.size = (Vector2)shape.size * scale;
                box.offset = (Vector2)shape.center * scale + Vector2.up * bottom;
                box.isTrigger = true;
            }
            configured = true;
        }
        if (original != null) original.enabled = false;
        display.sprite = IsActivated ? activatedSprite : idleSprite;
    }

    public void Activate()
    {
        IsActivated = true;
        InitializeVisual();
    }
}
