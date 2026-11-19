using TemporalContract.Interactions;
using UnityEngine;

namespace TemporalContract.Interactions.Testing
{
    /// <summary>TEST-ONLY stand-in for the production role provider. Lives only in the test scene.</summary>
    public static class TestRoles
    {
        public static uint Round;
        public static bool HasAttacker;
        public static ulong AttackerId;

        public static void Bind(ulong attackerClientId)
        {
            HasAttacker = true;
            AttackerId = attackerClientId;
            InteractionLifecycle.BindRoleProvider(id => HasAttacker && id == AttackerId);
        }

        public static void Bind(ulong? attackerClientId)
        {
            if (attackerClientId.HasValue)
            {
                Bind(attackerClientId.Value);
                return;
            }

            // Bound but nobody is attacker: attacker-only actions are denied for everyone.
            HasAttacker = false;
            InteractionLifecycle.BindRoleProvider(id => false);
        }

        public static void Unbind()
        {
            HasAttacker = false;
            InteractionLifecycle.BindRoleProvider(null);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Round = 0;
            HasAttacker = false;
            AttackerId = 0;
        }
    }
}
