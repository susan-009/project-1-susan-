using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class CavePlayModeLighting : MonoBehaviour
{
    [SerializeField] private Light2D ambientLight;
    [SerializeField] private Light2D playerLight;

    private void Awake()
    {
        if (ambientLight != null) ambientLight.intensity = 0f;
        if (playerLight != null) playerLight.enabled = true;
    }
}
