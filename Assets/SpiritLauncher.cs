using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(TrajectoryPredictor))]
public class SpiritLauncher : MonoBehaviour
{
    public float power = 5f;
    public string launchGroundName = "ground2";
    public Transform launchGround;
    public Transform dropPivot;
    [Min(0f)] public float settleSpeed = .15f;
    [Min(.02f)] public float settleDuration = .12f;
    float settledTime;
    static SpiritLauncher selected;
    enum LaunchState { Waiting, Dropping, Deployed, Launched }
    LaunchState state;
    Rigidbody2D body;
    TrajectoryPredictor predictor;
    Vector3 initialPosition;
    Quaternion initialRotation;
    Vector3 startPosition;
    bool aiming;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSelection() { selected = null; }

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        predictor = GetComponent<TrajectoryPredictor>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        state = LaunchState.Waiting;
        predictor.Hide();
    }

    void OnMouseDown()
    {
        if (state == LaunchState.Launched) return;
        if (state == LaunchState.Waiting)
        {
            Deploy();
            return; // Selection never starts aiming on the same click.
        }
        if (selected != this || state != LaunchState.Deployed) return;
        startPosition = transform.position;
        aiming = true;
        DrawPreview();
    }

    void Deploy()
    {
        if (launchGround == null)
        {
            GameObject ground = GameObject.Find(launchGroundName);
            if (ground == null) return;
            launchGround = ground.transform;
        }
        if (dropPivot == null) dropPivot = launchGround.Find("DropPivot");
        if (dropPivot == null)
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
        predictor.Hide();
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (state != LaunchState.Dropping || selected != this || launchGround == null) return;
        Transform surface = collision.collider.transform;
        if (surface != launchGround && !surface.IsChildOf(launchGround)) return;
        bool supported = false;
        for (int i = 0; i < collision.contactCount; i++)
            if (Vector2.Dot(collision.GetContact(i).normal, Vector2.up) > .5f)
                supported = true;
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
        predictor.Hide();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
        transform.SetPositionAndRotation(initialPosition, initialRotation);
        body.position = initialPosition;
        body.rotation = initialRotation.eulerAngles.z;
        state = LaunchState.Waiting;
        if (selected == this) selected = null;
    }

    bool TryGetMouseWorld(out Vector3 world)
    {
        world = default;
        Camera cam = Camera.main;
        if (cam == null) return false;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, startPosition);
        if (!plane.Raycast(ray, out float distance)) return false;
        world = ray.GetPoint(distance);
        return true;
    }

    void Update()
    {
        if (!aiming || selected != this || state != LaunchState.Deployed) return;
        if (Input.GetMouseButtonUp(0))
        {
            aiming = false;
            predictor.Hide();
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

    void DrawPreview()
    {
        if (TryGetMouseWorld(out Vector3 mouse))
            predictor.DrawTrajectory((Vector2)(startPosition - mouse) * power);
    }

    void OnDisable()
    {
        aiming = false;
        if (predictor != null) predictor.Hide();
        if (selected == this) selected = null;
    }
}