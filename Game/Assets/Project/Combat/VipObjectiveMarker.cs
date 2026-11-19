using UnityEngine;
namespace TemporalContract.Combat
{
    public sealed class VipObjectiveMarker : MonoBehaviour
    {
        private TextMesh label;
        private VipHealth health;
        private void Awake()
        {
            health=GetComponent<VipHealth>();var go=new GameObject("VIP identifier");go.transform.SetParent(transform,false);go.transform.localPosition=Vector3.up*2.12f;
            label=go.AddComponent<TextMesh>();label.text="VIP";label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.fontSize=40;label.characterSize=.15f;label.color=new Color(1,.76f,.32f);
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.GetComponent<Renderer>().sharedMaterial=label.font.material;
            var body=transform.Find("VIP Body")?.GetComponent<Renderer>();
            if(body){var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",new Color(.72f,.57f,.27f));block.SetColor("_Color",new Color(.72f,.57f,.27f));body.SetPropertyBlock(block);}
        }
        private void LateUpdate()
        {
            var camera=Camera.main;bool visible=camera&&health&&health.IsAlive&&Vector3.Distance(camera.transform.position,transform.position)<22;
            if(visible)
            {
                var delta=transform.position+Vector3.up*1.3f-camera.transform.position;
                if(Physics.Raycast(camera.transform.position,delta.normalized,out var hit,delta.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))visible=hit.collider.GetComponentInParent<VipHealth>()==health;
                label.transform.rotation=camera.transform.rotation;
            }
            label.GetComponent<Renderer>().enabled=visible;
        }
    }
}
