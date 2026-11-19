using Unity.Netcode.Components;

namespace TemporalContract.Networking
{
    /// <summary>Lets an owning player publish its movement; host gameplay still validates outcomes.</summary>
    public sealed class OwnerNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
