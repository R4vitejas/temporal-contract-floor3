using UnityEngine;

namespace TemporalContract.UI.Hud.Testing
{
    /// <summary>TEST-ONLY: IMGUI strip of buttons that jumps the mock provider through every UI state.</summary>
    public sealed class HudMockTestRig : MonoBehaviour
    {
        [SerializeField] private MockPresentationProvider provider;
        [SerializeField] private HudSessionUi ui;

        private void Start()
        {
            ui.Bind(provider, provider);
            provider.ApplyPreset(1);
        }

        private void OnGUI()
        {
            string[] names = MockPresentationProvider.PresetNames;
            const float width = 190f;
            GUILayout.BeginArea(new Rect(Screen.width - width - 6f, 6f, width, Screen.height - 12f), GUI.skin.box);
            GUILayout.Label("MOCK STATES");
            for (int i = 0; i < names.Length; i++)
            {
                if (GUILayout.Button($"{i}  {names[i]}"))
                {
                    provider.ApplyPreset(i);
                }
            }

            GUILayout.Label($"Last UI calls: {string.Join(", ", provider.Calls.GetRange(Mathf.Max(0, provider.Calls.Count - 3), Mathf.Min(3, provider.Calls.Count)))}");
            GUILayout.EndArea();
        }
    }
}
