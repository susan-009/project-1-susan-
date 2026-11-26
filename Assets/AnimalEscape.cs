using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(700)]
public class AnimalEscape : MonoBehaviour
{
    // Preserve old scene references; completion no longer depends on an exit or pillars.
    [HideInInspector] public GameObject[] pillars;
    [HideInInspector] public GameObject caveDoor;
    [HideInInspector] public Transform exitPoint;

    public bool IsCompleted { get; private set; }
    private GroundDragPlayer player;
    private CapsuleCollider2D playerBody;
    private CircleCollider2D animalBody;
    private Vector2 previousPlayerCenter;
    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();

    private void Start() { Initialize(FindObjectOfType<GroundDragPlayer>()); }

    public void Initialize(GroundDragPlayer owner)
    {
        player = owner;
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

    private void LateUpdate() { TryCompleteOnContact(); }

    public bool TryCompleteOnContact()
    {
        if (IsCompleted || player == null || player.IsLevelComplete || playerBody == null ||
            animalBody == null || !playerBody.enabled || !animalBody.enabled ||
            !playerBody.gameObject.activeInHierarchy || !animalBody.gameObject.activeInHierarchy) return false;
        Physics2D.SyncTransforms();
        Vector2 playerCenter = playerBody.bounds.center;
        Vector2 travel = playerCenter - previousPlayerCenter;
        bool touched = playerBody.Distance(animalBody).distance <= 0.01f;
        if (!touched && travel.sqrMagnitude > 0.000001f && travel.sqrMagnitude < 9f)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.NoFilter();
            filter.useTriggers = true;
            hits.Clear();
            PlayerCapsuleShape.Sweep(playerBody, previousPlayerCenter, travel.normalized,
                filter, hits, travel.magnitude);
            foreach (RaycastHit2D hit in hits) if (hit.collider == animalBody) touched = true;
        }
        previousPlayerCenter = playerCenter;
        if (!touched) return false;
        IsCompleted = true;
        player.CompleteLevel();
        return true;
    }

    private void OnGUI()
    {
        if (!IsCompleted) return;
        float width = Mathf.Min(420f, Screen.width - 24f);
        Rect panel = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.3f, width, 140f);
        Color previous = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.08f, 0.14f, 0.23f, 0.96f);
        GUI.Box(panel, GUIContent.none);
        GUI.backgroundColor = previous;
        var title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 32 };
        title.normal.textColor = new Color(1f, 0.85f, 0.4f);
        GUI.Label(new Rect(panel.x, panel.y + 15f, width, 60f), "通关成功！", title);
        var detail = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20 };
        detail.normal.textColor = Color.white;
        GUI.Label(new Rect(panel.x, panel.y + 80f, width, 40f), "已找到小兔子", detail);
    }
}
