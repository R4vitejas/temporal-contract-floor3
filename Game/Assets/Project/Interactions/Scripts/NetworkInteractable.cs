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
            bool granted = TryHandleInteraction(request);
            Debug.Log($"[{name}] request from client {sender}: " +
                      (granted ? "granted" : $"denied ({pendingDenyReason})"), this);
            if (!granted)
            {
                DeniedRpc(sender, pendingDenyReason);
            }
        }

        private string pendingDenyReason = string.Empty;

        /// <summary>Why the most recent request was denied (set on the host, then sent to the requester).</summary>
        public string LastDenialReason { get; private set; } = string.Empty;

        /// <summary>Record the denial reason and return false. Used by host-side validation.</summary>
        protected bool Deny(string reason)
        {
            pendingDenyReason = reason;
            return false;
        }

        public bool TryHandleInteraction(in InteractionRequest request)
        {
            pendingDenyReason = "rejected";
            if (!IsServer || !IsSpawned || !isActiveAndEnabled)
            {
                return Deny("not host or not spawned");
            }

            if (request.TargetNetworkObjectId != NetworkObjectId)
            {
                return Deny("wrong target id");
            }

            NetworkObject player = NetworkManager.SpawnManager.GetPlayerNetworkObject(request.ActorClientId);
            if (player == null)
            {
                return Deny("no player object for sender");
            }

            if (!InteractionRoleGate.CanActorInteract(request.ActorClientId))
            {
                return Deny("actor cannot interact right now");
            }

            float maxSqr = serverMaxDistance * serverMaxDistance;
            if ((player.transform.position - InteractionOrigin).sqrMagnitude > maxSqr)
            {
                return Deny("out of range");
            }

            if(RequiresVisibleApproach&&!HasVisibleApproach(player))return Deny("interaction blocked by geometry");

            return ExecuteOnServer(request, player);
        }
        protected virtual bool RequiresVisibleApproach=>true;
        private bool HasVisibleApproach(NetworkObject player)
        {
            Vector3 target=InteractionOrigin;
            if(interactionPoint==null)
                foreach(var collider in GetComponentsInChildren<Collider>())
                    if(collider.enabled&&!collider.isTrigger){target=collider.bounds.center;break;}
            var camera=player.GetComponentInChildren<Camera>(true);
            Vector3 eye=camera?camera.transform.position:player.transform.position+Vector3.up*1.62f;
            Vector3 delta=target-eye;if(delta.sqrMagnitude<.0001f)return true;
            var hits=Physics.RaycastAll(eye,delta.normalized,delta.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits)
            {
                if(hit.collider.GetComponentInParent<NetworkObject>()==player)continue;
                return hit.collider.transform.IsChildOf(transform);
            }
            return true;
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
        private void DeniedRpc(ulong deniedClientId, string reason)
        {
            if (NetworkManager.LocalClientId == deniedClientId)
            {
                LastDenialReason = reason;
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
