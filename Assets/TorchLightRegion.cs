using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public sealed class TorchLightRegion : MonoBehaviour
{
    [Tooltip("Width and height used to calculate the radius when Light Radius is zero.")]
    public Vector2 regionSize = new Vector2(6.6f, 11.3f);
    [Tooltip("Illuminate this area immediately when Play starts, without a torch hit.")]
    public bool startsLit;
    [Min(0f)] public float litIntensity = 1f;
    [SerializeField, Min(0f), Tooltip("Horizontal outer radius in world units. Zero uses half the diagonal of Region Size.")] private float lightRadius;
    [SerializeField, Min(0f), Tooltip("Full illuminated height in world units. Zero makes the light circular.")] private float lightHeight;
    [SerializeField] private Light2D regionLight;
    [SerializeField] private SpriteRenderer followSprite;

    public bool IsLit { get; private set; }
    public Light2D RegionLight => regionLight;
    private float Radius => lightRadius > 0f ? lightRadius : regionSize.magnitude * 0.5f;
    private float VerticalRadius => lightHeight > 0f ? lightHeight * 0.5f : Radius;
    private Vector3 Center => followSprite != null ? followSprite.bounds.center : transform.position;

    private void Awake()
    {
        IsLit = startsLit;
        FollowSprite();
        ConfigureLight();
    }

    private void LateUpdate() => FollowSprite();

    private void FollowSprite()
    {
        if (followSprite != null)
            transform.position = Center;
    }

    public void ConfigureLight()
    {
        if (regionLight == null) regionLight = GetComponent<Light2D>();
        if (regionLight == null) regionLight = gameObject.AddComponent<Light2D>();
        if (lightHeight > 0f)
        {
            // Freeform lights apply transform scale to both the shape and its falloff.
            const int vertexCount = 64;
            const float innerRadius = 0.3f;
            var shape = new Vector3[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                float angle = i * (2f * Mathf.PI / vertexCount);
                shape[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * innerRadius;
            }
            regionLight.lightType = Light2D.LightType.Freeform;
            regionLight.SetShapePath(shape);
            regionLight.shapeLightFalloffSize = 1f - innerRadius;
            transform.localScale = new Vector3(Radius, VerticalRadius, 1f);
        }
        else
        {
            regionLight.lightType = Light2D.LightType.Point;
            regionLight.pointLightInnerAngle = 360f;
            regionLight.pointLightOuterAngle = 360f;
            regionLight.pointLightInnerRadius = Radius * 0.3f;
            regionLight.pointLightOuterRadius = Radius;
        }
        regionLight.color = Color.white;
        regionLight.intensity = litIntensity;
        regionLight.blendStyleIndex = 0;
        regionLight.overlapOperation = Light2D.OverlapOperation.AlphaBlend;
        regionLight.lightOrder = 100;
        regionLight.enabled = IsLit;
    }

    public void Illuminate()
    {
        if (IsLit) return;
        IsLit = true;
        if (regionLight == null) ConfigureLight();
        regionLight.intensity = litIntensity;
        regionLight.enabled = true;
    }

    public bool ContainsWorldPoint(Vector2 point)
    {
        Vector2 offset = point - (Vector2)Center;
        return new Vector2(offset.x / Radius, offset.y / VerticalRadius).sqrMagnitude <= 1f;
    }

    private void OnValidate()
    {
        regionSize = new Vector2(Mathf.Max(0.01f, regionSize.x), Mathf.Max(0.01f, regionSize.y));
        if (regionLight != null) ConfigureLight();
    }

    private void OnDrawGizmosSelected()
    {
        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.color = IsLit ? Color.yellow : Color.cyan;
        Gizmos.matrix = Matrix4x4.TRS(Center, Quaternion.identity, new Vector3(Radius, VerticalRadius, 1f));
        Gizmos.DrawWireSphere(Vector3.zero, 1f);
        Gizmos.matrix = previous;
    }
}
