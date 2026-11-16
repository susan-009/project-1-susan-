using System.Collections.Generic;
using UnityEngine;

public class TorchPuzzleController : MonoBehaviour
{
    public string[] order = { "Torch2_Middle", "Torch1_Top", "Torch3_Bottom" };
    public CaveDarknessController darkness;
    private readonly List<string> sequence = new List<string>();
    private bool solved;

    void Start()
    {
        if (darkness == null) darkness = FindObjectOfType<CaveDarknessController>();
        foreach (Transform t in FindObjectsOfType<Transform>())
        {
            if (t.name != "TorchHandle") continue;
            if (t.GetComponent<Collider2D>() == null) t.gameObject.AddComponent<BoxCollider2D>();
            TorchHandleActivator a = t.GetComponent<TorchHandleActivator>();
            if (a == null) a = t.gameObject.AddComponent<TorchHandleActivator>();
            a.controller = this;
            Transform p = t.parent;
            while (p != null)
            {
                if (p.name.StartsWith("Torch")) { a.torchName = p.name; break; }
                p = p.parent;
            }
        }
    }

    public bool Ignite(TorchHandleActivator handle, GameObject fire)
    {
        if (solved || handle.ignited) return false;
        handle.ignited = true;
        handle.gameObject.SetActive(false);
        sequence.Add(handle.torchName);
        if (fire != null) fire.GetComponent<FireReturnToGround1>()?.ReturnNow();
        if (sequence.Count < order.Length) return true;
        bool correct = true;
        for (int i = 0; i < order.Length; i++) if (sequence[i] != order[i]) correct = false;
        if (correct) { solved = true; if (darkness != null) darkness.LightUpCave(); }
        else ResetPuzzle();
        return true;
    }

    private void ResetPuzzle()
    {
        sequence.Clear();
        foreach (TorchHandleActivator h in FindObjectsOfType<TorchHandleActivator>(true))
        {
            h.ignited = false;
            h.gameObject.SetActive(true);
        }
    }
}

