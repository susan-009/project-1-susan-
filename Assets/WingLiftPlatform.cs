using UnityEngine;

[DefaultExecutionOrder(-100)]
public sealed class WingLiftPlatform : MonoBehaviour
{
    public float lowY = -3.2f;
    public float highY = 0.5f;
    public float travelSeconds = 3.5f;
    public float pauseSeconds = 0.8f;

    private BoxCollider2D platform;
    private GroundDragPlayer player;
    private float elapsed;

    private void Start()
    {
        platform = GetComponent<BoxCollider2D>();
        player = FindObjectOfType<GroundDragPlayer>();
    }

    private void Update()
    {
        Bounds oldBounds = platform.bounds;
        bool carry = player != null && player.CanRideWingPlatform &&
            player.TryGetComponent(out CapsuleCollider2D ball) &&
            ball.bounds.max.x > oldBounds.min.x && ball.bounds.min.x < oldBounds.max.x &&
            Mathf.Abs(ball.bounds.min.y - oldBounds.max.y) < 0.035f;

        elapsed += Time.deltaTime;
        float travel = Mathf.Max(0.1f, travelSeconds);
        float pause = Mathf.Max(0f, pauseSeconds);
        float phase = elapsed % (2f * (travel + pause));
        float fraction = phase < pause ? 0f :
            phase < pause + travel ? (phase - pause) / travel :
            phase < 2f * pause + travel ? 1f :
            1f - (phase - 2f * pause - travel) / travel;
        Vector3 position = transform.position;
        float deltaY = Mathf.Lerp(lowY, highY, fraction) - position.y;
        position.y += deltaY;
        transform.position = position;
        if (carry)
            player.transform.position += Vector3.up * deltaY;
        Physics2D.SyncTransforms();
    }
}
