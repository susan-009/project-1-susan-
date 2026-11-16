using System.Collections;
using UnityEngine;

public class EarthMechanismController : MonoBehaviour
{
    [Header("四根石柱（共用四步顺序谜题）")]
    public StonePillarController pillar1;
    public StonePillarController pillar2;
    public StonePillarController pillar3;
    public StonePillarController pillar4;

    // Preserve serialized fields for compatibility with existing scenes.
    [HideInInspector] public bool togglePillar1;
    [HideInInspector] public bool togglePillar2;
    [HideInInspector] public bool togglePillar3;
    [HideInInspector] public bool togglePillar4;

    private StoneMechanismPuzzle puzzle;
    private SpriteRenderer visual;
    private Color originalColor;
    private Coroutine feedback;

    public char Symbol
    {
        get
        {
            string trimmed = gameObject.name.Trim();
            return trimmed.Length == 0 ? '?' : char.ToUpperInvariant(trimmed[trimmed.Length - 1]);
        }
    }

    private void Start()
    {
        visual = GetComponentInChildren<SpriteRenderer>();
        if (visual != null) originalColor = visual.color;
        EnsurePuzzle();
    }

    private bool EnsurePuzzle()
    {
        if (puzzle != null) return true;
        if (pillar1 == null || pillar2 == null || pillar3 == null || pillar4 == null) return false;
        puzzle = pillar1.GetComponent<StoneMechanismPuzzle>();
        if (puzzle == null) puzzle = pillar1.gameObject.AddComponent<StoneMechanismPuzzle>();
        puzzle.Initialize(new[] { pillar1, pillar2, pillar3, pillar4 });
        return true;
    }

    public void Activate()
    {
        if (Symbol != 'A' && Symbol != 'B' && Symbol != 'C') return;
        if (EnsurePuzzle()) puzzle.Hit(this);
    }

    public void ShowFeedback(Color color)
    {
        if (visual == null) return;
        if (feedback != null) StopCoroutine(feedback);
        feedback = StartCoroutine(Flash(color));
    }

    private IEnumerator Flash(Color color)
    {
        visual.color = color;
        yield return new WaitForSecondsRealtime(0.6f);
        if (visual != null) visual.color = originalColor;
        feedback = null;
    }
}
