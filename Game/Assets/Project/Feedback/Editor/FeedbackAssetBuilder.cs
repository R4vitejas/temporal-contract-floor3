using System;
using System.IO;
using TemporalContract.Core;
using TemporalContract.Feedback.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TemporalContract.Feedback.EditorTools
{
    /// <summary>
    /// Regenerates the simple generated sounds, the cue library and the two test scenes.
    /// Menu: Temporal Contract / Feedback. Headless:
    /// -executeMethod TemporalContract.Feedback.EditorTools.FeedbackAssetBuilder.BuildAll
    /// -executeMethod TemporalContract.Feedback.EditorTools.FeedbackAssetBuilder.RunRegression
    /// </summary>
    public static class FeedbackAssetBuilder
    {
        private const string Root = "Assets/Project/Feedback";
        private const string AudioDir = Root + "/Audio";
        private const string LibraryPath = Root + "/FeedbackCueLibrary.asset";
        private const string TestScene = Root + "/Scenes/FeedbackTest.unity";
        private const string RegressionScene = Root + "/Scenes/FeedbackRegression.unity";
        private const int Rate = 22050;

        [MenuItem("Temporal Contract/Feedback/Build Sounds, Library And Test Scenes")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(AudioDir);
            Directory.CreateDirectory(Root + "/Scenes");
            WriteSounds();
            AssetDatabase.Refresh();
            FeedbackCueLibrary library = BuildLibrary();
            BuildPrefab();
            BuildTestScene(library);
            BuildRegressionScene(library);
            AssetDatabase.SaveAssets();
            Debug.Log("[Feedback] Built sounds, library and scenes.");
        }

        [MenuItem("Temporal Contract/Feedback/Run Feedback Regression")]
        public static void RunRegression()
        {
            if (!File.Exists(RegressionScene))
            {
                BuildAll();
            }

            EditorSceneManager.OpenScene(RegressionScene, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        // ---------- generated sounds (no external assets) ----------

        private static void WriteSounds()
        {
            var rng = new System.Random(7);
            Save("door_move", Gen(0.55f, (t, n) => Noise(rng) * 0.35f * Env(t, n, 0.02f, 0.5f) * Mathf.Sin(2f * Mathf.PI * (140f - 90f * t / n) * t / Rate) * 2.5f
                + Mathf.Sin(2f * Mathf.PI * (180f - 100f * t / n) * t / Rate) * 0.25f * Env(t, n, 0.05f, 0.6f)));
            Save("switch_click", Gen(0.12f, (t, n) => (t < 300 || (t > 1100 && t < 1500) ? Noise(rng) : 0f) * 0.6f * Env(t, n, 0.001f, 0.2f)));
            Save("keycard_pickup", Gen(0.35f, (t, n) => (Sine(660f, t) * (t < n / 2 ? 1f : 0f) + Sine(990f, t) * (t >= n / 2 ? 1f : 0f)) * 0.4f * Env(t, n, 0.01f, 0.7f)));
            Save("cover_scrape", Gen(0.45f, (t, n) => Noise(rng) * 0.3f * Mathf.Abs(Mathf.Sin(2f * Mathf.PI * 9f * t / Rate)) * Env(t, n, 0.04f, 0.5f)));
            Save("denied", Gen(0.25f, (t, n) => Mathf.Sign(Sine(110f, t)) * 0.28f * Env(t, n, 0.005f, 0.5f)));
            Save("accepted", Gen(0.3f, (t, n) => (Sine(880f, t) + Sine(1320f, t) * 0.5f) * 0.3f * Env(t, n, 0.005f, 0.9f)));
            Save("gunshot", Gen(0.3f, (t, n) => (Noise(rng) * 0.8f + Sine(70f, t) * 0.6f) * Env(t, n, 0.001f, 0.9f)));
            Save("hit_confirm", Gen(0.08f, (t, n) => Sine(1500f, t) * 0.4f * Env(t, n, 0.002f, 0.8f)));
            Save("damage_taken", Gen(0.25f, (t, n) => (Sine(60f, t) * 0.8f + Noise(rng) * 0.2f) * Env(t, n, 0.003f, 0.8f)));
            Save("round_tone", Gen(0.4f, (t, n) => (Sine(523f, t) + Sine(659f, t)) * 0.25f * Env(t, n, 0.01f, 0.7f)));
        }

        private static float Noise(System.Random rng) => (float)(rng.NextDouble() * 2.0 - 1.0);
        private static float Sine(float hz, int t) => Mathf.Sin(2f * Mathf.PI * hz * t / Rate);

        // attack/decay envelope: attackFraction of the clip ramps up; the rest decays with the given curve
        private static float Env(int t, int n, float attack, float decayPower)
        {
            float x = (float)t / n;
            float a = Mathf.Clamp01(x / Mathf.Max(0.0001f, attack));
            return a * Mathf.Pow(1f - x, Mathf.Lerp(1f, 3f, decayPower));
        }

        private static float[] Gen(float seconds, Func<int, int, float> sample)
        {
            int n = (int)(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                data[i] = Mathf.Clamp(sample(i, n), -1f, 1f);
            }

            return data;
        }

        private static void Save(string name, float[] samples)
        {
            string path = $"{AudioDir}/{name}.wav";
            using (var stream = new FileStream(path, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                int bytes = samples.Length * 2;
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + bytes);
                writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(Rate);
                writer.Write(Rate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(bytes);
                foreach (float s in samples)
                {
                    writer.Write((short)(Mathf.Clamp(s, -1f, 1f) * 32000f));
                }
            }
        }

        // ---------- cue library ----------

        private static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/{name}.wav");

        private static FeedbackCueLibrary.Cue Cue(FeedbackKind kind, string clip, float volume, bool positional,
            float min = 2f, float max = 25f) => new FeedbackCueLibrary.Cue
            {
                kind = kind, clip = Clip(clip), volume = volume, positional = positional,
                minDistance = min, maxDistance = max, pitchVariance = 0.05f
            };

        private static FeedbackCueLibrary BuildLibrary()
        {
            AssetDatabase.DeleteAsset(LibraryPath);
            var library = ScriptableObject.CreateInstance<FeedbackCueLibrary>();
            library.cues = new[]
            {
                Cue(FeedbackKind.DoorMoved, "door_move", 0.9f, true, 2f, 25f),
                Cue(FeedbackKind.LightToggled, "switch_click", 0.8f, true, 1.5f, 20f),
                Cue(FeedbackKind.KeycardCollected, "keycard_pickup", 0.8f, true, 1.5f, 20f),
                Cue(FeedbackKind.CoverPickedUp, "cover_scrape", 0.7f, true, 1.5f, 20f),
                Cue(FeedbackKind.CoverPlaced, "cover_scrape", 0.8f, true, 1.5f, 20f),
                Cue(FeedbackKind.InteractionAccepted, "accepted", 0.7f, false),
                Cue(FeedbackKind.InteractionDenied, "denied", 0.7f, false),
                Cue(FeedbackKind.DamageReceived, "damage_taken", 0.8f, false),
                Cue(FeedbackKind.RoundStarted, "round_tone", 0.6f, false),
                Cue(FeedbackKind.RoundEnded, "round_tone", 0.6f, false),
                Cue(FeedbackKind.MatchEnded, "round_tone", 0.7f, false),
            };
            library.weaponFire = Cue(FeedbackKind.DamageReceived, "gunshot", 1f, true, 3f, 60f);
            library.hitConfirm = Cue(FeedbackKind.DamageReceived, "hit_confirm", 0.6f, false);
            library.damageTaken = Cue(FeedbackKind.DamageReceived, "damage_taken", 0.8f, false);
            AssetDatabase.CreateAsset(library, LibraryPath);
            return library;
        }

        // ---------- scenes ----------

        private static void BuildTestScene(FeedbackCueLibrary library)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene can unload the in-memory asset; always reference the saved asset.
            library = AssetDatabase.LoadAssetAtPath<FeedbackCueLibrary>(LibraryPath);
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.5f);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(60f, 0.2f, 60f);

            var pivot = new GameObject("RecoilPivot").transform;
            pivot.position = new Vector3(0f, 1.6f, 0f);
            var camGo = new GameObject("Listener Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(pivot, false);
            Camera cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<SingleAudioListenerGuard>();

            var flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "MuzzleFlash";
            UnityEngine.Object.DestroyImmediate(flash.GetComponent<Collider>());
            flash.transform.SetParent(camGo.transform, false);
            flash.transform.localPosition = new Vector3(0.25f, -0.2f, 0.9f);
            flash.transform.localScale = Vector3.one * 0.12f;
            flash.SetActive(false);

            Transform[] subjects =
            {
                Subject("Door (left, near)", new Vector3(-6f, 1f, 2f), new Color(0.7f, 0.4f, 0.2f)),
                Subject("Switch (right, near)", new Vector3(6f, 1f, 2f), new Color(0.9f, 0.9f, 0.3f)),
                Subject("Card (ahead, mid)", new Vector3(0f, 1f, 12f), new Color(0.2f, 0.8f, 0.4f)),
                Subject("Cover (behind, far)", new Vector3(0f, 1f, -22f), new Color(0.3f, 0.4f, 0.8f)),
            };

            var system = new GameObject("FeedbackTest");
            var presenter = system.AddComponent<FeedbackPresenter>();
            Set(presenter, "library", library);
            var source = system.AddComponent<MockFeedbackSource>();

            // minimal HUD: hit marker + damage flash
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280f, 720f);
            CanvasGroup damage = Overlay(canvasGo.transform, "DamageFlash", new Color(0.8f, 0.05f, 0.05f), stretch: true);
            CanvasGroup hit = Overlay(canvasGo.transform, "HitMarker", Color.white, stretch: false);

            var combat = system.AddComponent<CombatFeedbackHooks>();
            Set(combat, "presenter", presenter);
            Set(combat, "library", library);
            Set(combat, "muzzleFlash", flash);
            Set(combat, "recoilPivot", pivot);
            Set(combat, "hitMarker", hit);
            Set(combat, "damageFlash", damage);

            var rig = system.AddComponent<FeedbackTestRig>();
            Set(rig, "presenter", presenter);
            Set(rig, "combat", combat);
            Set(rig, "source", source);
            Set(rig, "listenerCamera", cam);
            var so = new SerializedObject(rig);
            SerializedProperty list = so.FindProperty("subjects");
            list.arraySize = subjects.Length;
            for (int i = 0; i < subjects.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = subjects[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, TestScene);
        }

        // Drop-in prefab for Main: one per client. Wire CombatFeedbackHooks' optional visuals per scene.
        private static void BuildPrefab()
        {
            Directory.CreateDirectory(Root + "/Prefabs");
            FeedbackCueLibrary library = AssetDatabase.LoadAssetAtPath<FeedbackCueLibrary>(LibraryPath);
            var root = new GameObject("FeedbackSystem");
            var presenter = root.AddComponent<FeedbackPresenter>();
            Set(presenter, "library", library);
            root.AddComponent<SingleAudioListenerGuard>();
            var hooks = root.AddComponent<CombatFeedbackHooks>();
            Set(hooks, "presenter", presenter);
            Set(hooks, "library", library);
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/FeedbackSystem.prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void BuildRegressionScene(FeedbackCueLibrary library)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            library = AssetDatabase.LoadAssetAtPath<FeedbackCueLibrary>(LibraryPath);
            var go = new GameObject("FeedbackRegression");
            Set(go.AddComponent<FeedbackRegression>(), "library", library);
            EditorSceneManager.SaveScene(scene, RegressionScene);
        }

        private static Transform Subject(string name, Vector3 position, Color color)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;
            cube.GetComponent<Renderer>().sharedMaterial = TestMaterial(color);
            return cube.transform;
        }

        private static Material TestMaterial(Color color)
        {
            Directory.CreateDirectory(Root + "/Materials");
            string path = $"{Root}/Materials/Test_{ColorUtility.ToHtmlStringRGB(color)}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static CanvasGroup Overlay(Transform parent, string name, Color color, bool stretch)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            if (stretch)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(36f, 36f);
            }

            go.GetComponent<Image>().color = color;
            go.GetComponent<Image>().raycastTarget = false;
            CanvasGroup group = go.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            return group;
        }

        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
