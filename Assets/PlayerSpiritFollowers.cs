using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerSpiritFollowers : MonoBehaviour
{
    [SerializeField] private Transform[] spirits;
    private enum FireState { Following, Ready, Flying, Returning }

    private const float DragThreshold = 8f;
    private const float FlightDuration = 0.85f;
    private const float ReturnSpeed = 12f;
    private static readonly Vector3 Gravity = new Vector3(0f, -9.81f, 0f);

    private Vector3[] offsets;
    private int wingReturnFrame = -1;

    public void MarkWingReturnClick() => wingReturnFrame = Time.frameCount;

    private GroundDragPlayer player;
    private StoneAbility stoneAbility;
    public float Facing => facing;
    public bool IsFireSelected => fireState != FireState.Following;
    public bool CanSummonStone => fireState == FireState.Following && player != null && !player.IsWingActive;
    private Transform wing;
    private SpriteRenderer wingSprite;
    private Transform fire;
    private SpriteRenderer fireSprite;
    private TrajectoryPredictor preview;
    private Camera aimCamera;
    private FireState fireState;
    private bool pressingFire;
    private Vector2 pressPosition;
    private Vector3 launchPosition;
    private Vector3 launchVelocity;
    private float flightTime;
    private float previousX;
    private float facing = 1f;

    private void Awake()
    {
        offsets = new Vector3[spirits.Length];
        for (int i = 0; i < spirits.Length; i++)
        {
            if (spirits[i] != null)
                offsets[i] = spirits[i].position - transform.position;
        }
    }

    private void Start()
    {
        aimCamera = Camera.main;
        player = GetComponent<GroundDragPlayer>();
        previousX = transform.position.x;
        foreach (Transform spirit in spirits)
        {
            if (spirit == null)
                continue;

            if (spirit.TryGetComponent(out SpiritLauncher launcher))
                launcher.enabled = false;
            if (spirit.TryGetComponent(out Rigidbody2D body))
                body.simulated = false;

            if (spirit.name == "stone")
            {
                stoneAbility = gameObject.AddComponent<StoneAbility>();
                stoneAbility.Initialize(spirit, this);
            }

            if (spirit.name == "wing")
            {
                wing = spirit;
                wingSprite = spirit.GetComponent<SpriteRenderer>();
            }

            if (spirit.name == "fire")
            {
                fire = spirit;
                fireSprite = spirit.GetComponent<SpriteRenderer>();
                preview = spirit.GetComponent<TrajectoryPredictor>();
            }
        }
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(1) || player == null || player.IsLevelComplete || player.IsShowingPillars ||
            fire == null || wing == null || stoneAbility == null)
            return;

        // Derive the selection from live state, including left-click summons.
        bool nextStone = fireState != FireState.Following;
        bool nextWing = !nextStone && !stoneAbility.IsFollowing;
        player.SetWingActive(false);
        pressingFire = false;
        if (preview != null) preview.Hide();
        fireState = FireState.Following;
        stoneAbility.SetSelected(false);

        if (nextStone)
            stoneAbility.SetSelected(true);
        else if (nextWing)
        {
            player.SetWingActive(true);
            wing.position = SelectedPosition(wing);
        }
        else
        {
            fireState = FireState.Ready;
            fire.position = SelectedPosition(fire);
        }
    }

    private void LateUpdate()
    {
        if (player != null && player.IsLevelComplete) return;
        Vector3 playerPosition = transform.position;
        float movement = playerPosition.x - previousX;
        if (movement != 0f)
            facing = Mathf.Sign(movement);
        previousX = playerPosition.x;

        for (int i = 0; i < spirits.Length; i++)
        {
            if (spirits[i] == null || (spirits[i] == fire && fireState != FireState.Following) ||
                (spirits[i] == wing && player != null && player.IsWingActive) ||
                (spirits[i].name == "stone" && stoneAbility != null && !stoneAbility.IsFollowing))
                continue;

            Vector3 offset = offsets[i];
            offset.x = -facing * Mathf.Abs(offset.x);
            spirits[i].position = playerPosition + offset;
        }
        if (player != null && player.IsShowingPillars) return;
        if (stoneAbility != null && !stoneAbility.IsFollowing)
            return;
        if (Input.GetMouseButtonDown(1))
        {
            if (player.IsWingActive)
                wing.position = SelectedPosition(wing);
            return;
        }
        UpdateWing(movement == 0f);
        if (player == null || !player.IsWingActive)
            UpdateFire(movement == 0f);
    }

    public Vector3 SelectedPosition(Transform spirit)
    {
        Vector3 desired = transform.position + new Vector3(facing * 0.85f, 0.35f, 0f);
        return SpiritHeldPosition.Resolve(transform, spirit, desired);
    }

    public bool IsPointerOnWing()
    {
        if (wing == null || wingSprite == null || aimCamera == null)
            return false;

        Vector3 mouse = Input.mousePosition;
        mouse.z = aimCamera.WorldToScreenPoint(wing.position).z;
        Vector3 world = aimCamera.ScreenToWorldPoint(mouse);
        Bounds bounds = wingSprite.bounds;
        return bounds.SqrDistance(new Vector3(world.x, world.y, bounds.center.z)) < 0.0025f;
    }

    private void UpdateWing(bool playerStopped)
    {
        if (wing == null || wingSprite == null || aimCamera == null || player == null || !player.enabled)
            return;

        if (wingReturnFrame != Time.frameCount && Input.GetMouseButtonDown(0) && !player.IsWingActive && playerStopped && player.CanToggleWing &&
            fireState == FireState.Following)
        {
            Vector3 mouse = Input.mousePosition;
            mouse.z = aimCamera.WorldToScreenPoint(wing.position).z;
            Vector3 world = aimCamera.ScreenToWorldPoint(mouse);
            Bounds bounds = wingSprite.bounds;
            if (bounds.SqrDistance(new Vector3(world.x, world.y, bounds.center.z)) < 0.0025f)
                player.SetWingActive(!player.IsWingActive);
        }

        if (player.IsWingActive)
            wing.position = SelectedPosition(wing);
    }

    private void UpdateFire(bool playerStopped)
    {
        if (fire == null || fireSprite == null || aimCamera == null)
            return;

        Vector3 readyPosition = SelectedPosition(fire);
        if (fireState == FireState.Flying)
        {
            flightTime = Mathf.Min(flightTime + Time.deltaTime, FlightDuration);
            Vector3 nextPosition = FlightPoint(launchPosition, launchVelocity, flightTime);
            if (TryBurnTarget(nextPosition))
            {
                fireState = FireState.Returning;
                return;
            }
            fire.position = nextPosition;
            if (flightTime >= FlightDuration)
                fireState = FireState.Returning;
            return;
        }

        if (fireState == FireState.Returning)
        {
            fire.position = Vector3.MoveTowards(fire.position, readyPosition, ReturnSpeed * Time.deltaTime);
            if (fire.position == readyPosition)
                fireState = FireState.Ready;
            return;
        }

        Vector3 mouse = Input.mousePosition;
        if (Input.GetMouseButtonDown(0))
        {
            mouse.z = aimCamera.WorldToScreenPoint(fire.position).z;
            Vector3 world = aimCamera.ScreenToWorldPoint(mouse);
            Bounds bounds = fireSprite.bounds;
            bool hit = bounds.SqrDistance(new Vector3(world.x, world.y, bounds.center.z)) < 0.0025f;
            if (hit && fireState == FireState.Following && playerStopped)
            {
                fireState = FireState.Ready;
                fire.position = readyPosition;
                return;
            }
            pressingFire = hit && fireState == FireState.Ready;
            pressPosition = mouse;
        }

        if (fireState != FireState.Ready)
            return;

        fire.position = readyPosition;
        if (!pressingFire)
            return;

        Vector2 drag = (Vector2)mouse - pressPosition;
        bool aiming = drag.sqrMagnitude >= DragThreshold * DragThreshold;
        float unitsPerPixel = 2f * aimCamera.orthographicSize / aimCamera.pixelHeight;
        Vector3 velocity = Vector2.ClampMagnitude(drag * unitsPerPixel * 6f, 12f);
        if (Input.GetMouseButtonUp(0))
        {
            pressingFire = false;
            if (preview != null)
                preview.Hide();
            if (!aiming)
            {
                fireState = FireState.Following;
                return;
            }
            launchPosition = fire.position;
            launchVelocity = velocity;
            flightTime = 0f;
            fireState = FireState.Flying;
        }
        else if (aiming && preview != null)
        {
            preview.Show();
            const int samples = 20;
            preview.line.positionCount = samples;
            for (int i = 0; i < samples; i++)
                preview.line.SetPosition(i, FlightPoint(fire.position, velocity, FlightDuration * i / (samples - 1)));
        }
        else if (preview != null)
        {
            preview.Hide();
        }
    }

    private bool TryBurnTarget(Vector3 nextPosition)
    {
        Vector2 travel = nextPosition - fire.position;
        float distance = travel.magnitude;
        if (distance <= 0f)
            return false;

        float radius = Mathf.Min(fireSprite.bounds.extents.x, fireSprite.bounds.extents.y);
        Physics2D.SyncTransforms();
        foreach (RaycastHit2D hit in Physics2D.CircleCastAll(fire.position, radius, travel / distance, distance))
        {
            bool burnedThorns = hit.collider.TryGetComponent(out BurnableThorns thorns) && thorns.TryBurn();
            SensorTorch sensor = hit.collider.GetComponentInParent<SensorTorch>();
            bool burnedTorch = !burnedThorns && sensor != null && sensor.TryIgnite();
            if (!burnedThorns && !burnedTorch && sensor == null)
                burnedTorch = hit.collider.TryGetComponent(out TorchHandleActivator handle) && handle.TryBurn();
            if (!burnedThorns && !burnedTorch) continue;

            fire.position = new Vector3(hit.centroid.x, hit.centroid.y, fire.position.z);
            return true;
        }
        return false;
    }

    private static Vector3 FlightPoint(Vector3 origin, Vector3 velocity, float time)
    {
        return origin + velocity * time + 0.5f * Gravity * time * time;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            return;

        pressingFire = false;
        if (preview != null)
            preview.Hide();
    }
}







