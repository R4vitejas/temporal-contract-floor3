using System;
using UnityEngine;

namespace TemporalContract.Interactions
{
    /// <summary>
    /// Bridge to the round/role system (owned by Samatma). The round coordinator assigns
    /// <see cref="IsAttacker"/> once roles exist. Until then it is null and attacker-only checks
    /// pass, so interactions can be tested without the match loop.
    /// </summary>
    public static class InteractionRoleGate
    {
        /// <summary>Host-side: returns true if the given client is currently the attacker.</summary>
        public static Func<ulong, bool> IsAttacker;

        public static bool CanActAsAttacker(ulong clientId) => IsAttacker == null || IsAttacker(clientId);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => IsAttacker = null;
    }
}
