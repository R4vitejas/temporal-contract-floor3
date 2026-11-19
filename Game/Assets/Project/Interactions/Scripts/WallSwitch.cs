using TemporalContract.Core;
using Unity.Netcode;
using UnityEngine;

namespace TemporalContract.Interactions
{
    /// <summary>
    /// Wall switch for a group of room lights. Either player can toggle it. The host owns the
    /// on/off state; every client applies it to the assigned lights, so late joiners are correct.
    /// Keep <see cref="dimIntensityScale"/> above zero so rooms stay navigable when "off".
    /// </summary>
    public sealed class WallSwitch : NetworkInteractable
    {
        [Header("Lights")]
        [SerializeField] private Light[] lights = System.Array.Empty<Light>();
        [Tooltip("Optional emissive meshes (fixtures) toggled together with the lights.")]
        [SerializeField] private Renderer[] fixtureRenderers = System.Array.Empty<Renderer>();
        [SerializeField] private bool onByDefault = true;
        [Tooltip("Fraction of the light's on-intensity kept when switched off. 0 = fully dark.")]
        [SerializeField, Range(0f, 0.5f)] private float dimIntensityScale;

        private readonly NetworkVariable<bool> isOn = new NetworkVariable<bool>(true);
        private float[] onIntensities;

        public bool IsOn => isOn.Value;

        private void Awake()
        {
            onIntensities = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++)
            {
                onIntensities[i] = lights[i] != null ? lights[i].intensity : 0f;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                isOn.Value = onByDefault;
            }

            isOn.OnValueChanged += OnStateChanged;
            ApplyState(isOn.Value);
        }

        public override void OnNetworkDespawn()
        {
            isOn.OnValueChanged -= OnStateChanged;
        }

        private void OnStateChanged(bool previous, bool current) => ApplyState(current);

        private void ApplyState(bool on)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null)
                {
                    continue;
                }

                lights[i].intensity = on ? onIntensities[i] : onIntensities[i] * dimIntensityScale;
                lights[i].enabled = on || dimIntensityScale > 0f;
            }

            foreach (Renderer fixture in fixtureRenderers)
            {
                if (fixture != null)
                {
                    fixture.enabled = on;
                }
            }
        }

        protected override bool TryBuildPrompt(IInteractionActor actor, out InteractionPrompt prompt)
        {
            prompt = new InteractionPrompt(isOn.Value ? "Lights off" : "Lights on", "E", true);
            return true;
        }

        protected override bool ExecuteOnServer(in InteractionRequest request, NetworkObject player)
        {
            isOn.Value = !isOn.Value;
            BroadcastFeedback(FeedbackKind.LightToggled, isOn.Value ? 1f : 0f);
            return true;
        }

        protected override void ResetState()
        {
            isOn.Value = onByDefault;
        }
    }
}
