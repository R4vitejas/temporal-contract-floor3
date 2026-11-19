using UnityEngine;

namespace TemporalContract.Players
{
    /// <summary>Small first-person weapon visual under the owner-only camera; no gameplay colliders.</summary>
    public sealed class LocalWeaponView : MonoBehaviour
    {
        public Material bodyMaterial,flashMaterial;
        public Transform RecoilPivot {get;private set;}
        public GameObject MuzzleFlash {get;private set;}
        private void OnEnable()
        {
            if(RecoilPivot)return;
            RecoilPivot=new GameObject("Weapon view").transform;
            RecoilPivot.SetParent(transform,false);RecoilPivot.localPosition=new Vector3(.22f,-.22f,.4f);
            Piece("Receiver",new Vector3(0,0,.06f),new Vector3(.075f,.075f,.22f),bodyMaterial);
            Piece("Grip",new Vector3(0,-.075f,0),new Vector3(.06f,.13f,.07f),bodyMaterial);
            Piece("Barrel",new Vector3(0,.005f,.2f),new Vector3(.045f,.045f,.1f),bodyMaterial);
            Piece("Sight",new Vector3(0,.052f,.12f),new Vector3(.018f,.025f,.04f),bodyMaterial);
            MuzzleFlash=Piece("Muzzle flash",new Vector3(0,.005f,.28f),new Vector3(.07f,.07f,.08f),flashMaterial);
            MuzzleFlash.SetActive(false);
        }
        private GameObject Piece(string label,Vector3 position,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=label;go.layer=2;
            go.transform.SetParent(RecoilPivot,false);go.transform.localPosition=position;go.transform.localScale=size;
            var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            if(material)go.GetComponent<Renderer>().sharedMaterial=material;
            go.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }
    }
}
