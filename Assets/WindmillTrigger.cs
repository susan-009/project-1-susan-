using UnityEngine;

public class WindmillTrigger : MonoBehaviour
{
    public GameObject barrier;


    private void OnTriggerEnter2D(Collider2D other)
    {
        // 判断是不是风灵
        if (other.gameObject.name == "wing")
        {
            Debug.Log("风灵激活风车");


            // 删除挡板
            Destroy(barrier);
        }
    }
}
