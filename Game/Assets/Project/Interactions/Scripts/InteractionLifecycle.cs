using System;
using TemporalContract.Core;
using Unity.Netcode;
using UnityEngine;

namespace TemporalContract.Interactions
{
    /// <summary>
    /// The small adapter the match coordinator (Samatma) calls. All methods are host-only and do
    /// nothing on clients. No interaction needs to be found or wired by the caller.
    /// </summary>
    public static class InteractionLifecycle
    {
        /// <summary>Bind the production role source. Pass null to unbind (attacker actions are denied).</summary>
        public static void BindRoleProvider(Func<ulong, bool> isAttacker)
        {
            InteractionRoleGate.IsAttacker = isAttacker;
        }

        /// <summary>
        /// Optional: bind a host-side check for "may this client interact right now" (alive, round active).
        /// While unbound every spawned player may interact; the client router already blocks input.
        /// </summary>
        public static void BindActorEligibility(Func<ulong, bool> canInteract)
        {
            InteractionRoleGate.IsActorEligible = canInteract;
        }

        /// <summary>
        /// Round boundary: restores every interaction to its baseline (doors closed and locked-by-default,
        /// card uncollected, panel not granted, lights default, cover at baseline and released).
        /// Safe to call repeatedly with the same or a new context.
        /// </summary>
        public static void ResetAll(in RoundResetContext context)
        {
            if (!IsHost())
            {
                return;
            }

            foreach (NetworkInteractable interactable in
                     UnityEngine.Object.FindObjectsByType<NetworkInteractable>(FindObjectsSortMode.None))
            {
                interactable.ResetForRound(context);
            }
        }

        /// <summary>Player death: any cover they hold is dropped to a valid spot.</summary>
        public static void OnPlayerDied(ulong clientId)
        {
            if (!IsHost())
            {
                return;
            }

            foreach (MovableCover cover in
                     UnityEngine.Object.FindObjectsByType<MovableCover>(FindObjectsSortMode.None))
            {
                cover.ReleaseIfHeldBy(clientId);
            }
        }

        private static bool IsHost()
        {
            NetworkManager manager = NetworkManager.Singleton;
            return manager != null && manager.IsListening && manager.IsServer;
        }
    }
}
