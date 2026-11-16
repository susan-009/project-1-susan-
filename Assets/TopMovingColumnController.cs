
using UnityEngine;

public class TopMovingColumnController : MonoBehaviour
{
    private bool removed = false;

    public void RemoveColumn()
    {
        if (removed) return;

        removed = true;
        gameObject.SetActive(false);

        Debug.Log("TopMovingColumn 已移除，风车通道开放！");
    }
}

