using System;
using TemporalContract.Core;
using Unity.Netcode;
using UnityEngine;

namespace TemporalContract.Interactions
{
    /// <summary>
    /// Base for host-authoritative world interactions (doors, panels, switches, cover).
    /// Clients send intent only; the host derives the actor from the RPC sender, validates range,
    /// then lets the subclass validate role/state and mutate replicated state.
    /// </summary>
    public abstract class NetworkInteractable : NetworkBehaviour,
        IInteractionTarget, IInteractionRequestHandler, IFeedbackEventSource, IRoundResetParticipant
    {
        [Header("Interaction")]
        [Tooltip("Optional point used for the host-side range check. Defaults to this transform.")]
        [SerializeField] private Transform interactionPoint;
        [Tooltip("Host-side range, measured from the player's root to the interaction point. Keep it a little larger than the router's raycast distance to tolerate latency.")]
        [SerializeField, Min(0.5f)] private float serverMaxDistance = 4.5f;
        [SerializeField] private bool logFeedbackToConsole;

        public event Action<FeedbackEvent> FeedbackRaised;

        ulong? IInteractionTarget.NetworkObjectId => IsSpawned ? NetworkObjectId : (ulong?)null;

        protected Vector3 InteractionOrigin => interactionPoint != null ? interactionPoint.position : transform.position;

        // ---- Local (client) side: hints and intent only, never authoritative ----

        public bool TryGetPrompt(IInteractionActor actor, out InteractionPrompt prompt)
        {
            if (!IsSpawned)
            {
                prompt = default;
                return false;
            }

            return TryBuildPrompt(actor, out prompt);
        }

        public bool IsLocallyEligible(IInteractionActor actor) => IsSpawned && CanAttemptLocally(actor);

        public void RequestInteraction(in InteractionRequest request)
        {
            if (!IsSpawned)
            {
                return;
            }

            InteractRpc();
        }

        protected abstract bool TryBuildPrompt(IInteractionActor actor, out InteractionPrompt prompt);

        /// <summary>Local key-press gate. Return true even for denied states so the host can send denial feedback.</summary>
        protected virtual bool CanAttemptLocally(IInteractionActor actor) => true;

        // ---- Host side: the only place shared outcomes are decided ----

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void InteractRpc(RpcParams rpcParams = default)
        {
            // Actor identity comes from the transport, never from client-supplied data.
            ulong sender = rpcParams.Receive.SenderClientId;
            var request = new InteractionRequest(sender, NetworkObjectId);
            if (!TryHandleInteraction(request))
            {
                DeniedRpc(sender);
            }
        }

        public bool TryHandleInteraction(in InteractionRequest request)
        {
            if (!IsServer || !IsSpawned)
            {
                return false;
            }

            if (request.TargetNetworkObjectId != NetworkObjectId)
            {
                return false;
            }

            NetworkObject player = NetworkManager.SpawnManager.GetPlayerNetworkObject(request.ActorClientId);
            if (player == null)
            {
                return false;
            }

            float maxSqr = serverMaxDistance * serverMaxDistance;
            if ((player.transform.position - InteractionOrigin).sqrMagnitude > maxSqr)
            {
                return false;
            }

            return ExecuteOnServer(request, player);
        }

        /// <summary>Validate role/state and mutate replicated state. Called on the host after range checks pass.</summary>
        protected abstract bool ExecuteOnServer(in InteractionRequest request, NetworkObject player);

        public void ResetForRound(in RoundResetContext context)
        {
            if (IsServer && IsSpawned)
            {
                ResetState();
            }
        }

        /// <summary>Return owned state to its round baseline. Must be idempotent.</summary>
        protected abstract void ResetState();

        // ---- Feedback (presentation only) ----

        protected void BroadcastFeedback(FeedbackKind kind, float amount = 0f)
        {
            if (IsServer && IsSpawned)
            {
                FeedbackRpc(kind, amount);
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void FeedbackRpc(FeedbackKind kind, float amount)
        {
            RaiseLocal(kind, amount);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void DeniedRpc(ulong deniedClientId)
        {
            if (NetworkManager.LocalClientId == deniedClientId)
            {
                RaiseLocal(FeedbackKind.InteractionDenied, 0f);
            }
        }

        private void RaiseLocal(FeedbackKind kind, float amount)
        {
            if (logFeedbackToConsole)
            {
                Debug.Log($"[{name}] feedback {kind} ({amount})", this);
            }

            FeedbackRaised?.Invoke(new FeedbackEvent(kind, NetworkObjectId, amount));
        }
    }
}
