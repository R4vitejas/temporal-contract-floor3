using System;
using System.Collections.Generic;
using TemporalContract.Core;
using Unity.Netcode;
using UnityEngine;

namespace TemporalContract.Interactions
{
    /// <summary>
    /// Chair / small desk that one player at a time can carry and place.
    ///
    /// Authority: the host holds the single-holder lock, moves the object with its holder, and
    /// validates placement (surface, line of sight, no wall/player overlap). Clients only send
    /// "interact" intent. Rotation is limited to 90-degree steps; no throwing.
    ///
    /// Prefab setup: NetworkObject + NetworkTransform (server authority, the default), pivot at the
    /// object's base, a non-trigger collider, and a BoxCollider assigned as <see cref="footprint"/>.
    /// While carried it stays in front of the holder's camera, so pressing E again places it.
    /// </summary>
    public sealed class MovableCover : NetworkInteractable
    {
        private const ulong NoHolder = ulong.MaxValue;

        [Header("Carry")]
        [SerializeField, Min(0.5f)] private float carryDistance = 1.4f;
        [SerializeField] private float carryHeight = 0.35f;
        [SerializeField] private float eyeHeight = 1.5f;

        [Header("Placement")]
        [SerializeField] private BoxCollider footprint;
        [SerializeField] private LayerMask surfaceLayers = ~0;
        [SerializeField] private LayerMask blockerLayers = ~0;
        [SerializeField, Range(0.5f, 1f)] private float minSurfaceNormalY = 0.7f;

        private readonly NetworkVariable<ulong> holderClientId = new NetworkVariable<ulong>(NoHolder);

        private Collider[] ownColliders;
        private Vector3 baselinePosition;
        private Quaternion baselineRotation;

        public bool IsHeld => holderClientId.Value != NoHolder;

        private void Awake()
        {
            ownColliders = GetComponentsInChildren<Collider>();
            if (GetComponent<Rigidbody>() == null)
            {
                gameObject.AddComponent<Rigidbody>().isKinematic = true;
            }
        }

        public override void OnNetworkSpawn()
        {
            baselinePosition = transform.position;
            baselineRotation = transform.rotation;

            if (IsServer)
            {
                holderClientId.Value = NoHolder;
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            }

            holderClientId.OnValueChanged += OnHolderChanged;
            if (IsHeld)
            {
                SetHolderCollisionIgnored(holderClientId.Value, true);
            }
        }

