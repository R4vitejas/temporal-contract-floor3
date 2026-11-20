using TemporalContract.Combat;
using TemporalContract.Rounds;
using UnityEngine;
namespace TemporalContract.Players
{
    public sealed class PlayerAppearance : MonoBehaviour
    {
        private Renderer body;
        private NetworkFpsPlayer player;
        private MaterialPropertyBlock properties;
        private void Awake(){player=GetComponent<NetworkFpsPlayer>();body=transform.Find("Body")?.GetComponent<Renderer>();properties=new MaterialPropertyBlock();}
        private void LateUpdate()
        {
            if(!body||!player||!player.IsSpawned)return;body.enabled=!player.IsOwner;
            var round=RoundCoordinator.Instance;var health=GetComponent<NetworkPlayerHealth>();
            Color color=round&&round.Phase!=MatchPhase.WaitingForStart?(round.AttackerClientId==player.OwnerClientId?new Color(.7f,.27f,.18f):new Color(.14f,.38f,.53f)):new Color(.32f,.4f,.45f);
            if(health&&!health.IsAlive)color*=.35f;
            properties.SetColor("_BaseColor",color);properties.SetColor("_Color",color);body.SetPropertyBlock(properties);
        }
    }
}
