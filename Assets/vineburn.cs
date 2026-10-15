using UnityEngine;

public class VineBurn : MonoBehaviour
{
    public Rigidbody2D stone;


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Fire"))
        {
            Burn();
        }
    }


    void Burn()
    {
        Destroy(gameObject);

        stone.bodyType = RigidbodyType2D.Dynamic;
    }
}