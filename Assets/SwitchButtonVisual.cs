using UnityEngine;

[DisallowMultipleComponent]
public sealed class SwitchButtonVisual : MonoBehaviour
{
    public Sprite unpressedSprite;
    public Sprite pressedSprite;
    [SerializeField] private SpriteRenderer display;
    [SerializeField, HideInInspector] private bool configured;
    public bool IsPressed { get; private set; }
    public bool IsConfigured => configured && display != null && unpressedSprite != null && pressedSprite != null;
    public SpriteRenderer Display => display;

    private void Awake() { InitializeVisual(); }

    public void InitializeVisual()
    {
        if (unpressedSprite == null || pressedSprite == null)
            foreach (Sprite sprite in Resources.LoadAll<Sprite>("SwitchButton/SwitchButton"))
            {
                if (sprite.name == "Button_Unpressed") unpressedSprite = sprite;
                if (sprite.name == "Button_Pressed") pressedSprite = sprite;
            }
        if (unpressedSprite == null || pressedSprite == null) return;
        SpriteRenderer original = GetComponent<SpriteRenderer>();
        if (display == null)
        {
            Transform existing = transform.Find("Button Artwork");
            if (existing != null) display = existing.GetComponent<SpriteRenderer>();
            if (display == null)
            {
                GameObject visual = new GameObject("Button Artwork");
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
            Bounds oldShape = original != null && original.sprite != null
                ? original.sprite.bounds : new Bounds(Vector3.zero, Vector3.one);
            float scale = oldShape.size.y * 1.1f / unpressedSprite.bounds.size.y;
            // Both sprites pivot on the housing's right edge: only the cap moves when pressed.
            Vector3 anchor = new Vector3(oldShape.max.x, oldShape.center.y, 0f);
            display.transform.localPosition = anchor;
            display.transform.localRotation = Quaternion.identity;
            display.transform.localScale = Vector3.one * scale;
            if (GetComponent<Collider2D>() == null)
            {
                Bounds shape = unpressedSprite.bounds;
                shape.Encapsulate(pressedSprite.bounds);
                BoxCollider2D contact = gameObject.AddComponent<BoxCollider2D>();
                contact.size = (Vector2)shape.size * scale;
                contact.offset = (Vector2)(shape.center * scale + anchor);
                contact.isTrigger = true;
            }
            configured = true;
        }
        if (original != null) original.enabled = false;
        display.sprite = IsPressed ? pressedSprite : unpressedSprite;
    }

    public void Press()
    {
        IsPressed = true;
        InitializeVisual();
    }
}
