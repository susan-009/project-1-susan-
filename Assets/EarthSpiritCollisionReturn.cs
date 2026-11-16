
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EarthSpiritCollisionReturn : MonoBehaviour
{
    [Header("返回位置")]
    public Transform ground1Point;

    [Header("需要排除的平台")]
    public Transform ground1Platform;
    public Transform ground2Platform;

    private Rigidbody2D rb;
    private SpiritLauncher launcher;

    private bool isReturning = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // 获取原来的土灵发射脚本
        launcher = GetComponent<SpiritLauncher>();

        if (launcher == null)
        {
            Debug.LogError("土灵缺少 SpiritLauncher 脚本！");
        }
    }

    // 碰到普通碰撞体
    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleContact(collision.collider);
    }

    // 碰到 Trigger 碰撞体
    void OnTriggerEnter2D(Collider2D other)
    {
        HandleContact(other);
    }

    // 统一处理碰撞
    void HandleContact(Collider2D other)
    {
        if (isReturning || other == null)
            return;

        // 忽略 Ground1 和 Ground2
        if (IsPlatform(other.transform, ground1Platform) ||
            IsPlatform(other.transform, ground2Platform))
        {
            return;
        }

        Debug.Log("土灵碰到了：" + other.name);

        // 检查是否碰到土灵机关
        EarthMechanismController mechanism =
            other.GetComponentInParent<EarthMechanismController>();

        if (mechanism != null)
        {
            Debug.Log("成功找到机关：" + mechanism.name);

            // 启动机关，控制对应石柱
            mechanism.Activate();
        }
        else
        {
            Debug.Log("碰到普通物体，不触发机关。");
        }

        // 无论碰到机关还是其他物体，都返回 Ground1
        StartCoroutine(ReturnToGround1());
    }

    // 判断是否为需要排除的平台
    bool IsPlatform(Transform target, Transform platform)
    {
        return platform != null &&
            (target == platform || target.IsChildOf(platform));
    }

    // 返回 Ground1
    IEnumerator ReturnToGround1()
    {
        isReturning = true;

        // 停止土灵移动
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // 暂停物理模拟
        rb.simulated = false;

        yield return new WaitForFixedUpdate();

        // 移动到 Ground1
        if (ground1Point != null)
        {
            transform.position = ground1Point.position;
            rb.position = new Vector2(
                ground1Point.position.x,
                ground1Point.position.y
            );

            Debug.Log("土灵已返回 Ground1！");
        }
        else
        {
            Debug.LogError("没有设置 StoneReturnPoint！");
        }

        // 恢复物理模拟
        rb.simulated = true;

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // 重置原来的鼠标操控和发射状态
        if (launcher != null)
        {
            launcher.ResetForRelauch();

            Debug.Log("土灵发射状态已重置，可以重新点击发射！");
        }

        isReturning = false;
    }
}

