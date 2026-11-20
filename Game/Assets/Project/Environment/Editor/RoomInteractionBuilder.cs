using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TemporalContract.Interactions;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace TemporalContract.Environment.EditorTools
{
    /// <summary>Additive room detail; original floor assets and doorway collision stay intact.</summary>
    public static class RoomInteractionBuilder
    {
        private static Transform root;
        private static Material metal,wood,white,glow,ink;
        private static Font font;
        private sealed class Room
        {
            public string name;public Vector3 center,control;public Vector2 size;
            public Room(string name,float x,float z,float sx,float sz,float cx,float cz){this.name=name;center=new Vector3(x,0,z);size=new Vector2(sx,sz);control=new Vector3(cx,1.35f,cz);}
        }
        private static readonly Room[] Rooms={
            new Room("CLASSROOM 01",-35.94f,-18.51f,8.7f,9.3f,-39.97f,-14.9f),
            new Room("CLASSROOM 02",-.02f,-18.51f,8.7f,9.3f,-3.99f,-14.9f),
            new Room("CLASSROOM 03",8.64f,-18.51f,8.6f,9.3f,12.65f,-14.8f),
            new Room("CLASSROOM 04",21.91f,2.34f,8.6f,9.3f,17.95f,.1f),
            new Room("CLASSROOM 05",8.59f,5.88f,8.6f,9.3f,12.65f,1.8f),
            new Room("CLASSROOM 06",-.02f,5.88f,8.7f,9.3f,-3.99f,1.8f),
            new Room("STAFF CABIN",-50.17f,-14.36f,6.1f,5.6f,-52.82f,-16.7f)
        };
        [MenuItem("Temporal Contract/Floor/Add Detailed Room Interactions")]
        public static void ApplyCurrent()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Apply room content in Edit Mode.");
            Apply(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            var validate=typeof(NetworkObject).GetMethod("OnValidate",BindingFlags.Instance|BindingFlags.NonPublic);
            foreach(var item in root.GetComponentsInChildren<NetworkObject>(true))validate.Invoke(item,null);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        public static void Apply(Scene scene)
        {
            var binding=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<FullFloorSceneBindings>(true)).Single();
            var previous=binding.GetComponentInChildren<RoomInteractionContent>(true);
            if(previous){previous.RestoreSourceWindow();Object.DestroyImmediate(previous.gameObject);}
            root=new GameObject("Detailed room interactions").transform;root.SetParent(binding.transform,false);
            var content=root.gameObject.AddComponent<RoomInteractionContent>();
            metal=Material("Room_Graphite",new Color(.12f,.16f,.19f));wood=Material("Room_ChairWood",new Color(.53f,.3f,.12f));
            white=Material("Room_Ceiling",new Color(.57f,.61f,.61f));glow=Material("Room_Lamp",new Color(.85f,.94f,1),true);
            ink=Material("Room_Control",new Color(.025f,.045f,.06f));font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildChair(binding.movableCover);
            foreach(var spec in new[]{new Vector3(-38.8f,.08f,-15.8f),new Vector3(-2.9f,.08f,3.3f)})
            {
                var chair=Object.Instantiate(binding.movableCover.gameObject,root).GetComponent<MovableCover>();chair.name="Movable wooden chair";chair.transform.position=spec;
            }
            // A roof and moderate ambient fill let local lights visibly change a room without total darkness.
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.22f,.25f,.29f);RenderSettings.ambientIntensity=1;
            var sun=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).FirstOrDefault(l=>l.type==LightType.Directional);
            if(sun){sun.intensity=.65f;sun.shadows=LightShadows.Soft;sun.shadowStrength=.9f;}
            var legacyFill=binding.transform.Find("Classroom 02 switched fill");if(legacyFill)Object.DestroyImmediate(legacyFill.gameObject);
            for(int i=0;i<Rooms.Length;i++)BuildRoom(Rooms[i],i==1?binding.lightSwitch:null);
            BuildWindow(content);
            binding.windowPower=root.GetComponentInChildren<WindowPowerConsole>();binding.vipWindow=root.GetComponentInChildren<SlidingWindow>();
            binding.GetComponent<TemporalContract.UI.Hud.ProductionPresentationAdapter>().windowPower=binding.windowPower;
            Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();
            Debug.Log("ROOM_INTERACTIONS_PREPARED: three detailed chairs, seven room light controls, six fan controls and power-gated VIP sliding glass.");
        }
        private static void BuildChair(MovableCover chair)
        {
            var old=chair.transform.Find("Floor3Chair");if(old)Object.DestroyImmediate(old.gameObject);
            old=chair.transform.Find("Detailed chair");if(old)Object.DestroyImmediate(old.gameObject);
            var visual=new GameObject("Detailed chair").transform;visual.SetParent(chair.transform,false);
            Part("Wooden seat",visual,new Vector3(0,.44f,.02f),new Vector3(.42f,.065f,.45f),wood);
            Part("Back rest",visual,new Vector3(0,.77f,-.185f),new Vector3(.42f,.22f,.055f),wood);
            foreach(float x in new[]{-.17f,.17f})foreach(float z in new[]{-.17f,.2f})
                Part("Steel leg",visual,new Vector3(x,.21f,z),new Vector3(.033f,.42f,.033f),metal);
            foreach(float x in new[]{-.17f,.17f})
            {
                Part("Back upright",visual,new Vector3(x,.63f,-.185f),new Vector3(.028f,.48f,.028f),metal);
                Part("Side brace",visual,new Vector3(x,.24f,.015f),new Vector3(.024f,.024f,.39f),metal);
                Part("Rubber foot",visual,new Vector3(x,.025f,.2f),new Vector3(.04f,.05f,.04f),ink);
            }
            Part("Front cross brace",visual,new Vector3(0,.2f,.2f),new Vector3(.36f,.025f,.025f),metal);
        }
        private static void BuildRoom(Room room,WallSwitch existing)
        {
            Vector3 face=room.control.x>room.center.x?Vector3.left:Vector3.right;
            var group=new GameObject(room.name).transform;group.SetParent(root,false);
            Part("Ceiling",group,room.center+Vector3.up*3.12f,new Vector3(room.size.x,.12f,room.size.y),white);
            var lights=new List<Light>();var emitters=new List<Renderer>();
            foreach(float side in new[]{-1f,1f})
            {
                Vector3 pos=room.center+new Vector3(room.size.x*.24f*side,2.94f,0);
                Part("Lamp housing",group,pos,new Vector3(1.4f,.1f,.38f),metal);
                var strip=Part("Switchable light diffuser",group,pos-Vector3.up*.06f,new Vector3(1.3f,.025f,.3f),glow);emitters.Add(strip.GetComponent<Renderer>());
                var lamp=new GameObject("Room light").AddComponent<Light>();lamp.transform.SetParent(group,false);lamp.transform.position=pos-Vector3.up*.25f;
                lamp.type=LightType.Point;lamp.intensity=3;lamp.range=8;lamp.color=new Color(1,.9f,.77f);lamp.shadows=LightShadows.None;lights.Add(lamp);
            }
            WallSwitch control=existing;
            if(!control)control=Control<WallSwitch>(room.name+" light switch",group,room.control,face);
            else
            {
                var oldPlate=control.transform.Find("Control plate");if(oldPlate)Object.DestroyImmediate(oldPlate.gameObject);
                foreach(var renderer in control.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
                control.transform.position=room.control;
                BuildControlVisual(control.transform,face);
            }
            Assign(control,"lights",lights.ToArray());Assign(control,"fixtureRenderers",emitters.ToArray());SetFloat(control,"dimIntensityScale",.035f);
            Label("LIGHTS",group,room.control+face*.09f+Vector3.up*.28f,face,.055f);
            Label(room.name,group,room.control+face*.09f+Vector3.up*.43f,face,.055f);
            var renderers=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            var rotors=renderers.Where(r=>r.name.StartsWith("Source_Blades")&&Mathf.Abs(r.bounds.center.x-room.center.x)<room.size.x*.5f&&Mathf.Abs(r.bounds.center.z-room.center.z)<room.size.y*.5f).Select(r=>r.transform).ToArray();
            if(rotors.Length==0)
            {
                var shaft=Part("Fan downrod",group,room.center+Vector3.up*2.87f,new Vector3(.045f,.3f,.045f),metal);
                var rotor=Part("Ceiling fan rotor",group,room.center+Vector3.up*2.67f,new Vector3(1.3f,.035f,.12f),wood);
                Part("Cross blades",rotor.transform,Vector3.zero,new Vector3(.0923f,1,10.833f),wood);
                Part("Motor hub",group,room.center+Vector3.up*2.73f,new Vector3(.18f,.14f,.18f),metal);
                rotors=new[]{rotor.transform};
            }
            if(rotors.Length>0)
            {
                var fan=Control<NetworkFanControl>(room.name+" fan control",group,room.control+new Vector3(0,0,.4f),face);fan.rotors=rotors;
                Label("FANS",group,fan.transform.position+face*.09f+Vector3.up*.28f,face,.055f);
            }
        }
        private static T Control<T>(string name,Transform parent,Vector3 position,Vector3 face) where T:NetworkInteractable
        {
            var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.transform.position=position;
            obj.AddComponent<NetworkObject>();var target=obj.AddComponent<T>();BuildControlVisual(obj.transform,face);return target;
        }
        private static void BuildControlVisual(Transform target,Vector3 face)
        {
            var visual=new GameObject("Control plate").transform;visual.SetParent(target,false);visual.rotation=Quaternion.LookRotation(face);
            var plate=Part("Switchboard",visual,Vector3.zero,new Vector3(.28f,.3f,.065f),white,true);
            Part("Rocker",visual,new Vector3(0,0,.045f),new Vector3(.085f,.14f,.04f),metal);
            Part("Status mark",visual,new Vector3(0,.11f,.04f),new Vector3(.07f,.012f,.01f),glow);
            Assign(target.GetComponent<NetworkInteractable>(),"interactionPoint",plate.transform);
        }
        private static void BuildWindow(RoomInteractionContent content)
        {
            var power=Control<WindowPowerConsole>("VIP window power console",root,new Vector3(-26.8f,1.3f,-9.05f),Vector3.forward);
            Part("Power cabinet",power.transform,new Vector3(0,-.3f,-.06f),new Vector3(.55f,1.15f,.25f),metal,true);
            power.indicator=Part("Power indicator",power.transform,new Vector3(0,.17f,.09f),new Vector3(.22f,.05f,.025f),glow).GetComponent<Renderer>();
            Label("VIP WINDOW MOTORS",power.transform,new Vector3(-26.8f,1.99f,-8.88f),Vector3.forward,.07f);
            Label("E  ENABLE POWER\nSTAFF CABIN / WEST WING",power.transform,new Vector3(-26.8f,1.78f,-8.88f),Vector3.forward,.045f);
            var glass=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Source_sweethome3d_opening_on_rail_1_glass.010"||r.name=="Source_sweethome3d_opening_on_rail_2_frame.010").ToArray();
            if(glass.Length!=2)throw new InvalidOperationException("VIP window source panes missing.");
            var bounds=glass[0].bounds;bounds.Encapsulate(glass[1].bounds);
            var disabled=new List<Collider>();
            var cut=new Bounds(bounds.center,new Vector3(.5f,bounds.size.y,bounds.size.z));
            foreach(var renderer in glass)
            {
                renderer.enabled=false;foreach(var collider in renderer.GetComponents<Collider>())if(collider.enabled){collider.enabled=false;disabled.Add(collider);}
            }
            // Split only baked rectangles covering this exact window; keep their remaining wall area solid.
            foreach(var box in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Where(c=>c.name.StartsWith("Solid wall ")&&c.enabled).ToArray())
            {
                var b=box.bounds;if(!b.Intersects(cut))continue;box.enabled=false;disabled.Add(box);
                float z0=Mathf.Max(b.min.z,cut.min.z),z1=Mathf.Min(b.max.z,cut.max.z);
                CollisionRemainder(new Vector3(b.min.x,b.min.y,b.min.z),new Vector3(b.max.x,b.max.y,z0));
                CollisionRemainder(new Vector3(b.min.x,b.min.y,z1),b.max);
                CollisionRemainder(new Vector3(b.min.x,b.min.y,z0),new Vector3(b.max.x,Mathf.Max(b.min.y,cut.min.y),z1));
                CollisionRemainder(new Vector3(b.min.x,Mathf.Min(b.max.y,cut.max.y),z0),new Vector3(b.max.x,b.max.y,z1));
            }
            content.replacedGlass=glass;content.replacedCollision=disabled.ToArray();
            foreach(var item in content.replacedGlass.Cast<Object>().Concat(content.replacedCollision))
                if(PrefabUtility.GetCorrespondingObjectFromSource(item)!=null)PrefabUtility.RecordPrefabInstancePropertyModifications(item);
            var window=Control<SlidingWindow>("Powered VIP sliding window",root,bounds.center,Vector3.right);window.power=power;
            // Remove the generic control plate; handle sits at the inner jamb and stays reachable when open.
            Object.DestroyImmediate(window.transform.Find("Control plate").gameObject);
            float width=(bounds.size.z-.06f)*.5f;
            var glassMaterial=glass[0].sharedMaterial;
            var fixedPane=Part("Fixed glass pane",window.transform,new Vector3(0,0,-width*.5f),new Vector3(.035f,bounds.size.y,width),glassMaterial,true);
            var sliding=Part("Sliding glass pane",window.transform,new Vector3(.045f,0,width*.5f),new Vector3(.035f,bounds.size.y,width),glassMaterial,true);
            window.slidingPane=sliding.transform;window.travel=new Vector3(0,0,-width);
            foreach(var pane in new[]{fixedPane,sliding})
            {
                Part("Window sash top",pane.transform,new Vector3(0,.49f,0),new Vector3(1.2f,.025f,1),metal);
                Part("Window sash bottom",pane.transform,new Vector3(0,-.49f,0),new Vector3(1.2f,.025f,1),metal);
            }
            var handle=Part("Window latch",window.transform,new Vector3(.14f,.05f,bounds.extents.z+.08f),new Vector3(.1f,.2f,.06f),metal,true);Assign(window,"interactionPoint",handle.transform);
            Label("VIP WINDOW\nPOWER: CORRIDOR CONSOLE",root,handle.transform.position+new Vector3(.03f,.3f,0),Vector3.right,.05f);
        }
        private static void CollisionRemainder(Vector3 min,Vector3 max)
        {
            Vector3 size=max-min;if(size.x<.001f||size.y<.001f||size.z<.001f)return;
            var obj=new GameObject("Window wall remainder");obj.transform.SetParent(root,false);obj.transform.position=(min+max)*.5f;obj.AddComponent<BoxCollider>().size=size;
        }
        private static GameObject Part(string name,Transform parent,Vector3 localPosition,Vector3 size,Material material,bool solid=false)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=localPosition;obj.transform.localScale=size;
            obj.GetComponent<Renderer>().sharedMaterial=material;if(!solid)Object.DestroyImmediate(obj.GetComponent<Collider>());return obj;
        }
        private static void Label(string text,Transform parent,Vector3 position,Vector3 face,float size)
        {
            var obj=new GameObject("Label - "+text.Split('\n')[0]);obj.transform.SetParent(parent,false);obj.transform.position=position;obj.transform.rotation=Quaternion.LookRotation(-face);
            var label=obj.AddComponent<TextMesh>();label.text=text;label.font=font;label.fontSize=48;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=new Color(.88f,.9f,.83f);label.GetComponent<Renderer>().sharedMaterial=font.material;
        }
        private static Material Material(string name,Color color,bool emissive=false)
        {
            string path="Assets/Project/Environment/Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.25f);
            if(emissive){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*2);}
            EditorUtility.SetDirty(material);return material;
        }
        private static void Assign(Object target,string field,Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        private static void Assign(Object target,string field,Object[] values){var so=new SerializedObject(target);var list=so.FindProperty(field);list.arraySize=values.Length;for(int i=0;i<values.Length;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
        private static void SetFloat(Object target,string field,float value){var so=new SerializedObject(target);so.FindProperty(field).floatValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    }
}
