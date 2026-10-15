using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    public string smallStoneName = "small stone";
    public string stoneName = "stone";
    public string movingColumnName = "rightMovingColumn";
    private bool smallStoneTouched;
    private bool activated;
    private Collider2D smallStoneCollider;
    private Collider2D stoneCollider;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.name == smallStoneName)
        {
            smallStoneTouched = true;
            smallStoneCollider = other;
        }
    }

    void Update()
    {
        if (!smallStoneTouched || activated) return;
        if (smallStoneCollider == null)
        {
            GameObject small = GameObject.Find(smallStoneName);
            if (small != null) smallStoneCollider = small.GetComponent<Collider2D>();
        }
        if (stoneCollider == null)
        {
            GameObject stone = GameObject.Find(stoneName);
            if (stone != null) stoneCollider = stone.GetComponent<Collider2D>();
        }
        if (smallStoneCollider != null && stoneCollider != null && smallStoneCollider.IsTouching(stoneCollider))
            Activate();
    }

    void Activate()
    {
        activated = true;
        DisableObject(stoneName);
        DisableObject(smallStoneName);
        DisableObject(movingColumnName);
        gameObject.SetActive(false);
    }

    void DisableObject(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        if (target != null) target.SetActive(false);
    }
}
