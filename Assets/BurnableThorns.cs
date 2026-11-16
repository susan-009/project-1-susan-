using UnityEngine;

public sealed class BurnableThorns : MonoBehaviour
{
    public LandThorns owner;
    public bool TryBurn() => owner != null && owner.Burn();
}
