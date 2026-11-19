using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class CavePlayModeLighting : MonoBehaviour
{
    [SerializeField] private Light2D ambientLight;
    [SerializeField] private Light2D playerLight;

    private void Awake()
    {
        ambientLight.intensity = 0f;
        playerLight.enabled = true;
    }
}
