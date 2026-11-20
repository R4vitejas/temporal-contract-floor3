using UnityEngine;
namespace TemporalContract.Environment
{
    /// <summary>Records only source window surfaces replaced by the movable window assembly.</summary>
    public sealed class RoomInteractionContent : MonoBehaviour
    {
        public Renderer[] replacedGlass=System.Array.Empty<Renderer>();
        public Collider[] replacedCollision=System.Array.Empty<Collider>();
        public void RestoreSourceWindow()
        {
            foreach(var renderer in replacedGlass)if(renderer)renderer.enabled=true;
            foreach(var collider in replacedCollision)if(collider)collider.enabled=true;
        }
    }
}
