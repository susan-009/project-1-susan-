using UnityEngine;

public class TorchHandleActivator : MonoBehaviour
{
    public TorchPuzzleController controller;
    public string torchName;
    [HideInInspector] public bool ignited;

    public bool TryBurn()
    {
        return controller != null && controller.Ignite(this, null);
    }

    private void OnTriggerEnter2D(Collider2D other) => TryLegacyIgnition(other.gameObject);
    private void OnCollisionEnter2D(Collision2D collision) => TryLegacyIgnition(collision.gameObject);

    private void TryLegacyIgnition(GameObject other)
    {
        FireReturnToGround1 fire = other.GetComponentInParent<FireReturnToGround1>();
        if (fire != null)
            controller?.Ignite(this, fire.gameObject);
    }
}
