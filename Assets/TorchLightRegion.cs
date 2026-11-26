using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public sealed class TorchLightRegion : MonoBehaviour
{
    [Tooltip("Width and height of the area illuminated by the linked torch.")]
    public Vector2 regionSize = new Vector2(6.6f, 11.3f);
    [Tooltip("Illuminate this area immediately when Play starts, without a torch hit.")]
    public bool startsLit;
    [Min(0f)] public float litIntensity = 1f;
    [Min(0f)] public float edgeFalloff = 0.05f;
    [SerializeField] private Light2D regionLight;

    public bool IsLit { get; private set; }
    public Light2D RegionLight => regionLight;

    private void Awake()
    {
        IsLit = startsLit;
        ConfigureLight();
    }

    public void ConfigureLight()
    {
        if (regionLight == null) regionLight = GetComponent<Light2D>();
        if (regionLight == null) regionLight = gameObject.AddComponent<Light2D>();
        Vector2 half = new Vector2(Mathf.Max(0.01f, regionSize.x), Mathf.Max(0.01f, regionSize.y)) * 0.5f;
        regionLight.lightType = Light2D.LightType.Freeform;
        regionLight.SetShapePath(new[] {
            new Vector3(-half.x, -half.y), new Vector3(half.x, -half.y),
            new Vector3(half.x, half.y), new Vector3(-half.x, half.y) });
        regionLight.shapeLightFalloffSize = edgeFalloff;
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
        Vector3 local = transform.InverseTransformPoint(new Vector3(point.x, point.y, transform.position.z));
        return Mathf.Abs(local.x) <= regionSize.x * 0.5f && Mathf.Abs(local.y) <= regionSize.y * 0.5f;
    }

    private void OnValidate()
    {
        regionSize = new Vector2(Mathf.Max(0.01f, regionSize.x), Mathf.Max(0.01f, regionSize.y));
        if (regionLight != null) ConfigureLight();
    }

    private void OnDrawGizmosSelected()
    {
        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = IsLit ? Color.yellow : Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(regionSize.x, regionSize.y, 0f));
        Gizmos.matrix = previous;
    }
}
