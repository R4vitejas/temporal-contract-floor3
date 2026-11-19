using UnityEngine;

namespace TemporalContract.Core
{
    /// <summary>Separate horizontal/gravity motion and bound step-up beneath the supplied low lintels.</summary>
    public static class FloorCharacterMotor
    {
        public static void Move(CharacterController body, ref float verticalSpeed, Vector3 horizontalVelocity, float elapsed, float gravity = -20f)
        {
            elapsed=Mathf.Clamp(elapsed,0,.1f);
            int count=Mathf.Max(1,Mathf.CeilToInt(elapsed/.016f));float step=elapsed/count;
            for(int i=0;i<count;i++)
            {
                float allowed=.32f;
                Vector3 head=body.transform.position+body.center+Vector3.up*(body.height*.5f-body.radius);
                foreach(var origin in new[]{head,head+horizontalVelocity*step})
                    foreach(var hit in Physics.SphereCastAll(origin,body.radius,Vector3.up,.42f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                        if(hit.collider!=body && hit.normal.y<-.1f)allowed=Mathf.Min(allowed,Mathf.Max(0,hit.distance-.04f));
                body.stepOffset=allowed;
                bool grounded=body.isGrounded;
                body.Move(new Vector3(horizontalVelocity.x,0,horizontalVelocity.z)*step);
                verticalSpeed=grounded&&verticalSpeed<=0?-2f:Mathf.Max(-30f,verticalSpeed+gravity*step);
                var flags=body.Move(Vector3.up*verticalSpeed*step);
                if((flags&CollisionFlags.Above)!=0 && verticalSpeed>0)verticalSpeed=0;
            }
        }
    }
}
