using UnityEngine;

public sealed class StoneAbility : MonoBehaviour
{
    private enum State { Following, Ready, Flying }
    private State state;
    private Transform stone;
    private SpriteRenderer visual;
    private Sprite originalSprite;
    private Vector3 originalScale;
    private Color originalColor;
    private Rigidbody2D body;
    private PlayerSpiritFollowers followers;
    private GroundDragPlayer player;
    private Camera cameraView;
    private Vector2 press;
    private bool pressing;
    private Vector3 direction;
    private float remaining;
    private Rect launchView;
    private TrajectoryPredictor preview;


    public bool IsFollowing => state == State.Following;

    public void Initialize(Transform target, PlayerSpiritFollowers owner)
    {
        stone = target;
        followers = owner;
        player = GetComponent<GroundDragPlayer>();
        cameraView = player.FollowCamera;
        visual = stone.GetComponent<SpriteRenderer>();
        originalSprite = visual.sprite;
        originalScale = stone.localScale;
        originalColor = visual.color;
        body = stone.GetComponent<Rigidbody2D>();
        preview = stone.GetComponent<TrajectoryPredictor>();
        if (stone.TryGetComponent(out EarthSpiritCollisionReturn legacyReturn))
            legacyReturn.enabled = false;
    }

    public bool IsWeightOn(Bounds plate) => false;

    public void SetSelected(bool selected)
    {
        Recall();
        if (!selected) return;
        state = State.Ready;
        stone.position = followers.SelectedPosition(stone);
    }

    private bool Hit()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = cameraView.WorldToScreenPoint(stone.position).z;
        Vector3 world = cameraView.ScreenToWorldPoint(mouse);
        Bounds bounds = visual.bounds;