        public override void OnNetworkDespawn()
        {
            holderClientId.OnValueChanged -= OnHolderChanged;
            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        // Host moves the object; NetworkTransform replicates it to clients.
        private void Update()
        {
            if (!IsServer || !IsSpawned || !IsHeld)
            {
                return;
            }

            NetworkObject holder = FindPlayer(holderClientId.Value);
            if (holder == null)
            {
                return;
            }

            transform.SetPositionAndRotation(CarryAnchor(holder.transform), SnappedYaw(holder.transform));
        }

        private Vector3 CarryAnchor(Transform playerTransform) =>
            playerTransform.position + playerTransform.forward * carryDistance + Vector3.up * carryHeight;

        private static Quaternion SnappedYaw(Transform playerTransform) =>
            Quaternion.Euler(0f, Mathf.Round(playerTransform.eulerAngles.y / 90f) * 90f, 0f);

        // ---- Interaction ----

        protected override bool TryBuildPrompt(IInteractionActor actor, out InteractionPrompt prompt)
        {
            if (!IsHeld)
            {
                prompt = new InteractionPrompt("Pick up", "E", true);
                return true;
            }

            if (actor != null && holderClientId.Value == actor.OwnerClientId)
            {
                prompt = new InteractionPrompt("Place", "E", true);
                return true;
            }

            prompt = default;
            return false;
        }

        protected override bool ExecuteOnServer(in InteractionRequest request, NetworkObject player)
        {
            if (!IsHeld)
            {
                // One cover per player, and one holder per cover (host handles requests serially).
                if (IsHoldingAnyCover(request.ActorClientId))
                {
                    return Deny("already holding another cover");
                }

                holderClientId.Value = request.ActorClientId;
                BroadcastFeedback(FeedbackKind.CoverPickedUp);
                return true;
            }

            if (holderClientId.Value != request.ActorClientId)
            {
                return Deny($"held by client {holderClientId.Value}");
            }

            if (!TryComputePlacement(player.transform, out Vector3 position, out Quaternion rotation))
            {
                return Deny("invalid placement (no surface, blocked line, or overlap)");
            }

            transform.SetPositionAndRotation(position, rotation);
            holderClientId.Value = NoHolder;
            BroadcastFeedback(FeedbackKind.CoverPlaced);
            return true;
        }

        private static bool IsHoldingAnyCover(ulong clientId)
        {
            foreach (MovableCover cover in FindObjectsByType<MovableCover>(FindObjectsSortMode.None))
            {
                if (cover.IsSpawned && cover.holderClientId.Value == clientId)
                {
                    return true;
                }
            }

            return false;
        }

        // ---- Placement validation (host) ----

        private bool TryComputePlacement(Transform playerTransform, out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = SnappedYaw(playerTransform);
            Vector3 anchor = CarryAnchor(playerTransform);

            if (!TryFindSurface(anchor, out RaycastHit surface) || surface.normal.y < minSurfaceNormalY)
            {
                return false;
            }

            position = surface.point;

            // The player must be able to reach the spot: no wall between eyes and target.
            Vector3 eye = playerTransform.position + Vector3.up * eyeHeight;
            if (IsSegmentBlocked(eye, position + Vector3.up * 0.3f))
            {
                return false;
            }

            return !OverlapsBlockers(position, rotation);
        }

        private bool TryFindSurface(Vector3 anchor, out RaycastHit result)
        {
            RaycastHit[] hits = Physics.RaycastAll(anchor + Vector3.up, Vector3.down, 3f, surfaceLayers,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (!IsSelf(hit.collider) && !IsPlayerCollider(hit.collider))
                {
                    result = hit;
                    return true;
                }
            }

            result = default;
            return false;
        }

        private bool IsSegmentBlocked(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            RaycastHit[] hits = Physics.RaycastAll(from, delta.normalized, delta.magnitude, blockerLayers,
                QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                if (!IsSelf(hit.collider) && !IsPlayerCollider(hit.collider))
                {
                    return true;
                }
            }

            return false;
        }

        private bool OverlapsBlockers(Vector3 basePosition, Quaternion rotation)
        {
            if (footprint == null)
            {
                return false;
            }

            // Footprint offset relative to the cover's pivot, re-applied at the candidate pose.
            Vector3 scale = footprint.transform.lossyScale;
            Vector3 localCenter = transform.InverseTransformPoint(footprint.transform.TransformPoint(footprint.center));
            Vector3 center = basePosition + rotation * Vector3.Scale(localCenter, transform.lossyScale)
                + Vector3.up * 0.03f;
            Vector3 half = Vector3.Scale(footprint.size * 0.5f, scale) - new Vector3(0.02f, 0.02f, 0.02f);
            half = Vector3.Max(half, new Vector3(0.01f, 0.01f, 0.01f));

            Collider[] hits = Physics.OverlapBox(center, half, rotation, blockerLayers,
                QueryTriggerInteraction.Ignore);
            foreach (Collider hit in hits)
            {
                if (!IsSelf(hit))
                {
                    return true; // walls, furniture, or a player standing there
                }
            }

            return false;
        }

        private bool IsSelf(Collider c) => c.transform.IsChildOf(transform);

        private static bool IsPlayerCollider(Collider c)
        {
            NetworkObject owner = c.GetComponentInParent<NetworkObject>();
            return owner != null && owner.IsPlayerObject;
        }

        // ---- Release, reset ----

        /// <summary>Host-only. Call when the holder dies; also invoked on disconnect.</summary>
        public void ReleaseIfHeldBy(ulong clientId)
        {
            if (!IsServer || !IsSpawned || holderClientId.Value != clientId)
            {
                return;
            }

            // Same rules as a manual placement (valid surface, no overlap, and the holder could reach
            // the spot without a wall in between), tried at: the carry point, then a ring around the
            // holder's feet. Disconnect has no holder, so only surface and overlap apply there.
            Quaternion rotation = transform.rotation;
            NetworkObject holder = FindPlayer(clientId);
            var candidates = new List<Vector3> { transform.position };
            if (holder != null)
            {
                Vector3 feet = holder.transform.position;
                candidates.Add(feet);
                candidates.Add(feet + holder.transform.forward * 0.9f);
                candidates.Add(feet - holder.transform.forward * 0.9f);
                candidates.Add(feet + holder.transform.right * 0.9f);
                candidates.Add(feet - holder.transform.right * 0.9f);
            }

            Vector3 eye = holder != null ? holder.transform.position + Vector3.up * eyeHeight : default;
            Vector3 target = default;
            bool placed = false;
            foreach (Vector3 candidate in candidates)
            {
                if (TryValidDropPoint(candidate, rotation, holder != null, eye, out target))
                {
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                // Fall back to the baseline, but never onto an occupied spot: search a ring around it.
                placed = TryFindFreeSpotNear(baselinePosition, baselineRotation, out target);
                rotation = baselineRotation;
                if (!placed)
                {
                    target = baselinePosition;
                    Debug.LogWarning($"[{name}] no free drop spot found; restoring exact baseline.", this);
                }
            }

            transform.SetPositionAndRotation(target, rotation);
            holderClientId.Value = NoHolder;
        }

        private bool TryValidDropPoint(Vector3 around, Quaternion rotation, bool checkReach, Vector3 eye,
            out Vector3 point)
        {
            point = default;
            if (!TryFindSurface(around + Vector3.up * 0.5f, out RaycastHit surface)
                || surface.normal.y < minSurfaceNormalY
                || OverlapsBlockers(surface.point, rotation))
            {
                return false;
            }

            if (checkReach && IsSegmentBlocked(eye, surface.point + Vector3.up * 0.3f))
            {
                return false;
            }

            point = surface.point;
            return true;
        }

        private bool TryFindFreeSpotNear(Vector3 center, Quaternion rotation, out Vector3 point)
        {
            if (TryValidDropPoint(center, rotation, false, default, out point))
            {
                return true;
            }

            foreach (float radius in new[] { 0.6f, 1.2f, 1.8f })
            {
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI / 4f;
                    Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    if (TryValidDropPoint(center + offset, rotation, false, default, out point))
                    {
                        return true;
                    }
                }
            }

            point = default;
            return false;
        }

        private void OnClientDisconnected(ulong clientId) => ReleaseIfHeldBy(clientId);

        protected override void ResetState()
        {
            holderClientId.Value = NoHolder;
            transform.SetPositionAndRotation(baselinePosition, baselineRotation);
        }

        // ---- Holder collision (all peers): the carried object must not shove its holder ----

        private void OnHolderChanged(ulong previous, ulong current)
        {
            if (previous != NoHolder)
            {
                SetHolderCollisionIgnored(previous, false);
            }

            if (current != NoHolder)
            {
                SetHolderCollisionIgnored(current, true);
            }
        }

        private void SetHolderCollisionIgnored(ulong clientId, bool ignored)
        {
            NetworkObject player = FindPlayer(clientId);
            if (player == null)
            {
                return;
            }

            var playerColliders = new List<Collider>();
            player.GetComponentsInChildren(playerColliders);
            foreach (Collider mine in ownColliders)
            {
                foreach (Collider theirs in playerColliders)
                {
                    Physics.IgnoreCollision(mine, theirs, ignored);
                }
            }
        }

        private NetworkObject FindPlayer(ulong clientId)
        {
            foreach (NetworkObject spawned in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (spawned.IsPlayerObject && spawned.OwnerClientId == clientId)
                {
                    return spawned;
                }
            }

            return null;
        }
    }
}
