using UnityEngine;
using UnityEngine.InputSystem;
using TemporalContract.Combat;

namespace TemporalContract.Players
{
    /// <summary>Small first-person weapon visual under the owner-only camera; no gameplay colliders.</summary>
    public sealed class LocalWeaponView : MonoBehaviour
    {
        public Material bodyMaterial,flashMaterial,accentMaterial;
        public Transform RecoilPivot {get;private set;}
        public GameObject MuzzleFlash {get;private set;}
        private Transform motion;
        private NetworkHitscanWeapon weapon;
        private NetworkFpsPlayer player;
        private Vector3 previous;
        private void OnEnable()
        {
            if(RecoilPivot)return;
            weapon=GetComponentInParent<NetworkHitscanWeapon>();player=GetComponentInParent<NetworkFpsPlayer>();previous=player?player.Position:Vector3.zero;
            motion=new GameObject("Weapon motion").transform;motion.SetParent(transform,false);motion.localPosition=new Vector3(.2f,-.2f,.48f);
            RecoilPivot=new GameObject("Weapon view").transform;
            RecoilPivot.SetParent(motion,false);
            var dark=accentMaterial?accentMaterial:bodyMaterial;
            Piece("Slide",new Vector3(0,.025f,.07f),new Vector3(.062f,.047f,.205f),bodyMaterial);
            Piece("Frame",new Vector3(0,-.006f,.04f),new Vector3(.066f,.03f,.17f),dark);
            var grip=Piece("Grip",new Vector3(0,-.068f,-.004f),new Vector3(.052f,.115f,.064f),dark);grip.transform.localRotation=Quaternion.Euler(-13,0,0);
            Piece("Magazine base",new Vector3(0,-.124f,-.017f),new Vector3(.059f,.014f,.071f),bodyMaterial);
            Piece("Trigger guard front",new Vector3(0,-.041f,.091f),new Vector3(.034f,.052f,.012f),dark);
            Piece("Trigger guard base",new Vector3(0,-.065f,.058f),new Vector3(.034f,.01f,.075f),dark);
            Piece("Trigger",new Vector3(0,-.03f,.044f),new Vector3(.009f,.035f,.012f),bodyMaterial);
            var barrel=Piece("Barrel",new Vector3(0,.025f,.19f),new Vector3(.031f,.047f,.031f),bodyMaterial,PrimitiveType.Cylinder);barrel.transform.localRotation=Quaternion.Euler(90,0,0);
            var bore=Piece("Muzzle bore",new Vector3(0,.025f,.238f),new Vector3(.021f,.001f,.021f),dark,PrimitiveType.Cylinder);bore.transform.localRotation=Quaternion.Euler(90,0,0);
            Piece("Front sight",new Vector3(0,.054f,.145f),new Vector3(.008f,.013f,.018f),dark);
            Piece("Rear sight left",new Vector3(-.018f,.054f,-.018f),new Vector3(.01f,.012f,.018f),dark);
            Piece("Rear sight right",new Vector3(.018f,.054f,-.018f),new Vector3(.01f,.012f,.018f),dark);
            MuzzleFlash=new GameObject("Muzzle flash");MuzzleFlash.transform.SetParent(RecoilPivot,false);MuzzleFlash.transform.localPosition=new Vector3(0,.025f,.254f);
            var flash=Piece("Flash star",Vector3.zero,new Vector3(.055f,.055f,1),flashMaterial,PrimitiveType.Quad);flash.transform.SetParent(MuzzleFlash.transform,false);
            flash=Piece("Flash rays",Vector3.zero,new Vector3(.075f,.022f,1),flashMaterial,PrimitiveType.Quad);flash.transform.SetParent(MuzzleFlash.transform,false);flash.transform.localRotation=Quaternion.Euler(0,0,45);
            MuzzleFlash.SetActive(false);
        }
        private void LateUpdate()
        {
            if(!motion||!player)return;
            float travel=Vector2.Distance(new Vector2(player.Position.x,player.Position.z),new Vector2(previous.x,previous.z));previous=player.Position;
            float movement=travel<2?Mathf.Clamp01(travel/Mathf.Max(.001f,Time.unscaledDeltaTime)/4.5f):0;
            var delta=Mouse.current!=null&&!player.IsGameplayInputCaptured?Mouse.current.delta.ReadValue():Vector2.zero;
            float reload=weapon&&weapon.IsReloading?Mathf.Sin(Mathf.Clamp01(1-(float)weapon.ReloadSecondsRemaining/weapon.ReloadDuration)*Mathf.PI):0;
            var target=new Vector3(.2f-Mathf.Clamp(delta.x*.00003f,-.007f,.007f),-.2f+Mathf.Sin(Time.unscaledTime*10)*.004f*movement-reload*.09f,.48f);
            float blend=1-Mathf.Exp(-14*Time.unscaledDeltaTime);motion.localPosition=Vector3.Lerp(motion.localPosition,target,blend);
            motion.localRotation=Quaternion.Slerp(motion.localRotation,Quaternion.Euler(reload*32,reload*-16,reload*-25),blend);
        }
        private GameObject Piece(string label,Vector3 position,Vector3 size,Material material,PrimitiveType primitive=PrimitiveType.Cube)
        {
            var go=GameObject.CreatePrimitive(primitive);go.name=label;go.layer=2;
            go.transform.SetParent(RecoilPivot,false);go.transform.localPosition=position;go.transform.localScale=size;
            var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            if(material)go.GetComponent<Renderer>().sharedMaterial=material;
            go.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }
    }
}
