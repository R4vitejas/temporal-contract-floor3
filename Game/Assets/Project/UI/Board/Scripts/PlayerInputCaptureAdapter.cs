using System;
using System.Collections.Generic;
using System.Reflection;
using TemporalContract.Core;
using UnityEngine;

namespace TemporalContract.UI.Board
{
    /// <summary>
    /// Production binding: forwards to the spawned local player's documented capture API
    /// (<c>NetworkFpsPlayer.LocalOwner.SetGameplayInputCaptured(owner, captured)</c> on the network-core branch).
    /// It uses reflection so this folder compiles without that unmerged code. If the player type or a local owner
    /// is missing it does nothing and reports not captured; it never throws. PENDING: verify against the merged
    /// player (see docs/BOARD_MAP.md).
    /// </summary>
    public sealed class PlayerInputCaptureAdapter : MonoBehaviour, IModalInputCapture
    {
        private const string PlayerType = "TemporalContract.Players.NetworkFpsPlayer, Assembly-CSharp";

        private bool warned;

        public bool IsGameplayInputCaptured
        {
            get
            {
                object player = LocalPlayer();
                if (player == null)
                {
                    return false;
                }

                PropertyInfo property = player.GetType().GetProperty("IsGameplayInputCaptured");
                return property != null && (bool)property.GetValue(player);
            }
        }

        public void SetCaptured(UnityEngine.Object owner, bool captured)
        {
            object player = LocalPlayer();
            MethodInfo method = player?.GetType().GetMethod("SetGameplayInputCaptured",
                new[] { typeof(UnityEngine.Object), typeof(bool) });
            if (method == null)
            {
                if (!warned)
                {
                    warned = true;
                    Debug.LogWarning("[Board] No local player capture API found; input capture is not applied.", this);
                }

                return;
            }

            method.Invoke(player, new object[] { owner, captured });
        }

        private static object LocalPlayer()
        {
            Type type = Type.GetType(PlayerType);
            return type?.GetProperty("LocalOwner", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        }
    }
}
