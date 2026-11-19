using UnityEngine;

public sealed class BurnableThorns : MonoBehaviour
{
    public LandThorns owner;
    public bool TryBurn()
    {
        RegrowingThorns regrowing = GetComponent<RegrowingThorns>();
        return regrowing != null ? regrowing.Burn() : owner != null && owner.Burn();
    }
}
