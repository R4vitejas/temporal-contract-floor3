using System.Collections;
using System.Reflection;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace TemporalContract.Interactions.Testing
{
    /// <summary>
    /// TEST-ONLY host Play Mode regression for MovableCover.ReleaseIfHeldBy. Not shipped, not in Main.
    /// Run headless with the editor menu "Temporal Contract > Interactions > Run Cover Release Regression"
    /// or: Unity -batchmode -nographics -projectPath Game -executeMethod
    /// TemporalContract.Interactions.EditorTools.CoverRegressionRunner.RunBatch
    /// It prints COVER_REGRESSION lines and exits 0 on pass, 2 on failure.
    /// </summary>
    public sealed class CoverReleaseRegression : MonoBehaviour
    {
        [SerializeField] private GameObject floor;
        [SerializeField] private MovableCover cover;

        private readonly StringBuilder report = new StringBuilder();
        private bool failed;

        private IEnumerator Start()
        {
            NetworkManager manager = NetworkManager.Singleton;
            manager.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", 7791);
            manager.StartHost();

            float timeout = Time.realtimeSinceStartup + 10f;
            while ((!manager.IsListening || !cover.IsSpawned) && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Check("host started and cover spawned", manager.IsListening && cover.IsSpawned);
            if (!failed)
            {
                yield return Scenario1OccupiedBaselineUsesFreeSpot();
                yield return Scenario2ExhaustedFallbackParks();
                yield return Scenario3ParkedCoverReappearsWhenSpaceReturns();
                yield return Scenario4ResetClearsParkedState();
            }

            Finish();
        }

        // Baseline and carry point are blocked, but free space exists nearby: must land on a free spot.
        private IEnumerator Scenario1OccupiedBaselineUsesFreeSpot()
        {
            GameObject obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstruction.name = "Obstruction";
            obstruction.transform.position = new Vector3(0f, 1f, 0f);
            obstruction.transform.localScale = new Vector3(1f, 2f, 1f);
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();

            ForceHolder(0);
            cover.ReleaseIfHeldBy(0);
            yield return new WaitForFixedUpdate();

            Check("S1 holder cleared", HolderValue() == ulong.MaxValue);
            Check("S1 not parked (a free spot existed)", !cover.IsParked);
            Check("S1 not placed inside the obstruction",
                !Overlaps(obstruction.GetComponent<Collider>(), cover));
            Check("S1 moved off the occupied baseline", Vector3.Distance(cover.transform.position, Vector3.zero) > 0.5f);
            Destroy(obstruction);
            cover.ResetForRound(new Core.RoundResetContext(1, 1));
            yield return new WaitForFixedUpdate();
        }

        // Nothing valid anywhere (no surface at all): must clear the holder and park, never place solid.
        private IEnumerator Scenario2ExhaustedFallbackParks()
        {
            floor.SetActive(false);
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();

            ForceHolder(0);
            cover.ReleaseIfHeldBy(0);
            yield return new WaitForFixedUpdate();

            Check("S2 holder cleared", HolderValue() == ulong.MaxValue);
            Check("S2 parked", cover.IsParked);
            Check("S2 colliders disabled while parked", AllCollidersDisabled());
            Check("S2 renderers hidden while parked", AllRenderersHidden());
            Check("S2 not interactable while parked", !CanPrompt());
        }

        private IEnumerator Scenario3ParkedCoverReappearsWhenSpaceReturns()
        {
            floor.SetActive(true);
            Physics.SyncTransforms();
            float timeout = Time.realtimeSinceStartup + 4f;
            while (cover.IsParked && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Check("S3 cover reappeared once a free spot existed", !cover.IsParked);
            Check("S3 colliders restored", !AllCollidersDisabled());
            Check("S3 renderers restored", !AllRenderersHidden());
            Check("S3 interactable again", CanPrompt());
        }

        private IEnumerator Scenario4ResetClearsParkedState()
        {
            floor.SetActive(false);
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            ForceHolder(0);
            cover.ReleaseIfHeldBy(0);
            yield return new WaitForFixedUpdate();
            Check("S4 parked before reset", cover.IsParked);

            floor.SetActive(true);
            Physics.SyncTransforms();
            cover.ResetForRound(new Core.RoundResetContext(2, 1));
            yield return new WaitForFixedUpdate();
            Check("S4 reset unparked the cover", !cover.IsParked);
            Check("S4 reset restored exact baseline", cover.transform.position == Vector3.zero);
            Check("S4 colliders restored by reset", !AllCollidersDisabled());
        }

        // ---- helpers ----

        private void ForceHolder(ulong clientId)
        {
            FieldInfo field = typeof(MovableCover).GetField("holderClientId",
                BindingFlags.NonPublic | BindingFlags.Instance);
            ((NetworkVariable<ulong>)field.GetValue(cover)).Value = clientId;
        }

        private ulong HolderValue()
        {
            FieldInfo field = typeof(MovableCover).GetField("holderClientId",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return ((NetworkVariable<ulong>)field.GetValue(cover)).Value;
        }

        private bool AllCollidersDisabled()
        {
            foreach (Collider c in cover.GetComponentsInChildren<Collider>(true))
            {
                if (c.enabled)
                {
                    return false;
                }
            }

            return true;
        }

        private bool AllRenderersHidden()
        {
            foreach (Renderer r in cover.GetComponentsInChildren<Renderer>(true))
            {
                if (r.enabled)
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanPrompt() =>
            ((Core.IInteractionTarget)cover).TryGetPrompt(null, out _);

        private static bool Overlaps(Collider obstruction, MovableCover target)
        {
            foreach (Collider c in target.GetComponentsInChildren<Collider>())
            {
                if (c.enabled && c.bounds.Intersects(obstruction.bounds))
                {
                    return true;
                }
            }

            return false;
        }

        private void Check(string name, bool ok)
        {
            if (!ok)
            {
                failed = true;
            }

            string line = $"COVER_REGRESSION {(ok ? "PASS" : "FAIL")}: {name}";
            report.AppendLine(line);
            Debug.Log(line);
        }

        private void Finish()
        {
            Debug.Log($"COVER_REGRESSION RESULT: {(failed ? "FAIL" : "PASS")}");
            int code = failed ? 2 : 0;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(code);
#else
            Application.Quit(code);
#endif
        }
    }
}
