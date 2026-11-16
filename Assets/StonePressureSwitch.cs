using UnityEngine;
using UnityEngine.Events;

public sealed class StonePressureSwitch : MonoBehaviour
{
    public UnityEvent onPressed = new UnityEvent();
    public UnityEvent onReleased = new UnityEvent();
    private bool pressed;

    private void Update()
    {
        bool touching = false;
        Collider2D plate = GetComponent<Collider2D>();
        if (plate != null)
        {
            foreach (StoneAbility stone in FindObjectsOfType<StoneAbility>())
                touching |= stone.IsWeightOn(plate.bounds);
        }
        if (touching == pressed)
            return;
        pressed = touching;
        if (pressed) onPressed.Invoke();
        else onReleased.Invoke();
    }
}
