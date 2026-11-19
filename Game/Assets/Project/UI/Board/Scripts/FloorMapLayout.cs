using System;
using UnityEngine;

namespace TemporalContract.UI.Board
{
    /// <summary>
    /// Floor artwork plus clickable room regions. Swap the sprite and edit the rectangles (all normalized 0-1,
    /// origin bottom-left of the image) to use the final Floor 3 map; no code changes are needed.
    /// </summary>
    [CreateAssetMenu(menuName = "Temporal Contract/Floor Map Layout", fileName = "FloorMapLayout")]
    public sealed class FloorMapLayout : ScriptableObject
    {
        [Serializable]
        public struct Room
        {
            public string id;
            public string displayName;
            [Tooltip("x, y, width, height as fractions of the artwork (0-1), origin bottom-left.")]
            public Rect area;
        }

        public Sprite artwork;
        public Room[] rooms = Array.Empty<Room>();
        [Tooltip("Draw each room's name inside its region (useful for the placeholder image).")]
        public bool showRegionLabels = true;
    }
}
