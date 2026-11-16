
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ScreenEdgeBoundary : MonoBehaviour
{
    public enum EdgeSide
    {
        Left,
        Right,
        Top,
        Bottom
    }

    public EdgeSide edgeSide;
    public Camera targetCamera;
    public float thickness = 0.5f;

    private BoxCollider2D box;

    void Awake()
    {
        box = GetComponent<BoxCollider2D>();

        // 必须是实体碰撞，不能是 Trigger
        box.isTrigger = false;

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    void LateUpdate()
    {
        if (targetCamera == null ||
            !targetCamera.orthographic)
        {
            return;
        }

        float halfHeight = targetCamera.orthographicSize;
        float halfWidth = halfHeight * targetCamera.aspect;

        Vector3 center = targetCamera.transform.position;

        switch (edgeSide)
        {
            case EdgeSide.Left:
                box.size = new Vector2(
                    thickness, halfHeight * 2 + thickness * 2);

                transform.position = new Vector3(
                    center.x - halfWidth - thickness / 2,
                    center.y, 0);
                break;

            case EdgeSide.Right:
                box.size = new Vector2(
                    thickness, halfHeight * 2 + thickness * 2);

                transform.position = new Vector3(
                    center.x + halfWidth + thickness / 2,
                    center.y, 0);
                break;

            case EdgeSide.Top:
                box.size = new Vector2(
                    halfWidth * 2 + thickness * 2, thickness);

                transform.position = new Vector3(
                    center.x,
                    center.y + halfHeight + thickness / 2, 0);
                break;

            case EdgeSide.Bottom:
                box.size = new Vector2(
                    halfWidth * 2 + thickness * 2, thickness);

                transform.position = new Vector3(
                    center.x,
                    center.y - halfHeight - thickness / 2, 0);
                break;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 只销毁带有自然灵发射脚本的物体
        SpiritLauncher spirit =
            collision.collider.GetComponentInParent<SpiritLauncher>();

        if (spirit != null)
        {
            Destroy(spirit.gameObject);
        }
    }
}
