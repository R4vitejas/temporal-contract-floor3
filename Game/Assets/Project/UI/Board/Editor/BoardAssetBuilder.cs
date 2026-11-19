using System.IO;
using TemporalContract.Networking;
using TemporalContract.UI.Board.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TemporalContract.UI.Board.EditorTools
{
    /// <summary>
    /// Regenerates the placeholder floor art, layouts, FloorMapPanel prefab and the board test scenes.
    /// Menu: Temporal Contract / Board. Headless: -executeMethod ...BoardAssetBuilder.BuildAll / RunRegression.
    /// </summary>
    public static class BoardAssetBuilder
    {
        private const string Root = "Assets/Project/UI/Board";
        private const string ArtPath = Root + "/Art/PlaceholderFloor.png";
        private const string LayoutPath = Root + "/PlaceholderFloorLayout.asset";
        private const string AltLayoutPath = Root + "/AlternateTestLayout.asset";
        private const string PrefabPath = Root + "/Prefabs/FloorMapPanel.prefab";
        private const string TestScene = Root + "/Scenes/BoardTest.unity";
        private const string RegressionScene = Root + "/Scenes/BoardRegression.unity";

        [MenuItem("Temporal Contract/Board/Build Placeholder Art, Prefab And Test Scenes")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(Root + "/Art");
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Root + "/Scenes");
            Directory.CreateDirectory(Root + "/Materials");
            BuildArt();
            AssetDatabase.Refresh();
            ConfigureSprite();
            BuildLayouts();
            BuildPrefab();
            BuildTestScene();
            BuildRegressionScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[Board] Built placeholder art, layouts, prefab and scenes.");
        }

        [MenuItem("Temporal Contract/Board/Run Board Regression")]
        public static void RunRegression()
        {
            if (!File.Exists(RegressionScene))
            {
                BuildAll();
            }

            EditorSceneManager.OpenScene(RegressionScene, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        // ---------- placeholder artwork and layouts ----------

        private static readonly (string id, string name, Rect area, Color color)[] Rooms =
        {
            ("corridor", "Corridor", new Rect(0.05f, 0.40f, 0.90f, 0.20f), new Color(0.35f, 0.38f, 0.45f)),
            ("class01", "Classroom 01", new Rect(0.05f, 0.62f, 0.28f, 0.33f), new Color(0.35f, 0.55f, 0.75f)),
            ("class02", "Classroom 02", new Rect(0.36f, 0.62f, 0.28f, 0.33f), new Color(0.45f, 0.65f, 0.5f)),
            ("class03", "Classroom 03", new Rect(0.67f, 0.62f, 0.28f, 0.33f), new Color(0.7f, 0.6f, 0.4f)),
            ("seminar", "Seminar Room", new Rect(0.05f, 0.05f, 0.28f, 0.33f), new Color(0.6f, 0.45f, 0.6f)),
            ("lab", "Lab", new Rect(0.36f, 0.05f, 0.28f, 0.33f), new Color(0.4f, 0.6f, 0.6f)),
            ("vip", "VIP Room", new Rect(0.67f, 0.05f, 0.28f, 0.33f), new Color(0.75f, 0.35f, 0.35f)),
        };

        private static void BuildArt()
        {
            const int w = 1024, h = 512;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.1f, 0.12f, 0.16f);
            }

            foreach (var room in Rooms)
            {
                int x0 = Mathf.RoundToInt(room.area.xMin * w), x1 = Mathf.RoundToInt(room.area.xMax * w);
                int y0 = Mathf.RoundToInt(room.area.yMin * h), y1 = Mathf.RoundToInt(room.area.yMax * h);
                for (int y = y0; y < y1; y++)
                {
                    for (int x = x0; x < x1; x++)
                    {
                        bool border = x - x0 < 3 || x1 - 1 - x < 3 || y - y0 < 3 || y1 - 1 - y < 3;
                        pixels[y * w + x] = border ? Color.Lerp(room.color, Color.white, 0.6f) : room.color;
                    }
                }
            }

            tex.SetPixels(pixels);
            File.WriteAllBytes(ArtPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void ConfigureSprite()
        {
            AssetDatabase.ImportAsset(ArtPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(ArtPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static void BuildLayouts()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
            AssetDatabase.DeleteAsset(LayoutPath);
            var layout = ScriptableObject.CreateInstance<FloorMapLayout>();
            layout.artwork = sprite;
            layout.showRegionLabels = true;
            layout.rooms = new FloorMapLayout.Room[Rooms.Length];
            for (int i = 0; i < Rooms.Length; i++)
            {
                layout.rooms[i] = new FloorMapLayout.Room { id = Rooms[i].id, displayName = Rooms[i].name, area = Rooms[i].area };
            }

            AssetDatabase.CreateAsset(layout, LayoutPath);

            AssetDatabase.DeleteAsset(AltLayoutPath);
            var alt = ScriptableObject.CreateInstance<FloorMapLayout>();
            alt.artwork = sprite;
            alt.rooms = new[]
            {
                new FloorMapLayout.Room { id = "a", displayName = "Alternate A", area = new Rect(0.05f, 0.05f, 0.4f, 0.9f) },
                new FloorMapLayout.Room { id = "b", displayName = "Alternate B", area = new Rect(0.55f, 0.05f, 0.4f, 0.9f) },
            };
            AssetDatabase.CreateAsset(alt, AltLayoutPath);
        }

        // ---------- UI prefab ----------

        private static Font BuiltinFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        private static void BuildPrefab()
        {
            var root = new GameObject("FloorMapPanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panelRoot = UiObject("PanelRoot", root.transform, stretch: true);
            panelRoot.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            GameObject frame = UiObject("Frame", panelRoot.transform, stretch: false);
            var frameRect = (RectTransform)frame.transform;
            frameRect.sizeDelta = new Vector2(960f, 480f);
            frameRect.anchoredPosition = new Vector2(0f, 20f);

            GameObject artGo = UiObject("Artwork", frame.transform, stretch: true);
            var art = artGo.AddComponent<Image>();
            art.preserveAspect = false;
            art.raycastTarget = false;

            GameObject regions = UiObject("Regions", frame.transform, stretch: true);

            Text roomName = LabelText("RoomName", panelRoot.transform, 28, TextAnchor.MiddleCenter);
            var nameRect = (RectTransform)roomName.transform;
            nameRect.anchorMin = nameRect.anchorMax = nameRect.pivot = new Vector2(0.5f, 0.5f);
            nameRect.sizeDelta = new Vector2(900f, 44f);
            nameRect.anchoredPosition = new Vector2(0f, -250f);

            Text hint = LabelText("Hint", panelRoot.transform, 20, TextAnchor.MiddleCenter);
            hint.text = "Click a room  |  Esc or Close to return";
            var hintRect = (RectTransform)hint.transform;
            hintRect.anchorMin = hintRect.anchorMax = hintRect.pivot = new Vector2(0.5f, 0.5f);
            hintRect.sizeDelta = new Vector2(900f, 32f);
            hintRect.anchoredPosition = new Vector2(0f, -295f);

            GameObject closeGo = UiObject("CloseButton", panelRoot.transform, stretch: false);
            var closeRect = (RectTransform)closeGo.transform;
            closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(0.5f, 0.5f);
            closeRect.sizeDelta = new Vector2(170f, 44f);
            closeRect.anchoredPosition = new Vector2(395f, 290f);
            closeGo.AddComponent<Image>().color = new Color(0.8f, 0.25f, 0.25f);
            var closeButton = closeGo.AddComponent<Button>();
            Text closeLabel = LabelText("Label", closeGo.transform, 22, TextAnchor.MiddleCenter);
            closeLabel.text = "Close (Esc)";
            closeLabel.color = Color.white;
            Stretch((RectTransform)closeLabel.transform);

            var panel = root.AddComponent<FloorMapPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            so.FindProperty("artworkImage").objectReferenceValue = art;
            so.FindProperty("regionsRoot").objectReferenceValue = (RectTransform)regions.transform;
            so.FindProperty("roomLabel").objectReferenceValue = roomName;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.FindProperty("layout").objectReferenceValue = AssetDatabase.LoadAssetAtPath<FloorMapLayout>(LayoutPath);
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        private static GameObject UiObject(string name, Transform parent, bool stretch)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            if (stretch)
            {
                Stretch(rect);
            }
            else
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            }

            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Text LabelText(string name, Transform parent, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = BuiltinFont;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        // ---------- scenes ----------

        private static Material Mat(Color color)
        {
            string path = $"{Root}/Materials/Test_{ColorUtility.ToHtmlStringRGB(color)}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static GameObject Cube(string name, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            return go;
        }

        private static FloorMapPanel PlacePanel()
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            return instance.GetComponent<FloorMapPanel>();
        }

        private static void AddEventSystem()
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetAsLastSibling();
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);
            Cube("Floor", new Vector3(0f, -0.1f, 0f), new Vector3(16f, 0.2f, 16f), new Color(0.3f, 0.3f, 0.34f));
            Cube("Wall_N", new Vector3(0f, 1.5f, 8f), new Vector3(16f, 3f, 0.2f), new Color(0.75f, 0.75f, 0.78f));
            Cube("Wall_S", new Vector3(0f, 1.5f, -8f), new Vector3(16f, 3f, 0.2f), new Color(0.75f, 0.75f, 0.78f));
            Cube("Wall_E", new Vector3(8f, 1.5f, 0f), new Vector3(0.2f, 3f, 16f), new Color(0.75f, 0.75f, 0.78f));
            Cube("Wall_W", new Vector3(-8f, 1.5f, 0f), new Vector3(0.2f, 3f, 16f), new Color(0.75f, 0.75f, 0.78f));

            var capture = new GameObject("InputCapture").AddComponent<LocalInputCapture>();
            var menu = new GameObject("MockSessionMenu").AddComponent<MockSessionMenu>();
            Set(menu, "capture", capture);
            FloorMapPanel panel = PlacePanel();
            Set(panel, "captureComponent", capture);

            // placeholder classroom board on the north wall
            GameObject boardMesh = Cube("Classroom Board (placeholder)", new Vector3(0f, 1.6f, 7.85f), new Vector3(3.2f, 1.4f, 0.1f), new Color(0.1f, 0.35f, 0.2f));
            var board = boardMesh.AddComponent<ClassroomBoard>();
            Set(board, "panel", panel);

            var player = new GameObject("MockPlayer", typeof(CharacterController));
            player.transform.position = new Vector3(0f, 0.1f, 0f);
            var camGo = new GameObject("PlayerCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            Camera cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            var cc = player.GetComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0f, 0.9f, 0f);
            var testPlayer = player.AddComponent<BoardTestPlayer>();
            var router = player.AddComponent<InteractionRouter>();
            Set(router, "playerCamera", cam);
            Set(router, "actorComponent", testPlayer);
            Set(router, "inputCaptureComponent", capture);
            Set(testPlayer, "playerCamera", cam);
            Set(testPlayer, "router", router);
            Set(testPlayer, "capture", capture);
            AddEventSystem();
            EditorSceneManager.SaveScene(scene, TestScene);
        }

        private static void BuildRegressionScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var capture = new GameObject("InputCapture").AddComponent<LocalInputCapture>();
            var menu = new GameObject("MockSessionMenu").AddComponent<MockSessionMenu>();
            Set(menu, "capture", capture);
            FloorMapPanel panel = PlacePanel();
            Set(panel, "captureComponent", capture);
            var board = new GameObject("Board").AddComponent<ClassroomBoard>();
            Set(board, "panel", panel);
            AddEventSystem();
            var runner = new GameObject("BoardRegression").AddComponent<BoardRegression>();
            Set(runner, "panel", panel);
            Set(runner, "board", board);
            Set(runner, "capture", capture);
            Set(runner, "sessionMenu", menu);
            Set(runner, "alternateLayout", AssetDatabase.LoadAssetAtPath<FloorMapLayout>(AltLayoutPath));
            EditorSceneManager.SaveScene(scene, RegressionScene);
        }
    }
}
