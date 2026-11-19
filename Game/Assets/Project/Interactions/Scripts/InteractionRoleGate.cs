using System;
using UnityEngine;

namespace TemporalContract.Interactions
{
    /// <summary>
    /// Bridge to the round/role system (owned by Samatma). The round coordinator binds
    /// <see cref="IsAttacker"/> (see <see cref="InteractionLifecycle.BindRoleProvider"/>).
    ///
    /// Production behavior: while no provider is bound, attacker-only actions are DENIED.
    /// <see cref="AllowAllWhenUnbound"/> exists only for standalone test scenes and is never set by
    /// shipped code.
    /// </summary>
    public static class InteractionRoleGate
    {
        /// <summary>Host-side: returns true if the given client is currently the attacker.</summary>
        public static Func<ulong, bool> IsAttacker;

        /// <summary>TEST ONLY. Lets every client act as attacker while no provider is bound.</summary>
        public static bool AllowAllWhenUnbound;

        /// <summary>
        /// Optional host-side gate: false means this client may not interact right now (dead, between
        /// rounds, in a menu). Unbound = allowed, because the match coordinator owns that state.
        /// </summary>
        public static Func<ulong, bool> IsActorEligible;

        public static bool CanActorInteract(ulong clientId) => IsActorEligible == null || IsActorEligible(clientId);

        public static bool IsBound => IsAttacker != null;

        public static bool CanActAsAttacker(ulong clientId)
        {
            return IsAttacker != null ? IsAttacker(clientId) : AllowAllWhenUnbound;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsAttacker = null;
            IsActorEligible = null;
            AllowAllWhenUnbound = false;
        }
    }
}
