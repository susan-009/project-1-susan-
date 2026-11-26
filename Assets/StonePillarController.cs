using System.Collections;
using System.Linq;
using UnityEngine;

public class StonePillarController : MonoBehaviour
{
    public bool startVisible = true;
    public float sinkDistance = 3f;
    public float moveDuration = 0.8f;

    private SpriteRenderer prototypeRenderer;
    private SpriteRenderer[] visualRenderers;
    private BoxCollider2D pillarCollider;
    private Vector3 raisedPosition;
    private Coroutine moveCoroutine;
    private bool isRaised;
    private bool initialized;

    private void Awake()
    {
        Initialize();
        SetRaisedImmediate(startVisible);
    }

    private void Initialize()
    {
        if (initialized) return;
        prototypeRenderer = GetComponent<SpriteRenderer>();
        pillarCollider = GetComponent<BoxCollider2D>();
        raisedPosition = transform.position;
        // Imported art lives on child objects. Keep the old root block as a fallback
        // only for legacy pillars without any child artwork.
        visualRenderers = GetComponentsInChildren<SpriteRenderer>(true)
            .Where(r => r != prototypeRenderer && r.sprite != null &&
                r.GetComponentInParent<StonePillarController>() == this).ToArray();
        if (visualRenderers.Length == 0 && prototypeRenderer != null)
            visualRenderers = new[] { prototypeRenderer };
        initialized = true;
    }

    private void SetVisible(bool visible)
    {
        if (prototypeRenderer != null && !visualRenderers.Contains(prototypeRenderer))
            prototypeRenderer.enabled = false;
        foreach (SpriteRenderer renderer in visualRenderers)
            if (renderer != null) renderer.enabled = visible;
        if (pillarCollider != null) pillarCollider.enabled = visible;
    }

    public bool IsRaised => isRaised;

    public void SetRaisedImmediate(bool raised)
    {
        Initialize();
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = null;
        isRaised = raised;
        transform.position = raisedPosition + (raised ? Vector3.zero : Vector3.down * sinkDistance);
        SetVisible(raised);
        Physics2D.SyncTransforms();
    }

    public Bounds CameraBounds
    {
        get
        {
            Vector3 size = Vector3.one;
            if (pillarCollider != null)
            {
                // Collider.bounds becomes empty when a lowered pillar disables it.
                // Keep framing based on the authored shape throughout the puzzle.
                Vector3 right = transform.TransformVector(Vector3.right * pillarCollider.size.x);
                Vector3 up = transform.TransformVector(Vector3.up * pillarCollider.size.y);
                size = new Vector3(Mathf.Abs(right.x) + Mathf.Abs(up.x), Mathf.Abs(right.y) + Mathf.Abs(up.y), 0f);
            }
            Bounds bounds = new Bounds(raisedPosition, size);
            bounds.Encapsulate(new Bounds(raisedPosition + Vector3.down * sinkDistance, size));
            return bounds;
        }
    }

    public void Raise() { MovePillar(true); }
    public void Lower() { MovePillar(false); }
    public void Toggle() { MovePillar(!isRaised); }

    private void MovePillar(bool rise)
    {
        Initialize();
        if (pillarCollider == null || visualRenderers.Length == 0) return;
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        isRaised = rise;
        SetVisible(true);
        Vector3 target = rise ? raisedPosition : raisedPosition + Vector3.down * sinkDistance;
        moveCoroutine = StartCoroutine(MoveAnimation(target, rise));
    }

    private IEnumerator MoveAnimation(Vector3 target, bool visible)
    {
        Vector3 start = transform.position;
        float time = 0f;
        while (time < moveDuration)
        {
            time += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, Mathf.Clamp01(time / moveDuration));
            yield return null;
        }
        transform.position = target;
        SetVisible(visible);
        Physics2D.SyncTransforms();
        moveCoroutine = null;
    }
}
