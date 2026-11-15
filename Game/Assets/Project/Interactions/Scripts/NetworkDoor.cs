using TemporalContract.Core;
using Unity.Netcode;
using UnityEngine;

namespace TemporalContract.Interactions
{
    /// <summary>
    /// Hinged door. Open/locked state is host-owned and replicated; every client animates the
    /// hinge locally from that state, so late joiners see the correct pose.
    ///
    /// Setup: put this on the root with a NetworkObject. Assign a child "hinge" transform that
    /// holds the door mesh and its (non-trigger) collider. Optionally assign a separate trigger
    /// BoxCollider covering the doorway (NOT under the hinge) so closing is refused while a
    /// player stands in it.
    /// </summary>
    public sealed class NetworkDoor : NetworkInteractable
    {
        [Header("Hinge")]
        [SerializeField] private Transform hinge;
        [SerializeField] private Vector3 hingeAxis = Vector3.up;
        [SerializeField] private float openAngle = 95f;
        [SerializeField, Min(1f)] private float swingSpeed = 180f;

        [Header("Lock")]
        [SerializeField] private bool lockedByDefault;
        [SerializeField] private string lockedLabel = "Locked";
        [SerializeField] private string lockedHint = "Keycard required";

        [Header("Close safety")]
        [Tooltip("Trigger volume covering the doorway when closed. Must not be a child of the hinge.")]
        [SerializeField] private BoxCollider doorwayClearance;
        [SerializeField] private LayerMask blockerLayers = ~0;

        private readonly NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false);
        private readonly NetworkVariable<bool> isLocked = new NetworkVariable<bool>(false);

        private Quaternion closedLocalRotation;
        private float currentAngle;

        public bool IsOpen => isOpen.Value;
        public bool IsLocked => isLocked.Value;

        private void Awake()
        {
            if (hinge == null)
            {
                hinge = transform;
            }

            hingeAxis = hingeAxis.sqrMagnitude < 0.0001f ? Vector3.up : hingeAxis.normalized;
            closedLocalRotation = hinge.localRotation;

            // A moving collider needs a kinematic body so players are pushed/blocked correctly.
            if (hinge != transform && hinge.GetComponent<Rigidbody>() == null)
            {
                var body = hinge.gameObject.AddComponent<Rigidbody>();
                body.isKinematic = true;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                isOpen.Value = false;
                isLocked.Value = lockedByDefault;
            }

            currentAngle = isOpen.Value ? openAngle : 0f;
            ApplyAngle();
        }

        private void Update()
        {
            if (!IsSpawned)
            {
                return;
            }

            float target = isOpen.Value ? openAngle : 0f;
            if (!Mathf.Approximately(currentAngle, target))
            {
                currentAngle = Mathf.MoveTowards(currentAngle, target, swingSpeed * Time.deltaTime);
                ApplyAngle();
            }
        }

        private void ApplyAngle()
        {
            hinge.localRotation = closedLocalRotation * Quaternion.AngleAxis(currentAngle, hingeAxis);
        }

        protected override bool TryBuildPrompt(IInteractionActor actor, out InteractionPrompt prompt)
        {
            if (isLocked.Value)
            {
                prompt = new InteractionPrompt(lockedLabel, lockedHint, false);
            }
            else
            {
                prompt = new InteractionPrompt(isOpen.Value ? "Close door" : "Open door", "E", true);
            }

            return true;
        }

        protected override bool ExecuteOnServer(in InteractionRequest request, NetworkObject player)
        {
            if (isLocked.Value)
            {
                return Deny("door locked");
            }

            bool opening = !isOpen.Value;
            if (!opening && IsDoorwayOccupied())
            {
                return Deny("a player is standing in the doorway");
            }

            isOpen.Value = opening;
            BroadcastFeedback(FeedbackKind.DoorMoved, opening ? 1f : 0f);
            return true;
        }

        /// <summary>Host-only. Used by the access panel to unlock for the rest of the round.</summary>
        public void SetLockedServer(bool locked)
        {
            if (IsServer && IsSpawned)
            {
                isLocked.Value = locked;
            }
        }

        protected override void ResetState()
        {
            isOpen.Value = false;
            isLocked.Value = lockedByDefault;
        }

        private bool IsDoorwayOccupied()
        {
            if (doorwayClearance == null)
            {
                return false;
            }

            Transform t = doorwayClearance.transform;
            Vector3 center = t.TransformPoint(doorwayClearance.center);
            Vector3 half = Vector3.Scale(doorwayClearance.size * 0.5f, t.lossyScale);
            Collider[] hits = Physics.OverlapBox(center, half, t.rotation, blockerLayers,
                QueryTriggerInteraction.Ignore);
            foreach (Collider hit in hits)
            {
                NetworkObject owner = hit.GetComponentInParent<NetworkObject>();
                if (owner != null && owner.IsPlayerObject)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
