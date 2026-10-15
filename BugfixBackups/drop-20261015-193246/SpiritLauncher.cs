using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(TrajectoryPredictor))]
public class SpiritLauncher : MonoBehaviour
{
    public float power = 5f;
    public string launchGroundName = "ground2";
    public Transform launchGround;
    static SpiritLauncher selected;
    enum LaunchState { Waiting, Deployed, Launched }
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
        if (selected != this || launchGround == null) return;
        if ((transform.position - launchGround.position).sqrMagnitude > .0001f) return;
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
        if (selected != null && selected != this) selected.ReturnToStart();
        selected = this;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
        transform.SetPositionAndRotation(launchGround.position, initialRotation);
        body.position = launchGround.position;
        state = LaunchState.Deployed;
        aiming = false;
        predictor.Hide();
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