using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public sealed class GroundDragPlayer : MonoBehaviour
{
    [SerializeField] private BoxCollider2D ground;
    [SerializeField] private Camera followCamera;
    [SerializeField, Min(0.01f)] private float cameraSmoothTime = 0.15f;

    [SerializeField, Min(0.01f)] private float moveSpeed = 4f;

    [SerializeField, Min(0.01f)] private float wingJumpSpeed = 7f;
    [SerializeField, Min(0.01f)] private float wingDashDistance = 2.5f;
    [SerializeField, Min(0.01f)] private float wingDashSpeed = 22f;

    private CircleCollider2D playerCollider;
    private bool wingActive;
    private bool wingSlamming;
    private bool wingPointerHeld;
    private bool wingPointerDragged;
    private Vector2 wingPointerStart;
    private float wingPointerLastX;
    private float wingPointerTargetX;
    private bool wingAirborne;
    private const int WingJumpLimit = 3;
    private int wingJumpsUsed;
    private bool wingHitGround;
    private SpriteRenderer[] wingJumpDots;
    private float verticalSpeed;
    private float dashRemaining;

    private bool stoneRelocated;
    private bool stoneAnchored;

    public void RelocateByStone(Vector3 position)
    {
        transform.position = position;
        targetX = position.x;
        wingSlamming = false;
        verticalSpeed = 0f;
        dashRemaining = 0f;
        dragging = false;
        wingAirborne = false;
        stoneAnchored = true;
        stoneRelocated = true;
        Physics2D.SyncTransforms();
    }

    public void ReturnToLanding(Vector3 position)
    {
        RelocateByStone(position);
        stoneAnchored = false;
        wingAirborne = false;
        wingPointerHeld = false;
        wingJumpsUsed = 0;
        cameraVelocity = Vector2.zero;
        UpdateCameraPosition(true);
    }

    public void StartFalling()
    {
        stoneAnchored = false;
        stoneRelocated = true;
        wingAirborne = true;
        wingSlamming = false;
        verticalSpeed = 0f;
        dashRemaining = 0f;
        dragging = false;
        wingPointerHeld = false;
        Physics2D.SyncTransforms();
    }

    public bool CanRideWingPlatform => isActiveAndEnabled && verticalSpeed <= 0f;
    public bool IsShowingPillars { get; private set; }
    public Camera FollowCamera => followCamera;

    public void ShowPillarAnimation(Bounds bounds, float duration, System.Action activate)
    {
        if (IsShowingPillars) return;
        StartCoroutine(PresentPillars(bounds, duration, activate));
    }

    private System.Collections.IEnumerator PresentPillars(Bounds bounds, float duration, System.Action activate)
    {
        IsShowingPillars = true;
        dragging = false;
        wingPointerHeld = false;
        Vector3 originalPosition = followCamera.transform.position;
        float originalSize = followCamera.orthographicSize;
        Vector3 destination = new Vector3(bounds.center.x, bounds.center.y, originalPosition.z);
        float viewSize = Mathf.Max(bounds.extents.y + 0.4f,
            (bounds.extents.x + 0.4f) / followCamera.aspect);
        bool activated = false;
        try
        {
            followCamera.transform.position = destination;
            followCamera.orthographicSize = viewSize;
            // Show the complete pillar area before starting the mechanism.
            yield return new WaitForSecondsRealtime(0.2f);
            activated = true;
            activate();
            yield return new WaitForSeconds(Mathf.Max(0f, duration) + 1f);
        }
        finally
        {
            if (!activated) activate();
            IsShowingPillars = false;
            if (followCamera != null)
            {
                followCamera.orthographicSize = originalSize;
                cameraVelocity = Vector2.zero;
                UpdateCameraPosition(true);
            }
        }
    }
    public bool IsWingActive => wingActive;
    public bool CanToggleWing => !dragging && !wingAirborne && dashRemaining == 0f;

    public void SetWingActive(bool active)
    {
        wingActive = active;
        wingSlamming = false;
        wingPointerHeld = false;
        dragging = false;
    }
    private bool dragging;
    private float lastMouseX;
    private float targetX;
    private Vector2 cameraVelocity;

    private void Awake()
    {
        playerCollider = GetComponent<CircleCollider2D>();
        if (followCamera == null)
            followCamera = Camera.main;

        if (ground == null || followCamera == null || !followCamera.orthographic)
        {
            Debug.LogError("Assign a flat ground BoxCollider2D and an orthographic camera.", this);
            enabled = false;
            return;
        }

        targetX = playerCollider.bounds.center.x;
        MoveAlongGround(float.PositiveInfinity);
        UpdateCameraPosition(true);
        CreateWingJumpDots();
        if (GetComponent<PlayerGateSwitch>() == null)
            gameObject.AddComponent<PlayerGateSwitch>();
        if (GetComponent<LandThorns>() == null)
            gameObject.AddComponent<LandThorns>();
        if (GetComponent<PlayerTrapReturn>() == null)
            gameObject.AddComponent<PlayerTrapReturn>();
    }

    private void Update()
    {
        if (IsShowingPillars) return;
        if (stoneRelocated && !wingActive)
        {
            Vector3 mousePoint = Input.mousePosition;
            if (Input.GetMouseButtonDown(0))
            {
                mousePoint.z = followCamera.WorldToScreenPoint(transform.position).z;
                dragging = playerCollider.OverlapPoint(followCamera.ScreenToWorldPoint(mousePoint));
                lastMouseX = mousePoint.x;
                targetX = transform.position.x;
            }
            if (!Input.GetMouseButton(0)) dragging = false;
            if (dragging)
            {
                if (mousePoint.x != lastMouseX) stoneAnchored = false;
                targetX += (mousePoint.x - lastMouseX) * 2f * followCamera.orthographicSize / followCamera.pixelHeight;
                lastMouseX = mousePoint.x;
                float step = Mathf.MoveTowards(transform.position.x, targetX, moveSpeed * Time.deltaTime) - transform.position.x;
                if (MoveWingStep(Vector2.right * step)) targetX = transform.position.x;
            }
            if (!stoneAnchored) UpdateWingMovement();
            return;
        }

        if (wingActive || wingAirborne || dashRemaining != 0f)
        {
            UpdateWingMovement();
            return;
        }

        Vector3 mouse = Input.mousePosition;
        if (Input.GetMouseButtonDown(0))
        {
            mouse.z = followCamera.WorldToScreenPoint(transform.position).z;
            dragging = playerCollider.OverlapPoint(followCamera.ScreenToWorldPoint(mouse));
            lastMouseX = mouse.x;
            targetX = playerCollider.bounds.center.x;
        }

        if (!Input.GetMouseButton(0))
            dragging = false;

        if (!dragging)
            return;

        float unitsPerPixel = 2f * followCamera.orthographicSize / followCamera.pixelHeight;
        targetX += (mouse.x - lastMouseX) * unitsPerPixel;
        MoveAlongGround(moveSpeed * Time.deltaTime);
        lastMouseX = mouse.x;
    }

    private void UpdateWingMovement()
    {
        Vector2 mouse = Input.mousePosition;
        if (wingActive && Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1))
        {
            PlayerSpiritFollowers followers = GetComponent<PlayerSpiritFollowers>();
            if (followers != null && followers.IsPointerOnWing())
            {
                SetWingActive(false);
                followers.MarkWingReturnClick();
            }
            else if (followCamera.pixelRect.Contains(mouse))
            {
                wingPointerHeld = true;
                wingPointerDragged = false;
                wingPointerStart = mouse;
                wingPointerLastX = mouse.x;
                wingPointerTargetX = transform.position.x;
            }
        }

        if (wingActive && wingPointerHeld &&
            (Input.GetMouseButton(0) || Input.GetMouseButtonUp(0)))
        {
            Vector2 displacement = mouse - wingPointerStart;
            bool startingDrag = !wingPointerDragged && displacement.sqrMagnitude >= 64f;
            if (startingDrag) wingPointerDragged = true;
            if (wingPointerDragged)
            {
                float pixels = startingDrag ? displacement.x : mouse.x - wingPointerLastX;
                float units = pixels * 2f * followCamera.orthographicSize / followCamera.pixelHeight;
                float currentX = transform.position.x;
                float pending = wingPointerTargetX - currentX;
                if (units != 0f && pending * units < 0f)
                    wingPointerTargetX = currentX;
                wingPointerTargetX += units;
                float step = Mathf.MoveTowards(currentX, wingPointerTargetX, moveSpeed * Time.deltaTime) - currentX;
                if (step != 0f)
                {
                    stoneAnchored = false;
                    dashRemaining = 0f;
                    if (MoveWingStep(Vector2.right * step))
                        wingPointerTargetX = transform.position.x;
                }
            }
            wingPointerLastX = mouse.x;
            if (Input.GetMouseButtonUp(0))
            {
                wingPointerHeld = false;
                if (!wingPointerDragged && wingJumpsUsed < WingJumpLimit)
                {
                    Vector2 jet = WingJetDirection();
                    if (jet != Vector2.zero)
                    {
                        wingJumpsUsed++;
                        stoneAnchored = false;
                        wingAirborne = true;
                        wingSlamming = jet.y < 0f;
                        verticalSpeed = jet.y * wingJumpSpeed;
                        dashRemaining = jet.x * wingDashDistance;
                    }
                }
            }
        }

        if (stoneAnchored) return;
        float dt = Time.deltaTime;
        if (dashRemaining != 0f)
        {
            float step = Mathf.MoveTowards(0f, dashRemaining, wingDashSpeed * dt);
            if (MoveWingStep(Vector2.right * step))
                dashRemaining = 0f;
            else
                dashRemaining -= step;
        }

        verticalSpeed -= 20f * dt;
        if (MoveWingStep(Vector2.up * (verticalSpeed * dt)))
        {
            wingAirborne = verticalSpeed > 0f || !wingHitGround;
            if (verticalSpeed <= 0f && wingHitGround)
                wingJumpsUsed = 0;
            wingSlamming = false;
            verticalSpeed = 0f;
        }
        else
            wingAirborne = true;

        if (transform.position.y < ground.bounds.min.y - 8f)
        {
            stoneRelocated = false;
            targetX = ground.bounds.center.x;
            MoveAlongGround(float.PositiveInfinity);
            wingSlamming = false;
            verticalSpeed = 0f;
            dashRemaining = 0f;
            wingAirborne = false;
            wingJumpsUsed = 0;
        }
    }

    private Vector2 WingJetDirection()
    {
        Vector2 mouse = Input.mousePosition;
        if (!followCamera.pixelRect.Contains(mouse)) return Vector2.zero;
        Vector2 center = followCamera.WorldToScreenPoint(transform.position);
        Vector2 offset = mouse - center;
        if (offset.sqrMagnitude < 24f * 24f) return Vector2.zero;
        return Mathf.Abs(offset.x) > Mathf.Abs(offset.y)
            ? Vector2.left * Mathf.Sign(offset.x)
            : Vector2.down * Mathf.Sign(offset.y);
    }

    private void OnGUI()
    {
        if (!enabled || !wingActive || followCamera == null || IsShowingPillars) return;
        Vector2 direction = WingJetDirection();
        if (direction == Vector2.zero) return;
        string arrow = direction.x > 0f ? "→" : direction.x < 0f ? "←" :
            direction.y > 0f ? "↑" : "↓";
        Vector2 mouse = Input.mousePosition;
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 28;
        style.normal.textColor = wingJumpsUsed < WingJumpLimit ? Color.cyan : Color.gray;
        GUI.Label(new Rect(mouse.x + 16f, Screen.height - mouse.y - 35f, 60f, 50f), arrow, style);
    }

    private bool MoveWingStep(Vector2 step)
    {
        wingHitGround = false;
        float distance = step.magnitude;
        if (distance == 0f)
            return false;

        Physics2D.SyncTransforms();
        Vector2 direction = step / distance;
        Vector2 center = transform.TransformPoint(playerCollider.offset);
        Vector3 scale = transform.lossyScale;
        const float skin = 0.003f;
        float radius = playerCollider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        float castRadius = Mathf.Max(0.001f, radius - skin);
        float allowed = distance;
        Vector2 blockingNormal = Vector2.zero;
        Collider2D blockingCollider = null;

        RaycastHit2D[] hits = Physics2D.CircleCastAll(center, castRadius, direction, distance + skin);
        System.Array.Sort(hits, (first, second) => first.distance.CompareTo(second.distance));
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == playerCollider || hit.collider.isTrigger ||
                !hit.collider.gameObject.activeInHierarchy)
                continue;

            Vector2 normal = hit.normal;
            if (hit.distance <= 0.0001f)
            {
                // Initial-overlap cast normals can simply be the inverse cast direction.
                Vector2 separation = center - hit.collider.ClosestPoint(center);
                if (separation.sqrMagnitude > 0.000001f)
                    normal = separation.normalized;
            }

            if (Vector2.Dot(normal, direction) >= -0.01f)
                continue;

            float hitDistance = Mathf.Max(0f, hit.distance - skin);
            if (hitDistance > allowed)
                continue;

            if (wingActive && wingSlamming && direction.y < -0.99f &&
                normal.y > 0.01f && TrySmashBuilding(hit.collider))
                continue;

            allowed = hitDistance;
            blockingNormal = normal;
            blockingCollider = hit.collider;
        }

        if (allowed < distance && Mathf.Abs(direction.x) > 0.99f &&
            TryStepOntoPlatform(blockingCollider, step, radius))
            return false;

        transform.position += (Vector3)(direction * allowed);
        Physics2D.SyncTransforms();
        bool blocked = allowed < distance;
        wingHitGround = blocked && direction.y < 0f && blockingNormal.y >= 0.5f;
        return blocked;
    }

    private static bool TrySmashBuilding(Collider2D collider)
    {
        for (Transform item = collider.transform; item != null; item = item.parent)
        {
            string name = item.name.Trim();
            if (name != "脆弱建筑" && !name.StartsWith("脆弱建筑 ("))
                continue;
            item.gameObject.SetActive(false);
            return true;
        }
        return false;
    }

    private bool TryStepOntoPlatform(Collider2D obstacle, Vector2 step, float radius)
    {
        if (!(obstacle is BoxCollider2D) || verticalSpeed > 0f)
            return false;
        // Limit assistance to a small lip near the ball's feet.
        if (Mathf.Abs(Mathf.DeltaAngle(obstacle.transform.eulerAngles.z, 0f)) > 0.1f)
            return false;

        Bounds bounds = playerCollider.bounds;
        float rise = obstacle.bounds.max.y - bounds.min.y + 0.006f;
        if (rise <= 0f || rise > radius * 0.8f)
            return false;

        Vector2 center = transform.TransformPoint(playerCollider.offset);
        foreach (RaycastHit2D hit in Physics2D.CircleCastAll(center, radius - 0.003f, Vector2.up, rise))
        {
            if (hit.collider == playerCollider || hit.collider == obstacle || hit.collider.isTrigger)
                continue;
            if (Vector2.Dot(hit.normal, Vector2.up) < -0.01f)
                return false;
        }

        Vector2 lifted = center + Vector2.up * rise;
        foreach (RaycastHit2D hit in Physics2D.CircleCastAll(lifted, radius - 0.003f,
            step.normalized, step.magnitude))
        {
            if (hit.collider == playerCollider || hit.collider.isTrigger) continue;
            if (Vector2.Dot(hit.normal, step.normalized) < -0.01f) return false;
        }

        Vector2 target = lifted + step;
        foreach (Collider2D other in Physics2D.OverlapCircleAll(target, radius - 0.003f))
            if (other != playerCollider && !other.isTrigger)
                return false;

        transform.position += new Vector3(step.x, rise, 0f);
        wingSlamming = false;
        verticalSpeed = 0f;
        Physics2D.SyncTransforms();
        return true;
    }

    private void MoveAlongGround(float maxDistance)
    {
        Bounds floor = ground.bounds;
        Bounds ball = playerCollider.bounds;
        Vector3 position = transform.position;
        float centerOffsetX = ball.center.x - position.x;
        float minX = floor.min.x + ball.extents.x;
        float maxX = floor.max.x - ball.extents.x;
        targetX = minX <= maxX ? Mathf.Clamp(targetX, minX, maxX) : floor.center.x;
        float centerX = Mathf.MoveTowards(ball.center.x, targetX, maxDistance);

        position.x = centerX - centerOffsetX;
        position.y += floor.max.y - ball.min.y;
        transform.position = position;
    }

    private void CreateWingJumpDots()
    {
        SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();
        wingJumpDots = new SpriteRenderer[WingJumpLimit];
        for (int i = 0; i < WingJumpLimit; i++)
        {
            GameObject dot = new GameObject("Wing Jump " + (i + 1));
            dot.transform.SetParent(transform, false);
            SpriteRenderer renderer = dot.AddComponent<SpriteRenderer>();
            renderer.sprite = playerSprite.sprite;
            renderer.sharedMaterial = playerSprite.sharedMaterial;
            renderer.sortingLayerID = playerSprite.sortingLayerID;
            renderer.sortingOrder = playerSprite.sortingOrder + 1;
            Vector3 scale = transform.lossyScale;
            Vector2 size = renderer.sprite.bounds.size;
            dot.transform.localScale = new Vector3(
                0.09f / (size.x * Mathf.Abs(scale.x)),
                0.09f / (size.y * Mathf.Abs(scale.y)), 1f);
            wingJumpDots[i] = renderer;
        }
    }

    private void LateUpdate()
    {
        UpdateCameraPosition(false);
        float top = playerCollider.bounds.max.y + 0.18f;
        for (int i = 0; i < wingJumpDots.Length; i++)
        {
            SpriteRenderer dot = wingJumpDots[i];
            dot.enabled = wingActive;
            dot.transform.position = new Vector3(transform.position.x + (i - 1) * 0.15f,
                top, transform.position.z);
            dot.color = i < WingJumpLimit - wingJumpsUsed
                ? Color.cyan : new Color(0.25f, 0.25f, 0.25f, 1f);
        }
    }
    
    private void UpdateCameraPosition(bool snap)
    {
        if (IsShowingPillars) return;
        Vector3 position = followCamera.transform.position;
        Vector2 target = transform.position;
        Vector2 center = snap ? target : Vector2.SmoothDamp(position, target,
            ref cameraVelocity, cameraSmoothTime);
        position.x = center.x;
        position.y = center.y;
        followCamera.transform.position = position;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            dragging = false;
            wingPointerHeld = false;
        }
    }
}














