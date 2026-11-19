using System;
using System.Collections.Generic;
using System.Reflection;
using TemporalContract.Core;
using UnityEngine;

namespace TemporalContract.UI.Board
{
    /// <summary>
    /// A modal UI (board, menu) acquires gameplay-input capture with its own owner object and releases the same
    /// owner later, so two overlapping modals never release each other's lock.
    /// </summary>
    public interface IModalInputCapture : IGameplayInputCapture
    {
        void SetCaptured(UnityEngine.Object owner, bool captured);
    }

    /// <summary>
    /// Reference-counted capture used by standalone test scenes and as a mock actor binding. Implements
    /// <see cref="IGameplayInputCapture"/> so it can be given directly to the InteractionRouter.
    /// </summary>
    public sealed class LocalInputCapture : MonoBehaviour, IModalInputCapture
    {
        private readonly HashSet<UnityEngine.Object> owners = new HashSet<UnityEngine.Object>();

        public event Action<bool> CaptureChanged;

        public bool IsGameplayInputCaptured
        {
            get
            {
                owners.RemoveWhere(o => o == null); // a destroyed modal never holds the lock
                return owners.Count > 0;
            }
        }

        public void SetCaptured(UnityEngine.Object owner, bool captured)
        {
            if (owner == null)
            {
                return;
            }

            bool before = IsGameplayInputCaptured;
            if (captured)
            {
                owners.Add(owner);
            }
            else
            {
                owners.Remove(owner);
            }

            bool after = IsGameplayInputCaptured;
            if (before != after)
            {
                CaptureChanged?.Invoke(after);
            }
        }
    }
}
