using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(TrajectoryPredictor))]
public class SpiritLauncher : MonoBehaviour
{
    public float power = 5f;
    public string launchGroundName = "ground2";
    public bool deployOnFirstClick = false;
    Rigidbody2D body;
    TrajectoryPredictor predictor;
    Vector3 startPosition;
    bool aiming;
    bool deployed;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        predictor = GetComponent<TrajectoryPredictor>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        deployed = !deployOnFirstClick;
        predictor.Hide();
    }

    void OnMouseDown()
    {
        if (!deployed)
        {
            GameObject ground = GameObject.Find(launchGroundName);
            if (ground == null) return;
            transform.position = ground.transform.position;
            body.position = transform.position;
            deployed = true;
            return;
        }
        if (body.bodyType != RigidbodyType2D.Kinematic) return;
        startPosition = transform.position;
        aiming = true;
        DrawPreview();
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
        if (!aiming) return;
        if (Input.GetMouseButtonUp(0))
        {
            aiming = false;
            predictor.Hide();
            if (!TryGetMouseWorld(out Vector3 mouse)) return;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.velocity = (Vector2)(startPosition - mouse) * power;
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
    }
}