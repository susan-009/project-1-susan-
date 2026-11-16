
using System.Collections;
using UnityEngine;

public class StonePillarController : MonoBehaviour
{
    public bool startVisible = true;
    public float sinkDistance = 3f;
    public float moveDuration = 0.8f;

    private SpriteRenderer sr;
    private BoxCollider2D pillarCollider;
    private Vector3 raisedPosition;
    private Coroutine moveCoroutine;
    private bool isRaised;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        pillarCollider = GetComponent<BoxCollider2D>();

        raisedPosition = transform.position;
        isRaised = startVisible;

        if (sr == null || pillarCollider == null)
        {
            Debug.LogError("ʯ��ȱ�� Sprite Renderer �� Box Collider 2D");
            return;
        }

        if (startVisible)
        {
            sr.enabled = true;
            pillarCollider.enabled = true;
        }
        else
        {
            sr.enabled = false;
            pillarCollider.enabled = false;
            transform.position = raisedPosition
                + Vector3.down * sinkDistance;
        }
    }

    public bool IsRaised => isRaised;

    // Used once at puzzle startup, after every pillar has initialized in Awake.
    public void SetRaisedImmediate(bool raised)
    {
        if (sr == null || pillarCollider == null) return;
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = null;
        isRaised = raised;
        transform.position = raisedPosition + (raised ? Vector3.zero : Vector3.down * sinkDistance);
        sr.enabled = raised;
        pillarCollider.enabled = raised;
        Physics2D.SyncTransforms();
    }

    public Bounds CameraBounds
    {
        get
        {
            Vector3 size = sr != null ? sr.bounds.size : Vector3.one;
            Bounds bounds = new Bounds(raisedPosition, size);
            bounds.Encapsulate(new Bounds(raisedPosition + Vector3.down * sinkDistance, size));
            return bounds;
        }
    }

    public void Raise()
    {
        MovePillar(true);
    }

    public void Lower()
    {
        MovePillar(false);
    }

    public void Toggle()
    {
        if (isRaised)
            Lower();
        else
            Raise();
    }

    void MovePillar(bool rise)
    {
        if (sr == null || pillarCollider == null)
            return;

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        isRaised = rise;

        sr.enabled = true;
        pillarCollider.enabled = true;

        Vector3 target = rise
            ? raisedPosition
            : raisedPosition + Vector3.down * sinkDistance;

        moveCoroutine = StartCoroutine(
            MoveAnimation(target, rise)
        );
    }

    IEnumerator MoveAnimation(Vector3 target, bool visible)
    {
        Vector3 start = transform.position;
        float time = 0;

        while (time < moveDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / moveDuration);

            transform.position = Vector3.Lerp(start, target, t);

            yield return null;
        }

        transform.position = target;

        sr.enabled = visible;
        pillarCollider.enabled = visible;

        moveCoroutine = null;
    }
}


