using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(TrajectoryPredictor))]
public class SpiritLauncher : MonoBehaviour
{
    [Header("操控模式")]
    public bool directMouseControl;

    [Header("发射设置")]
    public float power = 5f;
    public string launchGroundName = "ground2";
    public Transform launchGround;
    public Transform dropPivot;
    [Min(0f)] public float settleSpeed = .15f;
    [Min(.02f)] public float settleDuration = .12f;

    [Header("直接操控设置")]
    [Min(0f)] public float followSmooth = 0f;

    float settledTime;
    static SpiritLauncher selected;
    enum LaunchState { Waiting, Dropping, Deployed, Launched }
    LaunchState state;
    Rigidbody2D body;
    TrajectoryPredictor predictor;
    Vector3 initialPosition;
    Quaternion initialRotation;
    Vector3 startPosition;
    Vector3 dragOffset;
    bool aiming;
    bool dragging;

    bool IsDirectControl => directMouseControl || gameObject.name == "wing";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSelection() { selected = null; }

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        predictor = GetComponent<TrajectoryPredictor>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        ResolveGround();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        state = LaunchState.Waiting;
        if (predictor != null) predictor.Hide();
    }

    void ResolveGround()
    {
        if (launchGround == null)
        {
            GameObject ground = GameObject.Find(launchGroundName);
            if (ground != null) launchGround = ground.transform;
        }
        if (dropPivot == null && launchGround != null)
            dropPivot = launchGround.Find("DropPivot");
    }

    void OnMouseDown()
    {
        if (IsDirectControl)
        {
            // 第一次单击只把 wing 放到 ground2；停稳后才进入拖拽操控。
            if (state == LaunchState.Waiting)
            {
                Deploy();
                return;
            }
            if (state != LaunchState.Deployed) return;
            if (!TryGetMouseWorld(out Vector3 mouse)) return;
            dragging = true;
            dragOffset = transform.position - mouse;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            return;
        }

        if (state == LaunchState.Launched) return;
        if (state == LaunchState.Waiting)
        {
            Deploy();
            return;
        }
        if (selected != this || state != LaunchState.Deployed) return;
        startPosition = transform.position;
        aiming = true;
        DrawPreview();
    }

    void Deploy()
    {
        ResolveGround();
        if (launchGround == null || dropPivot == null)
        {
            Debug.LogWarning("Assign DropPivot before selecting a ball.", this);
            return;
        }
        if (selected != null && selected != this) selected.ReturnToStart();
        selected = this;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
        transform.SetPositionAndRotation(dropPivot.position, initialRotation);
        body.position = dropPivot.position;
        body.rotation = initialRotation.eulerAngles.z;
        settledTime = 0f;
        state = LaunchState.Dropping;
        body.bodyType = RigidbodyType2D.Dynamic;
        body.WakeUp();
        aiming = false;
        if (predictor != null) predictor.Hide();
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (state != LaunchState.Dropping || selected != this || launchGround == null) return;
        Transform surface = collision.collider.transform;
        if (surface != launchGround && !surface.IsChildOf(launchGround)) return;
        bool supported = false;
        for (int i = 0; i < collision.contactCount; i++)
            if (Vector2.Dot(collision.GetContact(i).normal, Vector2.up) > .5f) supported = true;
        if (!supported || body.velocity.sqrMagnitude > settleSpeed * settleSpeed)
        {
            settledTime = 0f;
            return;
        }
        settledTime += Time.fixedDeltaTime;
        if (settledTime < settleDuration) return;
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        state = LaunchState.Deployed;
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (state == LaunchState.Dropping && launchGround != null &&
            (collision.collider.transform == launchGround || collision.collider.transform.IsChildOf(launchGround)))
            settledTime = 0f;
    }

    void ReturnToStart()
    {
        aiming = false;
        if (predictor != null) predictor.Hide();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
        transform.SetPositionAndRotation(initialPosition, initialRotation);
        body.position = initialPosition;
        body.rotation = initialRotation.eulerAngles.z;
        state = LaunchState.Waiting;
        if (selected == this) selected = null;
    }

    public void ResetForRelauch() { aiming=false; dragging=false; body.bodyType=RigidbodyType2D.Kinematic; body.velocity=Vector2.zero; body.angularVelocity=0f; state=LaunchState.Waiting; if (predictor!=null) predictor.Hide(); if (selected==this) selected=null; }    public void ReturnToGround2()
    {
        ResolveGround();
        Transform target = dropPivot != null ? dropPivot : launchGround;
        Vector3 position = target != null ? target.position : initialPosition;
        transform.SetPositionAndRotation(position, initialRotation);
        body.position = position;
        body.rotation = initialRotation.eulerAngles.z;
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        aiming = false;
        dragging = false;
        state = LaunchState.Waiting;
        if (predictor != null) predictor.Hide();
        if (selected == this) selected = null;
    }

    void Update()
    {
        if (IsDirectControl)
        {
            if (state == LaunchState.Deployed) UpdateDirectControl();
            return;
        }
        if (!aiming || selected != this || state != LaunchState.Deployed) return;
        if (Input.GetMouseButtonUp(0))
        {
            aiming = false;
            if (predictor != null) predictor.Hide();
            if (!TryGetMouseWorld(out Vector3 mouse)) return;
            Vector2 velocity = (Vector2)(startPosition - mouse) * power;
            if (velocity.sqrMagnitude < .01f) return;
            state = LaunchState.Launched;
            selected = null;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.velocity = velocity;
            return;
        }
        if (Input.GetMouseButton(0)) DrawPreview();
    }

    void UpdateDirectControl()
    {
        if (!dragging) return;
        if (!Input.GetMouseButton(0))
        {
            dragging = false;
            return;
        }
        if (!TryGetMouseWorld(out Vector3 mouse)) return;
        Vector3 target = mouse + dragOffset;
        target.z = transform.position.z;
        if (followSmooth <= 0f)
            transform.position = target;
        else
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-followSmooth * Time.deltaTime));
        body.position = transform.position;
    }

    void DrawPreview()
    {
        if (TryGetMouseWorld(out Vector3 mouse) && predictor != null)
            predictor.DrawTrajectory((Vector2)(startPosition - mouse) * power);
    }

    bool TryGetMouseWorld(out Vector3 world)
    {
        world = default;
        Camera cam = Camera.main;
        if (cam == null) return false;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, transform.position);
        if (!plane.Raycast(ray, out float distance)) return false;
        world = ray.GetPoint(distance);
        return true;
    }

    void OnDisable()
    {
        aiming = false;
        dragging = false;
        if (predictor != null) predictor.Hide();
        if (selected == this) selected = null;
    }
 }







