using System.Linq;
using TemporalContract.Feedback;
using TemporalContract.Players;
using TemporalContract.Combat;
using TemporalContract.UI.Hud;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TemporalContract.Environment.EditorTools
{
    /// <summary>Authors presentation only. Original floor mesh, material and collision assets are untouched.</summary>
    public static class GamePolishBuilder
    {
        static readonly Color Ink=new Color(.035f,.05f,.065f,.9f),Muted=new Color(.62f,.7f,.76f),Gold=new Color(1,.75f,.35f),Blue=new Color(.15f,.39f,.5f);
        static Font font;
        static FullFloorSceneBindings binding;
        static HudSessionUi hud;
        static Transform generated;
        static GameMenuController menu;

        [MenuItem("Temporal Contract/Floor/Apply Game Audio And HUD Polish")]
        public static void ApplyCurrent()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Apply scene presentation in Edit Mode.");
            var scene=SceneManager.GetActiveScene();
            Apply(scene);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("GAME_POLISH_APPLIED: owned menu/player audio, session controls and readable production HUD.");
        }
        public static void Apply(Scene scene)
        {
            ConfigureActorAssets();
            binding=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<FullFloorSceneBindings>(true)).Single();hud=binding.hud;
            font=Get<Text>(hud,"roleText").font;
            var old=binding.transform.Find("Game presentation");if(old)Object.DestroyImmediate(old.gameObject);
            generated=new GameObject("Game presentation").transform;generated.SetParent(binding.transform,false);
            var camera=new GameObject("Menu view").AddComponent<Camera>();camera.transform.SetParent(generated,false);
            camera.transform.position=new Vector3(-13,2.1f,-7);camera.transform.LookAt(new Vector3(3,1.45f,-7));
            camera.fieldOfView=65;camera.nearClipPlane=.05f;camera.farClipPlane=150;camera.depth=-10;
            var director=generated.gameObject.AddComponent<LocalAudioDirector>();director.menuCamera=camera;director.menuListener=camera.gameObject.AddComponent<AudioListener>();

            var canvas=new GameObject("Session controls",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.transform.SetParent(generated,false);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=100;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            menu=canvas.AddComponent<GameMenuController>();menu.session=binding.session;menu.map=binding.map;
            var scrim=Panel("Session backdrop",canvas.transform,Vector2.zero,new Vector2(1280,720),new Color(.01f,.02f,.03f,.88f));
            Stretch(scrim.rectTransform);scrim.raycastTarget=true;menu.panel=scrim.gameObject;
            var card=Panel("Session card",scrim.transform,Vector2.zero,new Vector2(640,600),Ink);
            menu.title=Label("Session title",card.transform,"SESSION",new Vector2(0,246),new Vector2(540,44),32,Color.white,TextAnchor.MiddleCenter,true);
            menu.status=Label("Session status",card.transform,"",new Vector2(0,194),new Vector2(540,46),13,Muted);
            menu.volume=Setting(card.transform,"MASTER VOLUME",124,0,1,out menu.volumeValue);
            menu.sensitivity=Setting(card.transform,"MOUSE SENSITIVITY",36,.03f,.3f,out menu.sensitivityValue);
            menu.fov=Setting(card.transform,"FIELD OF VIEW",-52,55,95,out menu.fovValue);
            Label("Controls",card.transform,"WASD move · Mouse aim · LMB fire · R reload\nE interact · Space jump · Esc / F1 session menu",new Vector2(0,-135),new Vector2(540,50),13,Muted);
            menu.resume=Button("Continue",card.transform,"CONTINUE",new Vector2(-145,-206),new Vector2(260,46),Blue);
            menu.disconnect=Button("Disconnect",card.transform,"DISCONNECT",new Vector2(145,-206),new Vector2(260,46),new Color(.45f,.17f,.15f));
            var quit=Button("Quit",card.transform,"QUIT GAME",new Vector2(0,-264),new Vector2(240,32),new Color(.11f,.14f,.17f));
            var settingsButtons=new System.Collections.Generic.List<Button>();
            var quitButtons=new System.Collections.Generic.List<Button>{quit};

            StyleMenu(Get<GameObject>(hud,"mainMenuPanel").transform,settingsButtons,quitButtons);
            foreach(string field in new[]{"lobbyPanel","roundEndPanel","matchEndPanel","errorPanel"})
            {
                var panel=Get<GameObject>(hud,field).transform;Clean(panel);
                panel.GetComponent<Image>().color=new Color(.025f,.04f,.055f,.78f);
                var option=Button("Polish - Settings",panel,"SETTINGS",new Vector2(-510,-300),new Vector2(180,38),new Color(.08f,.13f,.17f));settingsButtons.Add(option);
            }
            menu.settingsButtons=settingsButtons.ToArray();menu.quitButtons=quitButtons.ToArray();scrim.gameObject.SetActive(false);
            StyleHud();
            generated.gameObject.AddComponent<InterfaceAudio>();
            // Main deliberately keeps a single explicit listener before a player exists.
            Debug.Log("GAME_PRESENTATION_PREPARED: menu listener/camera handoff, production session menu, preferences, HUD cards and reload progress.");
        }
        static void ConfigureActorAssets()
        {
            const string playerPath="Assets/Project/Prefabs/Resources/NetworkCore/NetworkPlayer.prefab";
            var actor=PrefabUtility.LoadPrefabContents(playerPath);
            try
            {
                if(!actor.GetComponent<PlayerFootsteps>())actor.AddComponent<PlayerFootsteps>();
                if(!actor.GetComponent<PlayerAppearance>())actor.AddComponent<PlayerAppearance>();
                var body=actor.transform.Find("Body");if(body){body.localPosition=Vector3.up*.9f;body.localScale=new Vector3(.6f,.9f,.6f);}
                var view=actor.GetComponentInChildren<LocalWeaponView>(true);
                view.bodyMaterial=WeaponMaterial("Game_GunMetal",new Color(.18f,.21f,.23f),.48f);
                view.accentMaterial=WeaponMaterial("Game_GunGrip",new Color(.035f,.044f,.05f),.05f);
                view.flashMaterial=WeaponMaterial("Game_MuzzleFlash",new Color(1,.76f,.24f),0,true);
                PrefabUtility.SaveAsPrefabAsset(actor,playerPath);
            }
            finally {PrefabUtility.UnloadPrefabContents(actor);}
            const string vipPath="Assets/Project/Prefabs/Resources/NetworkCore/Vip.prefab";
            actor=PrefabUtility.LoadPrefabContents(vipPath);
            try{if(!actor.GetComponent<VipObjectiveMarker>())actor.AddComponent<VipObjectiveMarker>();PrefabUtility.SaveAsPrefabAsset(actor,vipPath);}
            finally{PrefabUtility.UnloadPrefabContents(actor);}
        }
        static Material WeaponMaterial(string name,Color color,float smoothness,bool emissive=false)
        {
            string path="Assets/Project/Feedback/Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Project/Environment/Materials/Floor3_Metal.mat");material=new Material(source);AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",smoothness);
            if(emissive){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*3);}
            EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);return material;
        }
        static void StyleMenu(Transform panel,System.Collections.Generic.List<Button> settings,System.Collections.Generic.List<Button> quits)
        {
            Clean(panel);panel.GetComponent<Image>().color=new Color(.015f,.025f,.035f,.65f);
            var title=panel.GetComponentsInChildren<Text>(true).First(t=>t.name=="Title");title.text="TEMPORAL\nCONTRACT";title.fontSize=56;title.fontStyle=FontStyle.Bold;title.alignment=TextAnchor.MiddleLeft;Place(title.rectTransform,new Vector2(-330,130),new Vector2(520,160));
            Label("Polish - Eyebrow",panel,"FLOOR 03  /  TACTICAL LAN",new Vector2(-330,250),new Vector2(520,24),12,Gold,TextAnchor.MiddleLeft,true);
            Label("Polish - Objective",panel,"Attacker: breach Staff Cabin and eliminate the VIP.\nDefender: hold the floor until time runs out.",new Vector2(-330,-22),new Vector2(520,94),19,new Color(.82f,.87f,.9f),TextAnchor.MiddleLeft);
            Label("Polish - Controls",panel,"WASD / MOUSE   ·   E INTERACT   ·   R RELOAD",new Vector2(-330,-190),new Vector2(520,40),12,Muted,TextAnchor.MiddleLeft);
            var background=Panel("Polish - Session card",panel,new Vector2(300,0),new Vector2(440,540),Ink);background.transform.SetAsFirstSibling();
            Label("Polish - Session heading",panel,"CHOOSE YOUR SESSION",new Vector2(300,215),new Vector2(360,24),12,Gold,TextAnchor.MiddleLeft,true);
            StyleButton(Get<Button>(hud,"practiceButton"),new Vector2(300,140),new Vector2(360,52),Blue,"SOLO PRACTICE");
            Label("Polish - Solo hint",panel,"Play the objective on your own.",new Vector2(300,91),new Vector2(360,25),12,Muted);
            Label("Polish - LAN hint",panel,"OR CONNECT TWO PLAYERS OVER LAN",new Vector2(300,46),new Vector2(360,22),11,Muted);
            Label("Polish - Address label",panel,"HOST ADDRESS",new Vector2(300,8),new Vector2(360,18),11,Muted,TextAnchor.MiddleLeft,true);
            Label("Polish - Port label",panel,"PORT",new Vector2(300,-66),new Vector2(360,18),11,Muted,TextAnchor.MiddleLeft,true);
            var address=Get<InputField>(hud,"addressInput");var port=Get<InputField>(hud,"portInput");
            Place((RectTransform)address.transform,new Vector2(300,-24),new Vector2(360,44));Place((RectTransform)port.transform,new Vector2(300,-99),new Vector2(360,44));
            address.GetComponent<Image>().color=port.GetComponent<Image>().color=new Color(.08f,.11f,.14f);
            address.textComponent.fontSize=port.textComponent.fontSize=20;
            StyleButton(Get<Button>(hud,"hostButton"),new Vector2(205,-163),new Vector2(170,44),Blue,"HOST");
            StyleButton(Get<Button>(hud,"joinButton"),new Vector2(395,-163),new Vector2(170,44),new Color(.12f,.23f,.29f),"JOIN");
            var status=Get<Text>(hud,"menuStatusText");Place(status.rectTransform,new Vector2(300,-222),new Vector2(360,48));status.fontSize=12;status.color=Muted;
            settings.Add(Button("Polish - Settings",panel,"SETTINGS",new Vector2(-490,-280),new Vector2(200,40),new Color(.07f,.12f,.16f)));
            quits.Add(Button("Polish - Quit",panel,"QUIT",new Vector2(-300,-280),new Vector2(150,40),new Color(.07f,.12f,.16f)));
        }
        static void StyleHud()
        {
            var root=Get<GameObject>(hud,"hudRoot").transform;Clean(root);
            var role=Get<Text>(hud,"roleText");Place(role.rectTransform,new Vector2(-470,315),new Vector2(280,32));role.fontSize=22;role.fontStyle=FontStyle.Bold;role.alignment=TextAnchor.MiddleLeft;
            Background(root,"Role",new Vector2(-480,313),new Vector2(300,48));
            var objective=Get<Text>(hud,"objectiveText");Place(objective.rectTransform,new Vector2(-426,246),new Vector2(390,82));objective.fontSize=15;objective.alignment=TextAnchor.MiddleLeft;
            Background(root,"Objective",new Vector2(-426,246),new Vector2(410,98));
            var timer=Get<Text>(hud,"timerText");Place(timer.rectTransform,new Vector2(0,319),new Vector2(180,42));timer.fontSize=34;timer.fontStyle=FontStyle.Bold;
            var round=Get<Text>(hud,"roundText");Place(round.rectTransform,new Vector2(0,286),new Vector2(180,20));round.fontSize=11;
            Background(root,"Timer",new Vector2(0,308),new Vector2(190,88));
            Background(root,"Vitals",new Vector2(-470,-282),new Vector2(300,100));
            Label("Polish - Health label",root,"VITALS / HEALTH",new Vector2(-470,-252),new Vector2(240,18),11,Muted,TextAnchor.MiddleLeft,true);
            var health=Get<Text>(hud,"healthText");Place(health.rectTransform,new Vector2(-470,-282),new Vector2(240,36));health.fontSize=27;health.fontStyle=FontStyle.Bold;
            var fill=Get<Image>(hud,"healthFill");Place((RectTransform)fill.transform.parent,new Vector2(-470,-315),new Vector2(240,6));Stretch(fill.rectTransform);
            Background(root,"Weapon",new Vector2(475,-282),new Vector2(280,100));
            Label("Polish - Sidearm label",root,"SIDEARM",new Vector2(475,-250),new Vector2(230,18),11,Muted,TextAnchor.MiddleRight,true);
            var ammo=Get<Text>(hud,"ammoText");Place(ammo.rectTransform,new Vector2(475,-282),new Vector2(230,36));ammo.fontSize=27;ammo.fontStyle=FontStyle.Bold;
            var card=Get<Text>(hud,"keycardText");Place(card.rectTransform,new Vector2(475,-313),new Vector2(230,20));card.fontSize=12;
            var reload=Panel("Polish - Reload progress",root,new Vector2(475,-335),new Vector2(250,3),Gold);reload.sprite=fill.sprite;reload.type=Image.Type.Filled;reload.fillMethod=Image.FillMethod.Horizontal;Set(hud,"reloadFill",reload);reload.gameObject.SetActive(false);
            Label("Polish - Floor tag",root,"FIELD OPERATIONS  /  FLOOR 03",new Vector2(450,321),new Vector2(320,20),11,Muted,TextAnchor.MiddleRight,true);
            Notice(Get<Text>(hud,"promptText"),root,"Prompt",new Vector2(0,-78),new Vector2(540,42),18,Color.white);
            Notice(Get<Text>(hud,"messageText"),root,"Message",new Vector2(0,78),new Vector2(540,42),18,Gold);
            var cross=root.Find("Crosshair");if(cross)foreach(var image in cross.GetComponentsInChildren<Image>()){image.color=new Color(1,1,1,.85f);image.rectTransform.sizeDelta=image.name=="H"?new Vector2(14,2):new Vector2(2,14);}
            var damage=root.Find("Damage flash");if(damage){damage.SetAsFirstSibling();damage.GetComponent<Image>().color=new Color(.8f,.06f,.04f,1);}
        }
        static void Notice(Text text,Transform root,string name,Vector2 position,Vector2 size,int fontSize,Color color)
        {
            var card=Panel("Polish - "+name,root,position,size,new Color(.025f,.04f,.055f,.9f));
            var group=card.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;
            var driver=card.gameObject.AddComponent<HudNoticeBackdrop>();driver.target=text;driver.group=group;
            text.transform.SetParent(card.transform,false);Place(text.rectTransform,Vector2.zero,size-new Vector2(20,4));text.fontSize=fontSize;text.color=color;
        }
        static void Background(Transform parent,string name,Vector2 position,Vector2 size){var image=Panel("Polish - "+name,parent,position,size,new Color(.025f,.04f,.055f,.82f));image.transform.SetAsFirstSibling();}
        static Slider Setting(Transform parent,string label,float y,float min,float max,out Text value)
        {
            Label(label,parent,label,new Vector2(-30,y),new Vector2(440,22),11,Muted,TextAnchor.MiddleLeft,true);
            value=Label(label+" value",parent,"",new Vector2(225,y),new Vector2(60,22),14,Color.white,TextAnchor.MiddleRight);
            var go=new GameObject(label+" slider",typeof(RectTransform),typeof(Slider));go.transform.SetParent(parent,false);Place((RectTransform)go.transform,new Vector2(0,y-32),new Vector2(500,22));
            var background=Panel("Track",go.transform,Vector2.zero,new Vector2(500,4),new Color(.14f,.2f,.24f));
            var fillArea=Panel("Fill area",go.transform,Vector2.zero,new Vector2(500,4),Color.clear);
            var fill=Panel("Fill",fillArea.transform,Vector2.zero,Vector2.zero,Blue);Stretch(fill.rectTransform);
            var handle=Panel("Handle",go.transform,Vector2.zero,new Vector2(10,20),Gold);handle.raycastTarget=true;
            var slider=go.GetComponent<Slider>();slider.minValue=min;slider.maxValue=max;slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;return slider;
        }
        static Button Button(string name,Transform parent,string text,Vector2 position,Vector2 size,Color color)
        {
            var image=Panel(name,parent,position,size,color);image.raycastTarget=true;var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
            Label("Label",image.transform,text,Vector2.zero,size-new Vector2(10,4),14,Color.white,TextAnchor.MiddleCenter,true);return button;
        }
        static void StyleButton(Button button,Vector2 position,Vector2 size,Color color,string label)
        {Place((RectTransform)button.transform,position,size);button.GetComponent<Image>().color=color;var text=button.GetComponentInChildren<Text>(true);text.text=label;text.fontSize=16;text.fontStyle=FontStyle.Bold;}
        static Text Label(string name,Transform parent,string value,Vector2 position,Vector2 size,int fontSize,Color color,TextAnchor alignment=TextAnchor.MiddleCenter,bool bold=false)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);Place((RectTransform)go.transform,position,size);
            var text=go.GetComponent<Text>();text.font=font;text.text=value;text.fontSize=fontSize;text.color=color;text.alignment=alignment;text.raycastTarget=false;text.supportRichText=false;text.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;return text;
        }
        static Image Panel(string name,Transform parent,Vector2 position,Vector2 size,Color color)
        {var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);Place((RectTransform)go.transform,position,size);var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
        static void Place(RectTransform rect,Vector2 position,Vector2 size){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;}
        static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        static void Clean(Transform parent)
        {
            // Preserve notices when reapplying: their text references are part of the original HUD.
            foreach(var child in parent.Cast<Transform>().Where(t=>t.name.StartsWith("Polish -")).ToArray())
            {
                var notice=child.GetComponent<HudNoticeBackdrop>();if(notice&&notice.target)notice.target.transform.SetParent(parent,false);
                Object.DestroyImmediate(child.gameObject);
            }
        }
        static T Get<T>(Object target,string field) where T:Object => (T)new SerializedObject(target).FindProperty(field).objectReferenceValue;
        static void Set(Object target,string field,Object value){var settings=new SerializedObject(target);settings.FindProperty(field).objectReferenceValue=value;settings.ApplyModifiedPropertiesWithoutUndo();}
    }
}
