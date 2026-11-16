using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ThornResetWing : MonoBehaviour
{
    public string wingName = "wing";
    public bool resetOnStay;
    bool triggered;

    void OnTriggerEnter2D(Collider2D other) { TryReset(other); }
    void OnCollisionEnter2D(Collision2D collision) { TryReset(collision.collider); }
    void OnTriggerStay2D(Collider2D other) { if (resetOnStay) TryReset(other); }

    void TryReset(Collider2D other)
    {
        if (triggered) return;
        SpiritLauncher wing = other.GetComponentInParent<SpiritLauncher>();
        if (wing == null && other.gameObject.name == wingName)
            wing = other.GetComponent<SpiritLauncher>();
        if (wing == null) return;
        triggered = true;
        wing.ReturnToGround2();
        Invoke(nameof(Arm), .15f);
    }

    void Arm() { triggered = false; }
}
