namespace TemporalContract.Core
{
    /// <summary>Client-originated intent. The host must revalidate every field and current state.</summary>
    public readonly struct InteractionRequest
    {
        public InteractionRequest(ulong actorClientId, ulong? targetNetworkObjectId)
        {
            ActorClientId = actorClientId;
            TargetNetworkObjectId = targetNetworkObjectId;
        }

        public ulong ActorClientId { get; }
        public ulong? TargetNetworkObjectId { get; }
    }

    /// <summary>Host-created reset context for one round boundary.</summary>
    public readonly struct RoundResetContext
    {
        public RoundResetContext(uint roundIndex, ulong matchId)
        {
            RoundIndex = roundIndex;
            MatchId = matchId;
        }

        public uint RoundIndex { get; }
        public ulong MatchId { get; }
    }
}
