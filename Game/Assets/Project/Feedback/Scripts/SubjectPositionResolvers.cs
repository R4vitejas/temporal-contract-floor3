using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace TemporalContract.Feedback
{
    /// <summary>Turns a replicated subject id into a world position so sounds play where things happen.</summary>
    public interface ISubjectPositionResolver
    {
        bool TryGetPosition(ulong subjectNetworkObjectId, out Vector3 position);
    }

    /// <summary>Default resolver: looks the id up among spawned NetworkObjects. Missing/despawned ids return false.</summary>
    public sealed class NetworkSubjectResolver : ISubjectPositionResolver
    {
        public bool TryGetPosition(ulong subjectNetworkObjectId, out Vector3 position)
        {
            NetworkManager manager = NetworkManager.Singleton;
            if (manager != null && manager.SpawnManager != null
                && manager.SpawnManager.SpawnedObjects.TryGetValue(subjectNetworkObjectId, out NetworkObject spawned)
                && spawned != null)
            {
                position = spawned.transform.position;
                return true;
            }

            position = default;
            return false;
        }
    }

    /// <summary>Mock resolver for test scenes with no network.</summary>
    public sealed class DictionarySubjectResolver : ISubjectPositionResolver
    {
        private readonly Dictionary<ulong, Transform> subjects = new Dictionary<ulong, Transform>();

        public void Set(ulong id, Transform subject) => subjects[id] = subject;

        public bool TryGetPosition(ulong subjectNetworkObjectId, out Vector3 position)
        {
            if (subjects.TryGetValue(subjectNetworkObjectId, out Transform subject) && subject != null)
            {
                position = subject.position;
                return true;
            }

            position = default;
            return false;
        }
    }
}
