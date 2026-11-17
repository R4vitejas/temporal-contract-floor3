using UnityEngine;
using TemporalContract.Environment.Testing;
namespace TemporalContract.Environment.EditorTools
{
    public static class FullFloorEntranceChecks
    {
        public static FullFloorEntranceAudit.Result Run() => FullFloorEntranceAudit.Run(GameObject.Find("Full floor inspection player").GetComponent<EnvironmentWalkthrough>());
    }
}
