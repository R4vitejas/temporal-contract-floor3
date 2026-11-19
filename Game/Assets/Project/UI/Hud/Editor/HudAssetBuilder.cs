using System.IO;
using TemporalContract.Networking;
using TemporalContract.UI.Hud.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TemporalContract.UI.Hud.EditorTools
{
    /// <summary>
    /// Regenerates the HUD/session UI prefab and test scenes. Menu: Temporal Contract / HUD.
    /// Headless: -executeMethod ...HudAssetBuilder.BuildAll / RunRegression.
    /// </summary>
    public static class HudAssetBuilder
    {
        private const string Root = "Assets/Project/UI/Hud";
        private const string PrefabPath = Root + "/Prefabs/HudSessionUi.prefab";
        private const string TestScene = Root + "/Scenes/HudMockTest.unity";
        private const string RegressionScene = Root + "/Scenes/HudRegression.unity";

        private static readonly Color Dim = new Color(0.05f, 0.06f, 0.09f, 0.92f);
        private static readonly Color Accent = new Color(0.25f, 0.55f, 0.95f);
        private static readonly Color Danger = new Color(0.8f, 0.25f, 0.25f);

        private static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        [MenuItem("Temporal Contract/HUD/Build Prefab And Test Scenes")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Root + "/Scenes");
            BuildPrefab();
            BuildTestScene();
            BuildRegressionScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[HUD] Built prefab and scenes.");
        }

        [MenuItem("Temporal Contract/HUD/Run HUD Regression")]
        public static void RunRegression()
        {
            if (!File.Exists(RegressionScene))
            {
                BuildAll();
            }

            EditorSceneManager.OpenScene(RegressionScene, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        // ---------- prefab ----------

        private static void BuildPrefab()
        {
            var root = new GameObject("HudSessionUi", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            var ui = root.AddComponent<HudSessionUi>();
            var so = new SerializedObject(ui);

            // no state
            GameObject noState = Panel("NoStatePanel", root.transform, new Color(0f, 0f, 0f, 0.4f));
            Label("Label", noState.transform, "Waiting for game state...", 28, Center(0f, 0f, 700f, 50f));

            // main menu
            GameObject menu = Panel("MainMenuPanel", root.transform, Dim);
            Label("Title", menu.transform, "TEMPORAL CONTRACT", 54, Center(0f, 220f, 900f, 80f));
            InputField address = Input("AddressInput", menu.transform, "Host IPv4 address (join only)", "127.0.0.1", Center(0f, 90f, 460f, 52f));
            InputField port = Input("PortInput", menu.transform, "Port", "7777", Center(0f, 20f, 460f, 52f));
            Button host = Button("HostButton", "Host", menu.transform, Accent, Center(-125f, -60f, 220f, 56f), out _);
            Button join = Button("JoinButton", "Join", menu.transform, Accent, Center(125f, -60f, 220f, 56f), out _);
            Text menuStatus = Label("MenuStatusText", menu.transform, "Not connected", 24, Center(0f, -140f, 900f, 40f));
            Set(so, "mainMenuPanel", menu);
            Set(so, "addressInput", address);
            Set(so, "portInput", port);
            Set(so, "hostButton", host);
            Set(so, "joinButton", join);
            Set(so, "menuStatusText", menuStatus);

            // lobby
            GameObject lobby = Panel("LobbyPanel", root.transform, Dim);
            Label("Title", lobby.transform, "LOBBY", 48, Center(0f, 220f, 900f, 70f));
            Text players = Label("LobbyPlayersText", lobby.transform, "Players: 1/2", 32, Center(0f, 110f, 800f, 50f));
            Text ready = Label("LobbyReadyText", lobby.transform, string.Empty, 26, Center(0f, 50f, 900f, 44f));
            Button readyButton = Button("ReadyButton", "Ready", lobby.transform, Accent, Center(-125f, -40f, 220f, 56f), out Text readyLabel);
            readyLabel.name = "ReadyButtonText";
            Button start = Button("StartButton", "Start", lobby.transform, new Color(0.2f, 0.65f, 0.3f), Center(125f, -40f, 220f, 56f), out _);
            Button lobbyDisconnect = Button("LobbyDisconnectButton", "Disconnect", lobby.transform, Danger, Center(0f, -120f, 220f, 52f), out _);
            Set(so, "lobbyPanel", lobby);
            Set(so, "lobbyPlayersText", players);
            Set(so, "lobbyReadyText", ready);
            Set(so, "readyButton", readyButton);
            Set(so, "readyButtonText", readyLabel);
            Set(so, "startButton", start);
            Set(so, "lobbyDisconnectButton", lobbyDisconnect);

            // HUD
            GameObject hud = Panel("HudRoot", root.transform, Color.clear);
            Text role = Label("RoleText", hud.transform, string.Empty, 34, TopLeft(30f, -24f, 300f, 44f));
            role.alignment = TextAnchor.MiddleLeft;
            Text objective = Label("ObjectiveText", hud.transform, string.Empty, 22, TopLeft(30f, -70f, 560f, 56f));
            objective.alignment = TextAnchor.UpperLeft;
            Text timer = Label("TimerText", hud.transform, "--:--", 44, TopCenter(0f, -20f, 220f, 56f));
            Text round = Label("RoundText", hud.transform, string.Empty, 22, TopCenter(0f, -74f, 220f, 32f));
            Text healthText = Label("HealthText", hud.transform, string.Empty, 30, BottomLeft(30f, 70f, 320f, 40f));
            healthText.alignment = TextAnchor.MiddleLeft;
            GameObject healthBg = UiObject("HealthBar", hud.transform, BottomLeft(30f, 40f, 320f, 24f));
            healthBg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            GameObject fillGo = UiObject("HealthFill", healthBg.transform, Stretch());
            var fill = fillGo.AddComponent<Image>();
            fill.sprite = FillSprite();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1f;
            Text ammo = Label("AmmoText", hud.transform, string.Empty, 40, BottomRight(-30f, 70f, 320f, 50f));
            ammo.alignment = TextAnchor.MiddleRight;
            Text keycard = Label("KeycardText", hud.transform, string.Empty, 24, BottomRight(-30f, 40f, 320f, 32f));
            keycard.alignment = TextAnchor.MiddleRight;
            Crosshair(hud.transform);
            Text prompt = Label("PromptText", hud.transform, string.Empty, 26, Center(0f, -60f, 700f, 40f));
            prompt.gameObject.SetActive(false);
            Text message = Label("MessageText", hud.transform, string.Empty, 28, Center(0f, 90f, 700f, 44f));
            message.color = new Color(1f, 0.9f, 0.4f);
            message.gameObject.SetActive(false);
            GameObject defeated = Panel("DefeatedOverlay", hud.transform, new Color(0.3f, 0f, 0f, 0.55f));
            Label("Label", defeated.transform, "ELIMINATED", 64, Center(0f, 0f, 900f, 100f));
            defeated.SetActive(false);
            Set(so, "hudRoot", hud);
            Set(so, "roleText", role);
            Set(so, "healthText", healthText);
            Set(so, "healthFill", fill);
            Set(so, "ammoText", ammo);
            Set(so, "timerText", timer);
            Set(so, "roundText", round);
            Set(so, "objectiveText", objective);
            Set(so, "keycardText", keycard);
            Set(so, "promptText", prompt);
            Set(so, "messageText", message);
            Set(so, "defeatedOverlay", defeated);

            // round end
            GameObject roundEnd = Panel("RoundEndPanel", root.transform, Dim);
            Text roundResult = Label("RoundResultText", roundEnd.transform, string.Empty, 46, Center(0f, 60f, 1000f, 80f));
            Text roundScore = Label("RoundScoreText", roundEnd.transform, string.Empty, 32, Center(0f, -20f, 800f, 50f));
            Set(so, "roundEndPanel", roundEnd);
            Set(so, "roundResultText", roundResult);
            Set(so, "roundScoreText", roundScore);

            // match end
            GameObject matchEnd = Panel("MatchEndPanel", root.transform, Dim);
            Text matchResult = Label("MatchResultText", matchEnd.transform, string.Empty, 84, Center(0f, 150f, 1000f, 120f));
            Text matchScore = Label("MatchScoreText", matchEnd.transform, string.Empty, 28, Center(0f, 60f, 1000f, 44f));
            Text rematchStatus = Label("RematchStatusText", matchEnd.transform, string.Empty, 24, Center(0f, 10f, 900f, 40f));
            Button rematch = Button("RematchButton", "Ready for rematch", matchEnd.transform, Accent, Center(-150f, -70f, 280f, 56f), out Text rematchLabel);
            rematchLabel.name = "RematchButtonText";
            Button matchDisconnect = Button("MatchDisconnectButton", "Disconnect", matchEnd.transform, Danger, Center(150f, -70f, 240f, 56f), out _);
            Set(so, "matchEndPanel", matchEnd);
            Set(so, "matchResultText", matchResult);
            Set(so, "matchScoreText", matchScore);
            Set(so, "rematchStatusText", rematchStatus);
            Set(so, "rematchButton", rematch);
            Set(so, "rematchButtonText", rematchLabel);
            Set(so, "matchDisconnectButton", matchDisconnect);

            // error
            GameObject error = Panel("ErrorPanel", root.transform, new Color(0.15f, 0.04f, 0.04f, 0.94f));
            Label("Title", error.transform, "CONNECTION PROBLEM", 44, Center(0f, 120f, 1000f, 70f));
            Text errorText = Label("ErrorText", error.transform, string.Empty, 28, Center(0f, 30f, 1000f, 100f));
            Button errorDisconnect = Button("ErrorDisconnectButton", "Back to menu", error.transform, Danger, Center(0f, -80f, 260f, 56f), out _);
            Set(so, "errorPanel", error);
            Set(so, "errorText", errorText);
            Set(so, "errorDisconnectButton", errorDisconnect);
            Set(so, "noStatePanel", noState);

            so.ApplyModifiedPropertiesWithoutUndo();
            noState.SetActive(true);
            foreach (GameObject panel in new[] { menu, lobby, hud, roundEnd, matchEnd, error })
            {
                panel.SetActive(false);
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        // ---------- UI helpers ----------

        private struct Layout
        {
            public Vector2 anchorMin, anchorMax, pivot, position, size;
            public bool stretch;
        }

        private static Layout Stretch() => new Layout { stretch = true };

        private static Layout Center(float x, float y, float w, float h) => new Layout
        { anchorMin = new Vector2(0.5f, 0.5f), anchorMax = new Vector2(0.5f, 0.5f), pivot = new Vector2(0.5f, 0.5f), position = new Vector2(x, y), size = new Vector2(w, h) };

        private static Layout TopLeft(float x, float y, float w, float h) => new Layout
        { anchorMin = new Vector2(0f, 1f), anchorMax = new Vector2(0f, 1f), pivot = new Vector2(0f, 1f), position = new Vector2(x, y), size = new Vector2(w, h) };

        private static Layout TopCenter(float x, float y, float w, float h) => new Layout
        { anchorMin = new Vector2(0.5f, 1f), anchorMax = new Vector2(0.5f, 1f), pivot = new Vector2(0.5f, 1f), position = new Vector2(x, y), size = new Vector2(w, h) };

        private static Layout BottomLeft(float x, float y, float w, float h) => new Layout
        { anchorMin = new Vector2(0f, 0f), anchorMax = new Vector2(0f, 0f), pivot = new Vector2(0f, 0f), position = new Vector2(x, y), size = new Vector2(w, h) };

        private static Layout BottomRight(float x, float y, float w, float h) => new Layout
        { anchorMin = new Vector2(1f, 0f), anchorMax = new Vector2(1f, 0f), pivot = new Vector2(1f, 0f), position = new Vector2(x, y), size = new Vector2(w, h) };

        private static GameObject UiObject(string name, Transform parent, Layout layout)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            if (layout.stretch)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.anchorMin = layout.anchorMin;
                rect.anchorMax = layout.anchorMax;
                rect.pivot = layout.pivot;
                rect.sizeDelta = layout.size;
                rect.anchoredPosition = layout.position;
            }

            return go;
        }

        private static GameObject Panel(string name, Transform parent, Color color)
        {
            GameObject go = UiObject(name, parent, Stretch());
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = color.a > 0.01f;
            return go;
        }

        private static Text Label(string name, Transform parent, string text, int size, Layout layout)
        {
            GameObject go = UiObject(name, parent, layout);
            var label = go.AddComponent<Text>();
            label.font = Font;
            label.fontSize = size;
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private static Button Button(string name, string caption, Transform parent, Color color, Layout layout, out Text label)
        {
            GameObject go = UiObject(name, parent, layout);
            go.AddComponent<Image>().color = color;
            var button = go.AddComponent<Button>();
            label = Label("Label", go.transform, caption, 26, Stretch());
            return button;
        }

        private static InputField Input(string name, Transform parent, string placeholder, string initial, Layout layout)
        {
            GameObject go = UiObject(name, parent, layout);
            go.AddComponent<Image>().color = new Color(0.15f, 0.17f, 0.22f);
            var field = go.AddComponent<InputField>();
            Text text = Label("Text", go.transform, string.Empty, 24, Stretch());
            text.alignment = TextAnchor.MiddleLeft;
            ((RectTransform)text.transform).offsetMin = new Vector2(12f, 0f);
            ((RectTransform)text.transform).offsetMax = new Vector2(-12f, 0f);
            text.supportRichText = false;
            Text hint = Label("Placeholder", go.transform, placeholder, 22, Stretch());
            hint.alignment = TextAnchor.MiddleLeft;
            hint.color = new Color(1f, 1f, 1f, 0.4f);
            ((RectTransform)hint.transform).offsetMin = new Vector2(12f, 0f);
            ((RectTransform)hint.transform).offsetMax = new Vector2(-12f, 0f);
            field.textComponent = text;
            field.placeholder = hint;
            field.text = initial;
            return field;
        }

        private static void Crosshair(Transform parent)
        {
            GameObject root = UiObject("Crosshair", parent, Center(0f, 0f, 24f, 24f));
            GameObject h = UiObject("H", root.transform, Center(0f, 0f, 22f, 2f));
            h.AddComponent<Image>().color = Color.white;
            GameObject v = UiObject("V", root.transform, Center(0f, 0f, 2f, 22f));
            v.AddComponent<Image>().color = Color.white;
            h.GetComponent<Image>().raycastTarget = false;
            v.GetComponent<Image>().raycastTarget = false;
        }

        private static Sprite fillSprite;

        // A white sprite so Image.Type.Filled works without importing a texture asset.
        private static Sprite FillSprite()
        {
            if (fillSprite == null)
            {
                string path = Root + "/Art/WhiteFill.png";
                Directory.CreateDirectory(Root + "/Art");
                if (!File.Exists(path))
                {
                    var tex = new Texture2D(4, 4);
                    var pixels = new Color[16];
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        pixels[i] = Color.white;
                    }

                    tex.SetPixels(pixels);
                    File.WriteAllBytes(path, tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                    AssetDatabase.ImportAsset(path);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.SaveAndReimport();
                }

                fillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            return fillSprite;
        }

        private static void Set(SerializedObject so, string field, Object value)
        {
            SerializedProperty property = so.FindProperty(field);
            property.objectReferenceValue = value;
        }

        private static void SetOn(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- scenes ----------

        private static HudSessionUi Place()
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            return instance.GetComponent<HudSessionUi>();
        }

        private static void BuildTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Camera", typeof(Camera)) { tag = "MainCamera" };
            cam.GetComponent<Camera>().backgroundColor = new Color(0.18f, 0.2f, 0.26f);
            cam.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            HudSessionUi ui = Place();
            var provider = new GameObject("MockPresentationProvider").AddComponent<MockPresentationProvider>();
            var rig = new GameObject("HudMockTestRig").AddComponent<HudMockTestRig>();
            SetOn(rig, "provider", provider);
            SetOn(rig, "ui", ui);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            EditorSceneManager.SaveScene(scene, TestScene);
        }

        private static void BuildRegressionScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            HudSessionUi ui = Place();
            var provider = new GameObject("MockPresentationProvider").AddComponent<MockPresentationProvider>();
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var runner = new GameObject("HudRegression").AddComponent<HudRegression>();
            SetOn(runner, "ui", ui);
            SetOn(runner, "provider", provider);
            EditorSceneManager.SaveScene(scene, RegressionScene);
        }
    }
}