        return bounds.SqrDistance(new Vector3(world.x, world.y, bounds.center.z)) < 0.0025f;
    }

    private void LateUpdate()
    {
        if (stone == null || player == null || player.IsShowingPillars || cameraView == null || Input.GetMouseButtonDown(1))
            return;
        if (state == State.Following)
        {
            if (Input.GetMouseButtonDown(0) && followers.CanSummonStone && player.CanToggleWing && Hit())
            {
                state = State.Ready;
                stone.position = followers.SelectedPosition(stone);
            }
            return;
        }
        if (state == State.Flying)
        {
            float step = Mathf.Min(14f * Time.deltaTime, remaining);
            foreach (RaycastHit2D hit in ShotHits(direction, step))
            {
                if ((hit.collider.isTrigger && hit.collider.GetComponentInParent<EarthMechanismController>() == null) ||
                    hit.collider.transform == stone ||
                    hit.collider.transform == transform || hit.collider.GetComponentInParent<SpiritLauncher>() != null)
                    continue;
                EarthMechanismController mechanism = hit.collider.GetComponentInParent<EarthMechanismController>();
                if (mechanism != null)
                {
                    if (mechanism.isActiveAndEnabled) mechanism.Activate();
                    Recall();
                    return;
                }
                if (TryFindExit(hit.collider, hit.point, hit.normal, launchView, out Vector3 exit))
                    player.RelocateByStone(exit);
                Recall();
                return;
            }
            stone.position += direction * step;
            remaining -= step;
            if (remaining <= 0f) Recall();
            return;
        }

        stone.position = followers.SelectedPosition(stone);
        if (Input.GetMouseButtonDown(0) && Hit())
        {
            press = Input.mousePosition;
            pressing = true;
        }
        if (pressing)
        {
            Vector2 aim = (Vector2)Input.mousePosition - press;
            if (aim.sqrMagnitude >= 64f)
                DrawPreview(aim.normalized);
            else if (preview != null)
                preview.Hide();
        }
        if (pressing && Input.GetMouseButtonUp(0))
        {
            pressing = false;
            if (preview != null) preview.Hide();
            Vector2 drag = (Vector2)Input.mousePosition - press;
            if (drag.sqrMagnitude < 64f)
                Recall();
            else
            {
                direction = drag.normalized;
                launchView = VisibleBounds();
                remaining = VisibleDistance(stone.position, direction, launchView);
                state = State.Flying;
            }
        }
    }

    private RaycastHit2D[] ShotHits(Vector2 aim, float distance)
    {
        float radius = Mathf.Min(visual.bounds.extents.x, visual.bounds.extents.y);
        RaycastHit2D[] hits = Physics2D.CircleCastAll(stone.position, radius, aim, distance);
        System.Array.Sort(hits, (first, second) =>
        {
            // Prefer a mechanism attached flush to the wall within a small contact tolerance.
            float firstDistance = first.distance -
                (first.collider.GetComponentInParent<EarthMechanismController>() != null ? 0.02f : 0f);
            float secondDistance = second.distance -
                (second.collider.GetComponentInParent<EarthMechanismController>() != null ? 0.02f : 0f);
            return firstDistance.CompareTo(secondDistance);
        });
        return hits;
    }

    private bool TryFindExit(Collider2D wall, Vector2 entry, Vector2 surfaceNormal, Rect view, out Vector3 exit)
    {
        exit = Vector3.zero;
        CapsuleCollider2D ball = GetComponent<CapsuleCollider2D>();
        Vector2 halfSize = ball.bounds.extents;
        Vector2 centerOffset = ball.bounds.center - transform.position;
        Vector2 exitDirection = surfaceNormal;
        float startDistance = Mathf.Abs(surfaceNormal.x) * halfSize.x +
            Mathf.Abs(surfaceNormal.y) * halfSize.y + 0.04f;
        float maxDistance = startDistance + 0.3f;
        for (float distance = startDistance; distance <= maxDistance; distance += 0.1f)
        {
            Vector2 candidate = entry + exitDirection * distance;
            if (candidate.x - halfSize.x < view.xMin || candidate.x + halfSize.x > view.xMax ||
                candidate.y - halfSize.y < view.yMin || candidate.y + halfSize.y > view.yMax)
                continue;
            if (wall.OverlapPoint(candidate))
                continue;
            bool clear = true;
            foreach (Collider2D other in PlayerCapsuleShape.OverlapAll(ball, candidate, 0.02f))
            {
                if (!other.isTrigger && other != ball && other.transform != stone &&
                    other.GetComponentInParent<SpiritLauncher>() == null)
                    clear = false;
            }
            if (!clear) continue;
            exit = new Vector3(candidate.x - centerOffset.x, candidate.y - centerOffset.y, transform.position.z);
            return true;
        }
        return false;
    }

    private Rect VisibleBounds()
    {
        float depth = cameraView.WorldToViewportPoint(stone.position).z;
        Vector3 lower = cameraView.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
        Vector3 upper = cameraView.ViewportToWorldPoint(new Vector3(1f, 1f, depth));
        // Inset the centerline so the preview stroke stays inside the image.
        return Rect.MinMaxRect(lower.x + 0.05f, lower.y + 0.05f,
            upper.x - 0.05f, upper.y - 0.05f);
    }

    private static float VisibleDistance(Vector2 origin, Vector2 aim, Rect view)
    {
        if (!view.Contains(origin)) return 0f;
        float distance = 10f;
        if (aim.x > 0f) distance = Mathf.Min(distance, (view.xMax - origin.x) / aim.x);
        if (aim.x < 0f) distance = Mathf.Min(distance, (view.xMin - origin.x) / aim.x);
        if (aim.y > 0f) distance = Mathf.Min(distance, (view.yMax - origin.y) / aim.y);
        if (aim.y < 0f) distance = Mathf.Min(distance, (view.yMin - origin.y) / aim.y);
        return Mathf.Max(0f, distance);
    }

    private void DrawPreview(Vector2 aim)
    {
        if (preview == null) return;
        Physics2D.SyncTransforms();
        Rect view = VisibleBounds();
        float distance = VisibleDistance(stone.position, aim, view);
        if (distance <= 0f)
        {
            preview.Hide();
            return;
        }
        Vector3 end = stone.position + (Vector3)aim * distance;
        Vector3 exit = end;
        bool hasExit = false;
        foreach (RaycastHit2D hit in ShotHits(aim, distance))
        {
            if ((hit.collider.isTrigger && hit.collider.GetComponentInParent<EarthMechanismController>() == null) ||
                    hit.collider.transform == stone ||
                hit.collider.transform == transform || hit.collider.GetComponentInParent<SpiritLauncher>() != null)
                continue;
            end = new Vector3(hit.point.x, hit.point.y, stone.position.z);
            hasExit = hit.collider.GetComponentInParent<EarthMechanismController>() == null &&
                TryFindExit(hit.collider, hit.point, hit.normal, view, out exit);
            break;
        }
        preview.Show();
        preview.line.positionCount = hasExit ? 3 : 2;
        preview.line.SetPosition(0, stone.position);
        preview.line.SetPosition(1, end);
        if (hasExit) preview.line.SetPosition(2, exit);
    }

    private void Recall()
    {
        if (body != null)
        {
            body.velocity = Vector2.zero;
            body.simulated = false;
        }

        stone.localScale = originalScale;
        visual.sprite = originalSprite;
        visual.color = originalColor;
        state = State.Following;
        pressing = false;
        if (preview != null) preview.Hide();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
        {
            pressing = false;
            if (preview != null) preview.Hide();
        }
    }
}





