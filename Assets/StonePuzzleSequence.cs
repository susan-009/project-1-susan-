// Pure puzzle rules, shared by all three mechanisms for one group of pillars.
public sealed class StonePuzzleSequence
{
    public enum HitResult { Correct, Wrong, Solved, Ignored }
    private const string Order = "ABCA";
    // Bit 0 is pillar 1. A set bit means raised.
    // Each correct hit changes two pillars; only the fourth hit lowers all four.
    private static readonly int[] RaisedStates = { 15, 12, 10, 3, 0 };

    public int Progress { get; private set; }
    public int Length => Order.Length;
    public bool IsSolved => Progress == Length;
    public int RaisedMask => RaisedStates[Progress];
    public char SymbolAt(int index) => Order[index];

    public HitResult Hit(char mechanism)
    {
        if (IsSolved) return HitResult.Ignored;
        if (mechanism != Order[Progress])
        {
            Progress = 0;
            return HitResult.Wrong;
        }
        Progress++;
        return IsSolved ? HitResult.Solved : HitResult.Correct;
    }
}
