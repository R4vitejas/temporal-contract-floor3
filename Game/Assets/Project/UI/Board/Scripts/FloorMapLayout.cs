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
        [Tooltip("Used when no Sprite is set, so a plain texture asset (default importer) can be used as is.")]
        public Texture2D artworkTexture;
        /// <summary>The configured sprite, or one created from <see cref="artworkTexture"/>.</summary>
        public Sprite ResolveArtwork()
        {
            if (artwork != null)
            {
                return artwork;
            }

            if (artworkTexture == null)
            {
                return null;
            }

            if (runtimeSprite == null || runtimeSprite.texture != artworkTexture)
            {
                runtimeSprite = Sprite.Create(artworkTexture, new Rect(0f, 0f, artworkTexture.width, artworkTexture.height),
                    new Vector2(0.5f, 0.5f));
            }

            return runtimeSprite;
        }

        [NonSerialized] private Sprite runtimeSprite;
        public Room[] rooms = Array.Empty<Room>();
        [Tooltip("Draw each room's name inside its region (useful for the placeholder image).")]
        public bool showRegionLabels = true;
    }
}
