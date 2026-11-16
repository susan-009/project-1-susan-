#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
static class UsePivotHandle
{
    static UsePivotHandle()
    {
        Tools.pivotMode = PivotMode.Pivot;
    }
}
#endif
