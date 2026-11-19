using UnityEngine;
namespace TemporalContract.Environment
{
    /// <summary>Records integration readiness; these review gates do not claim multiplayer acceptance.</summary>
    public sealed class FloorAssemblyConfig : ScriptableObject
    {
        public bool subsystemReviewsAccepted;
        public bool productionCapsuleFitVerified;
        public string networkCommit;
        public string interactionCommit;
        public string presentationCommit;
        public GameObject presentationPrefab;
        [Tooltip("Set after the owner's documented board, modal input, state and feedback bindings have been verified.")]
        public bool presentationBindingsVerified;
        [TextArea] public string verificationScope;
    }
}
