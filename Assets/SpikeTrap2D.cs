
using System.Collections;
using UnityEngine;

public class SpikeTrap2D : MonoBehaviour
{
    [Header("尖刺设置")]
    public GameObject spikePrefab;
    public Transform firePoint;

    [Header("发射参数")]
    public Vector2 direction = Vector2.down;
    public float interval = 2.5f;
    public float firstDelay = 1f;
    public float speed = 5f;
    public float lifeTime = 5f;

    void Start()
    {
        if (spikePrefab == null || firePoint == null)
        {
            Debug.LogWarning("请设置尖刺预制体和 FirePoint！");
            return;
        }

        StartCoroutine(FireRoutine());
    }

    IEnumerator FireRoutine()
    {
        // 等待首次发射时间
        yield return new WaitForSeconds(firstDelay);

        while (true)
        {
            // 生成尖刺
            GameObject spike = Instantiate(
                spikePrefab,
                firePoint.position,
                Quaternion.identity
            );

            // 设置尖刺飞行速度
            Rigidbody2D rb = spike.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                rb.velocity = direction.normalized * speed;
            }
            else
            {
                Debug.LogWarning("尖刺预制体缺少 Rigidbody2D！");
            }

            // 一段时间后销毁尖刺
            Destroy(spike, lifeTime);

            // 等待下一次发射
            yield return new WaitForSeconds(interval);
        }
    }
}
