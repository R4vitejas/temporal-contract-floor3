using UnityEngine;

namespace TemporalContract.Core
{
    /// <summary>Scene-owned spawn and body configuration shared by session, player and rounds.</summary>
    public sealed class GameplaySceneLayout : MonoBehaviour
    {
        public Transform attackerSpawn, defenderSpawn, vipSpawn;
        public float playerHeight = 1.8f, playerRadius = .3f, skinWidth = .03f;
        public static GameplaySceneLayout Active { get; private set; }
        private void OnEnable() => Active = this;
        private void OnDisable() { if (Active == this) Active = null; }
        public static Pose PlayerPose(bool attacker)
        {
            var marker = Active ? (attacker ? Active.attackerSpawn : Active.defenderSpawn) : null;
            return marker ? new Pose(marker.position, marker.rotation)
                : new Pose(new Vector3(attacker ? -2.5f : 2.5f, 0, 0), Quaternion.Euler(0, attacker ? 0 : 180, 0));
        }
        public static Pose VipPose => Active && Active.vipSpawn
            ? new Pose(Active.vipSpawn.position, Active.vipSpawn.rotation) : new Pose(new Vector3(0,0,4), Quaternion.identity);
        public void Configure(CharacterController body)
        {
            body.height=playerHeight;body.radius=playerRadius;body.center=Vector3.up*(playerHeight*.5f);
            body.skinWidth=skinWidth;body.stepOffset=.05f;body.minMoveDistance=0;body.enableOverlapRecovery=true;
        }
    }
}
