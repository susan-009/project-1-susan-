
using UnityEngine;

public class FireReturnToGround1 : MonoBehaviour
{
    public Transform ground1Point;

    private Rigidbody2D rb;

    void Awake() { if (FindObjectOfType<TorchPuzzleController>() == null) new GameObject("TorchPuzzleController").AddComponent<TorchPuzzleController>();
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            rb = GetComponentInChildren<Rigidbody2D>();
        }
    }

    public void ReturnNow()
    {
        var launcher = GetComponent<SpiritLauncher>();
        if (launcher != null)
        {
            launcher.ResetForRelauch();
        }

        if (ground1Point == null)
        {
            var g = GameObject.Find("ground1");
            if (g != null)
            {
                ground1Point = g.transform;
            }
        }

        if (ground1Point == null)
        {
            Debug.LogWarning("Missing ground1 return point");
            return;
        }

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = ground1Point.position;
        }
        else
        {
            transform.position = ground1Point.position;
        }
    } bool IsLaunchGround(Transform t){var g=GameObject.Find("ground2");return g!=null&&(t==g.transform||t.IsChildOf(g.transform));} void OnTriggerEnter2D(Collider2D o){if(!IsLaunchGround(o.transform)&&o.GetComponentInParent<TorchHandleActivator>()==null)ReturnNow();} void OnCollisionEnter2D(Collision2D c){if(!IsLaunchGround(c.collider.transform)&&c.collider.GetComponentInParent<TorchHandleActivator>()==null)ReturnNow();}}







