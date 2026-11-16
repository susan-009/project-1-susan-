using System.Collections;
using UnityEngine;

public sealed class StoneMechanismPuzzle : MonoBehaviour
{
    private readonly StonePuzzleSequence sequence = new StonePuzzleSequence();
    private StonePillarController[] pillars;
    private GroundDragPlayer player;
    private bool busy;

    public int Progress => sequence.Progress;
    public bool IsSolved => sequence.IsSolved;

    public void Initialize(StonePillarController[] group)
    {
        if (pillars != null) return;
        pillars = group;
        player = FindObjectOfType<GroundDragPlayer>();
        foreach (StonePillarController pillar in pillars)
            pillar.SetRaisedImmediate(true);
    }

    public void Hit(EarthMechanismController mechanism)
    {
        if (pillars == null || busy || sequence.IsSolved ||
            (player != null && player.IsShowingPillars)) return;

        StonePuzzleSequence.HitResult result = sequence.Hit(mechanism.Symbol);
        bool lastHitWrong = result == StonePuzzleSequence.HitResult.Wrong;
        mechanism.ShowFeedback(lastHitWrong ? Color.red : new Color(0.4f, 1f, 0.55f));
        int mask = sequence.RaisedMask;
        busy = true;

        Bounds view = pillars[0].CameraBounds;
        float duration = 0f;
        foreach (StonePillarController pillar in pillars)
        {
            view.Encapsulate(pillar.CameraBounds);
            duration = Mathf.Max(duration, pillar.moveDuration);
        }

        if (player != null && player.isActiveAndEnabled && player.FollowCamera != null)
        {
            player.ShowPillarAnimation(view, duration, () => ApplyState(mask));
            StartCoroutine(WaitForPresentation(duration, true));
        }
        else
        {
            ApplyState(mask);
            StartCoroutine(WaitForPresentation(duration, false));
        }
    }

    private void ApplyState(int mask)
    {
        for (int i = 0; i < pillars.Length; i++)
        {
            bool raised = (mask & (1 << i)) != 0;
            if (pillars[i].IsRaised == raised) continue;
            if (raised) pillars[i].Raise();
            else pillars[i].Lower();
        }
    }

    private IEnumerator WaitForPresentation(float duration, bool hasCamera)
    {
        if (hasCamera)
        {
            while (player != null && player.IsShowingPillars) yield return null;
        }
        else yield return new WaitForSeconds(Mathf.Max(0f, duration));
        busy = false;
    }
}
